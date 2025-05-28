using HarmonyLib;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks;

public static class DebugPatch
{
    // 测试用
    [HarmonyPatch(typeof(KagScript), "CallTag")]
    [HarmonyPrefix]
    public static void KagScriptCallTag_Prefix(KagScript __instance)
    {
        var currentFileName = __instance.GetCurrentFileName();
        LogManager.Debug($"KagScriptCallTag currentFileName: {currentFileName}");
    }
}