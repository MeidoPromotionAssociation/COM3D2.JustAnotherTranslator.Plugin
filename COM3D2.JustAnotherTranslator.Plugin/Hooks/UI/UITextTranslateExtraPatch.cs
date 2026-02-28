using System;
using System.Collections.Generic;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using I2.Loc;
using Schedule;
using UnityEngine;
using UnityEngine.UI;
using wf;
using Yotogis;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.UI;

/// <summary>
///     覆盖 Product.supportMultiLanguage 门控的本地化代码路径
///     仅包含安全的 Postfix 值替换钩子，不包含任何 Prefix 方法重写
/// </summary>
public static class UITextTranslateExtraPatch
{
    #region YotogiSkillCommands

    /// <summary>
    ///     强制 YotogiCommandFactory.GetGroupName 返回 I2 term 而非原始日文名称
    ///     原方法在 !Product.supportMultiLanguage 时返回 basic.group_name（日文）
    ///     修改为始终返回 basic.termGroupName（I2 term）
    /// </summary>
    [HarmonyPatch(typeof(YotogiCommandFactory), "GetGroupName")]
    [HarmonyPostfix]
    private static void YotogiCommandFactory_GetGroupName_Postfix(
        Skill.Data.Command.Data.Basic commandDataBasic,
        ref string __result)
    {
        try
        {
            if (commandDataBasic != null && !string.IsNullOrEmpty(commandDataBasic.termGroupName))
                __result = commandDataBasic.termGroupName;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"YotogiCommandFactory_GetGroupName_Postfix unknown error, please report this issue/未知错误，请报告此问题: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     强制 YotogiCommandFactory.GetCommandName 返回 I2 term 而非原始日文名称
    ///     原方法在 !Product.supportMultiLanguage 时返回 basic.name（日文）
    ///     修改为始终返回 basic.termName（I2 term）
    /// </summary>
    [HarmonyPatch(typeof(YotogiCommandFactory), "GetCommandName")]
    [HarmonyPostfix]
    private static void YotogiCommandFactory_GetCommandName_Postfix(
        Skill.Data.Command.Data.Basic commandDataBasic,
        ref string __result)
    {
        try
        {
            if (commandDataBasic != null && !string.IsNullOrEmpty(commandDataBasic.termName))
                __result = commandDataBasic.termName;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"YotogiCommandFactory_GetCommandName_Postfix unknown error, please report this issue/未知错误，请报告此问题: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     在 YotogiCommandFactory.CreateCommand 创建技能指令按钮后，强制对按钮文本进行本地化
    ///     原方法中 Product.supportMultiLanguage 为 false 时跳过 Localize.SetTerm 调用
    ///     此 Postfix 通过 GetTranslation 直接获取翻译
    ///     回退逻辑：无翻译时使用 GetTermLastWord 提取原始日文名
    /// </summary>
    [HarmonyPatch(typeof(YotogiCommandFactory), "CreateCommand")]
    [HarmonyPostfix]
    private static void YotogiCommandFactory_CreateCommand_Postfix(
        GameObject __result,
        string name)
    {
        try
        {
            if (__result == null || string.IsNullOrEmpty(name))
                return;

            var label = UTY.GetChildObject(__result, "Name")?.GetComponent<UILabel>();
            if (label == null)
                return;

            var translation = LocalizationManager.GetTranslation(name);
            if (!string.IsNullOrEmpty(translation))
                label.text = translation;
            else
                label.text = Utility.GetTermLastWord(name);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"YotogiCommandFactory_CreateCommand_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     YotogiCommandFactory.CreateTitle 的安全回退
    ///     原方法中 Utility.SetLocalizeTerm 失败时，
    ///     会将 component.text 设为 name（此时为 I2 term 路径）
    ///     此 Postfix 检测到此情况时，通过 GetTranslation 获取翻译或提取原始日文名作为回退
    /// </summary>
    [HarmonyPatch(typeof(YotogiCommandFactory), "CreateTitle")]
    [HarmonyPostfix]
    private static void YotogiCommandFactory_CreateTitle_Postfix(
        GameObject __result,
        string name)
    {
        try
        {
            if (__result == null || string.IsNullOrEmpty(name))
                return;

            var label = __result.GetComponent<UILabel>();
            if (label == null)
                return;

            if (label.text == name && name.Contains("/"))
            {
                var translation = LocalizationManager.GetTranslation(name);
                label.text = !string.IsNullOrEmpty(translation)
                    ? translation
                    : Utility.GetTermLastWord(name);
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"YotogiCommandFactory_CreateTitle_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    #endregion

    #region ShopScene

    /// <summary>
    ///     在 GameInShopMain.Awake 之后，对商店分类按钮应用本地化
    ///     原方法中创建分类按钮的 lambda 内有 Product.supportMultiLanguage 检查
    ///     导致 JP 版分类按钮名称始终显示日文
    ///     此 Postfix 遍历所有已创建的分类按钮，通过 GetTranslation 应用翻译
    /// </summary>
    [HarmonyPatch(typeof(GameInShopMain), "Awake")]
    [HarmonyPostfix]
    private static void GameInShopMain_Awake_Postfix(GameInShopMain __instance)
    {
        try
        {
            var categoryButtonDic = Traverse.Create(__instance)
                .Field("category_button_dic_")
                .GetValue<Dictionary<string, KeyValuePair<UIButton,
                    Dictionary<string, UIButton>>>>();

            if (categoryButtonDic == null)
                return;

            string[] prefixes =
            {
                "SceneShop/メインカテゴリー/",
                "SceneShop/サブカテゴリー/",
                "SceneEdit/カテゴリー/サブ/"
            };

            foreach (var kvp in categoryButtonDic)
            {
                LocalizeShopCategoryButton(kvp.Value.Key, kvp.Key, prefixes);
                foreach (var subKvp in kvp.Value.Value)
                    LocalizeShopCategoryButton(subKvp.Value, subKvp.Key, prefixes);
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"GameInShopMain_Awake_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    private static void LocalizeShopCategoryButton(UIButton button, string categoryName,
        string[] termPrefixes)
    {
        try
        {
            if (button == null || string.IsNullOrEmpty(categoryName))
                return;

            var root = button.transform.parent;
            if (root == null) return;

            var labelObj = UTY.GetChildObject(root.gameObject, "Label");
            if (labelObj == null) return;

            var label = labelObj.GetComponent<UILabel>();
            if (label == null) return;

            foreach (var prefix in termPrefixes)
            {
                var translation = LocalizationManager.GetTranslation(prefix + categoryName);
                if (!string.IsNullOrEmpty(translation))
                {
                    label.text = translation;
                    return;
                }
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"LocalizeShopCategoryButton error for '{categoryName}'/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    #endregion

    #region ScheduleIcons

    /// <summary>
    ///     OnHoverTaskIcon.SetText Postfix
    ///     原方法在 !Product.supportMultiLanguage 时直接使用传入的日文 message
    ///     此 Postfix 在原方法设置日文文本后，用 I2 翻译覆盖
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
            var traverse = Traverse.Create(__instance);
            var labelName = traverse.Field("labelName").GetValue<UILabel>();
            var goNamePlate = traverse.Field("m_goNamePlate").GetValue<GameObject>();

            if (labelName == null || goNamePlate == null)
                return;

            var component = goNamePlate.GetComponent<UISprite>();
            if (component == null)
                return;

            // 提取任务名（去除换行后的附加信息）
            var text = message;
            if (message.IndexOf('\n') != -1)
                text = message.Split('\n')[0];

            // 尝试通过 I2 获取翻译
            var text2 = LocalizationManager.GetTranslation(
                "SceneDaily/スケジュール/項目/" + text.Replace("×", "_"));
            if (string.IsNullOrEmpty(text2))
                text2 = LocalizationManager.GetTranslation(
                    "SceneFacilityManagement/施設名/" + text);
            if (string.IsNullOrEmpty(text2))
                text2 = text;

            // 处理 "还需要N人" 的提示
            var text3 = string.Empty;
            if (needNum != -1)
                text3 = LocalizationManager.GetTranslation("SceneDaily/あと{0}人必要です");

            var text4 = text2;
            if (!string.IsNullOrEmpty(text3))
                text4 = text4 + "\n" + string.Format(text3, needNum);

            labelName.text = text4;
            labelName.MakePixelPerfect();

            // 调整背景尺寸
            component.width = labelName.width + 15;
            component.height = text4.Split('\n').Length * 33;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"OnHoverTaskIcon_SetText_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    #endregion

    #region StatusPanel

    /// <summary>
    ///     在 StatusCtrl.SetData 之后，确保性格等字段的翻译已应用
    ///     原方法先设置日文文本，再调用 Utility.SetLocalizeTerm
    ///     但如果 UILabel 没有 Localize 组件，SetLocalizeTerm 会静默失败
    ///     此 Postfix 作为回退方案，直接通过 GetTranslation 获取翻译
    /// </summary>
    [HarmonyPatch(typeof(StatusCtrl), "SetData")]
    [HarmonyPostfix]
    private static void StatusCtrl_SetData_Postfix(
        StatusCtrl __instance,
        StatusCtrl.Status status)
    {
        try
        {
            var traverse = Traverse.Create(__instance);

            TryLocalizeStatusLabel(
                traverse.Field("m_lPersonal").GetValue<UILabel>(),
                status.personalTerm, status.personal);
            TryLocalizeStatusLabel(
                traverse.Field("m_lContractType").GetValue<UILabel>(),
                status.contractTypeTerm, status.contractType);
            TryLocalizeStatusLabel(
                traverse.Field("m_lSexualExperience").GetValue<UILabel>(),
                status.sexualExperienceTerm, status.sexualExperience);
            TryLocalizeStatusLabel(
                traverse.Field("m_lMaidClass").GetValue<UILabel>(),
                status.maidClassNameTerm, status.maidClassName);
            TryLocalizeStatusLabel(
                traverse.Field("m_lRelation").GetValue<UILabel>(),
                status.relationTerm, status.relation);
            TryLocalizeStatusLabel(
                traverse.Field("m_lConditionText").GetValue<UILabel>(),
                status.conditionTextTerm, status.conditionText);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"StatusCtrl_SetData_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    private static void TryLocalizeStatusLabel(UILabel label, string term, string fallback)
    {
        if (label == null || string.IsNullOrEmpty(term))
            return;

        if (label.text == fallback || string.IsNullOrEmpty(label.text))
        {
            var translation = LocalizationManager.GetTranslation(term);
            if (!string.IsNullOrEmpty(translation))
                label.text = translation;
        }
    }

    #endregion

    #region UIWFConditionList

    /// <summary>
    ///     UIWFConditionList.SetTexts 的 Postfix
    ///     原方法在 !Product.supportMultiLanguage 时跳过了 Localize.SetTerm 调用
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
                    localizeList[i].TermArgs = null;
                    localizeList[i].SetTerm(string.Empty);
                    localizeList[i].SetTerm(texts[i].Key[0]);
                }
                else
                {
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

            if (lastLocalize != null)
                lastLocalize.LocalizeEvent.AddListener(() =>
                {
                    Traverse.Create(__instance).Field("updateFlag").SetValue(true);
                });

            Traverse.Create(__instance).Field("updateFlag").SetValue(true);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"UIWFConditionList_SetTexts_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    #endregion

    #region YotogiConditions

    /// <summary>
    ///     YotogiClassListManager.OnHoverOverItem Postfix
    ///     原方法在 supportMultiLanguage 为 true 时才通过 Localize 设置 term + TermArgs
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

    /// <summary>
    ///     YotogiSkillListManager.OnHoverOverItem Postfix
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
    ///     原方法在 supportMultiLanguage 时才通过 Localize 设置 term + TermArgs
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

    #region Casino

    /// <summary>
    ///     UIStates.CheckResultText Postfix
    ///     原方法中 5 个 if (Product.supportMultiLanguage) 块分别对应不同赌局结果
    ///     此 Postfix 复现 BjPlayer 状态判断，设置正确的 Localize TermArgs 和 term
    /// </summary>
    [HarmonyPatch(typeof(UIStates), "CheckResultText")]
    [HarmonyPostfix]
    private static void UIStates_CheckResultText_Postfix(UIStates __instance)
    {
        try
        {
            var traverse = Traverse.Create(__instance);

            var resultUI = traverse.Field("m_ResultUI").GetValue<GameObject>();
            if (resultUI == null || !resultUI.activeSelf)
                return;

            var getMoneyText = traverse.Field("m_GetMoneyText").GetValue<Text>();
            if (getMoneyText == null)
                return;

            var localize = getMoneyText.GetComponent<Localize>();
            if (localize == null)
                return;

            string term = null;
            string value = null;

            if (BjPlayer.Instance.TotalWinnings > 0L)
            {
                if (BjPlayer.Instance.ExistSurrenderHand())
                {
                    term = "SceneCasino/コインを{0}枚支払いました";
                    value = BjPlayer.Instance.TotalWinnings.ToString();
                }
                else if (BjPlayer.Instance.ExistWinHand())
                {
                    term = "SceneCasino/コインを{0}枚手に入れました!!";
                    value = BjPlayer.Instance.TotalWinnings.ToString();
                }
                else if (BjPlayer.Instance.IsEven())
                {
                    term = "SceneCasino/コイン{0}枚が手元に戻ってきます";
                    value = BjPlayer.Instance.TotalWinnings.ToString();
                }
                else
                {
                    term = "SceneCasino/コインを{0}枚失いました…";
                    value = BjPlayer.Instance.MoneyDifference.ToString();
                }
            }
            else
            {
                term = "SceneCasino/コインを{0}枚失いました…";
                value = (BjPlayer.Instance.CurrentBet + BjPlayer.Instance.SplitBet).ToString();
            }

            if (term != null && value != null)
            {
                localize.TermArgs = new[] { Localize.ArgsPair.Create(value) };
                Utility.SetLocalizeTerm(localize, term, false);
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"UIStates_CheckResultText_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     ExChangeUI.Awake Postfix
    ///     原方法在 !supportMultiLanguage 时不创建 Localize 组件，汇率文本始终为日文
    ///     此 Postfix 添加 Localize 组件并设置 TermArgs 和 term
    /// </summary>
    [HarmonyPatch(typeof(ExChangeUI), "Awake")]
    [HarmonyPostfix]
    private static void ExChangeUI_Awake_Postfix(ExChangeUI __instance)
    {
        try
        {
            var traverse = Traverse.Create(__instance);
            var rateText = traverse.Field("m_RateText").GetValue<Text>();
            if (rateText == null)
                return;

            var coinRate = traverse.Field("m_CoinRate").GetValue<int>();

            var localize = rateText.gameObject.GetComponent<Localize>();
            if (localize == null)
                localize = rateText.gameObject.AddComponent<Localize>();

            localize.TermArgs = new[]
            {
                Localize.ArgsPair.Create(Utility.ConvertMoneyText(coinRate))
            };
            Utility.SetLocalizeTerm(localize, "SceneCasino/コイン1枚 = {0}CR", false);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"ExChangeUI_Awake_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    #endregion

    #region Misc

    /// <summary>
    ///     Facility.Init Postfix
    ///     原方法在 supportMultiLanguage 时通过 GetTranslation(termName) 翻译设施名称
    ///     此 Postfix 重新获取 facilityStatus 并应用翻译
    /// </summary>
    [HarmonyPatch(typeof(Facility), "Init")]
    [HarmonyPostfix]
    private static void Facility_Init_Postfix(Facility __instance, int facilityTypeID)
    {
        try
        {
            var facilityStatus = FacilityDataTable.GetFacilityStatus(facilityTypeID, true);
            if (facilityStatus == null || string.IsNullOrEmpty(facilityStatus.termName))
                return;

            var translation = LocalizationManager.GetTranslation(facilityStatus.termName);
            if (!string.IsNullOrEmpty(translation))
                __instance.param.name = translation;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"Facility_Init_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     SimpleMaidPlate.SetMaidData Postfix
    ///     原方法在 supportMultiLanguage 时翻译主角/男角色名称
    ///     此 Postfix 通过 GetTranslation 应用翻译
    /// </summary>
    [HarmonyPatch(typeof(SimpleMaidPlate), "SetMaidData")]
    [HarmonyPostfix]
    private static void SimpleMaidPlate_SetMaidData_Postfix(
        SimpleMaidPlate __instance,
        Maid maid)
    {
        try
        {
            if (maid == null || !maid.boMAN)
                return;

            var firstNameLabel = Traverse.Create(__instance)
                .Field("first_name_label_").GetValue<UILabel>();
            if (firstNameLabel == null)
                return;

            if (maid.ActiveSlotNo == 0)
            {
                var translation = LocalizationManager.GetTranslation(
                    "ScenePhotoMode/プレイヤー名/主人公");
                if (!string.IsNullOrEmpty(translation))
                    firstNameLabel.text = translation;
            }
            else
            {
                var translation = LocalizationManager.GetTranslation(
                    "ScenePhotoMode/プレイヤー名/男");
                if (!string.IsNullOrEmpty(translation))
                    firstNameLabel.text = translation + maid.ActiveSlotNo.ToString();
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"SimpleMaidPlate_SetMaidData_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     YotogiSkillNameHoverPlate.SetName Postfix
    ///     原方法在 supportMultiLanguage 时创建 Localize 组件并设置技能名 term
    ///     此 Postfix 添加/获取 Localize 组件并设置正确的 TermArgs 和 term
    /// </summary>
    [HarmonyPatch(typeof(YotogiSkillNameHoverPlate), "SetName")]
    [HarmonyPostfix]
    private static void YotogiSkillNameHoverPlate_SetName_Postfix(
        YotogiSkillNameHoverPlate __instance,
        string skillName,
        string skillNameTerm,
        bool isExpLock)
    {
        try
        {
            if (string.IsNullOrEmpty(skillNameTerm))
                return;

            var label = Traverse.Create(__instance).Field("label").GetValue<UILabel>();
            if (label == null)
                return;

            var localize = label.GetComponent<Localize>();
            if (localize == null)
            {
                localize = label.gameObject.AddComponent<Localize>();
                var capturedInstance = __instance;
                localize.LocalizeEventExecEnd.AddListener(() =>
                {
                    Traverse.Create(capturedInstance).Method("LabelPixelPerfect").GetValue();
                });
            }

            localize.TermArgs = new[]
            {
                Localize.ArgsPair.Create(skillNameTerm, true),
                isExpLock
                    ? Localize.ArgsPair.Create("SceneYotogi/(EXPロック)", true)
                    : Localize.ArgsPair.Create(string.Empty, false)
            };
            localize.SetTerm("SceneYotogi/スキル選択スキル名表記");
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"YotogiSkillNameHoverPlate_SetName_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     Score_Mgr.SetNameUI Postfix
    ///     原方法在 supportMultiLanguage 且非玩家角色时，添加 Localize 组件并设置敌方名称 term
    ///     此 Postfix 对非玩家角色始终应用本地化
    /// </summary>
    [HarmonyPatch(typeof(Score_Mgr), "SetNameUI")]
    [HarmonyPostfix]
    private static void Score_Mgr_SetNameUI_Postfix(
        DanceBattle_Mgr.CharaType chara_type,
        GameObject score_ui)
    {
        try
        {
            if (chara_type == DanceBattle_Mgr.CharaType.Player)
                return;

            if (score_ui == null)
                return;

            var textTransform = score_ui.transform.Find("Text/NameText");
            if (textTransform == null)
                return;

            var component = textTransform.GetComponent<UILabel>();
            if (component == null)
                return;

            if (DanceBattle_Mgr.EnemyData == null ||
                string.IsNullOrEmpty(DanceBattle_Mgr.EnemyData.NameTerm))
                return;

            var localize = component.GetComponent<Localize>();
            if (localize == null)
                localize = component.gameObject.AddComponent<Localize>();

            Utility.SetLocalizeTerm(localize, DanceBattle_Mgr.EnemyData.NameTerm, false);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"Score_Mgr_SetNameUI_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     TrophyAchieveEffect.WriteUITrophyData Postfix
    ///     原方法在 supportMultiLanguage 时设置勋章标题的 Localize 组件
    ///     此 Postfix 获取 title_label_ 的 Localize 组件并设置正确的 term
    /// </summary>
    [HarmonyPatch(typeof(TrophyAchieveEffect), "WriteUITrophyData")]
    [HarmonyPostfix]
    private static void TrophyAchieveEffect_WriteUITrophyData_Postfix(
        TrophyAchieveEffect __instance,
        Trophy.Data trophy_data)
    {
        try
        {
            if (trophy_data == null || string.IsNullOrEmpty(trophy_data.nameTerm))
                return;

            var titleLabel = Traverse.Create(__instance)
                .Field("title_label_").GetValue<UILabel>();
            if (titleLabel == null)
                return;

            var localize = titleLabel.GetComponent<Localize>();
            if (localize == null)
                return;

            localize.TermPrefix = "『";
            localize.TermSuffix = "』";
            localize.SetTerm(trophy_data.nameTerm);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"TrophyAchieveEffect_WriteUITrophyData_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     PrivateEventListUnit.OnHoverOver Postfix
    ///     原方法在 supportMultiLanguage 时使用 conditionTerms（I2 term），否则使用 conditions（日文）
    ///     此 Postfix 重新调用 SetTexts 使用 conditionTerms
    /// </summary>
    [HarmonyPatch(typeof(PrivateEventListUnit), "OnHoverOver")]
    [HarmonyPostfix]
    private static void PrivateEventListUnit_OnHoverOver_Postfix(
        PrivateEventListUnit __instance)
    {
        try
        {
            var traverse = Traverse.Create(__instance);
            var conditonPanel = traverse.Field("conditonPanel")
                .GetValue<UIWFConditionList>();
            if (conditonPanel == null)
                return;

            var infomation = traverse.Property("infomation").GetValue();
            if (infomation == null)
                return;

            var conditionTerms = Traverse.Create(infomation)
                .Field("conditionTerms")
                .GetValue<List<KeyValuePair<string[], Color>>>();
            if (conditionTerms == null || conditionTerms.Count == 0)
                return;

            conditonPanel.SetTexts(conditionTerms.ToArray(), 500);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"PrivateEventListUnit_OnHoverOver_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    #endregion
}
