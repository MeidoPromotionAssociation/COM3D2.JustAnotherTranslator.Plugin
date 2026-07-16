using System;
using System.IO;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using I2.Loc;
using UnityEngine;
using UnityEngine.UI;
using wf;
using Yotogis;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.UI;

/// <summary>
///     覆盖一些因游戏检查 Product.supportMultiLanguage 或 Product.IsJapan 而未被 UI 翻译模块覆盖的路径
/// </summary>
public static class UITextTranslateExtraPatch
{
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
            if (Product.supportMultiLanguage)
                return;

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
    ///     1. 只使用 Unity 公开组件 Dropdown；
    ///     2. 不读取或写入游戏私有字段；
    ///     3. 以每个 Dropdown 已有的 OptionData 为准，逐项生成 term，不推测组件遍历顺序与
    ///     materialCategoryIDArray 的对应关系；
    ///     4. 只修改现有 OptionData.text，不重建 options，也不改变 Dropdown.value 对应的素材索引；
    ///     5. 单个 term 缺失时保留该项原文，避免 LocalizeDropdown 把 null 翻译写成空选项。
    /// </summary>
    [HarmonyPatch(typeof(FacilityUIPowerUpMaterialList), "Show")]
    [HarmonyPostfix]
    private static void FacilityUIPowerUpMaterialList_Show_Postfix(
        FacilityUIPowerUpMaterialList __instance)
    {
        try
        {
            // 多语言版本会由游戏原逻辑完整设置 LocalizeDropdown，无需重复处理。
            if (__instance == null || Product.supportMultiLanguage)
                return;

            // 不包含非激活的列表模板；uGUIListViewer 创建的实际条目在 Show 返回前均已激活。
            var dropdowns = __instance.GetComponentsInChildren<Dropdown>();
            if (dropdowns == null || dropdowns.Length == 0)
                return;

            foreach (var dropdown in dropdowns)
            {
                if (dropdown == null || dropdown.options == null || dropdown.options.Count == 0)
                    continue;

                // JP prefab 可能已经带有被禁用的 LocalizeDropdown，旧版 JAT 也可能曾启用它。
                // 该组件会在本地化事件中清空并重建 options，缺失 term 会产生空 OptionData；
                // 在此路径禁用它，完全保留游戏刚建立的选项集合和回调索引。
                var localizeDropdown = dropdown.GetComponent<LocalizeDropdown>();
                if (localizeDropdown != null)
                {
                    localizeDropdown.enabled = false;
                    localizeDropdown._Terms.Clear();
                }

                for (var optionIndex = 0; optionIndex < dropdown.options.Count; optionIndex++)
                {
                    var option = dropdown.options[optionIndex];
                    if (option == null || string.IsNullOrEmpty(option.text))
                        continue;

                    var term = "SceneFacilityManagement/強化素材/" + option.text;
                    var translation = LocalizationManager.GetTranslation(term);
                    if (!StringTool.IsNullOrWhiteSpace(translation))
                        option.text = translation;
                }

                // 不写入 Dropdown.value，避免触发游戏捕获 materialArray 的 onValueChanged 回调。
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

    #region YotogiSkillHover

    private const string YotogiSkillNameFormatTerm =
        "SceneYotogi/スキル選択スキル名表記";

    private const string YotogiExpLockTerm = "SceneYotogi/(EXPロック)";

    private const string YotogiExpLockFallback = "(EXPロック)";

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
        string skillName,
        string skillNameTerm,
        bool isExpLock)
    {
        try
        {
            // 多语言版本已经由游戏原方法完成本地化、回退和尺寸事件注册。
            if (__instance == null || Product.supportMultiLanguage)
                return;

            var label = __instance.GetComponent<UILabel>();
            if (label == null)
                return;

            var localize = label.GetComponent<Localize>();
            if (localize == null)
                localize = label.gameObject.AddComponent<Localize>();

            var fallbackSkillName = !StringTool.IsNullOrWhiteSpace(skillName)
                ? skillName
                : Utility.GetTermLastWord(skillNameTerm);
            var fallbackText = fallbackSkillName +
                               (isExpLock ? YotogiExpLockFallback : string.Empty);
            var support = label.GetComponent<YotogiSkillNameLocalizeSupport>();
            if (support == null)
                support = label.gameObject.AddComponent<YotogiSkillNameLocalizeSupport>();

            // support 会且只会注册一次 LocalizeEventExecEnd，并在每次 I2 更新完成后
            // 保证空翻译回退到游戏原文，再调用 LabelPixelPerfect 刷新背景宽度。
            support.Configure(__instance, label, localize, fallbackText);

            localize.TermArgs = new[]
            {
                CreateTermArgumentWithFallback(skillNameTerm, fallbackSkillName),
                !isExpLock
                    ? Localize.ArgsPair.Create(string.Empty, false)
                    : CreateTermArgumentWithFallback(YotogiExpLockTerm,
                        YotogiExpLockFallback)
            };
            localize.SetTerm(YotogiSkillNameFormatTerm);
            localize.enabled = true;

            // CurrentLanguage 尚未初始化等情况下 SetTerm 可能不会触发完成事件。
            support.EnsureFallbackAndResize();
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"YotogiSkillNameHoverPlate_SetName_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     清空复用槽位时，同时停用并清除 JAT 写入的 Localize 状态，避免旧技能 term 在
    ///     LocalizationManager.LocalizeAll 等后续本地化事件中重新写回标签。
    /// </summary>
    [HarmonyPatch(typeof(YotogiSkillNameHoverPlate), "ClearName")]
    [HarmonyPostfix]
    private static void YotogiSkillNameHoverPlate_ClearName_Postfix(
        YotogiSkillNameHoverPlate __instance)
    {
        try
        {
            if (__instance == null || Product.supportMultiLanguage)
                return;

            var support = __instance.GetComponent<YotogiSkillNameLocalizeSupport>();
            if (support != null)
                support.ClearLocalizationState();
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"YotogiSkillNameHoverPlate_ClearName_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     只有 term 当前确实能得到非空翻译时才把它交给 I2 继续解析；否则使用游戏已经
    ///     提供的原文作为普通参数，避免 I2 将缺失 term 格式化为空字符串。
    /// </summary>
    private static Localize.ArgsPair CreateTermArgumentWithFallback(string term,
        string fallback)
    {
        if (!string.IsNullOrEmpty(term))
        {
            var translation = LocalizationManager.GetTranslation(term);
            if (!StringTool.IsNullOrWhiteSpace(translation))
                return Localize.ArgsPair.Create(term, true);
        }

        return Localize.ArgsPair.Create(fallback ?? string.Empty, false);
    }

    #endregion

    #region Misc

    /// <summary>
    ///     对 menuNameCurrentLanguage 进行了重写，取消了原始的 !=Product.Language.Japanese 检查
    ///     以便使用 I2 对物品名称进行翻译
    ///     请注意 CountryReplace 会被文本翻译模块处理
    ///     term 为动态生成，表达式为 this.m_strCateName + "/" +
    ///     Path.GetFileNameWithoutExtension(this.m_strMenuFileName).ToLower() + "|name"
    ///     m_strCateName 是 menu 中的 category 命令的第一个参数，m_strMenuFileName 则是 .menu 文件的文件名
    ///     例如 dress789_wear_i_.menu 的物品名 term 为 wear/dress789_wear_i_|name
    ///     因此自行制作的 MOD 同样可以使用 term 进行翻译
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPatch(typeof(SceneEdit.SMenuItem), "menuNameCurrentLanguage", MethodType.Getter)]
    [HarmonyPrefix]
    private static bool SceneEdit_SMenuItem_GetMenuNameCurrentLanguage_Prefix(
        SceneEdit.SMenuItem __instance,
        ref string __result)
    {
        try
        {
            if (Product.supportMultiLanguage || __instance == null)
                return true;

            var baseText = __instance.m_strMenuName ?? string.Empty;
            var menuFileName = __instance.m_strMenuFileName ?? string.Empty;
            var cateName = __instance.m_strCateName ?? string.Empty;
            var term = cateName + "/" + Path.GetFileNameWithoutExtension(menuFileName).ToLower();
            var translation = LocalizationManager.GetTranslation(term + "|name");
            var translatedText = translation?.Replace("《改行》", "\n");
            var text = StringTool.IsNullOrWhiteSpace(translatedText)
                ? baseText
                : translatedText;
            __result = __instance.CountryReplace(text);
            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"SceneEdit_SMenuItem_GetMenuNameCurrentLanguage_Prefix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
            return true;
        }
    }


    /// <summary>
    ///     对 infoTextCurrentLanguage 进行了重写，取消了原始的 !=Product.Language.Japanese 检查
    ///     以便使用 I2 对物品说明进行翻译
    ///     请注意 CountryReplace 会被文本翻译模块处理
    ///     term 为动态生成，表达式为 this.m_strCateName + "/" +
    ///     Path.GetFileNameWithoutExtension(this.m_strMenuFileName).ToLower() + "|info"
    ///     m_strCateName 是 menu 中的 category 命令的第一个参数，m_strMenuFileName 则是 .menu 文件的文件名
    ///     例如 dress789_wear_i_.menu 的物品描述 term 为 wear/dress789_wear_i_|info
    ///     因此自行制作的 MOD 同样可以使用 term 进行翻译
    /// </summary>
    /// <param name="__instance"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    [HarmonyPatch(typeof(SceneEdit.SMenuItem), "infoTextCurrentLanguage", MethodType.Getter)]
    [HarmonyPrefix]
    private static bool SceneEdit_SMenuItem_GetInfoTextCurrentLanguage_Prefix(
        SceneEdit.SMenuItem __instance,
        ref string __result)
    {
        try
        {
            if (Product.supportMultiLanguage || __instance == null)
                return true;

            var baseText = __instance.m_strInfo ?? string.Empty;
            var menuFileName = __instance.m_strMenuFileName ?? string.Empty;
            var cateName = __instance.m_strCateName ?? string.Empty;
            var term = cateName + "/" + Path.GetFileNameWithoutExtension(menuFileName).ToLower();
            var translation = LocalizationManager.GetTranslation(term + "|info");
            var translatedText = translation?.Replace("《改行》", "\n");
            var text = StringTool.IsNullOrWhiteSpace(translatedText)
                ? baseText
                : translatedText;
            __result = __instance.CountryReplace(text);
            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"SceneEdit_SMenuItem_GetInfoTextCurrentLanguage_Prefix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
            return true;
        }
    }

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
            if (Product.supportMultiLanguage)
                return;

            var facilityStatus = FacilityDataTable.GetFacilityStatus(facilityTypeID);
            if (facilityStatus == null || string.IsNullOrEmpty(facilityStatus.termName))
                return;

            var translation = LocalizationManager.GetTranslation(facilityStatus.termName);
            if (!StringTool.IsNullOrWhiteSpace(translation))
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
    ///     在 JP 版让 YotogiCommandFactory.GetGroupName 返回 I2 term 而非原始日文名称
    ///     原方法在 !Product.supportMultiLanguage 时返回 basic.group_name（日文）
    ///     此 Postfix 仅在该分支返回 basic.termGroupName（I2 term）
    /// </summary>
    [HarmonyPatch(typeof(YotogiCommandFactory), "GetGroupName")]
    [HarmonyPostfix]
    private static void YotogiCommandFactory_GetGroupName_Postfix(
        Skill.Data.Command.Data.Basic commandDataBasic,
        ref string __result)
    {
        try
        {
            if (Product.supportMultiLanguage)
                return;

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
    ///     在 JP 版让 YotogiCommandFactory.GetCommandName 返回 I2 term 而非原始日文名称
    ///     原方法在 !Product.supportMultiLanguage 时返回 basic.name（日文）
    ///     此 Postfix 仅在该分支返回 basic.termName（I2 term）
    /// </summary>
    [HarmonyPatch(typeof(YotogiCommandFactory), "GetCommandName")]
    [HarmonyPostfix]
    private static void YotogiCommandFactory_GetCommandName_Postfix(
        Skill.Data.Command.Data.Basic commandDataBasic,
        ref string __result)
    {
        try
        {
            if (Product.supportMultiLanguage)
                return;

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
    ///     此 Postfix 为 JP 路径补上同等的 Localize term，并在无翻译时使用
    ///     GetTermLastWord 提取原始日文名。
    /// </summary>
    [HarmonyPatch(typeof(YotogiCommandFactory), "CreateCommand")]
    [HarmonyPostfix]
    private static void YotogiCommandFactory_CreateCommand_Postfix(
        GameObject __result,
        string name)
    {
        try
        {
            if (__result == null || string.IsNullOrEmpty(name) ||
                Product.supportMultiLanguage)
                return;

            var label = UTY.GetChildObject(__result, "Name")?.GetComponent<UILabel>();
            if (label == null)
                return;

            ApplyYotogiCommandTerm(label, name);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"YotogiCommandFactory_CreateCommand_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     YotogiCommandFactory.CreateTitle 的 JP 本地化补全。
    ///     ExtraPatch 可以独立于基础 UI Patch 启用，因此不能依赖后者把
    ///     Utility.SetLocalizeTerm 的 forceApply 改为 true；这里直接设置 Localize term。
    /// </summary>
    [HarmonyPatch(typeof(YotogiCommandFactory), "CreateTitle")]
    [HarmonyPostfix]
    private static void YotogiCommandFactory_CreateTitle_Postfix(
        GameObject __result,
        string name)
    {
        try
        {
            if (__result == null || string.IsNullOrEmpty(name) ||
                Product.supportMultiLanguage)
                return;

            var label = __result.GetComponent<UILabel>();
            if (label == null)
                return;

            ApplyYotogiCommandTerm(label, name);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"YotogiCommandFactory_CreateTitle_Postfix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    ///     将夜伽指令 term 同步到 prefab 自带（或补建）的 Localize 组件。
    ///     先写入原文回退，确保 I2 查不到 term 或返回空白时不会留下空标签；保持 Localize
    ///     启用则可让后续全局本地化继续使用正确 term，而不是 prefab 的旧状态。
    /// </summary>
    private static void ApplyYotogiCommandTerm(UILabel label, string term)
    {
        if (label == null || string.IsNullOrEmpty(term))
            return;

        var fallback = Utility.GetTermLastWord(term);
        label.text = fallback;

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

/// <summary>
///     保存 JAT 为夜伽技能名悬停标签添加的本地化状态。
///     该组件不修改游戏数据，只负责空翻译回退、背景尺寸刷新和复用槽位清理。
/// </summary>
internal sealed class YotogiSkillNameLocalizeSupport : MonoBehaviour
{
    private string _fallbackText = string.Empty;
    private YotogiSkillNameHoverPlate _hoverPlate;
    private UILabel _label;
    private bool _listenerRegistered;
    private Localize _localize;

    private void OnDestroy()
    {
        RemoveLocalizationListener();
    }

    /// <summary>
    ///     为当前槽位更新引用和原文回退，并确保本地化完成事件只注册一次。
    /// </summary>
    public void Configure(YotogiSkillNameHoverPlate hoverPlate, UILabel label,
        Localize localize, string fallbackText)
    {
        if (_localize != localize)
        {
            RemoveLocalizationListener();
            _localize = localize;
        }

        _hoverPlate = hoverPlate;
        _label = label;
        _fallbackText = fallbackText ?? string.Empty;

        if (_localize != null && !_listenerRegistered)
        {
            _localize.LocalizeEventExecEnd.AddListener(OnLocalizationFinished);
            _listenerRegistered = true;
        }
    }

    /// <summary>
    ///     I2 没有产生可见文本时恢复游戏原文，并在最终文本确定后更新背景宽度。
    /// </summary>
    public void EnsureFallbackAndResize()
    {
        if (_label == null)
            return;

        if (StringTool.IsNullOrWhiteSpace(_label.text) &&
            !StringTool.IsNullOrWhiteSpace(_fallbackText))
            _label.text = _fallbackText;

        if (_hoverPlate != null)
            _hoverPlate.LabelPixelPerfect();
    }

    /// <summary>
    ///     槽位被清空时停用 Localize 并删除动态 term，防止复用对象响应后续全局本地化事件。
    /// </summary>
    public void ClearLocalizationState()
    {
        _fallbackText = string.Empty;
        RemoveLocalizationListener();

        if (_localize == null)
            return;

        _localize.enabled = false;
        _localize.TermArgs = null;
        _localize.mTerm = string.Empty;
        _localize.mTermSecondary = string.Empty;
        _localize.FinalTerm = string.Empty;
        _localize.FinalSecondaryTerm = string.Empty;
    }

    private void OnLocalizationFinished()
    {
        EnsureFallbackAndResize();
    }

    private void RemoveLocalizationListener()
    {
        if (_localize != null && _listenerRegistered)
            _localize.LocalizeEventExecEnd.RemoveListener(OnLocalizationFinished);

        _listenerRegistered = false;
    }
}