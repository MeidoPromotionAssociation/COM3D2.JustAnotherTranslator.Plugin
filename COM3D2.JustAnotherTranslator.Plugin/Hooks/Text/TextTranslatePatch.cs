using System.Linq;
using System.Reflection;
using COM3D2.JustAnotherTranslator.Plugin.Translator;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using UnityEngine.UI;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.Text;

// <summary>
//     用于翻译文本的 Harmony 补丁
// </summary>
public static class TextTranslatePatch
{
    // ADV text
    [HarmonyPatch(typeof(KagScript), "GetText")]
    [HarmonyPostfix]
    private static void KagScriptGetTextPatch(ref string __result)
    {
        if (string.IsNullOrEmpty(__result) || TextTranslator.IsNumeric(__result))
            return;

        LogManager.Debug("KagScript GetText called: " + __result);

        string translated;
        if (TextTranslator.GetTranslateText(__result, out translated)) __result = translated;
    }

    // character name
    [HarmonyPatch(typeof(ScriptManager), nameof(ScriptManager.ReplaceCharaName), typeof(string))]
    [HarmonyPrefix]
    private static void ReplaceCharaName(ref string text)
    {
        if (string.IsNullOrEmpty(text) || TextTranslator.IsNumeric(text))
            return;

        LogManager.Debug("ScriptManager ReplaceCharaName called: " + text);

        string translated;
        if (TextTranslator.GetTranslateText(text, out translated)) text = translated;
    }

    // Unity UI Text
    [HarmonyPatch(typeof(Graphic), "SetVerticesDirty")]
    [HarmonyPrefix]
    private static void UITextSetTextPatch(object __instance)
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


    // NGUI Text
    // 这个方法将在 TextTranslate.Init() 中手动注册
    private static void NGUITextWrapTextPrefix(ref string text)
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

    // manual register NGUIText.WrapText patch
    public static void RegisterNGUITextPatches(Harmony harmony)
    {
        var wrapTextMethods = typeof(NGUIText).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == "WrapText" && m.GetParameters().Length > 0 &&
                        m.GetParameters()[0].ParameterType == typeof(string));

        var patchCount = 0;
        foreach (var method in wrapTextMethods)
        {
            var prefix = new HarmonyMethod(typeof(TextTranslatePatch).GetMethod(nameof(NGUITextWrapTextPrefix),
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