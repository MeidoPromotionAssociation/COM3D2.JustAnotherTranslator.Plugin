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
            LogManager.Debug($"KagScriptCallTag currentFileName: {__instance.GetCurrentFileName()}");
        }
        catch
        {
        }
    }
}