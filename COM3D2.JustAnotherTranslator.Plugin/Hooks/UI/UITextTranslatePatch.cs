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
public static class UITextTranslatePatch
{
    /// <summary>
    ///     L2 Localization 获取翻译，针对 UI 文本
    ///     有2种类型
    ///     SceneDaily/ボタン文字/男エディット
    ///     SceneDaily/ボタン画像/男エディット
    /// </summary>
    /// <param name="Term"></param>
    /// <param name="Translation"></param>
    /// <param name="FixForRTL"></param>
    /// <param name="maxLineLengthForRTL"></param>
    /// <param name="ignoreRTLnumbers"></param>
    /// <param name="applyParameters"></param>
    /// <param name="localParametersRoot"></param>
    /// <param name="overrideLanguage"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPatch(typeof(LocalizationManager), "TryGetTranslation")]
    [HarmonyPrefix]
    public static bool LocalizationManager_TryGetTranslation_Prefix(
        string Term,
        ref string Translation,
        bool FixForRTL,
        int maxLineLengthForRTL,
        bool ignoreRTLnumbers,
        bool applyParameters,
        GameObject localParametersRoot,
        string overrideLanguage,
        ref bool __result)
    {
        try
        {
            LogManager.Debug($"LocalizationManager_TryGetTranslation_Prefix Term: {Term}");

            var result = UITranslateManager.HandleTextTermTranslation(Term);

            // 空内容则让原函数处理
            if (string.IsNullOrEmpty(result)) return true;

            // 参数替换
            if (applyParameters)
            {
                LocalizationManager.ApplyLocalizationParams(ref result, localParametersRoot);
            }

            Translation = result;
            __result = true;
            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"LocalizationManager_TryGetTranslation_Prefix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}\n{e.StackTrace}");
        }

        return true;
    }
}