using System;
using COM3D2.JustAnotherTranslator.Plugin.Translator;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using I2.Loc;
using UnityEngine;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.UI;

public static class UITranslatePatch
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
    public static bool LocalizationManager_GetTranslation_Prefix(string Term, bool FixForRTL, int maxLineLengthForRTL,
        bool ignoreRTLnumbers, bool applyParameters, GameObject localParametersRoot, string overrideLanguage,
        ref string __result)
    {
        try
        {
            LogManager.Debug($"LocalizationManager_GetTranslation_Prefix Term: {Term}");

            __result = UITranslator.HandleTerm(Term);
            if (string.IsNullOrEmpty(__result)) return true;

            return false;
        }
        catch (Exception e)
        {
            LogManager.Error($"Error in LocalizationManager_GetTranslation_Prefix: {e.Message}");
        }

        return true;
    }
}