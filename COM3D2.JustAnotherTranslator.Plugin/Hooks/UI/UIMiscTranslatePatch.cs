using System;
using System.Collections.Generic;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using I2.Loc;
using Schedule;
using UnityEngine;
using wf;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.UI;

/// <summary>
///     其他杂项 UI 组件的本地化补丁
///     覆盖 Facility、SimpleMaidPlate、YotogiSkillNameHoverPlate、Score_Mgr、
///     TrophyAchieveEffect、PrivateEventListUnit
///     中被 Product.supportMultiLanguage 门控的本地化代码路径
/// </summary>
public static class UIMiscTranslatePatch
{
    #region Facility

    /// <summary>
    ///     Facility.Init Postfix
    ///     原方法在 supportMultiLanguage 时通过 GetTranslation(termName) 翻译设施名称
    ///     JP 版跳过此块，导致设施名始终为日文
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

    #endregion

    #region SimpleMaidPlate

    /// <summary>
    ///     SimpleMaidPlate.SetMaidData Postfix
    ///     原方法在 supportMultiLanguage 时翻译主角/男角色名称
    ///     JP 版始终显示 "主人公" / "男N"
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

    #endregion

    #region YotogiSkillNameHoverPlate

    /// <summary>
    ///     YotogiSkillNameHoverPlate.SetName Postfix
    ///     原方法在 supportMultiLanguage 时创建 Localize 组件并设置技能名 term
    ///     JP 版始终显示 skillName（日文）+ "(EXPロック)"
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
                // 注册 LocalizeEvent 监听器，确保翻译应用后重新调整标签尺寸
                // 原方法中 OnLocalizeEvent 调用 LabelPixelPerfect
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

    #endregion

    #region Score_Mgr

    /// <summary>
    ///     Score_Mgr.SetNameUI Postfix
    ///     原方法在 supportMultiLanguage 且非玩家角色时，添加 Localize 组件并设置敌方名称 term
    ///     JP 版使用 DanceBattle_Mgr.EnemyData.Name（日文）
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

    #endregion

    #region TrophyAchieveEffect

    /// <summary>
    ///     TrophyAchieveEffect.WriteUITrophyData Postfix
    ///     原方法在 supportMultiLanguage 时设置勋章标题的 Localize 组件（带 TermPrefix/TermSuffix）
    ///     JP 版仅显示 "『" + trophy_data.name + "』"
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

    #endregion

    #region PrivateEventListUnit

    /// <summary>
    ///     PrivateEventListUnit.OnHoverOver Postfix
    ///     原方法在 supportMultiLanguage 时使用 conditionTerms（I2 term），否则使用 conditions（日文）
    ///     此 Postfix 在原方法执行后，重新调用 SetTexts 使用 conditionTerms
    ///     配合 UIWFConditionList.SetTexts Postfix 实现完整本地化
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

            // infomation 是 public auto-property
            var infomation = traverse.Property("infomation").GetValue();
            if (infomation == null)
                return;

            // conditionTerms 是 List<KeyValuePair<string[], Color>>
            // 使用 conditionTerms（I2 term）重新调用 SetTexts 覆盖原方法使用 conditions（日文）的结果
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

    // TODO: 以下 hooks 因实现复杂度较高，暂时延迟
    // - ObjectManagerWindow.Awake: 附着点标签本地化在 lambda/delegate 中，需替换整个 delegate
    // - ProfileCtrl: 多个方法中的多处门控（性格、关系、契约、职业等），需完整分析
    // - ScheduleMaidStatusUnit.Warinig: 警告标签本地化需重建局部变量
    // - FacilityInfoUI.UpdateViewTypeUI: 设施类型图片本地化需访问局部变量
    // - BGWindow/FaceWindow/MotionWindow.Awake: popup_term_list 数据流较复杂
    // - PopupAndTabList/PopupAndButtonList.OnChangePopUpList: term 数据传播较复杂
    // - SubMaid.Data.ApplyStatus: 类型层次不确定
    // - SceneKasizukiMainMenu.CallDialog: 协程方法，Harmony 无法直接 patch
    // - ScheduleFacillityPanelCtrl.AddMaid: 对话框已在原方法中 Show，Postfix 无法撤销
}
