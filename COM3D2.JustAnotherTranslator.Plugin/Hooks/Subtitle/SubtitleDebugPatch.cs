using System;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.Subtitle;

/// <summary>
///     用于字幕调试的 Harmony 补丁
/// </summary>
public static class SubtitleDebugPatch
{
    /// <summary>
    ///     获取当前脚本的文件名
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPatch(typeof(KagScript), "CallTag")]
    [HarmonyPrefix]
    public static void KagScriptCallTag_Prefix(KagScript __instance)
    {
        try
        {
            LogManager.Debug($"KagScriptCallTag_Prefix currentFileName: {__instance.GetCurrentFileName()}");
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"KagScriptCallTag_Prefix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}\n{e.StackTrace}");
        }
    }
}