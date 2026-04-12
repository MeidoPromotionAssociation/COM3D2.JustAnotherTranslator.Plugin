using System;
using COM3D2.JustAnotherTranslator.Plugin.Manger;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using MaidCafe;
using UnityEngine;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.Fixer;

/// <summary>
///     修复 MaidCafe DLC 中可能出现的翻译后文本长度不一致导致的数组越界
/// </summary>
public static class MaidCafeDlcLineBreakCommentFix
{
    private const string MaidCafeCommentDataTypeName = "MaidCafe.MaidCafeStreamManager+CommentData";

    /// <summary>
    ///     修改 MaidCafe private CommentDataGetter，以便布局代码在计算宽度和高度之前看到翻译后的文本。
    /// </summary>
    public static void ApplyTranslatePatches(Harmony harmony)
    {
        TryPatchCommentDataGetter(harmony, "comment", nameof(CommentDataCommentGetterPostfix));
        TryPatchCommentDataGetter(harmony, "playerName",
            nameof(CommentDataPlayerNameGetterPostfix));
    }

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
            var fallbackText = text;
            var safeText = fallbackText.Length > FALLBACK_MAX_LENGTH
                ? fallbackText.Substring(0, FALLBACK_MAX_LENGTH) + "..."
                : fallbackText;

            __instance.m_commentText.text = safeText;


            LogManager.Error(
                $"LineBreakCommentPrefix failed (input: '{text}'), please report this issue/发生错误，请报告此错误: {e.Message}\n{e.StackTrace}");
        }

        // prevent original method execution
        return false;
    }

    /// <summary>
    ///     覆盖 MaidCafeComment.LineBreakComment 以免翻译后出现数组越界
    ///     same method in MaidCafeComment
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    [HarmonyPatch(typeof(MaidCafeSuperChatComment), "LineBreakComment")]
    [HarmonyPrefix]
    private static bool SuperChatLineBreakCommentPrefix(MaidCafeComment __instance, string text)
    {
        try
        {
            return LineBreakCommentPrefix(__instance, text);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"SuperChatLineBreakCommentPrefix failed (input: '{text}') unknow error, please report this issue/未知错误，请报告此错误: {e.Message}\n{e.StackTrace}");
            return true;
        }
    }


    /// <summary>
    ///     尝试提前翻译弹幕数据
    /// </summary>
    /// <param name="__result"></param>
    private static void CommentDataCommentGetterPostfix(ref string __result)
    {
        TryTranslateMaidCafeText(ref __result);
    }

    /// <summary>
    ///     尝试提前翻译玩家名称
    /// </summary>
    /// <param name="__result"></param>
    private static void CommentDataPlayerNameGetterPostfix(ref string __result)
    {
        TryTranslateMaidCafeText(ref __result);
    }

    /// <summary>
    ///     尝试翻译给定的文本内容
    /// </summary>
    /// <param name="text">需要翻译的文本字符串引用。</param>
    private static void TryTranslateMaidCafeText(ref string text)
    {
        try
        {
            if (StringTool.IsNullOrWhiteSpace(text) || StringTool.IsNumeric(text) ||
                TextTranslateManger.IsTranslatedText(text))
                return;

            if (TextTranslateManger.GetTranslateText(text, out var translated))
                text = translated;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"TryTranslateMaidCafeText failed (input: '{text}'): {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     尝试修补 MaidCafe CommentData 类型中指定属性的 getter 方法
    ///     以便在执行布局计算之前预翻译文本值。
    /// </summary>
    /// <param name="harmony">用于应用修补的 Harmony 实例。</param>
    /// <param name="propertyName">要修补其 getter 的属性的名称。</param>
    /// <param name="postfixMethodName">将处理修补行为的后缀方法的名称。</param>
    private static void TryPatchCommentDataGetter(Harmony harmony, string propertyName,
        string postfixMethodName)
    {
        try
        {
            var commentDataType = AccessTools.TypeByName(MaidCafeCommentDataTypeName);
            if (commentDataType == null)
            {
                LogManager.Warning(
                    $"Failed to find {MaidCafeCommentDataTypeName}, skipping MaidCafe extra patch");
                return;
            }

            var original = AccessTools.PropertyGetter(commentDataType, propertyName);
            var postfix = AccessTools.Method(typeof(MaidCafeDlcLineBreakCommentFix),
                postfixMethodName);
            if (original == null || postfix == null)
            {
                LogManager.Warning(
                    $"Failed to find MaidCafe getter patch target: {propertyName}/{postfixMethodName}");
                return;
            }

            harmony.Patch(original, postfix: new HarmonyMethod(postfix));
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"TryPatchCommentDataGetter failed ({propertyName}): {e.Message}\n{e.StackTrace}");
        }
    }
}