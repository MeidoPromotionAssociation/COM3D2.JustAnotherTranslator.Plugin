using System;
using COM3D2.JustAnotherTranslator.Plugin.Translator;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using I2.Loc;
using UnityEngine;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.UI;

/// <summary>
///     Hooks into various UI components to apply translations
/// </summary>
public static class UITranslatePatch
{
    /// <summary>
    ///     L2 Localization 获取翻译，针对 UI 文本
    ///     有2种类型
    ///     SceneDaily/ボタン文字/男エディット
    ///     SceneDaily/ボタン画像/男エディット
    /// </summary>
    /// <param name="Term"></param>
    /// <param name="FixForRTL"></param>
    /// <param name="maxLineLengthForRTL"></param>
    /// <param name="ignoreRTLnumbers"></param>
    /// <param name="applyParameters"></param>
    /// <param name="localParametersRoot"></param>
    /// <param name="overrideLanguage"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPatch(typeof(LocalizationManager), "GetTranslation",
        typeof(string),
        typeof(bool),
        typeof(int),
        typeof(bool),
        typeof(bool),
        typeof(GameObject),
        typeof(string)
    )]
    [HarmonyPrefix]
    public static bool LocalizationManager_GetTranslation_Prefix(string Term, bool FixForRTL, int maxLineLengthForRTL,
        bool ignoreRTLnumbers, bool applyParameters, GameObject localParametersRoot, string overrideLanguage,
        ref string __result)
    {
        try
        {
            LogManager.Debug($"LocalizationManager_GetTranslation_Prefix Term: {Term}");

            var result = UITranslateMancger.HandleTextTermTranslation(Term);

            // 空内容则让原函数处理
            if (string.IsNullOrEmpty(result)) return true;

            __result = result;
            return false;
        }
        catch (Exception e)
        {
            LogManager.Error($"Error in LocalizationManager_GetTranslation_Prefix: {e.Message}");
        }

        return true;
    }

    /// <summary>
    ///     挂钩到 UIButton.SetSprite 中以拦截精灵变化并应用替换
    ///     当 L2 Localization 翻译图片时，最后会得到 SpriteName 然后此方法会被调用
    /// </summary>
    [HarmonyPatch(typeof(UIButton), "SetSprite", typeof(string))]
    [HarmonyPostfix]
    public static void UIButton_SetSprite_Postfix(UIButton __instance, string sp)
    {
        LogManager.Debug($"UIButton_SetSprite_Postfix called with sp: {sp}");

        if (__instance.mSprite == null) return;

        UITranslateMancger.ProcessSpriteReplacementWithNewAtlas(__instance.mSprite, sp);
    }

    // /// <summary>
    // ///     此方法用于获取 UIWidget 的纹理
    // ///     对于 UI 来说，图片是一整张 UIAtlas，而替换整个 atlas 会有游戏更新后 atlas 布局改变的问题
    // ///     因此我们直接替换组件的子类 UISprite 的贴图，达到只替换 sprite 的目的
    // ///     __result 通常是整张 Atlas，不能直接替换
    // ///
    // ///     弃用，因为替换后 UI 坐标不同，导致替换后显示不正确
    // /// </summary>
    // [HarmonyPatch(typeof(UIWidget), "mainTexture", MethodType.Getter)]
    // [HarmonyPostfix]
    // private static void UIWidget_get_mainTexture_Postfix(UIWidget __instance, ref UnityEngine.Texture __result)
    // {
    //     if (UITranslator.TryGetSpriteReplacement(__instance, out var customTexture))
    //     {
    //         if (customTexture != null)
    //         {
    //             __result = customTexture;
    //             LogManager.Debug($"UIWidget_get_mainTexture_Postfix replaced: {customTexture.name}");
    //         }
    //     }
    // }
}