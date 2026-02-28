using System;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.Fixer;

/// <summary>
///     替换 UI 字体
/// </summary>
public static class UIFontReplace
{
    /// <summary>
    ///     替换 UGUI 文本字体
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPatch(typeof(UnityEngine.UI.Text), "OnEnable")]
    [HarmonyPrefix]
    public static void ChangeUEUIFont(UnityEngine.UI.Text __instance)
    {
        try
        {
            __instance.font = FontTool.SwapUIFont(__instance.font);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"UIFontReplace failed (input: '{__instance.font.name}') unknow error, please report this issue/未知错误，请报告此错误: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     替换 NGUI 字体
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPatch(typeof(UILabel), "ProcessAndRequest")]
    [HarmonyPrefix]
    public static void ChangeFont(UILabel __instance)
    {
        try
        {
            __instance.trueTypeFont = FontTool.SwapUIFont(__instance.trueTypeFont);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"UIFontReplace failed (input: '{__instance.trueTypeFont.name}') unknow error, please report this issue/未知错误，请报告此错误: {e.Message}\n{e.StackTrace}");
        }
    }
}