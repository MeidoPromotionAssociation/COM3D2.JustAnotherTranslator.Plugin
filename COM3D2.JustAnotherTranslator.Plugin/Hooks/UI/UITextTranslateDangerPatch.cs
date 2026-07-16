using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using Edit;
using HarmonyLib;
using I2.Loc;
using MaidStatus;
using MyRoomCustom;
using UnityEngine;
using wf;
using Math = System.Math;
using Object = UnityEngine.Object;
using UnityText = UnityEngine.UI.Text;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.UI;

/// <summary>
///     高风险 UI 文本翻译补丁。
///     这里的补丁和 UITextTranslateExtraPatch 的边界不同：
///     1. 会读取或写入游戏类的 private 字段；
///     2. 会依赖 NGUI prefab 的子节点顺序、字段名、私有嵌套 ElementData 结构；
///     3. 有些补丁会替换原方法的显示层逻辑，以避开 Product.supportMultiLanguage 分支。
///     这些实现能覆盖 JP 版完全绕开 I2 Localization 的路径，但游戏更新、反编译字段名变化、
///     或 UI prefab 层级调整时都可能失效。因此管理器只在 EnableUITextDangerPatch=true 时注册，
///     且每个入口都尽量在字段缺失时回退到原逻辑。
/// </summary>
public static class UITextTranslateDangerPatch
{
    private static readonly FieldInfo PopupAndTabListDataField =
        AccessTools.Field(typeof(PopupAndTabList), "popup_and_button_name_list_");

    private static readonly FieldInfo PopupAndButtonListDataField =
        AccessTools.Field(typeof(PopupAndButtonList), "popup_and_button_name_list_");

    private static readonly MethodInfo UIWFConditionListAwakeMethod =
        AccessTools.Method(typeof(UIWFConditionList), "Awake");

    private static readonly FieldInfo UIWFConditionListLabelsField =
        AccessTools.Field(typeof(UIWFConditionList), "condition_label_list_");

    private static readonly FieldInfo UIWFConditionListLocalizesField =
        AccessTools.Field(typeof(UIWFConditionList), "condition_label_localize_list_");

    private static readonly FieldInfo UIWFConditionListLastLimitTextWidthField =
        AccessTools.Field(typeof(UIWFConditionList), "lastLimitTextWidth");

    private static readonly FieldInfo UIWFConditionListUpdateFlagField =
        AccessTools.Field(typeof(UIWFConditionList), "updateFlag");

    private static readonly MethodInfo UIWFConditionListHeightSetter =
        AccessTools.PropertySetter(typeof(UIWFConditionList), "height");

    private static readonly FieldInfo UIWFConditionListHeightBackingField =
        AccessTools.Field(typeof(UIWFConditionList), "<height>k__BackingField");

    private static readonly FieldInfo ShopItemInfoWindowField =
        AccessTools.Field(typeof(ShopItem), "info_window_");

    private static readonly FieldInfo ShopItemIconSpriteField =
        AccessTools.Field(typeof(ShopItem), "icon_sprite_");

    private static readonly FieldInfo ShopItemClickEventField =
        AccessTools.Field(typeof(ShopItem), "click_event_");

    private static readonly FieldInfo CasinoItemUIItemDataField =
        AccessTools.Field(typeof(CasinoItemUI), "m_ItemData");

    private static readonly MethodInfo SceneCasinoShopUpdateUIStateMethod =
        AccessTools.Method(typeof(SceneCasinoShop), "UpdateUIState");

    #region MaidProfileComment

    /// <summary>
    ///     MaidProfile.Create 内部的 GetTranslation 委托在 JP 版（!Product.supportMultiLanguage）
    ///     下直接返回 CSV 中的日文原文，不调用 I2 Localization。
    ///     此 Patcher 通过运行时反射定位编译器生成的委托方法，并在返回后用当前 JAT/I2
    ///     翻译替换结果。不能伪造 supportMultiLanguage=true：原生多语言分支显式请求
    ///     Product.systemLanguage，而 JAT 的单语言 UI 字典不应覆盖这种 overrideLanguage 查询。
    /// </summary>
    public static class MaidProfileCommentPatcher
    {
        private static bool _patched;

        public static void Reset()
        {
            _patched = false;
        }

        public static void ApplyPatch(Harmony harmony)
        {
            try
            {
                if (_patched) return;

                var targetMethod = FindGetTranslationMethod();
                if (targetMethod == null)
                {
                    LogManager.Warning(
                        "MaidProfileCommentPatcher: Could not find compiler-generated GetTranslation method in MaidProfile, skip patch/" +
                        "无法找到 MaidProfile 中编译器生成的 GetTranslation 方法，跳过补丁");
                    return;
                }

                var postfix = new HarmonyMethod(typeof(MaidProfileCommentPatcher),
                    nameof(GetTranslationPostfix));
                harmony.Patch(targetMethod, postfix: postfix);
                _patched = true;

                LogManager.Debug(
                    $"MaidProfileCommentPatcher: Successfully patched {targetMethod.DeclaringType?.Name}.{targetMethod.Name}");
            }
            catch (Exception e)
            {
                LogManager.Error(
                    $"MaidProfileCommentPatcher.ApplyPatch unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
            }
        }

        private static MethodInfo FindGetTranslationMethod()
        {
            var supportMultiLanguageGetter = AccessTools.PropertyGetter(
                typeof(Product), "supportMultiLanguage");
            if (supportMultiLanguageGetter == null)
                return null;

            var nestedTypes = typeof(MaidProfile).GetNestedTypes(
                BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var type in nestedTypes)
            {
                var methods = type.GetMethods(
                    BindingFlags.Instance | BindingFlags.Static |
                    BindingFlags.NonPublic | BindingFlags.Public);

                foreach (var method in methods)
                {
                    if (method.ReturnType != typeof(string))
                        continue;

                    var parameters = method.GetParameters();
                    if (parameters.Length != 1 || parameters[0].ParameterType != typeof(string))
                        continue;

                    if (!MethodContainsCall(method, supportMultiLanguageGetter))
                        continue;

                    return method;
                }
            }

            return null;
        }

        private static bool MethodContainsCall(MethodInfo method, MethodInfo target)
        {
            try
            {
                var instructions = PatchProcessor.GetOriginalInstructions(method);
                return instructions.Any(instr =>
                    (instr.opcode == OpCodes.Call || instr.opcode == OpCodes.Callvirt) &&
                    instr.operand is MethodInfo mi &&
                    mi == target);
            }
            catch
            {
                return false;
            }
        }

        // 编译器生成方法的参数名可能随编译器版本变化，使用 __args 按位置读取 termSuffix。
        private static void GetTranslationPostfix(object[] __args, ref string __result)
        {
            var originalResult = __result;
            try
            {
                if (Product.supportMultiLanguage || __args == null || __args.Length == 0)
                    return;

                var termSuffix = __args[0] as string;
                if (string.IsNullOrEmpty(termSuffix))
                    return;

                __result = TranslateTermOrFallback("ProfileComment/" + termSuffix,
                    originalResult);
            }
            catch (Exception e)
            {
                __result = originalResult;
                LogManager.Error(
                    $"MaidProfileCommentPatcher.GetTranslationPostfix unknown error, preserving original text/未知错误，保留原文 {e.Message}\n{e.StackTrace}");
            }
        }
    }

    #endregion

    #region PopupAndTabList / PopupAndButtonList

    /// <summary>
    ///     PopupAndTabList.SetData 的 term 回填。
    ///     BGWindow / FaceWindow / MotionWindow 在 JP 版会把 buttonTermList 置为 null，
    ///     PopupAndTabList.SetData 因此会创建 term 为空的私有 ElementData。
    ///     后续 OnChangePopUpList 即使被我们强制执行 Localize，也没有 term 可用。
    ///     这里通过 private 字段 popup_and_button_name_list_ 访问 ElementData，
    ///     再从 ElementData.value 指向的 PhotoBGData / PhotoFaceData / PhotoMotionData 等
    ///     数据对象上读取 nameTerm 或 termName。风险点是字段名和嵌套类结构都来自当前游戏实现。
    /// </summary>
    [HarmonyPatch(typeof(PopupAndTabList), "SetData")]
    [HarmonyPostfix]
    private static void PopupAndTabList_SetData_Postfix(PopupAndTabList __instance)
    {
        try
        {
            if (Product.supportMultiLanguage)
                return;

            FillPopupElementTerms(__instance, PopupAndTabListDataField);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"PopupAndTabList_SetData_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     PopupAndTabList.AddData 的 term 回填。
    ///     AddData 用于“マイポーズ”等运行时追加项。原方法创建的 ElementData 没有 term 字段赋值，
    ///     所以这里复用 SetData 的扫描逻辑补齐可推导的 term。无法推导时保持空值。
    /// </summary>
    [HarmonyPatch(typeof(PopupAndTabList), "AddData")]
    [HarmonyPostfix]
    private static void PopupAndTabList_AddData_Postfix(PopupAndTabList __instance)
    {
        try
        {
            if (Product.supportMultiLanguage)
                return;

            FillPopupElementTerms(__instance, PopupAndTabListDataField);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"PopupAndTabList_AddData_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     PopupAndTabList.OnChangePopUpList 的 Localize 强制应用。
    ///     原方法在设置按钮文本后，只在 Product.supportMultiLanguage 为 true 时才读取
    ///     private ElementData.term 并调用 Localize.SetTerm。JP 版不会进入该分支。
    ///     这里在原方法完成后重新按“私有 ElementData 列表顺序 == TabPanel 子节点顺序”的假设
    ///     把 term 写回按钮标签。这个顺序假设来自当前 PopupAndTabList.OnChangePopUpList 的实现，
    ///     也是本补丁最大的版本兼容风险。
    /// </summary>
    [HarmonyPatch(typeof(PopupAndTabList), "OnChangePopUpList")]
    [HarmonyPostfix]
    private static void PopupAndTabList_OnChangePopUpList_Postfix(
        PopupAndTabList __instance,
        KeyValuePair<string, Object> popup_val)
    {
        try
        {
            if (Product.supportMultiLanguage || __instance == null ||
                __instance.TabPanel == null)
                return;

            // Harmony003 is a false positive here: we only read popup_val.Key, never assign to it.
#pragma warning disable Harmony003
            var key = popup_val.Key;
#pragma warning restore Harmony003
            if (!TryGetPopupElementList(__instance, PopupAndTabListDataField, key,
                    out var list))
                return;

            ApplyElementTermsToChildren(list, __instance.TabPanel.transform);

            var grid = __instance.TabPanel.GetComponent<UIGrid>();
            if (grid != null)
                grid.Reposition();
            if (__instance.ScrollView != null)
                __instance.ScrollView.ResetPosition();
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"PopupAndTabList_OnChangePopUpList_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     PopupAndButtonList.SetData 的 term 回填。
    ///     CreateBGObjectSubWindow 目前会传入 buttonTermList，但 PopupAndButtonList 自身仍然
    ///     在 OnChangePopUpList 中用 Product.supportMultiLanguage 拦住 Localize.SetTerm。
    ///     另外 AddData 追加的“マイオブジェクト”等条目没有 term，因此这里也补齐可从 value 推导的 term。
    /// </summary>
    [HarmonyPatch(typeof(PopupAndButtonList), "SetData")]
    [HarmonyPostfix]
    private static void PopupAndButtonList_SetData_Postfix(PopupAndButtonList __instance)
    {
        try
        {
            if (Product.supportMultiLanguage)
                return;

            FillPopupElementTerms(__instance, PopupAndButtonListDataField);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"PopupAndButtonList_SetData_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     PopupAndButtonList.AddData 的 term 回填。
    ///     风险同 PopupAndTabList.AddData：依赖 private ElementData.value / term 字段名。
    /// </summary>
    [HarmonyPatch(typeof(PopupAndButtonList), "AddData")]
    [HarmonyPostfix]
    private static void PopupAndButtonList_AddData_Postfix(PopupAndButtonList __instance)
    {
        try
        {
            if (Product.supportMultiLanguage)
                return;

            FillPopupElementTerms(__instance, PopupAndButtonListDataField);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"PopupAndButtonList_AddData_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     PopupAndButtonList.OnChangePopUpList 的 Localize 强制应用。
    ///     与 PopupAndTabList 的补丁相同，依赖 private ElementData 列表与 Grid 子节点顺序一致。
    ///     这能覆盖照片模式背景物体列表按钮等路径，但 prefab 或原方法循环方式变化时可能失效。
    /// </summary>
    [HarmonyPatch(typeof(PopupAndButtonList), "OnChangePopUpList")]
    [HarmonyPostfix]
    private static void PopupAndButtonList_OnChangePopUpList_Postfix(
        PopupAndButtonList __instance,
        KeyValuePair<string, Object> popup_val)
    {
        try
        {
            if (Product.supportMultiLanguage || __instance == null ||
                __instance.Grid == null)
                return;

            // Harmony003 is a false positive here: we only read popup_val.Key, never assign to it.
#pragma warning disable Harmony003
            var key = popup_val.Key;
#pragma warning restore Harmony003
            if (!TryGetPopupElementList(__instance, PopupAndButtonListDataField, key,
                    out var list))
                return;

            ApplyElementTermsToChildren(list, __instance.Grid.transform);

            Utility.ResetNGUI(__instance.Grid);
            if (__instance.ScrollView != null)
                Utility.ResetNGUI(__instance.ScrollView);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"PopupAndButtonList_OnChangePopUpList_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    private static void FillPopupElementTerms(object instance, FieldInfo dataField)
    {
        if (instance == null || dataField == null)
            return;

        if (!(dataField.GetValue(instance) is IDictionary data))
            return;

        foreach (DictionaryEntry category in data)
        {
            if (!(category.Value is IList elements))
                continue;

            foreach (var element in elements)
            {
                if (element == null)
                    continue;

                var elementType = element.GetType();
                var termField = AccessTools.Field(elementType, "term");
                var valueField = AccessTools.Field(elementType, "value");
                if (termField == null || valueField == null)
                    continue;

                var currentTerm = termField.GetValue(element) as string;
                if (!string.IsNullOrEmpty(currentTerm))
                    continue;

                var term = GetTermFromValueObject(valueField.GetValue(element));
                if (!string.IsNullOrEmpty(term))
                    termField.SetValue(element, term);
            }
        }
    }

    private static bool TryGetPopupElementList(
        object instance,
        FieldInfo dataField,
        string key,
        out IList list)
    {
        list = null;
        if (instance == null || dataField == null || string.IsNullOrEmpty(key))
            return false;

        if (!(dataField.GetValue(instance) is IDictionary data) || !data.Contains(key))
            return false;

        list = data[key] as IList;
        return list != null;
    }

    private static void ApplyElementTermsToChildren(IList elements, Transform parent)
    {
        if (elements == null || parent == null)
            return;

        var count = Math.Min(elements.Count, parent.childCount);
        for (var i = 0; i < count; i++)
        {
            var child = parent.GetChild(i);
            if (child == null || !child.gameObject.activeSelf)
                continue;

            var term = GetElementTerm(elements[i]);
            var label = child.GetComponentInChildren<UILabel>();
            if (label == null)
                continue;

            var localize = label.GetComponent<Localize>();
            if (string.IsNullOrEmpty(term))
            {
                if (localize != null)
                {
                    localize.enabled = false;
                    localize.TermArgs = null;
                    localize.mTerm = string.Empty;
                    localize.mTermSecondary = string.Empty;
                    localize.FinalTerm = string.Empty;
                    localize.FinalSecondaryTerm = string.Empty;
                }

                continue;
            }

            if (localize == null)
                localize = label.gameObject.AddComponent<Localize>();

            var fallback = label.text;
            // SetTerm forces one localization pass even while the component is disabled. Doing it
            // before enabling prevents OnEnable from applying the previous category's stale term.
            localize.SetTerm(term);
            localize.enabled = true;
            if (StringTool.IsNullOrWhiteSpace(label.text))
                label.text = fallback;
        }
    }

    private static string GetElementTerm(object element)
    {
        if (element == null)
            return string.Empty;

        var termField = AccessTools.Field(element.GetType(), "term");
        return termField?.GetValue(element) as string ?? string.Empty;
    }

    private static string GetTermFromValueObject(object value)
    {
        if (value == null)
            return string.Empty;

        foreach (var memberName in new[]
                 {
                     "termName",
                     "nameTerm",
                     "NameTerm",
                     "DocumentTerm"
                 })
        {
            var term = GetStringMember(value, memberName);
            if (!string.IsNullOrEmpty(term))
                return term;
        }

        return string.Empty;
    }

    private static string GetStringMember(object value, string memberName)
    {
        var type = value.GetType();
        var property = AccessTools.Property(type, memberName);
        if (property != null && property.PropertyType == typeof(string))
            return property.GetValue(value, null) as string;

        var field = AccessTools.Field(type, memberName);
        if (field != null && field.FieldType == typeof(string))
            return field.GetValue(value) as string;

        return string.Empty;
    }

    #endregion

    #region PhotoModePopupCategories

    /// <summary>
    ///     照片模式背景窗口分类下拉框 term。
    ///     BGWindow.Awake 在 JP 版会明确写入 null：
    ///     PopupAndTabList.popup_term_list = Product.supportMultiLanguage ? terms : null。
    ///     这里使用公开属性写回 PhotoBGData.popup_category_term_list。
    ///     这本身不需要私有字段，但它依赖 BGWindow 当前 Awake 初始化顺序和 PhotoBGData 的具体公开字段，
    ///     属于照片模式实现细节，因此和 PopupAndTabList 的危险补丁放在同一开关下。
    /// </summary>
    [HarmonyPatch(typeof(BGWindow), "Awake")]
    [HarmonyPostfix]
    private static void BGWindow_Awake_Postfix(BGWindow __instance)
    {
        try
        {
            if (Product.supportMultiLanguage)
                return;

            if (__instance?.PopupAndTabList != null && PhotoBGData.popup_category_term_list != null)
                __instance.PopupAndTabList.popup_term_list = PhotoBGData.popup_category_term_list;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"BGWindow_Awake_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     照片模式表情窗口分类下拉框 term。
    ///     原方法在 maid 使用 PhotoFaceData 时写入表情分类数据，但 JP 版仍会把 term 列表置空。
    ///     COM3D2 只允许非男性，COM3D2.5 还允许 IsNowRealMan 的真实男性。
    ///     这里按各版本相同条件写回 PhotoFaceData.popup_category_term_list；没有表情数据时清空，
    ///     避免 UIPopupList.items 与 itemTerms 数量不一致。
    /// </summary>
    [HarmonyPatch(typeof(FaceWindow), "OnMaidChangeEvent")]
    [HarmonyPostfix]
    private static void FaceWindow_OnMaidChangeEvent_Postfix(FaceWindow __instance, Maid maid)
    {
        try
        {
            if (Product.supportMultiLanguage || __instance?.PopupAndTabList == null)
                return;

#if COM3D25_UNITY_2022
            var hasFaceData = maid != null && (!maid.boMAN || maid.IsNowRealMan);
#else
            var hasFaceData = maid != null && !maid.boMAN;
#endif
            __instance.PopupAndTabList.popup_term_list = hasFaceData
                ? PhotoFaceData.popup_category_term_list
                : null;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"FaceWindow_OnMaidChangeEvent_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     照片模式动作窗口分类下拉框 term。
    ///     男性和女性使用不同的 popup_category_term_list。这个分支来自 MotionWindow.OnMaidChangeEvent
    ///     当前实现；如果未来游戏拆分了动作数据结构，这里可能需要同步调整。
    /// </summary>
    [HarmonyPatch(typeof(MotionWindow), "OnMaidChangeEvent")]
    [HarmonyPostfix]
    private static void MotionWindow_OnMaidChangeEvent_Postfix(MotionWindow __instance, Maid maid)
    {
        try
        {
            if (Product.supportMultiLanguage || __instance?.PopupAndTabList == null)
                return;

            if (maid == null)
            {
                __instance.PopupAndTabList.popup_term_list = null;
                return;
            }

            __instance.PopupAndTabList.popup_term_list = maid.boMAN
                ? PhotoMotionData.popup_category_term_list_for_man
                : PhotoMotionData.popup_category_term_list;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"MotionWindow_OnMaidChangeEvent_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    #endregion

    #region UIWFConditionList

    /// <summary>
    ///     UIWFConditionList.SetTexts 的 JP 版 term 翻译。
    ///     原方法只有 Product.supportMultiLanguage 为 true 且行内存在 Localize 组件时，
    ///     才会把 texts[i].Key 当作 I2 term；JP 版直接 Utility.GetTermLastWord，
    ///     所以“条件文/...”这类 term 会退化成最后一段日文。
    ///     这里选择完整替换 KeyValuePair
    ///     <string[], Color>
    ///         [] 重载：
    ///         1. 通过 private condition_label_list_ 取得每行 UILabel；
    ///         2. 禁用每行已有 Localize，避免稍后的 I2 事件把我们写入的文本覆盖；
    ///         3. 用 LocalizationManager.GetTranslation 直接取翻译，失败时保持原有
    ///         Utility.GetTermLastWord 回退；
    ///         4. 写 private height / lastLimitTextWidth / updateFlag，并调用公开 ResizeUI。
    ///         风险点是 private 字段名、Awake 初始化方式、height 私有 setter 都是当前实现细节。
    /// </summary>
    [HarmonyPatch(typeof(UIWFConditionList), "SetTexts",
        typeof(KeyValuePair<string[], Color>[]),
        typeof(int))]
    [HarmonyPrefix]
    private static bool UIWFConditionList_SetTexts_Prefix(
        UIWFConditionList __instance,
        KeyValuePair<string[], Color>[] texts,
        int limitTextWidth)
    {
        var replacementStarted = false;
        try
        {
            if (__instance == null || texts == null)
                return true;
            if (Product.supportMultiLanguage)
                return true;

            if (!TryEnsureConditionListAwake(__instance))
                return true;

            if (UIWFConditionListLocalizesField == null ||
                UIWFConditionListLastLimitTextWidthField == null ||
                UIWFConditionListUpdateFlagField == null ||
                (UIWFConditionListHeightSetter == null &&
                 UIWFConditionListHeightBackingField == null))
                return true;

            if (!(UIWFConditionListLabelsField?.GetValue(__instance) is List<UILabel> labels) ||
                labels.Count == 0)
                return true;

            if (!(UIWFConditionListLocalizesField.GetValue(__instance)
                    is List<Localize> localizes) || localizes.Count < labels.Count)
                return true;

            // Validate every row and build every translated string before mutating the component.
            // If reflection or the prefab shape has drifted, the untouched game method can run.
            var rows = new Transform[labels.Count];
            var translatedTexts = new string[labels.Count];
            var colors = new Color[labels.Count];

            var firstY = 0f;
            var lastBottomY = 0f;
            for (var i = 0; i < labels.Count; i++)
            {
                var label = labels[i];
                if (label == null || label.transform == null || label.transform.parent == null)
                    return true;

                var row = label.transform.parent;
                rows[i] = row;
                if (i < texts.Length)
                {
                    var widget = row.gameObject.GetComponent<UIWidget>();
                    if (widget == null)
                        return true;

                    if (i == 0)
                        firstY = Mathf.Abs(row.localPosition.y);

                    translatedTexts[i] = BuildConditionText(texts[i].Key);
                    colors[i] = texts[i].Value;
                    lastBottomY = Mathf.Abs(row.localPosition.y) + widget.height;
                }
            }

            UIWFConditionListLastLimitTextWidthField.SetValue(__instance, limitTextWidth);
            replacementStarted = true;
            for (var i = 0; i < labels.Count; i++)
            {
                // Disabling is sufficient to stop a later I2 event from overwriting the direct
                // JP-path translation. Preserve every existing runtime and persistent listener.
                if (localizes[i] != null)
                    localizes[i].enabled = false;

                if (i < texts.Length)
                {
                    rows[i].gameObject.SetActive(true);
                    labels[i].text = translatedTexts[i];
                    labels[i].color = colors[i];
                }
                else
                {
                    rows[i].gameObject.SetActive(false);
                }
            }

            SetConditionListHeight(__instance, (int)(lastBottomY - firstY));
            __instance.ResizeUI(limitTextWidth);
            UIWFConditionListUpdateFlagField.SetValue(__instance, false);
            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"UIWFConditionList_SetTexts_Prefix unknown error/未知错误 " +
                $"(replacementStarted={replacementStarted}) {e.Message}\n{e.StackTrace}");
            return !replacementStarted;
        }
    }

    private static bool TryEnsureConditionListAwake(UIWFConditionList instance)
    {
        if (UIWFConditionListLabelsField?.GetValue(instance) != null)
            return true;

        if (UIWFConditionListAwakeMethod == null)
            return false;

        UIWFConditionListAwakeMethod.Invoke(instance, null);
        return UIWFConditionListLabelsField.GetValue(instance) != null;
    }

    private static void SetConditionListHeight(UIWFConditionList instance, int height)
    {
        if (UIWFConditionListHeightSetter != null)
        {
            UIWFConditionListHeightSetter.Invoke(instance, new object[] { height });
            return;
        }

        UIWFConditionListHeightBackingField.SetValue(instance, height);
    }

    private static string BuildConditionText(string[] terms)
    {
        if (terms == null || terms.Length == 0 || string.IsNullOrEmpty(terms[0]))
            return string.Empty;

        if (terms.Length == 1)
            return TranslateTermOrLastWord(terms[0]);

        var format = TranslateTermOrLastWord(terms[0]);
        var argumentsAreTerms = ConditionArgumentsAreTerms(terms[0]);
        var args = new string[terms.Length - 1];
        for (var i = 1; i < terms.Length; i++)
        {
            var argument = terms[i] ?? string.Empty;
            args[i - 1] = argumentsAreTerms
                ? TranslateTermOrLastWord(argument)
                : argument;
        }

        try
        {
            return string.Format(format, args);
        }
        catch (FormatException)
        {
            return format + " " + string.Join(" ", args);
        }
    }

    private static bool ConditionArgumentsAreTerms(string formatTerm)
    {
        return !string.IsNullOrEmpty(formatTerm) &&
               (formatTerm.Contains("性経験") ||
                formatTerm.Contains("契約タイプ") ||
                formatTerm.Contains("性癖") ||
                formatTerm.Contains("ヒロインタイプ"));
    }

    #endregion

    #region ShopItem

    /// <summary>
    ///     ショップ ItemInfo hover 的名称和说明翻译。
    ///     ShopItem.OnHoverOver 是 private，JP 版直接使用 item_data.name/detail_text。
    ///     若要避免重复打开窗口，只能替换该 private 方法，并读取 private info_window_ / icon_sprite_。
    ///     同时还复刻了原方法对 "Base" 子节点位置的调整，因此 UI 层级变化时可能失效。
    /// </summary>
    [HarmonyPatch(typeof(ShopItem), "OnHoverOver")]
    [HarmonyPrefix]
    private static bool ShopItem_OnHoverOver_Prefix(ShopItem __instance)
    {
        var replacementStarted = false;
        try
        {
            if (Product.supportMultiLanguage)
                return true;

            if (__instance == null || ShopItemInfoWindowField == null ||
                ShopItemIconSpriteField == null)
                return true;

            var itemData = __instance.item_data;
            var infoWindow = ShopItemInfoWindowField.GetValue(__instance) as ItemInfoWnd;
            var iconSprite = ShopItemIconSpriteField.GetValue(__instance) as UI2DSprite;
            if (itemData == null || infoWindow == null || iconSprite?.sprite2D?.texture == null)
                return true;

            var baseObject = UTY.GetChildObject(infoWindow.gameObject, "Base", false);
            if (baseObject == null)
                return true;

            var position = __instance.transform.position;
            var title = TranslateShopItemName(itemData);
            var info = TranslateShopItemDescription(itemData);
            var transform = baseObject.transform;

            // Open mutates the shared info window. Once it has been attempted, never run the
            // original method as a fallback or the same window can be opened a second time.
            replacementStarted = true;
            infoWindow.Open(position, iconSprite.sprite2D.texture, title, info);

            transform.position = position;
            var y = transform.localPosition.y;
            transform.localPosition = infoWindow.m_vecOffsetPos + new Vector3(-337f, y, 0f);
            if (-412f > transform.localPosition.y - 90f)
            {
                var offset = infoWindow.m_vecOffsetPos;
                offset.y *= -1f;
                transform.localPosition = offset + new Vector3(-337f, y, 0f);
            }

            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"ShopItem_OnHoverOver_Prefix unknown error/未知错误 " +
                $"(replacementStarted={replacementStarted}) {e.Message}\n{e.StackTrace}");
            return !replacementStarted;
        }
    }

    /// <summary>
    ///     ショップ购买确认对话框的商品名翻译。
    ///     原 ShopItem.OnClick 在 JP 版把 item_data.name 作为 {0} 参数传入
    ///     ShowFromLanguageTerm；对话框模板能被 JAT 翻译，但参数仍是日文。
    ///     这里替换 private OnClick，仅改变显示参数和 OK 回调入口，不改变资金检查和购买逻辑。
    /// </summary>
    [HarmonyPatch(typeof(ShopItem), "OnClick")]
    [HarmonyPrefix]
    private static bool ShopItem_OnClick_Prefix(ShopItem __instance)
    {
        var replacementStarted = false;
        try
        {
            if (Product.supportMultiLanguage)
                return true;

            if (__instance == null || ShopItemClickEventField == null)
                return true;

            var itemData = __instance.item_data;
            if (itemData == null)
                return true;

            var remainingMoney =
                GameMain.Instance.CharacterMgr.status.money - itemData.price;
            if (remainingMoney < 0L)
            {
                replacementStarted = true;
                GameMain.Instance.SysDlg.ShowFromLanguageTerm("Dialog/ショップ/資金が不足しています。",
                    null, SystemDialog.TYPE.OK);
                return false;
            }

            var args = new[]
            {
                TranslateShopItemName(itemData),
                Utility.ConvertMoneyText(itemData.price)
            };
            replacementStarted = true;
            GameMain.Instance.SysDlg.ShowFromLanguageTerm(
                "Dialog/ショップ/{0}を購入しますか?資金 -{1}CR",
                args,
                SystemDialog.TYPE.OK_CANCEL,
                () => RunShopItemClickEvent(__instance));
            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"ShopItem_OnClick_Prefix unknown error/未知错误 " +
                $"(replacementStarted={replacementStarted}) {e.Message}\n{e.StackTrace}");
            return !replacementStarted;
        }
    }

    /// <summary>
    ///     ショップ购买完成对话框的商品名翻译。
    ///     该 private 方法可能被原 OnClick 以外的路径调用，因此也单独 patch。
    /// </summary>
    [HarmonyPatch(typeof(ShopItem), "CallOnClickEvent")]
    [HarmonyPrefix]
    private static bool ShopItem_CallOnClickEvent_Prefix(ShopItem __instance)
    {
        var clickEventDispatched = false;
        try
        {
            if (Product.supportMultiLanguage)
                return true;

            if (__instance == null || ShopItemClickEventField == null ||
                __instance.item_data == null)
                return true;

            // Resolve every fallible display value before the purchase callback. If the callback
            // is attempted, it must have at-most-once semantics even when it or the dialog throws.
            var translatedName = TranslateShopItemName(__instance.item_data);
            var clickEvent = ShopItemClickEventField.GetValue(__instance) as ShopItem.OnClickEvent;
            if (clickEvent != null)
            {
                clickEventDispatched = true;
                clickEvent.Invoke(__instance);
            }

            GameMain.Instance.SysDlg.ShowFromLanguageTerm(
                "Dialog/ショップ/{0}を購入しました",
                new[] { translatedName },
                SystemDialog.TYPE.OK);
            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"ShopItem_CallOnClickEvent_Prefix unknown error/未知错误 " +
                $"(clickEventDispatched={clickEventDispatched}) {e.Message}\n{e.StackTrace}");
            return !clickEventDispatched;
        }
    }

    private static void RunShopItemClickEvent(ShopItem instance)
    {
        // Translate before invoking the callback because the callback performs the purchase.
        // Translation failures must not occur after that irreversible business operation.
        var translatedName = TranslateShopItemName(instance.item_data);
        var clickEvent = ShopItemClickEventField.GetValue(instance) as ShopItem.OnClickEvent;
        clickEvent?.Invoke(instance);

        GameMain.Instance.SysDlg.ShowFromLanguageTerm(
            "Dialog/ショップ/{0}を購入しました",
            new[] { translatedName },
            SystemDialog.TYPE.OK);
    }

    private static string TranslateShopItemName(Shop.ItemDataBase itemData)
    {
        if (itemData == null)
            return string.Empty;

        return TranslateTermOrFallback("SceneShop/" + itemData.id + "/名前", itemData.name);
    }

    private static string TranslateShopItemDescription(Shop.ItemDataBase itemData)
    {
        if (itemData == null)
            return string.Empty;

        return TranslateTermOrFallback("SceneShop/" + itemData.id + "/説明",
            itemData.detail_text).Replace("《改行》", "\n");
    }

    #endregion

    #region CasinoShop

    /// <summary>
    ///     カジノショップ购买确认对话框的商品名翻译。
    ///     CasinoItemUI.ItemBuy 是 private，且通过 private m_ItemData 取得商品数据。
    ///     原方法在 JP 版把 CasinoShopItem.Name 作为 {0} 参数传给对话框模板。
    ///     这里替换该方法，只调整参数翻译和确认回调。
    /// </summary>
    [HarmonyPatch(typeof(CasinoItemUI), "ItemBuy")]
    [HarmonyPrefix]
    private static bool CasinoItemUI_ItemBuy_Prefix(CasinoItemUI __instance)
    {
        var replacementStarted = false;
        try
        {
            if (Product.supportMultiLanguage)
                return true;

            if (__instance == null || CasinoItemUIItemDataField == null)
                return true;

            var itemData = CasinoItemUIItemDataField.GetValue(__instance) as CasinoShopItem;
            if (itemData == null)
                return true;

            if (!itemData.IsCanBuy)
            {
                replacementStarted = true;
                GameMain.Instance.SysDlg.ShowFromLanguageTerm(
                    "SceneCasino/ダイアログ/カジノコインが不足しています。",
                    null,
                    SystemDialog.TYPE.OK);
                return false;
            }

            var args = new[]
            {
                TranslateCasinoItemName(itemData),
                Utility.ConvertMoneyText(itemData.Price)
            };
            replacementStarted = true;
            GameMain.Instance.SysDlg.ShowFromLanguageTerm(
                "SceneCasino/ダイアログ/{0}を購入しますか?",
                args,
                SystemDialog.TYPE.OK_CANCEL,
                () => KasaSceneMgr<SceneCasinoShop>.Instance.ItemBuy(itemData));
            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"CasinoItemUI_ItemBuy_Prefix unknown error/未知错误 " +
                $"(replacementStarted={replacementStarted}) {e.Message}\n{e.StackTrace}");
            return !replacementStarted;
        }
    }

    /// <summary>
    ///     カジノショップ购买完成对话框的商品名翻译。
    ///     SceneCasinoShop.ItemBuy 是 public，但内部调用 private UpdateUIState。
    ///     为避免原方法显示日文参数的完成对话框，这里替换整个方法，并通过反射调用 UpdateUIState。
    ///     如果 UpdateUIState 名称变化，则回退原方法。
    /// </summary>
    [HarmonyPatch(typeof(SceneCasinoShop), "ItemBuy")]
    [HarmonyPrefix]
    private static bool SceneCasinoShop_ItemBuy_Prefix(
        SceneCasinoShop __instance,
        CasinoShopItem item_data)
    {
        var purchaseStarted = false;
        try
        {
            if (Product.supportMultiLanguage)
                return true;

            if (__instance == null || item_data == null ||
                SceneCasinoShopUpdateUIStateMethod == null)
                return true;

            // Resolve the display-only value before changing coins or inventory. From the first
            // mutation onward the original method must never be replayed as an exception fallback.
            var translatedName = TranslateCasinoItemName(item_data);
            var status = GameMain.Instance.CharacterMgr.status;
            purchaseStarted = true;
            status.casinoCoin -= item_data.Price;
            item_data.ItemBuy();
            SceneCasinoShopUpdateUIStateMethod.Invoke(__instance, null);

            GameMain.Instance.SysDlg.ShowFromLanguageTerm(
                "SceneCasino/ダイアログ/{0}を購入しました",
                new[] { translatedName },
                SystemDialog.TYPE.OK);
            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"SceneCasinoShop_ItemBuy_Prefix unknown error/未知错误 " +
                $"(purchaseStarted={purchaseStarted}) {e.Message}\n{e.StackTrace}");
            return !purchaseStarted;
        }
    }

    private static string TranslateCasinoItemName(CasinoShopItem itemData)
    {
        if (itemData == null)
            return string.Empty;

        return TranslateTermOrFallback(itemData.NameTerm, itemData.Name);
    }

    #endregion

    #region DanceSingPartSetting

    private static readonly FieldInfo DanceSingPartSettingPopupDicField =
        AccessTools.Field(typeof(DanceSingPartSettingUI), "PopupUITupleDic");

    /// <summary>
    ///     DanceSingPartSettingUI.SetUpUI 在 JP 版用 vocalJPName 填 popup.items 并把
    ///     label.text 设为日文，跳过 itemTerms 和 Localize 路径。
    ///     此 Postfix 保留 vocalJPName 业务值及其原始索引，只建立同长度的 itemTerms，
    ///     并替换 onChange 委托，让显示标签按索引使用对应的 vocalNameTerm。
    ///     依赖：私有字段 PopupUITupleDic（Dictionary&lt;UIPopupList, Tuple&lt;UILabel, Localize&gt;&gt;）。
    /// </summary>
    [HarmonyPatch(typeof(DanceSingPartSettingUI), "SetUpUI")]
    [HarmonyPostfix]
    private static void DanceSingPartSettingUI_SetUpUI_Postfix(
        DanceSingPartSettingUI __instance,
        DanceData dance_data)
    {
        try
        {
            if (__instance == null || dance_data == null) return;
            if (!Product.isJapan) return; // 非 JP 已走 term
            if (dance_data.singPartList == null || dance_data.singPartList.Count == 0) return;
            if (DanceSingPartSettingPopupDicField == null) return;

            var dic = DanceSingPartSettingPopupDicField.GetValue(__instance)
                as Dictionary<UIPopupList, Tuple<UILabel, Localize>>;
            if (dic == null || dic.Count == 0) return;

            var settingData = DanceSetting.Settings.GetSingPartSettingData(dance_data.ID);
            if (settingData == null) return;

            var i = 0;
            foreach (var kvp in dic)
            {
                var popup = kvp.Key;
                var tuple = kvp.Value;
                if (popup == null || tuple == null)
                {
                    i++;
                    continue;
                }

                var label = tuple.Item1;
                var loc = tuple.Item2;
                if (label == null || loc == null ||
                    popup.items.Count != dance_data.singPartList.Count)
                {
                    i++;
                    continue;
                }

                if (popup.itemTerms == null)
                    popup.itemTerms = new List<string>();
                else
                    popup.itemTerms.Clear();

                var hasTerm = false;
                foreach (var part in dance_data.singPartList)
                {
                    var term = part?.vocalNameTerm;
                    if (StringTool.IsNullOrWhiteSpace(term))
                    {
                        popup.itemTerms.Add(string.Empty);
                        continue;
                    }

                    popup.itemTerms.Add(term);
                    hasTerm = true;
                }

                popup.isLocalized = hasTerm;

                var idx = i < settingData.partDataIndexList.Count
                    ? settingData.partDataIndexList[i]
                    : 0;
                if (idx < 0 || idx >= dance_data.singPartList.Count) idx = 0;
                ApplyDanceSingPartLabel(label, loc, dance_data.singPartList[idx]);

                var capturedPopup = popup;
                var capturedLabel = label;
                var capturedLoc = loc;
                var capturedIdx = i;
                var capturedSetting = settingData;
                var capturedParts = dance_data.singPartList;
                EventDelegate.Set(popup.onChange, delegate
                {
                    try
                    {
                        var num = capturedPopup.items.IndexOf(capturedPopup.value);
                        if (num < 0 || num >= capturedParts.Count)
                            num = 0;

                        while (capturedSetting.partDataIndexList.Count <= capturedIdx)
                            capturedSetting.partDataIndexList.Add(0);
                        capturedSetting.partDataIndexList[capturedIdx] = num;
                        ApplyDanceSingPartLabel(
                            capturedLabel,
                            capturedLoc,
                            capturedParts[num]);
                    }
                    catch (Exception ex)
                    {
                        LogManager.Error(
                            $"DanceSingPartSettingUI onChange unknown error: {ex.Message}");
                    }
                });

                i++;
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"DanceSingPartSettingUI_SetUpUI_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    private static void ApplyDanceSingPartLabel(
        UILabel label,
        Localize localize,
        DanceSingPartData part)
    {
        if (label == null || localize == null)
            return;

        var fallback = part?.vocalJPName ?? string.Empty;
        var term = part?.vocalNameTerm;
        label.text = fallback;

        if (StringTool.IsNullOrWhiteSpace(term))
        {
            localize.enabled = false;
            localize.TermArgs = null;
            localize.mTerm = string.Empty;
            localize.mTermSecondary = string.Empty;
            localize.FinalTerm = string.Empty;
            localize.FinalSecondaryTerm = string.Empty;
            return;
        }

        localize.TermArgs = null;
        localize.SetTerm(term);
        localize.enabled = true;
        if (StringTool.IsNullOrWhiteSpace(label.text))
            label.text = fallback;
    }

    #endregion

    #region CreativeRoomSize

    private static readonly FieldInfo CreativeRoomXTextField =
        AccessTools.Field(typeof(CreativeRoom), "m_UITextRoomSizeX");

    private static readonly FieldInfo CreativeRoomYTextField =
        AccessTools.Field(typeof(CreativeRoom), "m_UITextRoomSizeY");

    private static readonly FieldInfo CreativeRoomZTextField =
        AccessTools.Field(typeof(CreativeRoom), "m_UITextRoomSizeZ");

    /// <summary>
    ///     CreativeRoom.UpdateRoomSizeText 在 JP 版直接拼接日文 "横幅：" / "奥行：" / "高さ："
    ///     到 m_UITextRoomSizeX/Y/Z.text，跳过 Localize + TermSuffix 路径。
    ///     此 Prefix 在 JP 版下为每个标签先写完整日文回退；翻译存在时再设置 I2 term
    ///     和 TermSuffix。尺寸值直接读取游戏公开的 mapSizeX/Y/Z、unitSize 属性。
    ///     依赖：私有字段 m_UITextRoomSizeX/Y/Z (UnityEngine.UI.Text)。
    /// </summary>
    [HarmonyPatch(typeof(CreativeRoom), "UpdateRoomSizeText")]
    [HarmonyPrefix]
    private static bool CreativeRoom_UpdateRoomSizeText_Prefix(CreativeRoom __instance)
    {
        try
        {
            if (__instance == null) return true;
            if (Product.supportMultiLanguage) return true; // 原方法已走 I2
            if (CreativeRoomXTextField == null || CreativeRoomYTextField == null ||
                CreativeRoomZTextField == null)
                return true;

            var tx = CreativeRoomXTextField.GetValue(__instance) as UnityText;
            var ty = CreativeRoomYTextField.GetValue(__instance) as UnityText;
            var tz = CreativeRoomZTextField.GetValue(__instance) as UnityText;
            if (tx == null || ty == null || tz == null) return true;

            var mx = __instance.mapSizeX;
            var my = __instance.mapSizeY;
            var mz = __instance.mapSizeZ;
            var us = __instance.unitSize;

            // Resolve every fallible dictionary lookup before changing any label or Localize
            // component. A lookup failure can then safely fall through to the untouched method.
            var translationX = LocalizationManager.GetTranslation("SceneCreativeRoom/横幅");
            var translationZ = LocalizationManager.GetTranslation("SceneCreativeRoom/奥行");
            var translationY = LocalizationManager.GetTranslation("SceneCreativeRoom/高さ");

            ApplyCreativeRoomSize(tx, mx, us, "SceneCreativeRoom/横幅", "横幅",
                translationX);
            ApplyCreativeRoomSize(tz, mz, us, "SceneCreativeRoom/奥行", "奥行",
                translationZ);
            ApplyCreativeRoomSize(ty, my, us, "SceneCreativeRoom/高さ", "高さ",
                translationY);

            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"CreativeRoom_UpdateRoomSizeText_Prefix unknown error, fallback to original/未知错误，回退原方法 {e.Message}\n{e.StackTrace}");
            return true;
        }
    }

    private static void ApplyCreativeRoomSize(
        UnityText textComponent,
        int mapSize,
        float unitSize,
        string term,
        string fallbackPrefix,
        string translation)
    {
        if (textComponent == null) return;

        var value = string.Format("{0:#0.##}", mapSize * unitSize);
        var suffix = ":" + value + "m";
        var fallback = fallbackPrefix + "：" + value + "m";
        Localize localize = null;
        try
        {
            textComponent.text = fallback;
            localize = textComponent.GetComponent<Localize>();
            if (StringTool.IsNullOrWhiteSpace(translation))
            {
                ClearCreativeRoomLocalize(localize);
                return;
            }

            textComponent.text = translation + suffix;
            if (localize == null)
                return;

            localize.TermArgs = null;
            localize.TermSuffix = suffix;
            localize.SetTerm(term);
            localize.enabled = true;
            if (StringTool.IsNullOrWhiteSpace(textComponent.text))
                textComponent.text = fallback;
        }
        catch (Exception e)
        {
            // Keep this label self-consistent and continue with the other dimensions. Returning
            // to the original method after another label succeeded would leave enabled stale terms.
            textComponent.text = fallback;
            ClearCreativeRoomLocalize(localize);
            LogManager.Error(
                $"ApplyCreativeRoomSize failed for {term}, preserving original text/保留原文 {e.Message}\n{e.StackTrace}");
        }
    }

    private static void ClearCreativeRoomLocalize(Localize localize)
    {
        if (localize == null)
            return;

        localize.enabled = false;
        localize.TermArgs = null;
        localize.TermSuffix = string.Empty;
        localize.mTerm = string.Empty;
        localize.mTermSecondary = string.Empty;
        localize.FinalTerm = string.Empty;
        localize.FinalSecondaryTerm = string.Empty;
    }

    #endregion

    #region DanceSelectCharaSelectLabel

    private static readonly FieldInfo DanceSelectCharaSelectLabelField =
        AccessTools.Field(typeof(DanceSelect), "m_CharaSelectLabel");

    private static readonly FieldInfo DanceSelectSelectedDanceField =
        AccessTools.Field(typeof(DanceSelect), "m_SelectedDance");

    /// <summary>
    ///     DanceSelect.CallCharaSelect 在 JP 版 + Challenge/VS 舞蹈时直接拼接
    ///     "ダンスを行うメイドを{n}人選択してください。" 到 m_CharaSelectLabel.text。
    ///     由于 DanceSelect.Awake 在 JP 版主动销毁了该 label 的 Localize 组件（破坏性，记录但不修补），
    ///     这里无法重新挂回 Localize；只能用 LocalizationManager.GetTranslation 试探查询
    ///     推测的 term（带 {0} 占位符或带 num 后缀），命中则覆盖 text。
    ///     未命中时保持原文，由 NGUIText.WrapText Prefix 走原文-译文字典兜底。
    /// </summary>
    [HarmonyPatch(typeof(DanceSelect), "CallCharaSelect")]
    [HarmonyPostfix]
    private static void DanceSelect_CallCharaSelect_Postfix(DanceSelect __instance)
    {
        try
        {
            if (__instance == null) return;
            if (Product.supportMultiLanguage) return;
            if (RhythmAction_Mgr.NowDance != RhythmAction_Mgr.DanceType.Challenge
                && !RhythmAction_Mgr.IsVSDance)
                return;
            if (DanceSelectCharaSelectLabelField == null
                || DanceSelectSelectedDanceField == null)
                return;

            var label = DanceSelectCharaSelectLabelField.GetValue(__instance) as UILabel;
            if (label == null) return;

            var selectedDance =
                DanceSelectSelectedDanceField.GetValue(null) as List<DanceData>;
            if (selectedDance == null || selectedDance.Count == 0) return;
            var num = selectedDance[0].select_chara_num;

            var fmt = LocalizationManager.GetTranslation(
                "SceneDanceSelect/ダンスを行うメイドを{0}人選択してください。");
            if (!StringTool.IsNullOrWhiteSpace(fmt))
                try
                {
                    label.text = string.Format(fmt, num);
                    return;
                }
                catch (FormatException)
                {
                    // 落到下面的备用 term
                }

            var exact = LocalizationManager.GetTranslation(
                "SceneDanceSelect/ダンスを行うメイドを" + num + "人選択してください。");
            if (!StringTool.IsNullOrWhiteSpace(exact))
                label.text = exact;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"DanceSelect_CallCharaSelect_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    #endregion

    #region TranslationHelpers

    private static string TranslateTermOrLastWord(string term)
    {
        return TranslateTermOrFallback(term, Utility.GetTermLastWord(term));
    }

    private static string TranslateTermOrFallback(string term, string fallback)
    {
        if (string.IsNullOrEmpty(term))
            return fallback ?? string.Empty;

        var translation = LocalizationManager.GetTranslation(term);
        if (StringTool.IsNullOrWhiteSpace(translation))
            return fallback ?? string.Empty;

        var translatedText = translation.Replace("《改行》", "\n");
        return StringTool.IsNullOrWhiteSpace(translatedText)
            ? fallback ?? string.Empty
            : translatedText;
    }

    #endregion


    #region ScheduleTaskHoverIcon

    private static readonly FieldInfo OnHoverTaskIconLabelNameField =
        AccessTools.Field(typeof(OnHoverTaskIcon), "labelName");

    private static readonly FieldInfo OnHoverTaskIconSpritePlateField =
        AccessTools.Field(typeof(OnHoverTaskIcon), "spritePlate");

    /// <summary>
    ///     スケジュール画面任务图标的 hover 提示翻译。
    ///     原方法 OnHoverTaskIcon.SetText 在 !Product.supportMultiLanguage 分支直接
    ///     this.labelName.text = message，完全绕开 I2。此 Postfix 复刻多语言分支的
    ///     term 推导逻辑，让 JP 版也能命中 I2 字典：
    ///     1) SceneDaily/スケジュール/項目/{message}（× 替换为 _）
    ///     2) 回退到 SceneFacilityManagement/施設名/{message}
    ///     3) 人数提示 SceneDaily/あと{0}人必要です
    ///     如果主 term 没有命中，则不动 labelName.text；主 term 命中时也只替换首行，
    ///     其余原始行会保留，人数 term 有效时才替换原人数行。
    /// </summary>
    [HarmonyPatch(typeof(OnHoverTaskIcon), "SetText")]
    [HarmonyPostfix]
    private static void OnHoverTaskIcon_SetText_Postfix(
        OnHoverTaskIcon __instance,
        string message,
        int needNum)
    {
        try
        {
            if (__instance == null || string.IsNullOrEmpty(message))
                return;
            if (Product.supportMultiLanguage)
                return;
            if (OnHoverTaskIconLabelNameField == null)
                return;

            var label = OnHoverTaskIconLabelNameField.GetValue(__instance) as UILabel;
            if (label == null)
                return;

            var newlineIndex = message.IndexOf('\n');
            var head = newlineIndex < 0 ? message : message.Substring(0, newlineIndex);
            var hasTail = newlineIndex >= 0;
            var tail = hasTail ? message.Substring(newlineIndex + 1) : string.Empty;

            var translated = LocalizationManager.GetTranslation(
                "SceneDaily/スケジュール/項目/" + head.Replace("×", "_"));
            if (StringTool.IsNullOrWhiteSpace(translated))
                translated = LocalizationManager.GetTranslation(
                    "SceneFacilityManagement/施設名/" + head);
            if (StringTool.IsNullOrWhiteSpace(translated))
                return;

            if (needNum != -1)
            {
                var fmt = LocalizationManager.GetTranslation(
                    "SceneDaily/あと{0}人必要です");
                if (!StringTool.IsNullOrWhiteSpace(fmt))
                    try
                    {
                        var remainingLinesIndex = tail.IndexOf('\n');
                        var remainingLines = remainingLinesIndex >= 0
                            ? tail.Substring(remainingLinesIndex)
                            : string.Empty;
                        tail = string.Format(fmt, needNum) + remainingLines;
                        hasTail = true;
                    }
                    catch (FormatException)
                    {
                        // 保留 message 中完整的原始人数提示及其它后续行。
                    }
            }

            var final = hasTail ? translated + "\n" + tail : translated;
            label.text = final;
            label.MakePixelPerfect();

            var plate = OnHoverTaskIconSpritePlateField?.GetValue(__instance) as UISprite;
            if (plate != null)
            {
                var lineCount = 1;
                for (var i = 0; i < final.Length; i++)
                    if (final[i] == '\n')
                        lineCount++;

                plate.width = label.width + 15;
                plate.height = lineCount * 33;
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"OnHoverTaskIcon_SetText_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    #endregion


    #region SimpleMaidPlate

    private static readonly FieldInfo SimpleMaidPlateFirstNameField =
        AccessTools.Field(typeof(SimpleMaidPlate), "first_name_label_");

    /// <summary>
    ///     SimpleMaidPlate 男性 maid 的名字标签翻译。
    ///     原方法在 JP 版直接硬编码 "主人公" / "男{n}"，跳过 I2。
    ///     多语言分支的 term：
    ///     - ScenePhotoMode/プレイヤー名/主人公
    ///     - ScenePhotoMode/プレイヤー名/男 + ActiveSlotNo
    /// </summary>
    [HarmonyPatch(typeof(SimpleMaidPlate), "SetMaidData")]
    [HarmonyPostfix]
    private static void SimpleMaidPlate_SetMaidData_Postfix(
        SimpleMaidPlate __instance,
        Maid maid)
    {
        try
        {
            if (__instance == null || maid == null)
                return;
            if (Product.supportMultiLanguage)
                return;
            if (!maid.boMAN)
                return;
            if (SimpleMaidPlateFirstNameField == null)
                return;

            var label = SimpleMaidPlateFirstNameField.GetValue(__instance) as UILabel;
            if (label == null)
                return;

            string translation;
            if (maid.ActiveSlotNo == 0)
            {
                translation = LocalizationManager.GetTranslation(
                    "ScenePhotoMode/プレイヤー名/主人公");
            }
            else
            {
                var prefix = LocalizationManager.GetTranslation(
                    "ScenePhotoMode/プレイヤー名/男");
                translation = StringTool.IsNullOrWhiteSpace(prefix)
                    ? null
                    : prefix + maid.ActiveSlotNo;
            }

            if (!StringTool.IsNullOrWhiteSpace(translation))
                label.text = translation;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"SimpleMaidPlate_SetMaidData_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    #endregion


    #region ProfileCtrl

    private static readonly FieldInfo ProfileCtrlPPersonalField =
        AccessTools.Field(typeof(ProfileCtrl), "m_pPersonal");

    private static readonly FieldInfo ProfileCtrlLPersonalField =
        AccessTools.Field(typeof(ProfileCtrl), "m_lPersonal");

    private static readonly FieldInfo ProfileCtrlMaidStatusField =
        AccessTools.Field(typeof(ProfileCtrl), "m_maidStatus");

    private static readonly FieldInfo ProfileCtrlLRelationField =
        AccessTools.Field(typeof(ProfileCtrl), "m_lRelation");

    private static readonly FieldInfo ProfileCtrlLConditionField =
        AccessTools.Field(typeof(ProfileCtrl), "m_lConditionText");

    private static readonly FieldInfo ProfileCtrlDicPersonalField =
        AccessTools.Field(typeof(ProfileCtrl), "m_dicPersonal");

    /// <summary>
    ///     ProfileCtrl.Init 在 JP 版用 drawName 填 m_pPersonal.items，并把 m_dicPersonal
    ///     的键设为 drawName，UIPopupList 不走 I2。此 Postfix 保留这些业务值，只按 items
    ///     的既有顺序把 Personal.Data.termName 写入 itemTerms，让下拉框仅翻译显示文本。
    ///     SetPersonal(selectValue) 继续接收原始 drawName，因此不会因空/重复 term 丢失性格。
    /// </summary>
    [HarmonyPatch(typeof(ProfileCtrl), "Init")]
    [HarmonyPostfix]
    private static void ProfileCtrl_Init_Postfix(ProfileCtrl __instance)
    {
        try
        {
            if (__instance == null) return;
            if (Product.supportMultiLanguage) return;
            if (ProfileCtrlPPersonalField == null || ProfileCtrlDicPersonalField == null)
                return;

            var popup = ProfileCtrlPPersonalField.GetValue(__instance) as UIPopupList;
            if (popup == null) return;

            var oldDic =
                ProfileCtrlDicPersonalField.GetValue(null) as Dictionary<string, Personal.Data>;
            if (oldDic == null || oldDic.Count == 0) return;

            var terms = new List<string>(popup.items.Count);
            var hasTerm = false;
            foreach (var item in popup.items)
            {
                Personal.Data data;
                var term = item != null && oldDic.TryGetValue(item, out data) && data != null
                    ? data.termName
                    : null;
                if (StringTool.IsNullOrWhiteSpace(term))
                {
                    terms.Add(string.Empty);
                    continue;
                }

                terms.Add(term);
                hasTerm = true;
            }

            if (popup.itemTerms == null)
                popup.itemTerms = new List<string>();
            else
                popup.itemTerms.Clear();
            popup.itemTerms.AddRange(terms);
            popup.isLocalized = hasTerm;

            // LoadMaidParamData selected the current raw drawName before this postfix ran.
            // Update only its display label; assigning popup.value would replay every business
            // and third-party onChange callback even though the selected value did not change.
            var selectedIndex = popup.items.IndexOf(popup.value);
            if (hasTerm && selectedIndex >= 0 && selectedIndex < terms.Count)
            {
                var label = ProfileCtrlLPersonalField?.GetValue(__instance) as UILabel;
                if (label != null)
                {
                    label.text = popup.items[selectedIndex] ?? string.Empty;
                    var selectedTerm = terms[selectedIndex];
                    if (!StringTool.IsNullOrWhiteSpace(selectedTerm))
                        EnableLocalizeIfTermExists(label, selectedTerm);
                }
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"ProfileCtrl_Init_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     ProfileCtrl.LoadMaidParamData 在 JP 版禁用 m_lRelation / m_lConditionText 的
    ///     Localize 组件。此 Postfix 强制 enable Localize 并 SetTerm，让两个标签走 I2。
    ///     - m_lConditionText 用 status.conditionTermText
    ///     - m_lRelation 用 EnumConvert.GetTerm(contract, relation, addRelation, specialRelation)
    ///     只在 I2 实际有该 term 翻译时才 SetTerm，避免 Localize.OnLocalize 用 GetTermLastWord 回退。
    /// </summary>
    [HarmonyPatch(typeof(ProfileCtrl), "LoadMaidParamData")]
    [HarmonyPostfix]
    private static void ProfileCtrl_LoadMaidParamData_Postfix(ProfileCtrl __instance)
    {
        try
        {
            if (__instance == null) return;
            if (Product.supportMultiLanguage) return;
            if (ProfileCtrlMaidStatusField == null) return;

            var status = ProfileCtrlMaidStatusField.GetValue(__instance) as Status;
            if (status == null) return;

            if (ProfileCtrlLConditionField != null)
            {
                var conditionLabel =
                    ProfileCtrlLConditionField.GetValue(__instance) as UILabel;
                if (conditionLabel != null &&
                    !string.IsNullOrEmpty(status.conditionTermText))
                    EnableLocalizeIfTermExists(conditionLabel, status.conditionTermText);
            }

            if (ProfileCtrlLRelationField != null)
            {
                var relationLabel =
                    ProfileCtrlLRelationField.GetValue(__instance) as UILabel;
                if (relationLabel != null)
                {
                    // JP ProfileCtrl deliberately suppresses its generic relation term when an
                    // imported OldStatus exists because it may have replaced the visible value
                    // with legacy-only labels such as "嫁" or "新妻". Derive that case from the
                    // final game-written label instead of overwriting it from the new enum state.
                    var relationTerm = status.OldStatus != null
                        ? (StringTool.IsNullOrWhiteSpace(relationLabel.text)
                            ? null
                            : "MaidStatus/関係タイプ/" + relationLabel.text)
                        : EnumConvert.GetTerm(
                            status.contract,
                            status.relation,
                            status.additionalRelation,
                            status.specialRelation);
                    if (!string.IsNullOrEmpty(relationTerm))
                        EnableLocalizeIfTermExists(relationLabel, relationTerm);
                }
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"ProfileCtrl_LoadMaidParamData_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    private static void EnableLocalizeIfTermExists(UILabel label, string term)
    {
        if (label == null || string.IsNullOrEmpty(term)) return;
        var translation = LocalizationManager.GetTranslation(term);
        if (StringTool.IsNullOrWhiteSpace(translation)) return;

        var fallback = label.text;
        var localize = label.GetComponent<Localize>();
        if (localize == null)
            localize = label.gameObject.AddComponent<Localize>();
        localize.SetTerm(term);
        localize.enabled = true;
        if (StringTool.IsNullOrWhiteSpace(label.text))
            label.text = fallback;
    }

    #endregion
}
