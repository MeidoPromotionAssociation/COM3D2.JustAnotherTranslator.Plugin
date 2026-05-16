using System;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using I2.Loc;
using UnityEngine;
using UnityEngine.UI;
using wf;
using Yotogis;
using Math = System.Math;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.UI;

/// <summary>
///     覆盖一些因游戏检查 Product.supportMultiLanguage 或 Product.IsJapan 而未被 UI 翻译模块覆盖的路径
/// </summary>
public static class UITextTranslateExtraPatch
{
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
            var facilityStatus = FacilityDataTable.GetFacilityStatus(facilityTypeID);
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

    #region PhotoMode

    /// <summary>
    ///     背景物体创建窗口分类下拉框的 term 补全。
    ///     CreateBGObjectSubWindow.Awake 在 JP 版会把 PopupList.isLocalized 设为 false，
    ///     并且不写入 PhotoBGObjectData.popup_category_term_list，导致分类下拉框只显示日文。
    ///     这个补丁只使用 CreateBGObjectSubWindow.addButtonList 公开字段、UIPopupList 的
    ///     公开 isLocalized/itemTerms 和 PhotoBGObjectData 的公开分类 term 列表，不接触私有字段，
    ///     因此放在 ExtraPatch。按钮本体的 term 应用仍由 DangerPatch 处理，因为
    ///     PopupAndButtonList.OnChangePopUpList 会读取私有 ElementData.term。
    /// </summary>
    [HarmonyPatch(typeof(CreateBGObjectSubWindow), "Awake")]
    [HarmonyPostfix]
    private static void CreateBGObjectSubWindow_Awake_Postfix(
        CreateBGObjectSubWindow __instance)
    {
        try
        {
            if (__instance?.addButtonList?.PopUpList?.PopupList == null ||
                PhotoBGObjectData.popup_category_term_list == null)
                return;

            var popupList = __instance.addButtonList.PopUpList.PopupList;
            popupList.itemTerms = PhotoBGObjectData.popup_category_term_list;
            popupList.isLocalized = popupList.itemTerms.Count > 0;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"CreateBGObjectSubWindow_Awake_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    #endregion

    #region FacilityPowerUpMaterialDropdown

    /// <summary>
    ///     设施强化素材下拉框的 term 补全。
    ///     游戏原始路径：
    ///     FacilityUIPowerUpMaterialList.SetupFacilityPowerUpMaterialListDropDown 内部只有
    ///     Product.supportMultiLanguage 为 true 时才会启用 LocalizeDropdown，并把
    ///     "SceneFacilityManagement/強化素材/..." 写入 LocalizeDropdown._Terms。
    ///     JP 版 Product.supportMultiLanguage 为 false，因此 Dropdown 只会得到日文 OptionData，
    ///     后续不会再进入 I2 Localization。
    ///     这个补丁放在 ExtraPatch 而不是 DangerPatch 的原因：
    ///     1. 只使用 Show 的公开参数 id/facility 和 Unity 公开组件 Dropdown/LocalizeDropdown；
    ///     2. 不读取或写入游戏私有字段；
    ///     3. 不改变 facility 的实际素材选择数据，只替换 Dropdown.options 的显示文本；
    ///     4. 如果没有翻译，LocalizationManager.GetTranslation 仍会回退为空，LocalizeDropdown
    ///     会保留同数量选项，不影响 Dropdown.value 对应的素材索引。
    /// </summary>
    [HarmonyPatch(typeof(FacilityUIPowerUpMaterialList), "Show")]
    [HarmonyPostfix]
    private static void FacilityUIPowerUpMaterialList_Show_Postfix(
        FacilityUIPowerUpMaterialList __instance,
        int id,
        Facility facility)
    {
        try
        {
            if (__instance == null || facility == null)
                return;

            var recipe = FacilityDataTable.GetFacilityPowerUpRecipe(id);
            if (recipe == null || recipe.materialCategoryIDArray == null)
                return;

            var dropdowns = __instance.GetComponentsInChildren<Dropdown>(true);
            if (dropdowns == null || dropdowns.Length == 0)
                return;

            var count = Math.Min(recipe.materialCategoryIDArray.Length, dropdowns.Length);
            for (var i = 0; i < count; i++)
            {
                var dropdown = dropdowns[i];
                if (dropdown == null)
                    continue;

                var selected = dropdown.value;
                var materialCategoryId = recipe.materialCategoryIDArray[i];
                var materialArray =
                    GameMain.Instance.FacilityMgr.GetFacilityPowerUpItemEnableArray(
                        materialCategoryId);

                var localizeDropdown = dropdown.GetComponent<LocalizeDropdown>();
                if (localizeDropdown == null)
                    localizeDropdown = dropdown.gameObject.AddComponent<LocalizeDropdown>();

                localizeDropdown.enabled = true;
                localizeDropdown._Terms.Clear();

                if (materialArray != null && materialArray.Length > 0)
                    foreach (var material in materialArray)
                    {
                        if (material == null || string.IsNullOrEmpty(material.name))
                            continue;

                        localizeDropdown._Terms.Add(
                            "SceneFacilityManagement/強化素材/" + material.name);
                    }
                else
                    localizeDropdown._Terms.Add("SceneFacilityManagement/強化素材/無し");

                localizeDropdown.UpdateLocalization();

                if (dropdown.options.Count > 0)
                    dropdown.value = Math.Min(selected, dropdown.options.Count - 1);

                dropdown.RefreshShownValue();
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"FacilityUIPowerUpMaterialList_Show_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    #endregion

    #region YotogiSkillHover

    /// <summary>
    ///     夜伽技能名 hover 浮层的 term 补全。
    ///     原方法 YotogiSkillNameHoverPlate.SetName 会先写入日文 skillName，
    ///     只有 Product.supportMultiLanguage 为 true 时才创建 Localize 并组合：
    ///     SceneYotogi/スキル選択スキル名表記
    ///     {0}=skillNameTerm
    ///     {1}=SceneYotogi/(EXPロック) 或空字符串。
    ///     这里不读取私有字段，直接从组件自身取 UILabel，并复用原有 public 的
    ///     LabelPixelPerfect 来刷新背景宽度，因此属于相对安全的显示层补丁。
    /// </summary>
    [HarmonyPatch(typeof(YotogiSkillNameHoverPlate), "SetName")]
    [HarmonyPostfix]
    private static void YotogiSkillNameHoverPlate_SetName_Postfix(
        YotogiSkillNameHoverPlate __instance,
        string skillNameTerm,
        bool isExpLock)
    {
        try
        {
            if (__instance == null || string.IsNullOrEmpty(skillNameTerm))
                return;

            var label = __instance.GetComponent<UILabel>();
            if (label == null)
                return;

            var localize = label.GetComponent<Localize>();
            if (localize == null)
                localize = label.gameObject.AddComponent<Localize>();

            localize.TermArgs = new[]
            {
                Localize.ArgsPair.Create(skillNameTerm, true),
                !isExpLock
                    ? Localize.ArgsPair.Create(string.Empty, false)
                    : Localize.ArgsPair.Create("SceneYotogi/(EXPロック)", true)
            };
            localize.SetTerm("SceneYotogi/スキル選択スキル名表記");

            __instance.LabelPixelPerfect();
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"YotogiSkillNameHoverPlate_SetName_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    #endregion


    #region CharaSelectStatus

    /// <summary>
    ///     CharaSelectStatusMgr.Init 在 JP 版选择非本地化的 statusCtrl prefab，
    ///     导致整个 status 面板上的子 UI 都不走 I2。
    ///     此 Prefix 在 Init 执行前把 statusCtrl 重定向到 statusLocalizeCtrl，
    ///     让条件表达式 (supportMultiLanguage ? statusLocalizeCtrl : statusCtrl)
    ///     的两个分支都指向 localize 版本。
    /// </summary>
    [HarmonyPatch(typeof(CharaSelectStatusMgr), "Init")]
    [HarmonyPrefix]
    private static void CharaSelectStatusMgr_Init_Prefix(CharaSelectStatusMgr __instance)
    {
        try
        {
            if (__instance == null) return;
            if (Product.supportMultiLanguage) return;
            if (__instance.statusLocalizeCtrl == null) return;

            __instance.statusCtrl = __instance.statusLocalizeCtrl;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"CharaSelectStatusMgr_Init_Prefix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    #endregion

    #region YotogiParamScroll

    /// <summary>
    ///     YotogiParamScroll.SetLabelText 在 JP 版（!Product.supportMultiLanguage）
    ///     跳过 Localize term 设置，导致夜伽参数名不走 I2 翻译路径。
    ///     此 Postfix 复制多语言分支逻辑，设置 TermArgs 和 term。
    /// </summary>
    [HarmonyPatch(typeof(YotogiParamScroll), "SetLabelText",
        typeof(YotogiParamScroll.LabelAndLocalize), typeof(string), typeof(int))]
    [HarmonyPostfix]
#pragma warning disable Harmony003
    private static void YotogiParamScroll_SetLabelText_Postfix(
        YotogiParamScroll.LabelAndLocalize labelAndLocalize, string text, int num)
    {
        try
        {
            if (Product.supportMultiLanguage)
                return;

            var localize = labelAndLocalize.localize;
            if (localize == null)
                return;

            localize.TermArgs = new Localize.ArgsPair[2];
            localize.TermArgs[0] = Localize.ArgsPair.Create("  {0}", false);
            localize.TermArgs[1] = Localize.ArgsPair.Create(num.ToString(), false);
            localize.SetTerm("MaidStatus/" + text);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"YotogiParamScroll_SetLabelText_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }
#pragma warning restore Harmony003

    #endregion

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
}