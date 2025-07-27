using System;
using System.Linq;
using System.Reflection;
using COM3D2.JustAnotherTranslator.Plugin.Translator;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using Scourt.Loc;
using UnityEngine.UI;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.Text;

/// <summary>
///     用于翻译文本的 Harmony 补丁
/// </summary>
public static class TextTranslatePatch
{
    /// <summary>
    ///     Hook for ADV text
    /// </summary>
    /// <param name="__result"></param>
    [HarmonyPatch(typeof(KagScript), "GetText")]
    [HarmonyPostfix]
    private static void KagScript_GetText_Postfix(ref string __result)
    {
        try
        {
            LogManager.Debug("KagScript_GetText_Postfix called: " + __result);

            if (TextTranslateManger.GetTranslateText(__result, out var translated))
                __result = translated;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"KagScript_GetText_Postfix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}/n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     Hook for character name
    /// </summary>
    /// <param name="text"></param>
    [HarmonyPatch(typeof(ScriptManager), nameof(ScriptManager.ReplaceCharaName), typeof(string))]
    [HarmonyPrefix]
    private static void ScriptManger_ReplaceCaraName_Prefix(ref string text)
    {
        try
        {
            LogManager.Debug("ScriptManager ReplaceCharaName called: " + text);

            if (TextTranslateManger.GetTranslateText(text, out var translated))
                text = translated;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"ScriptManager_ReplaceCaraName_Prefix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}/n{e.StackTrace}");
        }
    }


    /// <summary>
    ///     Hook for LocalizationManager.GetTranslationText
    ///     部分插件例如 COM3D2.WildParty，会直接调用 MessageClass SetText
    ///     然后 MessageClass SetText 再调用 LocalizationManager.GetTranslationText
    ///     注意这个LocalizationManager 不是 I2.Loc 的，是 Scourt.Loc
    ///     该方法传入形如此的多语言标记文本：'这是日文原文lt;e&gt;This is English text&lt;sc&gt;这是中文文本
    ///     然后将其解析到 LocalizationString 对象中，LocalizationString 可以用语言为键来访问文本
    ///     所以我们获取日文原文后再翻译
    /// </summary>
    /// <param name="__result"></param>
    [HarmonyPatch(typeof(LocalizationManager), "GetTranslationText", typeof(string))]
    [HarmonyPostfix]
    private static void LocalizationManager_GetTranslationText_Postfix(ref LocalizationString __result)
    {
        try
        {
            if (__result == null) return;

            // 提取日文原文
            var originalText = __result[Product.Language.Japanese];

            if (StringTool.IsNullOrWhiteSpace(originalText) || StringTool.IsNumeric(originalText))
                return;

            LogManager.Debug($"LocalizationManager GetTranslationText called: {originalText}");

            // 如果包含特殊符号，就不翻译
            // 例如翻译到纯 [HF] 时如果被添加了特殊标记，会导致游戏崩溃，游戏还有其他的特殊标记，因此这里直接检查 [
            if (originalText.Contains("["))
            {
                LogManager.Debug($"LocalizationManager GetTranslationText skip: {originalText}");
                return;
            }

            if (TextTranslateManger.GetTranslateText(originalText, out var translatedText))
            {
                // WARNING: If the game tries to translate a special tag and it is marked by XUAT, it may crash the game
                __result[Product.Language.Japanese] = translatedText;
                __result[Product.baseScenarioLanguage] = translatedText;
                __result[Product.subTitleScenarioLanguage] = translatedText;
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"LocalizationManager_GetTranslationText_Postfix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}/n{e.StackTrace}");
        }
    }


    /// <summary>
    ///     Hook for Unity UI Text
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPatch(typeof(Graphic), "SetVerticesDirty")]
    [HarmonyPrefix]
    private static void Graphic_SetVerticesDirty_Prefix(object __instance)
    {
        try
        {
            // LogManager.Debug($"Graphic SetVerticesDirty instance: {__instance}");
            if (__instance is UnityEngine.UI.Text)
            {
                var traverse = Traverse.Create(__instance).Field("m_Text");
                var text = traverse.GetValue() as string;

                // Just too much logs
                if (StringTool.IsNullOrWhiteSpace(text) || StringTool.IsNumeric(text)) return;

                if (text.Contains(XUATInterop.XuatSpicalMaker))
                    return;

                LogManager.Debug($"Graphic SetVerticesDirty called: {text}");

                if (TextTranslateManger.GetTranslateText(text, out var translated))
                    traverse.SetValue(translated);
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"Graphic_SetVerticesDirty_Prefix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}/n{e.StackTrace}");
        }
    }


    /// <summary>
    ///     Hook for NGUI Text (NGUIText.WrapText(string, out string))
    ///     This method should manually register use RegisterNGUITextPatches
    /// </summary>
    /// <param name="text"></param>
    private static void NGUIText_WrapText_Prefix(ref string text)
    {
        try
        {
            // Just too much logs
            if (StringTool.IsNullOrWhiteSpace(text) || StringTool.IsNumeric(text))
                return;

            LogManager.Debug($"NGUIText WrapText(string, out string) called: {text}");

            if (TextTranslateManger.GetTranslateText(text, out var translated))
            {
                LogManager.Debug("NGUIText WrapText translated: " + translated);
                text = translated;
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"NGUIText_WrapText_Prefix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}/n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     Hook for NGUI Text
    ///     manual register NGUIText_WrapText_Prefix
    /// </summary>
    /// <param name="harmony"></param>
    public static void RegisterNGUITextPatches(Harmony harmony)
    {
        try
        {
            var wrapTextMethods = typeof(NGUIText).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name == "WrapText" && m.GetParameters().Length > 0 &&
                            m.GetParameters()[0].ParameterType == typeof(string));

            var patchCount = 0;
            foreach (var method in wrapTextMethods)
            {
                var prefix = new HarmonyMethod(typeof(TextTranslatePatch).GetMethod(nameof(NGUIText_WrapText_Prefix),
                    BindingFlags.Static | BindingFlags.NonPublic));

                harmony.Patch(method, prefix);
                patchCount++;
                LogManager.Debug($"NGUIText.{method.Name} patched successfully");
            }

            if (patchCount > 0)
                LogManager.Debug(
                    $"Total {patchCount} NGUIText.WrapText methods patched");
            else
                LogManager.Debug("Failed to find any NGUIText.WrapText methods");
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"RegisterNGUITextPatches unknown error, please report this issue/未知错误，请报告此错误 {e.Message}/n{e.StackTrace}");
        }
    }
}