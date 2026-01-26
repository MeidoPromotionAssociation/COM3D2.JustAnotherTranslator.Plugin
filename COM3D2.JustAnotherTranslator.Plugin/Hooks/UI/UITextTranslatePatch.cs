using System;
using System.IO;
using COM3D2.JustAnotherTranslator.Plugin.Translator;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using I2.Loc;
using UnityEngine;
using wf;

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
    [HarmonyPostfix]
    public static void LocalizationManager_TryGetTranslation_Postfix(
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
            LogManager.Debug(
                $"LocalizationManager_TryGetTranslation_Postfix: Term {Term}, Translation {Translation}, FixForRTL {FixForRTL}, maxLineLengthForRTL {maxLineLengthForRTL}, ignoreRTLnumbers {ignoreRTLnumbers}, applyParameters {applyParameters}, localParametersRoot {localParametersRoot}, overrideLanguage {overrideLanguage}, __result {__result}");

            // 如果原方法已经找到翻译，尝试替换
            var customTranslation = UITranslateManager.HandleTextTermTranslation(Term);

            // 如果有自定义翻译，替换它
            if (!string.IsNullOrEmpty(customTranslation))
            {
                // 应用参数
                if (applyParameters)
                    LocalizationManager.ApplyLocalizationParams(ref customTranslation,
                        localParametersRoot);

                // 应用 RTL 修正（如果需要）
                if (LocalizationManager.IsRight2Left && FixForRTL)
                    customTranslation = LocalizationManager.ApplyRTLfix(customTranslation,
                        maxLineLengthForRTL, ignoreRTLnumbers);

                LogManager.Debug(
                    $"LocalizationManager_TryGetTranslation_Postfix: Translation replaced {Translation}    =>    {customTranslation}");

                Translation = customTranslation;
                __result = true;
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"LocalizationManager_TryGetTranslation_Postfix error, please report this issue/未知错误，请报告此错误: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// 对 menuNameCurrentLanguage 进行了重写，取消了原始的 if (Product.systemLanguage != Product.Language.Japanese) 检查
    /// 以便使用 I2 对物品名称进行翻译
    /// 请注意 CountryReplace 会被文本翻译模块处理
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPatch(typeof(SceneEdit.SMenuItem), "menuNameCurrentLanguage", MethodType.Getter)]
    [HarmonyPrefix]
    private static bool SceneEdit_SMenuItem_GetMenuNameCurrentLanguage_Prefix(
        SceneEdit.SMenuItem __instance,
        ref string __result)
    {
        try
        {
            if (__instance == null)
                return true;

            var baseText = __instance.m_strMenuName ?? string.Empty;
            var menuFileName = __instance.m_strMenuFileName ?? string.Empty;
            var cateName = __instance.m_strCateName ?? string.Empty;
            var term = cateName + "/" + Path.GetFileNameWithoutExtension(menuFileName).ToLower();
            var translation = LocalizationManager.GetTranslation(term + "|name", true, 0, true,
                false, null, null);
            var text = !string.IsNullOrEmpty(translation)
                ? translation.Replace("《改行》", "\n")
                : baseText;
            __result = __instance.CountryReplace(text);
            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"SceneEdit_SMenuItem_GetMenuNameCurrentLanguage_Prefix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}\n{e.StackTrace}");
            return true;
        }
    }


    /// <summary>
    /// 对 infoTextCurrentLanguage 进行了重写，取消了原始的 if (Product.systemLanguage != Product.Language.Japanese) 检查
    /// 以便使用 I2 对物品说明进行翻译
    /// 请注意 CountryReplace 会被文本翻译模块处理
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPatch(typeof(SceneEdit.SMenuItem), "infoTextCurrentLanguage", MethodType.Getter)]
    [HarmonyPrefix]
    private static bool SceneEdit_SMenuItem_GetInfoTextCurrentLanguage_Prefix(
        SceneEdit.SMenuItem __instance,
        ref string __result)
    {
        try
        {
            if (__instance == null)
                return true;

            var baseText = __instance.m_strInfo ?? string.Empty;
            var menuFileName = __instance.m_strMenuFileName ?? string.Empty;
            var cateName = __instance.m_strCateName ?? string.Empty;
            var term = cateName + "/" + Path.GetFileNameWithoutExtension(menuFileName).ToLower();
            var translation = LocalizationManager.GetTranslation(term + "|info", true, 0, true,
                false, null, null);
            var text = !string.IsNullOrEmpty(translation)
                ? translation.Replace("《改行》", "\n")
                : baseText;
            __result = __instance.CountryReplace(text);
            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"SceneEdit_SMenuItem_GetInfoTextCurrentLanguage_Prefix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}\n{e.StackTrace}");
            return true;
        }
    }

    /// <summary>
    ///     强制将 wf.Utility.SetLocalizeTerm 的 forceApply 设为 true
    ///     部分组件是通过此方法来获取翻译的
    ///     然而原方法有一个条件判断 (!Product.supportMultiLanguage && !forceApply) || ……
    ///     JP 版 Product.supportMultiLanguage 始终为 false，强行启用会导致奇怪的问题
    ///     因此我们直接让 forceApply 为 true 通过此条件
    /// </summary>
    /// <param name="localize"></param>
    /// <param name="term"></param>
    /// <param name="forceApply"></param>
    /// <returns></returns>
    [HarmonyPatch(typeof(Utility), "SetLocalizeTerm",
        typeof(Localize),
        typeof(string),
        typeof(bool))]
    [HarmonyPrefix]
    public static bool WF_Utility_SetLocalizeTerm_Prefix(Localize localize, string term,
        ref bool forceApply)
    {
        try
        {
            forceApply = true;
            return true;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"WF_Utility_SetLocalizeTerm_Prefix unknown error, please report this issue/未知错误，请报告此错误: {e.Message}\n{e.StackTrace}");
            return true;
        }
    }

    /// <summary>
    ///     强制将 wf.Utility.SetLocalizeTerm 的 forceApply 设为 true
    ///     部分组件是通过此方法来获取翻译的
    ///     然而原方法有一个条件判断 (Product.supportMultiLanguage || forceApply) && ……
    ///     JP 版 Product.supportMultiLanguage 始终为 false，强行启用会导致奇怪的问题
    ///     因此我们直接让 forceApply 为 true 通过此条件
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="term"></param>
    /// <param name="forceApply"></param>
    /// <returns></returns>
    [HarmonyPatch(typeof(Utility), "SetLocalizeTerm",
        typeof(Component),
        typeof(string),
        typeof(bool))]
    [HarmonyPrefix]
    public static bool WF_Utility_SetLocalizeTerm_Component_Prefix(Component obj, string term,
        ref bool forceApply)
    {
        try
        {
            forceApply = true;
            return true;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"WF_Utility_SetLocalizeTerm_Component_Prefix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}\n{e.StackTrace}");
            return true;
        }
    }
}