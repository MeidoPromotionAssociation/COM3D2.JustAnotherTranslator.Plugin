using System;
using HarmonyLib;
using I2.Loc;
using UnityEngine;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks;

public class UIDebugPatch
{
    [HarmonyPatch(typeof(LocalizationManager), "GetTranslation",
        typeof(string),
        typeof(bool),
        typeof(int),
        typeof(bool),
        typeof(bool),
        typeof(GameObject),
        typeof(string)
    )]
    [HarmonyPrefix]
    public static void LocalizationManager_GetTranslation_Prefix(string Term, bool FixForRTL, int maxLineLengthForRTL,
        bool ignoreRTLnumbers, bool applyParameters, GameObject localParametersRoot, string overrideLanguage,
        ref string __result)
    {
        try
        {
            LogManager.Debug($"LocalizationManager_GetTranslation_Prefix Term: {Term}");
        }
        catch (Exception)
        {
        }
    }
}