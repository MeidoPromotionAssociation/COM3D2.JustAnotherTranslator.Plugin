using System;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using I2.Loc;
using UnityEngine;
using wf;
using Yotogis;

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