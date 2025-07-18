using System;
using System.IO;
using COM3D2.JustAnotherTranslator.Plugin.Translator;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.Texture;

/// <summary>
///     用于替换贴图的Harmony补丁
///     部分代码来自 https://github.com/Pain-Brioche/COM3D2.i18nEx
///     COM3D2.i18nEx 基于 MIT 许可证，协议开源，作者为 ghorsington、Pain-Brioche
/// </summary>
public static class TextureReplacePatch
{
    private static readonly byte[] EmptyBytes = new byte[0];

    /// <summary>
    ///     Patch for FileSystemArchive.IsExistentFile and FileSystemWindows.IsExistentFile
    ///     Detect if the texture file exist
    /// </summary>
    /// <param name="file_name"></param>
    /// <param name="__result"></param>
    [HarmonyPatch(typeof(FileSystemArchive), nameof(FileSystemArchive.IsExistentFile))]
    [HarmonyPatch(typeof(FileSystemWindows), nameof(FileSystemWindows.IsExistentFile))]
    [HarmonyPostfix]
    private static void IsExistentFile_Postfix(ref bool __result, string file_name)
    {
        // LogManager.Debug("IsExistentFile called: " + file_name);
        // LogManager.Debug("IsExistentFile result: " + __result);

        if (string.IsNullOrEmpty(file_name) ||
            !Path.GetExtension(file_name).Equals(".tex", StringComparison.InvariantCultureIgnoreCase))
            return;

        if (TextureReplacer.IsTextureExist(file_name))
            __result = true;
    }

    /// <summary>
    ///     Patch for ImportCM.LoadTexture
    ///     All Texture will pass this function
    /// </summary>
    /// <param name="__result"></param>
    /// <param name="f_fileSystem"></param>
    [HarmonyPatch(typeof(ImportCM), nameof(ImportCM.LoadTexture))]
    [HarmonyPrefix]
    private static bool ImportCM_LoadTexture_Prefix(ref TextureResource __result, AFileSystemBase f_fileSystem,
        string f_strFileName,
        bool usePoolBuffer)
    {
        LogManager.Debug("ImportCM_LoadTexture_Prefix called: " + f_strFileName);

        var fileName = Path.GetFileNameWithoutExtension(f_strFileName);

        if (string.IsNullOrEmpty(fileName))
            return true;

        if (!TextureReplacer.GetReplaceTexture(fileName, out var newTexture))
            return true;

        // create new texture
        __result = new TextureResource(1, 1, TextureFormat.ARGB32, __result?.uvRects, newTexture);

        LogManager.Debug("ImportCM_LoadTexture_Prefix Texture replaced: " + f_strFileName);
        return false;
    }

    /// <summary>
    ///     替换 UIWidget 的贴图
    ///     通常是整张 Atlas
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="__result"></param>
    [HarmonyPatch(typeof(UIWidget), nameof(UIWidget.mainTexture), MethodType.Getter)]
    [HarmonyPostfix]
    private static void UIWidget_mainTexture_Getter_Postfix(UIWidget __instance,
        ref UnityEngine.Texture __result)
    {
        LogManager.Debug(
            $"UIWidget_mainTexture_Getter_Postfix called: {__instance.name}, mainTexture name: {__result?.name}");

        var tex = __instance.material?.mainTexture;

        if (tex == null || string.IsNullOrEmpty(tex.name) || tex.name.StartsWith("JAT_"))
            return;

        if (!TextureReplacer.GetReplaceTexture(tex.name, out var newTexture))
            return;

        // 检查并转换为 Texture2D
        if (tex is Texture2D tex2d)
        {
            tex2d.LoadImage(EmptyBytes);
            tex2d.LoadImage(newTexture);
            // add JAT_ prefix to avoid infinite loop
            tex2d.name = $"JAT_{tex2d.name}";
            LogManager.Debug($"UIWidget Texture replaced: {tex.name}");
        }
        else
        {
            LogManager.Warning(
                $"UIWidget_mainTexture_Getter_Postfix Texture {tex.name} is of type {tex.GetType().FullName}, which is unexpected, please report this issue/贴图 {tex.name} 类型为未预期的 {tex.GetType().FullName}，请报告此问题");
        }
    }

    /// <summary>
    ///     替换 UI2DSprite 的贴图
    ///     很少使用，目前只发现 logo 和警告使用
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="__result"></param>
    [HarmonyPatch(typeof(UI2DSprite), nameof(UI2DSprite.mainTexture), MethodType.Getter)]
    [HarmonyPostfix]
    private static void UI2DSprite_mainTexture_Getter_Postfix(UI2DSprite __instance,
        ref UnityEngine.Texture __result)
    {
        LogManager.Debug(
            $"UI2DSprite_mainTexture_Getter_Postfix called: {__instance.name}, mainTexture name: {__result?.name}");

        var tex = __instance.sprite2D?.texture;

        if (tex == null || string.IsNullOrEmpty(tex.name) || tex.name.StartsWith("JAT_"))
            return;

        if (!TextureReplacer.GetReplaceTexture(tex.name, out var newTexture))
            return;

        tex.LoadImage(EmptyBytes);
        tex.LoadImage(newTexture);
        // add JAT_ prefix to avoid infinite loop
        tex.name = $"JAT_{tex.name}";
        LogManager.Debug($"UI2DSprite Texture replaced: {tex.name}");
    }


    /// <summary>
    ///     替换 UITexture 的贴图
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="__result"></param>
    /// <param name="___mTexture"></param>
    [HarmonyPatch(typeof(UITexture), nameof(UITexture.mainTexture), MethodType.Getter)]
    [HarmonyPostfix]
    private static void UITexture_mainTexture_Getter_Postfix(UITexture __instance, ref UnityEngine.Texture __result,
        ref UnityEngine.Texture ___mTexture)
    {
        LogManager.Debug(
            $"UITexture_mainTexture_Getter_Postfix called: {__instance.name}, mainTexture name: {__result?.name}");

        var tex = ___mTexture ?? __instance.material?.mainTexture;

        if (tex == null || string.IsNullOrEmpty(tex.name) || tex.name.StartsWith("JAT_"))
            return;

        if (!TextureReplacer.GetReplaceTexture(tex.name, out var newTexture))
            return;

        // 检查并转换为 Texture2D
        if (tex is Texture2D tex2d)
        {
            tex2d.LoadImage(EmptyBytes);
            tex2d.LoadImage(newTexture);
            // add JAT_ prefix to avoid infinite loop
            tex2d.name = $"JAT_{tex2d.name}";
            LogManager.Debug($"UITexture Texture replaced: {tex.name}");
        }
        else
        {
            LogManager.Warning(
                $"GetMainTexturePostTex Texture {tex.name} is of type {tex.GetType().FullName}, which is unexpected, which is unexpected, please report this issue/贴图 {tex.name} 类型为未预期的 {tex.GetType().FullName}，请报告此问题");
        }
    }


    // 为Image的sprite属性setter添加前缀补丁
    // 用于替换Image控件的精灵纹理
    [HarmonyPatch(typeof(Image), nameof(Image.sprite), MethodType.Setter)]
    [HarmonyPrefix]
    private static void Image_sprite_Setter_Prefix(ref Sprite value)
    {
        LogManager.Debug("Image_sprite_Setter_Prefix called: " + value?.name);

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


    /// <summary>
    ///     强制替换启用时的 Image 纹理
    /// </summary>
    /// <param name="__instance"></param>
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