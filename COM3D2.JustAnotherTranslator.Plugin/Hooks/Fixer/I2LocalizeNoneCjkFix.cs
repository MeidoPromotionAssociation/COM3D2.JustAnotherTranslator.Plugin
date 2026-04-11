using System;
using System.Reflection;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using I2.Loc;
using MaidCafe;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.Fixer;

/// <summary>
///     修复 I2 Localization 在 JAT 提供非 CJK 翻译时的两个错误行为：
///     1. AddSpacesToJoinedLanguages 在翻译文本的每个字符间插入空格（如 "Lust" → "L u s t"）
///     2. NGUILabelLocalizeSupport 始终走 "ja" 分支，使用日语标签属性导致英文文本溢出重叠
/// </summary>
public static class I2LocalizeNoneCjkFix
{
    // 缓存 NGUILabelLocalizeSupport 的私有字段和方法的 FieldInfo/MethodInfo，避免每次调用反射
    private static readonly FieldInfo CallAwakeField =
        AccessTools.Field(typeof(NGUILabelLocalizeSupport), "callAwake");

    private static readonly FieldInfo UpdateReqField =
        AccessTools.Field(typeof(NGUILabelLocalizeSupport), "updateReq");

    private static readonly MethodInfo AwakeMethod =
        AccessTools.Method(typeof(NGUILabelLocalizeSupport), "Awake");


    /// <summary>
    ///     Prefix: 在 Localize.OnLocalize() 执行前，如果目标语言不是 CJK，
    ///     临时将实例的 AddSpacesToJoinedLanguages 设为 false，防止每个字符间被插入空格。
    ///     原始值保存在 __state 中，由 Finalizer 恢复。
    /// </summary>
    [HarmonyPatch(typeof(Localize), "OnLocalize")]
    [HarmonyPrefix]
    public static void Localize_OnLocalize_Prefix(Localize __instance, ref bool __state)
    {
        try
        {
            __state = __instance.AddSpacesToJoinedLanguages;

            if (__instance.AddSpacesToJoinedLanguages)
                __instance.AddSpacesToJoinedLanguages = false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"Localize_OnLocalize_Prefix unknow error, please report this issue/未知错误，请报告此错误: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     Finalizer: 恢复 AddSpacesToJoinedLanguages 的原始值。
    ///     使用 Finalizer 而非 Postfix 确保即使原方法抛出异常也能恢复。
    /// </summary>
    [HarmonyPatch(typeof(Localize), "OnLocalize")]
    [HarmonyFinalizer]
    public static void Localize_OnLocalize_Finalizer(Localize __instance, bool __state)
    {
        try
        {
            __instance.AddSpacesToJoinedLanguages = __state;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"Localize_OnLocalize_Finalizer unknow error, please report this issue/未知错误，请报告此错误: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     Prefix: 替换 NGUILabelLocalizeSupport.OnLocalize() 的分支逻辑。
    ///     原方法检查 CurrentLanguageCode == "ja" 来决定走日语分支还是覆盖属性分支，
    ///     但 JAT 不修改语言代码，导致始终走日语分支。
    ///     此 Prefix 在非 CJK 目标语言时直接执行覆盖属性分支（else 分支）。
    /// </summary>
    [HarmonyPatch(typeof(NGUILabelLocalizeSupport), "OnLocalize")]
    [HarmonyPrefix]
    public static bool NGUILabelLocalizeSupport_OnLocalize_Prefix(
        NGUILabelLocalizeSupport __instance)
    {
        try
        {
            // 检查 callAwake，如果未调用则触发 Awake
            if (CallAwakeField != null && !(bool)CallAwakeField.GetValue(__instance))
                AwakeMethod?.Invoke(__instance, null);

            // 检查 updateReq
            if (UpdateReqField != null && !(bool)UpdateReqField.GetValue(__instance))
                return false;

            // 执行非日语覆盖属性分支：Apply(overRidePropertys)
            __instance.Apply(__instance.overRidePropertys);

            // 检查特定语言的覆盖属性
            if (__instance.languageOverRidePropertys != null)
            {
                var targetLang = JustAnotherTranslator.TargetLanguage.Value;
                foreach (var langOverride in __instance.languageOverRidePropertys)
                    if (langOverride.LanguageCode == targetLang)
                    {
                        __instance.Apply(langOverride.Property);
                        break;
                    }
            }

            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"NGUILabelLocalizeSupport_OnLocalize_Prefix unknow error, please report this issue/未知错误，请报告此错误: {e.Message}\n{e.StackTrace}");
            return true;
        }
    }
}

/// <summary>
/// 修复 I2 Localization 在 Maid Cafe 场景中处理非 CJK 翻译时的两个问题：
/// 1. 在 MaidCafe Stream UI 初始化前，强制切换到英文评论布局以避免翻译错位。
/// 2. 在 StartStreamingPart 执行后，覆盖系统语言回写的评论布局设置，确保评论布局始终使用英文模式。
/// </summary>
public static class I2LocalizeNoneCjkFixMaidCafe
{
    /// <summary>
    ///     在 MaidCafe Stream UI 初始化前，切换到英文评论布局。
    /// </summary>
    [HarmonyPatch(typeof(MaidCafeStreamManager), "Awake")]
    [HarmonyPrefix]
    public static void MaidCafeStreamManager_Awake_Prefix(MaidCafeStreamManager __instance)
    {
        try
        {
            if (MaidCafeManager.streamingPartManager != null)
                __instance.isEnCommentMode = true;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"MaidCafeStreamManager_Awake_Prefix unknow error, please report this issue/未知错误，请报告此错误: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     StartStreamingPart 会在实例化后再次按 Product.systemLanguage 回写 isEnCommentMode。
    ///     在 Postfix 中覆盖回来，保证后续 CommentData 和坐标计算都使用英文布局分支。
    /// </summary>
    [HarmonyPatch(typeof(MaidCafeManager), "StartStreamingPart")]
    [HarmonyPostfix]
    public static void MaidCafeManager_StartStreamingPart_Postfix()
    {
        try
        {
            if (MaidCafeManager.streamingPartManager != null)
                MaidCafeManager.streamingPartManager.isEnCommentMode = true;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"MaidCafeManager_StartStreamingPart_Postfix unknow error, please report this issue/未知错误，请报告此错误: {e.Message}\n{e.StackTrace}");
        }
    }
}