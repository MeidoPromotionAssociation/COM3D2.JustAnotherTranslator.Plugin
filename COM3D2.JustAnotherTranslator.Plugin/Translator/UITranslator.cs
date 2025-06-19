using BepInEx.Logging;
using COM3D2.JustAnotherTranslator.Plugin.Hooks;
using HarmonyLib;

namespace COM3D2.JustAnotherTranslator.Plugin.Translator;

public static class UITranslator
{
    private static Harmony _debugPatch;

    private static bool _initialized;

    public static void Init()
    {
        if (_initialized) return;

        if (JustAnotherTranslator.LogLevelConfig.Value >= LogLevel.Debug)
            _debugPatch = Harmony.CreateAndPatchAll(typeof(UIDebugPatch));

        _initialized = true;
    }

    public static void Unload()
    {
        if (_initialized) return;

        _debugPatch?.UnpatchSelf();
        _debugPatch = null;

        _initialized = false;
    }
}