using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
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
            if (__instance == null || __instance.TabPanel == null)
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
            if (__instance == null || __instance.Grid == null)
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
                    localize.enabled = false;
                continue;
            }

            if (localize == null)
                localize = label.gameObject.AddComponent<Localize>();

            localize.enabled = true;
            localize.SetTerm(term);
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
    ///     原方法在 maid 有效且不是男性时才写入表情分类数据，但 JP 版仍会把 term 列表置空。
    ///     这里在同一时机写回 PhotoFaceData.popup_category_term_list；maid 为 null 或男性时清空，
    ///     避免 UIPopupList.items 与 itemTerms 数量不一致。
    /// </summary>
    [HarmonyPatch(typeof(FaceWindow), "OnMaidChangeEvent")]
    [HarmonyPostfix]
    private static void FaceWindow_OnMaidChangeEvent_Postfix(FaceWindow __instance, Maid maid)
    {
        try
        {
            if (__instance?.PopupAndTabList == null)
                return;

            __instance.PopupAndTabList.popup_term_list =
                maid != null && !maid.boMAN ? PhotoFaceData.popup_category_term_list : null;
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
            if (__instance?.PopupAndTabList == null)
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
        try
        {
            if (__instance == null || texts == null)
                return true;

            if (!TryEnsureConditionListAwake(__instance))
                return true;

            if (!(UIWFConditionListLabelsField?.GetValue(__instance) is List<UILabel> labels) ||
                labels.Count == 0)
                return true;

            var localizes =
                UIWFConditionListLocalizesField?.GetValue(__instance) as List<Localize>;

            UIWFConditionListLastLimitTextWidthField?.SetValue(__instance, limitTextWidth);

            var firstY = 0f;
            var lastBottomY = 0f;
            for (var i = 0; i < labels.Count; i++)
            {
                var label = labels[i];
                if (label == null || label.transform == null || label.transform.parent == null)
                    continue;

                if (localizes != null && i < localizes.Count && localizes[i] != null)
                {
                    localizes[i].LocalizeEvent.RemoveAllListeners();
                    localizes[i].enabled = false;
                }

                var row = label.transform.parent;
                if (i < texts.Length)
                {
                    if (i == 0)
                        firstY = Mathf.Abs(row.localPosition.y);

                    row.gameObject.SetActive(true);
                    label.text = BuildConditionText(texts[i].Key);
                    label.color = texts[i].Value;

                    var widget = row.gameObject.GetComponent<UIWidget>();
                    if (widget != null)
                        lastBottomY = Mathf.Abs(row.localPosition.y) + widget.height;
                }
                else
                {
                    row.gameObject.SetActive(false);
                }
            }

            SetConditionListHeight(__instance, (int)(lastBottomY - firstY));
            __instance.ResizeUI(limitTextWidth);
            UIWFConditionListUpdateFlagField?.SetValue(__instance, false);
            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"UIWFConditionList_SetTexts_Prefix unknown error, fallback to original/未知错误，回退原方法 {e.Message}\n{e.StackTrace}");
            return true;
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

        var backingField = AccessTools.Field(typeof(UIWFConditionList),
            "<height>k__BackingField");
        backingField?.SetValue(instance, height);
    }

    private static string BuildConditionText(string[] terms)
    {
        if (terms == null || terms.Length == 0 || string.IsNullOrEmpty(terms[0]))
            return string.Empty;

        if (terms.Length == 1)
            return TranslateTermOrLastWord(terms[0]);

        var format = TranslateTermOrLastWord(terms[0]);
        var args = new string[terms.Length - 1];
        for (var i = 1; i < terms.Length; i++)
            args[i - 1] = TranslateTermArgument(terms[i]);

        try
        {
            return string.Format(format, args);
        }
        catch (FormatException)
        {
            return format + " " + string.Join(" ", args);
        }
    }

    private static string TranslateTermArgument(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (!IsLikelyTerm(value))
            return value;

        return TranslateTermOrLastWord(value);
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
        try
        {
            if (__instance == null || ShopItemInfoWindowField == null ||
                ShopItemIconSpriteField == null)
                return true;

            var itemData = __instance.item_data;
            var infoWindow = ShopItemInfoWindowField.GetValue(__instance) as ItemInfoWnd;
            var iconSprite = ShopItemIconSpriteField.GetValue(__instance) as UI2DSprite;
            if (itemData == null || infoWindow == null || iconSprite?.sprite2D?.texture == null)
                return true;

            var position = __instance.transform.position;
            var title = TranslateShopItemName(itemData);
            var info = TranslateShopItemDescription(itemData);
            infoWindow.Open(position, iconSprite.sprite2D.texture, title, info);

            var transform = UTY.GetChildObject(infoWindow.gameObject, "Base").transform;
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
                $"ShopItem_OnHoverOver_Prefix unknown error, fallback to original/未知错误，回退原方法 {e.Message}\n{e.StackTrace}");
            return true;
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
        try
        {
            if (__instance == null || ShopItemClickEventField == null)
                return true;

            var itemData = __instance.item_data;
            if (itemData == null)
                return true;

            var remainingMoney =
                GameMain.Instance.CharacterMgr.status.money - itemData.price;
            if (remainingMoney < 0L)
            {
                GameMain.Instance.SysDlg.ShowFromLanguageTerm("Dialog/ショップ/資金が不足しています。",
                    null, SystemDialog.TYPE.OK);
                return false;
            }

            var args = new[]
            {
                TranslateShopItemName(itemData),
                Utility.ConvertMoneyText(itemData.price)
            };
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
                $"ShopItem_OnClick_Prefix unknown error, fallback to original/未知错误，回退原方法 {e.Message}\n{e.StackTrace}");
            return true;
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
        try
        {
            if (__instance == null || ShopItemClickEventField == null ||
                __instance.item_data == null)
                return true;

            RunShopItemClickEvent(__instance);
            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"ShopItem_CallOnClickEvent_Prefix unknown error, fallback to original/未知错误，回退原方法 {e.Message}\n{e.StackTrace}");
            return true;
        }
    }

    private static void RunShopItemClickEvent(ShopItem instance)
    {
        var clickEvent = ShopItemClickEventField.GetValue(instance) as ShopItem.OnClickEvent;
        clickEvent?.Invoke(instance);

        GameMain.Instance.SysDlg.ShowFromLanguageTerm(
            "Dialog/ショップ/{0}を購入しました",
            new[] { TranslateShopItemName(instance.item_data) },
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
        try
        {
            if (__instance == null || CasinoItemUIItemDataField == null)
                return true;

            var itemData = CasinoItemUIItemDataField.GetValue(__instance) as CasinoShopItem;
            if (itemData == null)
                return true;

            if (!itemData.IsCanBuy)
            {
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
                $"CasinoItemUI_ItemBuy_Prefix unknown error, fallback to original/未知错误，回退原方法 {e.Message}\n{e.StackTrace}");
            return true;
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
        try
        {
            if (__instance == null || item_data == null ||
                SceneCasinoShopUpdateUIStateMethod == null)
                return true;

            var status = GameMain.Instance.CharacterMgr.status;
            status.casinoCoin -= item_data.Price;
            item_data.ItemBuy();
            SceneCasinoShopUpdateUIStateMethod.Invoke(__instance, null);

            GameMain.Instance.SysDlg.ShowFromLanguageTerm(
                "SceneCasino/ダイアログ/{0}を購入しました",
                new[] { TranslateCasinoItemName(item_data) },
                SystemDialog.TYPE.OK);
            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"SceneCasinoShop_ItemBuy_Prefix unknown error, fallback to original/未知错误，回退原方法 {e.Message}\n{e.StackTrace}");
            return true;
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
    ///     此 Postfix 重建 popup：
    ///     1) 用 vocalNameTerm 填 items 和 itemTerms；
    ///     2) 强制 isLocalized = true 并 SetTerm；
    ///     3) 替换 onChange 委托，让用户切换时也走 Localize.SetTerm。
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

                var loc = tuple.Item2;
                if (loc == null)
                {
                    i++;
                    continue;
                }

                popup.items.Clear();
                popup.itemTerms.Clear();
                foreach (var part in dance_data.singPartList)
                {
                    if (part == null || string.IsNullOrEmpty(part.vocalNameTerm)) continue;
                    popup.items.Add(part.vocalNameTerm);
                    popup.itemTerms.Add(part.vocalNameTerm);
                }

                popup.isLocalized = true;

                var idx = i < settingData.partDataIndexList.Count
                    ? settingData.partDataIndexList[i]
                    : 0;
                if (idx < 0 || idx >= dance_data.singPartList.Count) idx = 0;
                var currentTerm = dance_data.singPartList[idx].vocalNameTerm;
                if (!string.IsNullOrEmpty(currentTerm))
                {
                    popup.value = currentTerm;
                    loc.SetTerm(currentTerm);
                }

                var capturedPopup = popup;
                var capturedLoc = loc;
                var capturedIdx = i;
                var capturedSetting = settingData;
                EventDelegate.Set(popup.onChange, delegate
                {
                    try
                    {
                        var num = Mathf.Max(
                            capturedPopup.items.IndexOf(capturedPopup.value), 0);
                        if (capturedSetting.partDataIndexList.Count > capturedIdx)
                            capturedSetting.partDataIndexList[capturedIdx] = num;
                        else
                            capturedSetting.partDataIndexList.Add(num);
                        capturedLoc.SetTerm(capturedPopup.value);
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

    #endregion

    #region CreativeRoomSize

    private static readonly FieldInfo CreativeRoomXTextField =
        AccessTools.Field(typeof(CreativeRoom), "m_UITextRoomSizeX");

    private static readonly FieldInfo CreativeRoomYTextField =
        AccessTools.Field(typeof(CreativeRoom), "m_UITextRoomSizeY");

    private static readonly FieldInfo CreativeRoomZTextField =
        AccessTools.Field(typeof(CreativeRoom), "m_UITextRoomSizeZ");

    private static readonly FieldInfo CreativeRoomMapXField =
        AccessTools.Field(typeof(CreativeRoom), "m_MapSizeX");

    private static readonly FieldInfo CreativeRoomMapYField =
        AccessTools.Field(typeof(CreativeRoom), "m_MapSizeY");

    private static readonly FieldInfo CreativeRoomMapZField =
        AccessTools.Field(typeof(CreativeRoom), "m_MapSizeZ");

    private static readonly FieldInfo CreativeRoomUnitSizeField =
        AccessTools.Field(typeof(CreativeRoom), "m_UnitSize");

    /// <summary>
    ///     CreativeRoom.UpdateRoomSizeText 在 JP 版直接拼接日文 "横幅：" / "奥行：" / "高さ："
    ///     到 m_UITextRoomSizeX/Y/Z.text，跳过 Localize + TermSuffix 路径。
    ///     此 Prefix 在 JP 版下完全替换为多语言分支逻辑：用 Utility.SetLocalizeTerm 走 I2 term，
    ///     并设置 TermSuffix 为 ":{数值}m"。
    ///     依赖：私有字段 m_UITextRoomSizeX/Y/Z (UnityEngine.UI.Text)、mapSizeX/Y/Z、unitSize。
    /// </summary>
    [HarmonyPatch(typeof(CreativeRoom), "UpdateRoomSizeText")]
    [HarmonyPrefix]
    private static bool CreativeRoom_UpdateRoomSizeText_Prefix(CreativeRoom __instance)
    {
        try
        {
            if (__instance == null) return true;
            if (Product.supportMultiLanguage) return true; // 原方法已走 I2

            var tx = CreativeRoomXTextField?.GetValue(__instance) as UnityText;
            var ty = CreativeRoomYTextField?.GetValue(__instance) as UnityText;
            var tz = CreativeRoomZTextField?.GetValue(__instance) as UnityText;
            if (tx == null && ty == null && tz == null) return true;

            var mx = CreativeRoomMapXField?.GetValue(__instance) as int? ?? 0;
            var my = CreativeRoomMapYField?.GetValue(__instance) as int? ?? 0;
            var mz = CreativeRoomMapZField?.GetValue(__instance) as int? ?? 0;
            var us = CreativeRoomUnitSizeField?.GetValue(__instance) as float? ?? 0f;

            ApplyCreativeRoomSize(tx, mx, us, "SceneCreativeRoom/横幅");
            ApplyCreativeRoomSize(tz, mz, us, "SceneCreativeRoom/奥行");
            ApplyCreativeRoomSize(ty, my, us, "SceneCreativeRoom/高さ");

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
        UnityText textComponent, int mapSize, float unitSize, string term)
    {
        if (textComponent == null) return;

        var suffix = ":" + string.Format("{0:#0.##}", mapSize * unitSize) + "m";
        var localize = textComponent.GetComponent<Localize>();
        if (localize != null)
        {
            localize.enabled = true;
            localize.TermSuffix = suffix;
            Utility.SetLocalizeTerm(localize, term);
            return;
        }

        // 无 Localize 组件：直接查表，命中则覆盖，否则由 Graphic.SetVerticesDirty Prefix 兜底
        var translation = LocalizationManager.GetTranslation(term);
        if (!string.IsNullOrEmpty(translation))
            textComponent.text = translation + suffix;
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
            if (!string.IsNullOrEmpty(fmt))
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
            if (!string.IsNullOrEmpty(exact))
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

    private static bool IsLikelyTerm(string value)
    {
        return !string.IsNullOrEmpty(value) &&
               (value.IndexOf("/", StringComparison.Ordinal) >= 0 ||
                value.IndexOf("|", StringComparison.Ordinal) >= 0);
    }

    private static string TranslateTermOrLastWord(string term)
    {
        return TranslateTermOrFallback(term, Utility.GetTermLastWord(term));
    }

    private static string TranslateTermOrFallback(string term, string fallback)
    {
        if (string.IsNullOrEmpty(term))
            return fallback ?? string.Empty;

        var translation = LocalizationManager.GetTranslation(term);
        return !string.IsNullOrEmpty(translation)
            ? translation.Replace("《改行》", "\n")
            : fallback ?? string.Empty;
    }

    #endregion


    #region ScheduleTaskHoverIcon

    private static readonly FieldInfo OnHoverTaskIconLabelNameField =
        AccessTools.Field(typeof(OnHoverTaskIcon), "labelName");

    /// <summary>
    ///     スケジュール画面任务图标的 hover 提示翻译。
    ///     原方法 OnHoverTaskIcon.SetText 在 !Product.supportMultiLanguage 分支直接
    ///     this.labelName.text = message，完全绕开 I2。此 Postfix 复刻多语言分支的
    ///     term 推导逻辑，让 JP 版也能命中 I2 字典：
    ///     1) SceneDaily/スケジュール/項目/{message}（× 替换为 _）
    ///     2) 回退到 SceneFacilityManagement/施設名/{message}
    ///     3) 人数提示 SceneDaily/あと{0}人必要です
    ///     如果 I2 没有命中，则不动 labelName.text，由 NGUIText.WrapText 兜底翻译。
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

            var head = message;
            if (message.IndexOf('\n') != -1)
                head = message.Split('\n')[0];

            var translated = LocalizationManager.GetTranslation(
                "SceneDaily/スケジュール/項目/" + head.Replace("×", "_"));
            if (string.IsNullOrEmpty(translated))
                translated = LocalizationManager.GetTranslation(
                    "SceneFacilityManagement/施設名/" + head);
            if (string.IsNullOrEmpty(translated))
                return;

            var final = translated;
            if (needNum != -1)
            {
                var fmt = LocalizationManager.GetTranslation(
                    "SceneDaily/あと{0}人必要です");
                if (!string.IsNullOrEmpty(fmt))
                    try
                    {
                        final = translated + "\n" + string.Format(fmt, needNum);
                    }
                    catch (FormatException)
                    {
                        // 保留 translated
                    }
            }

            label.text = final;
            label.MakePixelPerfect();
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
                translation = string.IsNullOrEmpty(prefix)
                    ? null
                    : prefix + maid.ActiveSlotNo;
            }

            if (!string.IsNullOrEmpty(translation))
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

    private static readonly FieldInfo ProfileCtrlMaidStatusField =
        AccessTools.Field(typeof(ProfileCtrl), "m_maidStatus");

    private static readonly FieldInfo ProfileCtrlLRelationField =
        AccessTools.Field(typeof(ProfileCtrl), "m_lRelation");

    private static readonly FieldInfo ProfileCtrlLConditionField =
        AccessTools.Field(typeof(ProfileCtrl), "m_lConditionText");

    private static readonly FieldInfo ProfileCtrlDicPersonalField =
        AccessTools.Field(typeof(ProfileCtrl), "m_dicPersonal");

    /// <summary>
    ///     ProfileCtrl.Init 在 JP 版用 drawName 填 m_pPersonal.items 并把 m_dicPersonal
    ///     的键设为 drawName，UIPopupList 不走 I2。此 Postfix 把 items 和 m_dicPersonal
    ///     的键替换为 termName，并开启 isLocalized，让下拉框走 I2。
    ///     注意 SetPersonal(selectValue) 用 m_dicPersonal[selectValue] 取数据，所以两者必须同步替换。
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

            var newDic = new Dictionary<string, Personal.Data>();
            var newItems = new List<string>();
            foreach (var kvp in oldDic)
            {
                var data = kvp.Value;
                if (data == null || string.IsNullOrEmpty(data.termName)) continue;
                if (newDic.ContainsKey(data.termName)) continue;
                newDic[data.termName] = data;
                newItems.Add(data.termName);
            }

            if (newDic.Count == 0) return;

            ProfileCtrlDicPersonalField.SetValue(null, newDic);
            popup.items.Clear();
            popup.items.AddRange(newItems);
            popup.isLocalized = true;
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
                    var relationTerm = EnumConvert.GetTerm(
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
        if (string.IsNullOrEmpty(translation)) return;

        var localize = label.GetComponent<Localize>();
        if (localize == null)
            localize = label.gameObject.AddComponent<Localize>();
        localize.enabled = true;
        localize.SetTerm(term);
    }

    #endregion
}