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
        if (string.IsNullOrEmpty(__result) || TextTranslator.IsNumeric(__result))
            return;

        LogManager.Debug("KagScript GetText called: " + __result);

        string translated;
        if (TextTranslator.GetTranslateText(__result, out translated)) __result = translated;
    }

    /// <summary>
    ///     Hook for character name
    /// </summary>
    /// <param name="text"></param>
    [HarmonyPatch(typeof(ScriptManager), nameof(ScriptManager.ReplaceCharaName), typeof(string))]
    [HarmonyPrefix]
    private static void ScriptManger_ReplaceCaraName_Prefix(ref string text)
    {
        if (string.IsNullOrEmpty(text) || TextTranslator.IsNumeric(text))
            return;

        LogManager.Debug("ScriptManager ReplaceCharaName called: " + text);

        string translated;
        if (TextTranslator.GetTranslateText(text, out translated)) text = translated;
    }


    /// <summary>
    ///     Hook for LocalizationManager.GetTranslationText
    ///     部分插件例如 WildParty，会直接调用 MessageClass SetText
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
        if (__result == null || string.IsNullOrEmpty(__result[Product.Language.Japanese])) return;

        // 提取日文原文
        var originalText = __result[Product.Language.Japanese];

        if (TextTranslator.IsNumeric(originalText)) return;

        LogManager.Debug($"LocalizationManager GetTranslationText called: {originalText}");

        string translatedText;

        if (TextTranslator.GetTranslateText(originalText, out translatedText))
        {
            __result[Product.Language.Japanese] = translatedText;
            __result[Product.baseScenarioLanguage] = translatedText;
            __result[Product.subTitleScenarioLanguage] = translatedText;
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
        // LogManager.Debug($"Graphic SetVerticesDirty instance: {__instance}");
        if (__instance is UnityEngine.UI.Text)
        {
            var traverse = Traverse.Create(__instance).Field("m_Text");
            var text = traverse.GetValue() as string;
            if (string.IsNullOrEmpty(text) || TextTranslator.IsNumeric(text))
                return;
            if (text.Contains(XUATInterop.XuatSpicalMaker))
                return;
            LogManager.Debug($"Graphic SetVerticesDirty called: {text}");
            string translated;
            if (TextTranslator.GetTranslateText(text, out translated))
                traverse.SetValue(translated);
        }
    }


    /// <summary>
    ///     Hook for NGUI Text (NGUIText.WrapText(string, out string))
    ///     This method should manually register use RegisterNGUITextPatches
    /// </summary>
    /// <param name="text"></param>
    private static void NGUIText_WrapText_Prefix(ref string text)
    {
        if (string.IsNullOrEmpty(text) || TextTranslator.IsNumeric(text))
            return;

        LogManager.Debug("NGUIText WrapText(string, out string) called: " + text);

        string translated;
        if (TextTranslator.GetTranslateText(text, out translated))
        {
            LogManager.Debug("NGUIText WrapText translated: " + translated);
            text = translated;
        }
    }

    /// <summary>
    ///     Hook for NGUI Text
    ///     manual register NGUIText_WrapText_Prefix
    /// </summary>
    /// <param name="harmony"></param>
    public static void RegisterNGUITextPatches(Harmony harmony)
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
}