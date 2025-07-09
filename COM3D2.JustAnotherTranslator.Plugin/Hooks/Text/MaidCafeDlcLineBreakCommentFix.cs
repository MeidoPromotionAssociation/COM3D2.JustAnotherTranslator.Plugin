using System;
using HarmonyLib;
using MaidCafe;
using UnityEngine;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.Text;

public static class MaidCafeDlcLineBreakCommentFix
{
    /// <summary>
    ///     覆盖 MaidCafeComment.LineBreakComment 以免翻译后出现数组越界
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    [HarmonyPatch(typeof(MaidCafeComment), "LineBreakComment")]
    [HarmonyPrefix]
    private static bool LineBreakCommentPrefix(MaidCafeComment __instance, string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            __instance.m_commentText.text = string.Empty;
            return false;
        }

        try
        {
            // separate text safely
            var splitIndex = Mathf.Min(20, text.Length);
            var modifiedText = text.Substring(0, splitIndex);

            // add a second line only if there is leftover text
            if (splitIndex < text.Length) modifiedText += "\n" + text.Substring(splitIndex);

            // keep original UI update logic
            __instance.m_commentText.text = modifiedText;
        }
        catch (Exception e)
        {
            const int FALLBACK_MAX_LENGTH = 30;
            var fallbackText = text ?? string.Empty;
            var safeText = fallbackText.Length > FALLBACK_MAX_LENGTH
                ? fallbackText.Substring(0, FALLBACK_MAX_LENGTH) + "..."
                : fallbackText;

            __instance.m_commentText.text = safeText;


            Debug.LogError($"LineBreakCommentPrefix failed (input: '{text}'): {e}");
        }

        // prevent original method execution
        return false;
    }

    // There is a same methon in MaidCafeSuperChatComment
    [HarmonyPatch(typeof(MaidCafeSuperChatComment), "LineBreakComment")]
    [HarmonyPrefix]
    private static bool SuperChatLineBreakCommentPrefix(MaidCafeComment __instance, string text)
    {
        return LineBreakCommentPrefix(__instance, text);
    }
}