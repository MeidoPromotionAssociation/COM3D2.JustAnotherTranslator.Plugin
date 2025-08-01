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
        try
        {
            // LogManager.Debug("IsExistentFile called: " + file_name);
            // LogManager.Debug("IsExistentFile result: " + __result);

            if (StringTool.IsNullOrWhiteSpace(file_name) ||
                !Path.GetExtension(file_name).Equals(".tex", StringComparison.InvariantCultureIgnoreCase))
                return;

            if (TextureReplaceManger.IsReplaceTextureExist(file_name))
                __result = true;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"IsExistentFile_Postfix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     Patch for ImportCM.LoadTexture
    ///     All Texture will pass this function
    /// </summary>
    /// <param name="__result"></param>
    /// <param name="f_fileSystem"></param>
    /// <param name="f_strFileName"></param>
    /// <param name="usePoolBuffer"></param>
    [HarmonyPatch(typeof(ImportCM), nameof(ImportCM.LoadTexture))]
    [HarmonyPrefix]
    private static bool ImportCM_LoadTexture_Prefix(ref TextureResource __result, AFileSystemBase f_fileSystem,
        string f_strFileName,
        bool usePoolBuffer)
    {
        try
        {
            LogManager.Debug($"ImportCM_LoadTexture_Prefix called: {f_strFileName}");

            var fileName = Path.GetFileNameWithoutExtension(f_strFileName);

            if (StringTool.IsNullOrWhiteSpace(fileName))
                return true;

            if (!TextureReplaceManger.GetReplaceTexture(fileName, out var newTexture))
                return true;

            // create new texture
            __result = new TextureResource(1, 1, TextureFormat.ARGB32, __result?.uvRects, newTexture);

            LogManager.Debug($"ImportCM_LoadTexture_Prefix Texture replaced: {f_strFileName}");
            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"ImportCM_LoadTexture_Prefix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}\n{e.StackTrace}");
            return true;
        }
    }

    /// <summary>
    ///     替换 UIWidget 的贴图
    ///     通常是整张 Atlas
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="__result"></param>
    [HarmonyPatch(typeof(UIWidget), nameof(UIWidget.mainTexture), MethodType.Getter)]
    [HarmonyPostfix]
    private static void UIWidget_mainTexture_Getter_Postfix(UIWidget __instance, ref UnityEngine.Texture __result)
    {
        try
        {
            if (__result == null || StringTool.IsNullOrWhiteSpace(__result.name) ||
                __result.name.StartsWith("JAT_") ||
                __result.name == "Font Texture")
                return;

            LogManager.Debug(
                $"UIWidget_mainTexture_Getter_Postfix called: {__instance?.name}, mainTexture name: {__result?.name}");

            if (!TextureReplaceManger.GetReplaceTexture(__result.name, out var newTexture, __result))
                return;

            // 检查并转换为 Texture2D
            if (__result is Texture2D tex2d)
            {
                tex2d.LoadImage(EmptyBytes); // I don't know why, but i18nEx did, so
                tex2d.LoadImage(newTexture);
                // add JAT_ prefix to avoid infinite loop
                tex2d.name = $"JAT_{__result.name}";
                LogManager.Debug($"UIWidget Texture replaced: {__result.name}");
            }
            else
            {
                LogManager.Warning(
                    $"UIWidget_mainTexture_Getter_Postfix Texture {__result.name} is of type {__result.GetType().FullName}, which is unexpected, please report this issue/贴图 {__result.name} 类型为未预期的 {__result.GetType().FullName}，请报告此问题");
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"UIWidget_mainTexture_Getter_Postfix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}\n{e.StackTrace}");
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
    private static void UI2DSprite_mainTexture_Getter_Postfix(UI2DSprite __instance, ref UnityEngine.Texture __result)
    {
        try
        {
            if (__result == null || StringTool.IsNullOrWhiteSpace(__result.name) || __result.name.StartsWith("JAT_") ||
                __result.name == "Font Texture")
                return;

            LogManager.Debug(
                $"UI2DSprite_mainTexture_Getter_Postfix called: {__instance?.name}, mainTexture name: {__result.name}");

            if (!TextureReplaceManger.GetReplaceTexture(__result.name, out var newTexture, __result))
                return;

            // 检查并转换为 Texture2D
            if (__result is Texture2D tex2d)
            {
                tex2d.LoadImage(EmptyBytes); // I don't know why, but i18nEx did, so
                tex2d.LoadImage(newTexture);
                // add JAT_ prefix to avoid infinite loop
                tex2d.name = $"JAT_{__result.name}";
                LogManager.Debug($"UI2DSprite Texture replaced: {__result.name}");
            }
            else
            {
                LogManager.Warning(
                    $"UI2DSprite_mainTexture_Getter_Postfix Texture {__result.name} is of type {__result.GetType().FullName}, which is unexpected, please report this issue/贴图 {__result.name} 类型为未预期的 {__result.GetType().FullName}，请报告此问题");
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"UI2DSprite_mainTexture_Getter_Postfix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}\n{e.StackTrace}");
        }
    }


    /// <summary>
    ///     替换 UITexture 的贴图
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="__result"></param>
    [HarmonyPatch(typeof(UITexture), nameof(UITexture.mainTexture), MethodType.Getter)]
    [HarmonyPostfix]
    private static void UITexture_mainTexture_Getter_Postfix(UITexture __instance, ref UnityEngine.Texture __result)
    {
        try
        {
            if (__result == null || StringTool.IsNullOrWhiteSpace(__result.name) ||
                __result.name.StartsWith("JAT_")) return;

            LogManager.Debug(
                $"UITexture_mainTexture_Getter_Postfix called: {__instance?.name}, mainTexture name: {__result?.name}");

            if (!TextureReplaceManger.GetReplaceTexture(__result.name, out var newTexture, __result))
                return;

            // 检查并转换为 Texture2D
            if (__result is Texture2D tex2d)
            {
                tex2d.LoadImage(EmptyBytes); // I don't know why, but i18nEx did, so
                tex2d.LoadImage(newTexture);
                // add JAT_ prefix to avoid infinite loop
                tex2d.name = $"JAT_{__result.name}";
                LogManager.Debug($"UITexture Texture replaced: {__result.name}");
            }
            else
            {
                LogManager.Warning(
                    $"GetMainTexturePostTex Texture {__result.name} is of type {__result.GetType().FullName}, which is unexpected, which is unexpected, please report this issue/贴图 {__result.name} 类型为未预期的 {__result.GetType().FullName}，请报告此问题");
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"UITexture_mainTexture_Getter_Postfix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}\n{e.StackTrace}");
        }
    }


    /// <summary>
    ///     替换 Image 控件的精灵图
    /// </summary>
    /// <param name="value"></param>
    [HarmonyPatch(typeof(Image), nameof(Image.sprite), MethodType.Setter)]
    [HarmonyPrefix]
    private static void Image_sprite_Setter_Prefix(ref Sprite value)
    {
        try
        {
            LogManager.Debug($"Image_sprite_Setter_Prefix called: {value?.name}");

            if (value == null || value.texture == null || StringTool.IsNullOrWhiteSpace(value.texture.name) ||
                value.texture.name.StartsWith("JAT_"))
                return;

            if (!TextureReplaceManger.GetReplaceTexture(value.texture.name, out var newTexture, value.texture))
                return;

            if (newTexture == null)
                return;

            value.texture.LoadImage(EmptyBytes); // I don't know why, but i18nEx did, so
            value.texture.LoadImage(newTexture);
            value.texture.name = $"JAT_{value.texture.name}";
            LogManager.Debug($"Texture replaced: {value.texture.name}");
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"Image_sprite_Setter_Prefix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}\n{e.StackTrace}");
        }
    }


    /// <summary>
    ///     强制替换启用时的 Image 纹理
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPatch(typeof(MaskableGraphic), "OnEnable")]
    [HarmonyPrefix]
    private static void OnMaskableGraphicEnable(MaskableGraphic __instance)
    {
        try
        {
            if (__instance is not Image img || img.sprite is null)
                return;

            // 通过重新设置sprite属性来触发SetSprite补丁
            var tmp = img.sprite;
            img.sprite = tmp;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"OnMaskableGraphicEnable unknown error, please report this issue/未知错误，请报告此错误 {e.Message}\n{e.StackTrace}");
        }
    }
}