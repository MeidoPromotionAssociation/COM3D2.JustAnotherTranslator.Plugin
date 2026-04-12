using System;
using System.Reflection;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using I2.Loc;
using MaidCafe;
using UnityEngine;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.Fixer;

/// <summary>
///     修复 I2 Localization 在启用 NoneCjkFix 时的多个错误行为：
///     1. AddSpacesToJoinedLanguages 在翻译文本的每个字符间插入空格（如 "Lust" → "L u s t"）
///     2. 多个 *LocalizeSupport 组件始终走 "ja" 分支，导致英文布局覆盖属性完全失效
///     3. 若场景代码按 Product.supportMultiLanguage 选择状态表面板，则会错误地继续使用日文布局
/// </summary>
public static class I2LocalizeNoneCjkFix
{
    // 缓存多个 LocalizeSupport 的私有字段和方法，避免每次调用反射
    private static readonly FieldInfo LabelCallAwakeField =
        AccessTools.Field(typeof(NGUILabelLocalizeSupport), "callAwake");

    private static readonly FieldInfo LabelUpdateReqField =
        AccessTools.Field(typeof(NGUILabelLocalizeSupport), "updateReq");

    private static readonly MethodInfo LabelAwakeMethod =
        AccessTools.Method(typeof(NGUILabelLocalizeSupport), "Awake");

    private static readonly FieldInfo WidgetCallAwakeField =
        AccessTools.Field(typeof(UIWidgetlLocalizeSupport), "callAwake");

    private static readonly MethodInfo WidgetAwakeMethod =
        AccessTools.Method(typeof(UIWidgetlLocalizeSupport), "Awake");

    private static readonly FieldInfo RectTransformCallAwakeField =
        AccessTools.Field(typeof(UIRectTransformlLocalizeSupport), "callAwake");

    private static readonly MethodInfo RectTransformAwakeMethod =
        AccessTools.Method(typeof(UIRectTransformlLocalizeSupport), "Awake");

    private static readonly FieldInfo TransformCallAwakeField =
        AccessTools.Field(typeof(TransformLocalizeSupport), "callAwake");

    private static readonly MethodInfo TransformAwakeMethod =
        AccessTools.Method(typeof(TransformLocalizeSupport), "Awake");

    private static void EnsureAwake(object instance, FieldInfo callAwakeField,
        MethodInfo awakeMethod)
    {
        if (instance == null)
            return;

        if (callAwakeField == null || awakeMethod == null)
            return;

        if (!(bool)callAwakeField.GetValue(instance))
            awakeMethod.Invoke(instance, null);
    }

    private static bool ShouldUseLocalizedStatusPanel(CharaSelectStatusMgr statusMgr)
    {
        return statusMgr != null &&
               statusMgr.statusLocalizeCtrl != null;
    }

    private static GameObject GetPreferredStatusPanel(CharaSelectStatusMgr statusMgr)
    {
        return ShouldUseLocalizedStatusPanel(statusMgr)
            ? statusMgr.statusLocalizeCtrl.gameObject
            : statusMgr?.statusCtrl?.gameObject;
    }

    private static bool IsLocalizedStatusPanel(StatusCtrl statusCtrl)
    {
        return statusCtrl != null &&
               statusCtrl.gameObject != null &&
               statusCtrl.gameObject.name.IndexOf("localize", StringComparison.OrdinalIgnoreCase) >=
               0;
    }

    private static UILabel GetDaysOfEmploymentTitleLabel(StatusCtrl statusCtrl)
    {
        if (statusCtrl?.gameObject == null)
            return null;

        var titleTransform = statusCtrl.gameObject.transform.Find("DaysOfEmployment/Title");
        return titleTransform?.GetComponent<UILabel>();
    }

    /// <summary>
    ///     Prefix: 在 Localize.OnLocalize() 执行前，
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
    ///     此 Prefix 启用后直接执行覆盖属性分支（else 分支）。
    /// </summary>
    [HarmonyPatch(typeof(NGUILabelLocalizeSupport), "OnLocalize")]
    [HarmonyPrefix]
    public static bool NGUILabelLocalizeSupport_OnLocalize_Prefix(
        NGUILabelLocalizeSupport __instance)
    {
        try
        {
            EnsureAwake(__instance, LabelCallAwakeField, LabelAwakeMethod);

            // 检查 updateReq
            if (LabelUpdateReqField != null && !(bool)LabelUpdateReqField.GetValue(__instance))
                return false;

            // 执行非日语覆盖属性分支：Apply(overRidePropertys)
            __instance.Apply(__instance.overRidePropertys);

            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"NGUILabelLocalizeSupport_OnLocalize_Prefix unknow error, please report this issue/未知错误，请报告此错误: {e.Message}\n{e.StackTrace}");
            return true;
        }
    }

    /// <summary>
    ///     Prefix: 强制 UIWidgetlLocalizeSupport 走覆盖属性分支。
    /// </summary>
    [HarmonyPatch(typeof(UIWidgetlLocalizeSupport), "OnLocalize")]
    [HarmonyPrefix]
    public static bool UIWidgetlLocalizeSupport_OnLocalize_Prefix(
        UIWidgetlLocalizeSupport __instance)
    {
        try
        {
            EnsureAwake(__instance, WidgetCallAwakeField, WidgetAwakeMethod);
            __instance.Apply(__instance.overRidePropertys);

            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"UIWidgetlLocalizeSupport_OnLocalize_Prefix unknow error, please report this issue/未知错误，请报告此错误: {e.Message}\n{e.StackTrace}");
            return true;
        }
    }

    /// <summary>
    ///     Prefix: 强制 UIRectTransformlLocalizeSupport 走覆盖属性分支。
    /// </summary>
    [HarmonyPatch(typeof(UIRectTransformlLocalizeSupport), "OnLocalize")]
    [HarmonyPrefix]
    public static bool UIRectTransformlLocalizeSupport_OnLocalize_Prefix(
        UIRectTransformlLocalizeSupport __instance)
    {
        try
        {
            EnsureAwake(__instance, RectTransformCallAwakeField, RectTransformAwakeMethod);
            __instance.Apply(__instance.overRidePropertys);

            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"UIRectTransformlLocalizeSupport_OnLocalize_Prefix unknow error, please report this issue/未知错误，请报告此错误: {e.Message}\n{e.StackTrace}");
            return true;
        }
    }

    /// <summary>
    ///     Prefix: 强制 TransformLocalizeSupport 走覆盖属性分支。
    /// </summary>
    [HarmonyPatch(typeof(TransformLocalizeSupport), "OnLocalize")]
    [HarmonyPrefix]
    public static bool TransformLocalizeSupport_OnLocalize_Prefix(
        TransformLocalizeSupport __instance)
    {
        try
        {
            EnsureAwake(__instance, TransformCallAwakeField, TransformAwakeMethod);
            __instance.Apply(__instance.overRidePropertys);

            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"TransformLocalizeSupport_OnLocalize_Prefix unknow error, please report this issue/未知错误，请报告此错误: {e.Message}\n{e.StackTrace}");
            return true;
        }
    }

    /// <summary>
    ///     Prefix: 在带有 localized 状态表的场景中，优先使用 statusLocalizeCtrl。
    ///     这能避免 JP 版 supportMultiLanguage=false 时错误选回日文布局。
    /// </summary>
    [HarmonyPatch(typeof(CharaSelectStatusMgr), "Init")]
    [HarmonyPrefix]
    public static bool CharaSelectStatusMgr_Init_Prefix(CharaSelectStatusMgr __instance)
    {
        try
        {
            if (!ShouldUseLocalizedStatusPanel(__instance))
                return true;

            var preferredCtrl = __instance.statusLocalizeCtrl;
            __instance.statusCtrl?.gameObject.SetActive(false);
            preferredCtrl.gameObject.SetActive(false);

            var traverse = Traverse.Create(__instance);
            traverse.Field("m_ctrl").SetValue(preferredCtrl);
            traverse.Field("m_goPanel").SetValue(preferredCtrl.gameObject);

            preferredCtrl.Init(__instance, preferredCtrl.gameObject);
            preferredCtrl.gameObject.SetActive(false);
            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"CharaSelectStatusMgr_Init_Prefix unknow error, please report this issue/未知错误，请报告此错误: {e.Message}\n{e.StackTrace}");
            return true;
        }
    }

    /// <summary>
    ///     Postfix: MaidManagementMain 仍会按 Product.supportMultiLanguage 记录 status_panel_。
    ///     启用补丁后，将其改为 localized 状态表，确保按钮逻辑和显示状态一致。
    /// </summary>
    [HarmonyPatch(typeof(MaidManagementMain), "Awake")]
    [HarmonyPostfix]
    public static void MaidManagementMain_Awake_Postfix(MaidManagementMain __instance)
    {
        try
        {
            var statusMgr =
                Traverse.Create(__instance).Field("status_mgr_").GetValue<CharaSelectStatusMgr>();
            if (!ShouldUseLocalizedStatusPanel(statusMgr))
                return;

            var preferredPanel = GetPreferredStatusPanel(statusMgr);
            if (preferredPanel == null)
                return;

            Traverse.Create(__instance).Field("status_panel_").SetValue(preferredPanel);
            statusMgr.statusCtrl?.gameObject.SetActive(false);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"MaidManagementMain_Awake_Postfix unknow error, please report this issue/未知错误，请报告此错误: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     Postfix: MaidTransferMain.Awake 原逻辑仍会激活日文状态表。
    ///     启用补丁后，切回 localized 状态表以匹配 CharaSelectStatusMgr.Init 的选择。
    /// </summary>
    [HarmonyPatch(typeof(MaidTransferMain), "Awake")]
    [HarmonyPostfix]
    public static void MaidTransferMain_Awake_Postfix(MaidTransferMain __instance)
    {
        try
        {
            var statusMgr = Traverse.Create(__instance).Field("statusMgr")
                .GetValue<CharaSelectStatusMgr>();
            if (!ShouldUseLocalizedStatusPanel(statusMgr))
                return;

            statusMgr.statusCtrl?.gameObject.SetActive(false);
            statusMgr.statusLocalizeCtrl.gameObject.SetActive(true);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"MaidTransferMain_Awake_Postfix unknow error, please report this issue/未知错误，请报告此错误: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     Prefix: 记录 localized 状态表标题原始文本。
    ///     StatusCtrl.SetData 在 !supportMultiLanguage 时会把“雇用日数”直接写回标题，
    ///     这会覆盖 localized 面板原本的英文文本或 Localize 结果。
    /// </summary>
    [HarmonyPatch(typeof(StatusCtrl), "SetData")]
    [HarmonyPrefix]
    public static void StatusCtrl_SetData_Prefix(StatusCtrl __instance, out string __state)
    {
        __state = null;
        try
        {
            if (!IsLocalizedStatusPanel(__instance))
                return;

            __state = GetDaysOfEmploymentTitleLabel(__instance)?.text;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"StatusCtrl_SetData_Prefix unknow error, please report this issue/未知错误，请报告此错误: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     Postfix: 还原 localized 状态表标题。
    ///     若标题挂有 Localize 组件则重新执行本地化，否则回退到 Prefix 保存的原始文本。
    /// </summary>
    [HarmonyPatch(typeof(StatusCtrl), "SetData")]
    [HarmonyPostfix]
    public static void StatusCtrl_SetData_Postfix(StatusCtrl __instance, string __state)
    {
        try
        {
            if (!IsLocalizedStatusPanel(__instance))
                return;

            var titleLabel = GetDaysOfEmploymentTitleLabel(__instance);
            if (titleLabel == null)
                return;

            var localize = titleLabel.GetComponent<Localize>();
            if (localize != null)
            {
                localize.OnLocalize(true);
                return;
            }

            if (!string.IsNullOrEmpty(__state))
                titleLabel.text = __state;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"StatusCtrl_SetData_Postfix unknow error, please report this issue/未知错误，请报告此错误: {e.Message}\n{e.StackTrace}");
        }
    }
}

/// <summary>
///     修复 I2 Localization 在 Maid Cafe 场景中处理非 CJK 翻译时的两个问题：
///     1. 在 MaidCafe Stream UI 初始化前，强制切换到英文评论布局以避免翻译错位。
///     2. 在 StartStreamingPart 执行后，覆盖系统语言回写的评论布局设置，确保评论布局始终使用英文模式。
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