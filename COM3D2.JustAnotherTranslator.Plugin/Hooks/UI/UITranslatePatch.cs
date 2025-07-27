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

            var result = UITranslateManager.HandleTextTermTranslation(Term);

            // 空内容则让原函数处理
            if (string.IsNullOrEmpty(result)) return true;

            __result = result;
            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"LocalizationManager_GetTranslation_Prefix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}\n{e.StackTrace}");
        }

        return true;
    }

    /// <summary>
    ///     挂钩到 UIButton.SetSprite 中以拦截精灵变化并应用替换
    ///     当 L2 Localization 翻译图片时，最后会得到 SpriteName 然后此方法会被调用
    ///     当然，不通过 L2 Localization 翻译时也会调用
    /// </summary>
    [HarmonyPatch(typeof(UIButton), "SetSprite", typeof(string))]
    [HarmonyPostfix]
    public static void UIButton_SetSprite_Postfix(UIButton __instance, string sp)
    {
        try
        {
            if (__instance == null || string.IsNullOrEmpty(sp)) return;

            LogManager.Debug($"UIButton_SetSprite_Postfix called with sp: {sp}");

            // 检查 atlas 名称
            var sprite = __instance.mSprite;
            if (sprite != null && sprite.atlas != null && sprite.atlas.name.StartsWith("JAT_"))
            {
                // 如果 atlas 已经被替换，并且 spriteName 也匹配，就无需任何操作
                if (sprite.spriteName == sp) return;

                // 如果 spriteName 不匹配（例如按钮状态改变），只需更新 spriteName 即可，无需重新创建图集
                sprite.spriteName = sp;
                return;
            }

            // 如果 atlas 不是我们的动态图集，则按需判断是否替换
            UITranslateManager.ProcessSpriteReplacementWithNewAtlas(__instance, sp);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"UIButton_SetSprite_Postfix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}\n{e.StackTrace}");
        }
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