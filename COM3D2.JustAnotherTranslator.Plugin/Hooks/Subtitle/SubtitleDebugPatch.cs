using HarmonyLib;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks;

public static class SubtitleDebugPatch
{
    // 测试用
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