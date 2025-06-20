using System;
using System.IO;
using COM3D2.JustAnotherTranslator.Plugin.Translator;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.Texture;

// <summary>
//      用于替换贴图的Harmony补丁
//      部分代码来自 https://github.com/Pain-Brioche/COM3D2.i18nEx
//      COM3D2.i18nEx 基于 MIT 许可证，协议开源，作者为 ghorsington、Pain-Brioche
// </summary>
public static class TextureReplacePatch
{
    private static readonly byte[] EmptyBytes = new byte[0];


    // <summary>
    // Patch for FileSystemArchive.IsExistentFile and FileSystemWindows.IsExistentFile
    // Detect if the texture file exist
    // </summary>
    [HarmonyPatch(typeof(FileSystemArchive), nameof(FileSystemArchive.IsExistentFile))]
    [HarmonyPatch(typeof(FileSystemWindows), nameof(FileSystemWindows.IsExistentFile))]
    [HarmonyPostfix]
    private static void IsExistentFileCheck(ref bool __result, string file_name)
    {
        // LogManager.Debug("IsExistentFile called: " + file_name);
        // LogManager.Debug("IsExistentFile result: " + __result);

        if (file_name == null ||
            (!Path.GetExtension(file_name)?.Equals(".tex", StringComparison.InvariantCultureIgnoreCase) ?? true))
            return;

        if (!string.IsNullOrEmpty(file_name) && TextureReplacer.IsTextureExist(file_name))
            __result = true;
    }

    // <summary>
    // Patch for ImportCM.LoadTexture
    // All Texture will pass this function
    // </summary>
    [HarmonyPatch(typeof(ImportCM), nameof(ImportCM.LoadTexture))]
    [HarmonyPrefix]
    private static bool LoadTexture(ref TextureResource __result,
        AFileSystemBase f_fileSystem,
        string f_strFileName,
        bool usePoolBuffer)
    {
        LogManager.Debug("LoadTexture called: " + f_strFileName);

        var fileName = Path.GetFileNameWithoutExtension(f_strFileName);

        // 没有后缀
        if (string.IsNullOrEmpty(fileName))
            return true;

        byte[] newTexture = null;

        if (!string.IsNullOrEmpty(f_strFileName))
            TextureReplacer.GetReplaceTexture(f_strFileName, out newTexture);

        if (newTexture == null)
            return true;

        LogManager.Debug("Texture replaced: " + f_strFileName);

        // create new texture
        __result = new TextureResource(1, 1, TextureFormat.ARGB32, __result?.uvRects, newTexture);

        return false;
    }


    [HarmonyPatch(typeof(UIWidget), nameof(UIWidget.mainTexture), MethodType.Getter)]
    [HarmonyPatch(typeof(UI2DSprite), nameof(UI2DSprite.mainTexture), MethodType.Getter)]
    [HarmonyPostfix]
    private static void GetMainTexturePost(UIWidget __instance, ref UnityEngine.Texture __result)
    {
        // LogManager.Debug("GetMainTexturePost called: " + __instance.name);

        UnityEngine.Texture tex;

        switch (__instance)
        {
            case UI2DSprite sprite:
                tex = sprite.sprite2D?.texture;
                break;
            default:
                tex = __instance.material?.mainTexture;
                break;
        }

        if (tex is null || string.IsNullOrEmpty(tex.name) || tex.name.StartsWith("JAT_"))
            return;


        byte[] newTexture = null;
        if (!string.IsNullOrEmpty(tex.name))
            TextureReplacer.GetReplaceTexture(tex.name, out newTexture);

        if (newTexture == null)
            return;

        if (tex is Texture2D tex2d)
        {
            tex2d.LoadImage(EmptyBytes);
            tex2d.LoadImage(newTexture);
            // add JAT_ prefix to avoid infinite loop
            tex2d.name = $"JAT_{tex2d}";
            LogManager.Debug($"Texture replaced: {tex.name}");
        }
        else
        {
            LogManager.Warning(
                $"GetMainTexturePost Texture {tex.name} is of type {tex.GetType().FullName} and not tex2d, please report this to the plugin author/贴图 {tex.name} 类型为 {tex.GetType().FullName}，不是 tex2d，请向插件作者报告此问题");
        }
    }


    // 为UITexture的mainTexture属性getter添加后缀补丁
    // 用于替换UITexture控件的主纹理
    [HarmonyPatch(typeof(UITexture), nameof(UITexture.mainTexture), MethodType.Getter)]
    [HarmonyPostfix]
    private static void GetMainTexturePostTex(UITexture __instance, ref UnityEngine.Texture __result,
        ref UnityEngine.Texture ___mTexture)
    {
        LogManager.Debug("GetMainTexturePostTex called: " + __instance.name);

        var tex = ___mTexture ?? __instance.material?.mainTexture;

        if (tex is null || string.IsNullOrEmpty(tex.name) || tex.name.StartsWith("JAT_"))
            return;

        byte[] newTexture = null;
        if (!string.IsNullOrEmpty(tex.name))
            TextureReplacer.GetReplaceTexture(tex.name, out newTexture);

        if (newTexture == null)
            return;

        // 如果纹理是Texture2D类型
        if (tex is Texture2D tex2d)
        {
            tex2d.LoadImage(EmptyBytes);
            tex2d.LoadImage(newTexture);
            // add JAT_ prefix to avoid infinite loop
            tex2d.name = $"JAT_{tex2d}";
            LogManager.Debug($"Texture replaced: {tex.name}");
        }
        else
        {
            LogManager.Warning(
                $"GetMainTexturePostTex Texture {tex.name} is of type {tex.GetType().FullName} and not tex2d, please report this to the plugin author/贴图 {tex.name} 类型为 {tex.GetType().FullName}，不是 tex2d，请向插件作者报告此问题");
        }
    }


    // 为Image的sprite属性setter添加前缀补丁
    // 用于替换Image控件的精灵纹理
    [HarmonyPatch(typeof(Image), nameof(Image.sprite), MethodType.Setter)]
    [HarmonyPrefix]
    private static void SetSprite(ref Sprite value)
    {
        LogManager.Debug("SetSprite called: " + value?.name);

        if (value is null || value.texture is null || string.IsNullOrEmpty(value.texture.name) ||
            value.texture.name.StartsWith("JAT_"))
            return;

        byte[] newTexture = null;
        if (!string.IsNullOrEmpty(value.texture.name))
            TextureReplacer.GetReplaceTexture(value.texture.name, out newTexture);

        if (newTexture == null)
            return;

        value.texture.LoadImage(EmptyBytes);
        value.texture.LoadImage(newTexture);
        value.texture.name = $"JAT_{value.texture.name}";
        LogManager.Debug($"Texture replaced: {value.texture.name}");
    }

    // 为MaskableGraphic的OnEnable方法添加前缀补丁
    // 用于强制替换启用时的Image纹理
    [HarmonyPatch(typeof(MaskableGraphic), "OnEnable")]
    [HarmonyPrefix]
    private static void OnMaskableGraphicEnable(MaskableGraphic __instance)
    {
        if (__instance is not Image img || img.sprite is null)
            return;

        // 通过重新设置sprite属性来触发SetSprite补丁
        var tmp = img.sprite;
        img.sprite = tmp;
    }
}