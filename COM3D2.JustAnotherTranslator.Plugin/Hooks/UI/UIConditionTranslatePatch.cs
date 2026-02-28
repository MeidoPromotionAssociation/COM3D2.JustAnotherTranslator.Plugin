using System;
using System.Collections.Generic;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using I2.Loc;
using UnityEngine;
using wf;
using Yotogis;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.UI;

/// <summary>
///     条件列表及 Yotogi 技能详情相关的本地化补丁
///     覆盖 UIWFConditionList.SetTexts、YotogiClassListManager/YotogiSkillListManager/YotogiSkillUnit
///     中被 Product.supportMultiLanguage 门控的本地化代码路径
/// </summary>
public static class UIConditionTranslatePatch
{
    #region UIWFConditionList

    /// <summary>
    ///     UIWFConditionList.SetTexts 的 Postfix
    ///     原方法在 !Product.supportMultiLanguage 时使用 GetTermLastWord 提取日文名作为回退
    ///     跳过了 Localize.SetTerm 调用
    ///     此 Postfix 为每个有 Localize 组件的条件标签应用 I2 本地化
    /// </summary>
    [HarmonyPatch(typeof(UIWFConditionList), "SetTexts",
        typeof(KeyValuePair<string[], Color>[]), typeof(int))]
    [HarmonyPostfix]
    private static void UIWFConditionList_SetTexts_Postfix(
        UIWFConditionList __instance,
        KeyValuePair<string[], Color>[] texts)
    {
        try
        {
            var localizeList = Traverse.Create(__instance)
                .Field("condition_label_localize_list_")
                .GetValue<List<Localize>>();
            if (localizeList == null)
                return;

            Localize lastLocalize = null;

            for (var i = 0; i < texts.Length && i < localizeList.Count; i++)
            {
                if (localizeList[i] == null)
                    continue;

                if (texts[i].Key.Length == 1)
                {
                    // 单 term: 先清空再设置，确保 Localize 组件正确更新
                    localizeList[i].TermArgs = null;
                    localizeList[i].SetTerm(string.Empty);
                    localizeList[i].SetTerm(texts[i].Key[0]);
                }
                else
                {
                    // 多 term: 构建 ArgsPair 数组
                    // 包含特定关键词时 flag=true，参数会被作为 term key 翻译
                    var flag = texts[i].Key[0].Contains("性経験")
                               || texts[i].Key[0].Contains("契約タイプ")
                               || texts[i].Key[0].Contains("性癖")
                               || texts[i].Key[0].Contains("ヒロインタイプ");

                    var args = new Localize.ArgsPair[texts[i].Key.Length - 1];
                    for (var k = 1; k < texts[i].Key.Length; k++)
                        args[k - 1] = Localize.ArgsPair.Create(texts[i].Key[k], flag);

                    localizeList[i].TermArgs = args;
                    localizeList[i].SetTerm(texts[i].Key[0]);
                    lastLocalize = localizeList[i];
                }
            }

            // 注册 LocalizeEvent 监听器，确保本地化更新时触发 ResizeUI
            if (lastLocalize != null)
                lastLocalize.LocalizeEvent.AddListener(() =>
                {
                    Traverse.Create(__instance).Field("updateFlag").SetValue(true);
                });

            // SetTerm 可能改变文本宽度，标记需要重新调整大小
            Traverse.Create(__instance).Field("updateFlag").SetValue(true);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"UIWFConditionList_SetTexts_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    #endregion

    #region YotogiClassListManager

    /// <summary>
    ///     YotogiClassListManager.OnHoverOverItem Postfix
    ///     原方法设置 conditionTitleLabel.text = drawName + " 取得条件"
    ///     在 supportMultiLanguage 为 true 时才通过 Localize 设置 term + TermArgs
    ///     此 Postfix 始终应用 Localize，并处理 "etc..." 标签的 Localize 重置
    /// </summary>
    [HarmonyPatch(typeof(YotogiClassListManager), "OnHoverOverItem")]
    [HarmonyPostfix]
    private static void YotogiClassListManager_OnHoverOverItem_Postfix(
        YotogiClassListManager __instance,
        int instanceId)
    {
        try
        {
            var traverse = Traverse.Create(__instance);

            var instansToDatas = traverse.Field("instansToDatas")
                .GetValue<Dictionary<int, YotogiClassListManager.Data>>();
            if (instansToDatas == null || !instansToDatas.ContainsKey(instanceId))
                return;

            var data = instansToDatas[instanceId];
            if (data.classData == null)
                return;

            // 应用条件标题的 Localize
            var conditionTitleLabel = traverse.Field("conditionTitleLabel").GetValue<UILabel>();
            if (conditionTitleLabel != null)
            {
                var localize = conditionTitleLabel.GetComponent<Localize>();
                if (localize != null && !string.IsNullOrEmpty(data.classData.termName))
                {
                    localize.TermArgs = new[]
                    {
                        Localize.ArgsPair.Create(" {0}", false),
                        Localize.ArgsPair.Create("System/取得条件", true)
                    };
                    localize.SetTerm(data.classData.termName);
                }
            }

            // 处理 "etc..." 标签：原方法在 supportMultiLanguage 时才重置 Localize
            // JAT 的 SetLocalizeTerm hook 已使技能名的 Localize 生效，需确保 "etc..." 不被覆盖
            var specialSkillLabelList = traverse.Field("specialSkillLabelList")
                .GetValue<List<UILabel>>();
            if (specialSkillLabelList != null)
                foreach (var label in specialSkillLabelList)
                    if (label != null && label.text == "etc...")
                    {
                        var loc = label.GetComponent<Localize>();
                        if (loc != null)
                            loc.SetTerm(" ");
                    }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"YotogiClassListManager_OnHoverOverItem_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    #endregion

    #region YotogiSkillListManager

    /// <summary>
    ///     YotogiSkillListManager.OnHoverOverItem Postfix
    ///     模式与 YotogiClassListManager 相同
    ///     注意：Skill.Old.Data 无 termName，仅在 Skill.Data (Key) 非空时处理
    /// </summary>
    [HarmonyPatch(typeof(YotogiSkillListManager), "OnHoverOverItem")]
    [HarmonyPostfix]
    private static void YotogiSkillListManager_OnHoverOverItem_Postfix(
        YotogiSkillListManager __instance,
        int instanceId)
    {
        try
        {
            var traverse = Traverse.Create(__instance);

            var instansToSkillDatas = traverse.Field("instansToSkillDatas")
                .GetValue<Dictionary<int, KeyValuePair<Skill.Data, Skill.Old.Data>>>();
            if (instansToSkillDatas == null || !instansToSkillDatas.ContainsKey(instanceId))
                return;

            var kvp = instansToSkillDatas[instanceId];
            // Skill.Old.Data 没有 termName，只有新版 Skill.Data 有
            if (kvp.Key == null)
                return;

            var termName = kvp.Key.termName;
            if (string.IsNullOrEmpty(termName))
                return;

            var conditionTitleLabel = traverse.Field("conditionTitleLabel").GetValue<UILabel>();
            if (conditionTitleLabel == null)
                return;

            var localize = conditionTitleLabel.GetComponent<Localize>();
            if (localize == null)
                return;

            localize.TermArgs = new[]
            {
                Localize.ArgsPair.Create(" {0}", false),
                Localize.ArgsPair.Create("System/取得条件", true)
            };
            localize.SetTerm(termName);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"YotogiSkillListManager_OnHoverOverItem_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    #endregion

    #region YotogiSkillUnit

    /// <summary>
    ///     YotogiSkillUnit.SetSkillData Postfix
    ///     原方法在 !supportMultiLanguage 时跳过 HP 消耗标签的 SetTerm + TermArgs
    ///     此 Postfix 应用 "SceneYotogi/消費体力プラス数値" term 并隐藏数值子对象
    /// </summary>
    [HarmonyPatch(typeof(YotogiSkillUnit), "SetSkillData")]
    [HarmonyPostfix]
    private static void YotogiSkillUnit_SetSkillData_Postfix(
        YotogiSkillUnit __instance)
    {
        try
        {
            var traverse = Traverse.Create(__instance);
            var skillData = traverse.Field("skill_data_").GetValue<Skill.Data>();
            if (skillData == null)
                return;

            var costOfHpObj = traverse.Field("cost_of_hp_obj").GetValue<GameObject>();
            if (costOfHpObj == null)
                return;

            var parent = costOfHpObj.transform.parent;
            if (parent == null)
                return;

            var localize = parent.GetComponent<Localize>();
            if (localize == null)
                return;

            localize.TermArgs = new[]
            {
                Localize.ArgsPair.Create(skillData.exec_need_hp.ToString())
            };
            localize.SetTerm("SceneYotogi/消費体力プラス数値");
            costOfHpObj.SetActive(false);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"YotogiSkillUnit_SetSkillData_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     YotogiSkillUnit.OnHoverOverItem Postfix
    ///     原方法设置 conditionTitleLabel.text = name + " 習得条件"
    ///     在 supportMultiLanguage 时才通过 Localize 设置 term + TermArgs
    ///     此 Postfix 始终应用 Localize
    /// </summary>
    [HarmonyPatch(typeof(YotogiSkillUnit), "OnHoverOverItem")]
    [HarmonyPostfix]
    private static void YotogiSkillUnit_OnHoverOverItem_Postfix(
        YotogiSkillUnit __instance)
    {
        try
        {
            var traverse = Traverse.Create(__instance);
            var skillData = traverse.Field("skill_data_").GetValue<Skill.Data>();
            if (skillData == null || string.IsNullOrEmpty(skillData.termName))
                return;

            // OnHoverOverItem 只在 conditionDatas_ != null 时显示条件
            var conditionDatas = traverse.Field("conditionDatas_")
                .GetValue<KeyValuePair<string[], Color>[]>();
            if (conditionDatas == null)
                return;

            var conditionTitleLabel = traverse.Field("conditionTitleLabel").GetValue<UILabel>();
            if (conditionTitleLabel == null)
                return;

            var localize = conditionTitleLabel.GetComponent<Localize>();
            if (localize != null)
            {
                localize.TermArgs = new[]
                {
                    Localize.ArgsPair.Create(" {0}", false),
                    Localize.ArgsPair.Create("System/取得条件", true)
                };
                localize.SetTerm(skillData.termName);
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"YotogiSkillUnit_OnHoverOverItem_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    #endregion
}
