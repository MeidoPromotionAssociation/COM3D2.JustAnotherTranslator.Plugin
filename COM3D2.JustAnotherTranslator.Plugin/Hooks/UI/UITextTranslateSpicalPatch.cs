using System;
using System.Collections.Generic;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using I2.Loc;
using UnityEngine;
using wf;
using Yotogis;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.UI;

public static class UITextTranslateSpicalPatch
{
    #region YotogiSkillCommands

    /// <summary>
    ///     强制 YotogiCommandFactory.GetGroupName 返回 I2 term 而非原始日文名称
    ///     原方法在 !Product.supportMultiLanguage 时返回 basic.group_name（日文）
    ///     修改为始终返回 basic.termGroupName（I2 term，如 "YotogiSkillName/キスをする"）
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
    ///     修改为始终返回 basic.termName（I2 term，如 "YotogiSkillCommand/キスをする"）
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
    ///     此 Postfix 通过 GetTranslation 直接获取翻译，不依赖 Localize 组件
    ///     回退逻辑：无翻译时使用 GetTermLastWord 提取原始日文名（如 "YotogiSkillCommand/キスをする" → "キスをする"）
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
    ///     原方法中 Utility.SetLocalizeTerm 失败时（如 Localize 组件不存在），
    ///     会将 component.text 设为 name（此时为 I2 term 路径，如 "YotogiSkillName/キスをする"）
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

            // 如果文本仍然是 term 路径（包含 "/"），说明 SetLocalizeTerm 未能成功翻译
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
    ///     替换 ShopItem.OnHoverOver，强制使用 I2 本地化获取物品名称和描述
    ///     原方法在 !Product.supportMultiLanguage 时使用 item_data_.name 和 item_data_.detail_text（日文）
    ///     此 Prefix 始终通过 GetTranslation 获取翻译后的名称和描述
    /// </summary>
    [HarmonyPatch(typeof(ShopItem), "OnHoverOver")]
    [HarmonyPrefix]
    private static bool ShopItem_OnHoverOver_Prefix(ShopItem __instance)
    {
        try
        {
            var traverse = Traverse.Create(__instance);
            var itemData = traverse.Field("item_data_").GetValue<Shop.ItemDataBase>();
            var infoWindow = traverse.Field("info_window_").GetValue<ItemInfoWnd>();
            var iconSprite = traverse.Field("icon_sprite_").GetValue<UI2DSprite>();

            if (itemData == null || infoWindow == null || iconSprite == null)
                return true;

            var position = __instance.transform.position;

            // 获取翻译后的名称，无翻译时回退到原始日文
            var text = LocalizationManager.GetTranslation(
                "SceneShop/" + itemData.id + "/名前");
            if (string.IsNullOrEmpty(text))
                text = itemData.name;

            // 获取翻译后的描述，无翻译时回退到原始日文
            var text2 = LocalizationManager.GetTranslation(
                "SceneShop/" + itemData.id + "/説明");
            if (!string.IsNullOrEmpty(text2))
                text2 = text2.Replace("《改行》", "\n");
            else
                text2 = itemData.detail_text;

            infoWindow.Open(position, iconSprite.sprite2D.texture, text, text2);

            // 复制原始的定位逻辑
            var transform =
                UTY.GetChildObject(infoWindow.gameObject, "Base").transform;
            transform.position = position;
            var y = transform.localPosition.y;
            transform.localPosition =
                infoWindow.m_vecOffsetPos + new Vector3(-337f, y, 0f);
            if (-412f > transform.localPosition.y - 90f)
            {
                var vecOffsetPos = infoWindow.m_vecOffsetPos;
                vecOffsetPos.y *= -1f;
                transform.localPosition = vecOffsetPos + new Vector3(-337f, y, 0f);
            }

            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"ShopItem_OnHoverOver_Prefix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
            return true;
        }
    }

    /// <summary>
    ///     替换 ShopItem.OnClick，强制使用 I2 本地化获取物品名称用于购买确认对话框
    ///     原方法在 !Product.supportMultiLanguage 时使用 item_data_.name（日文）
    /// </summary>
    [HarmonyPatch(typeof(ShopItem), "OnClick")]
    [HarmonyPrefix]
    private static bool ShopItem_OnClick_Prefix(ShopItem __instance)
    {
        try
        {
            var traverse = Traverse.Create(__instance);
            var itemData = traverse.Field("item_data_").GetValue<Shop.ItemDataBase>();

            if (itemData == null)
                return true;

            var num = GameMain.Instance.CharacterMgr.status.money - itemData.price;
            if (num < 0L)
            {
                GameMain.Instance.SysDlg.ShowFromLanguageTerm(
                    "Dialog/ショップ/資金が不足しています。", null,
                    SystemDialog.TYPE.OK);
                return false;
            }

            var text = LocalizationManager.GetTranslation(
                "SceneShop/" + itemData.id + "/名前");
            if (string.IsNullOrEmpty(text))
                text = itemData.name;

            string[] array = { text, Utility.ConvertMoneyText(itemData.price) };

            var callback = (SystemDialog.OnClick)Delegate.CreateDelegate(
                typeof(SystemDialog.OnClick), __instance,
                AccessTools.Method(typeof(ShopItem), "CallOnClickEvent"));

            GameMain.Instance.SysDlg.ShowFromLanguageTerm(
                "Dialog/ショップ/{0}を購入しますか?資金 -{1}CR", array,
                SystemDialog.TYPE.OK_CANCEL, callback);

            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"ShopItem_OnClick_Prefix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
            return true;
        }
    }

    /// <summary>
    ///     替换 ShopItem.CallOnClickEvent，强制使用 I2 本地化获取物品名称用于购买完成对话框
    ///     原方法在 !Product.supportMultiLanguage 时使用 item_data_.name（日文）
    /// </summary>
    [HarmonyPatch(typeof(ShopItem), "CallOnClickEvent")]
    [HarmonyPrefix]
    private static bool ShopItem_CallOnClickEvent_Prefix(ShopItem __instance)
    {
        try
        {
            var traverse = Traverse.Create(__instance);
            var itemData = traverse.Field("item_data_").GetValue<Shop.ItemDataBase>();
            var clickEvent = traverse.Field("click_event_")
                .GetValue<ShopItem.OnClickEvent>();

            if (itemData == null)
                return true;

            if (clickEvent != null)
                clickEvent(__instance);

            var text = LocalizationManager.GetTranslation(
                "SceneShop/" + itemData.id + "/名前");
            if (string.IsNullOrEmpty(text))
                text = itemData.name;

            GameMain.Instance.SysDlg.ShowFromLanguageTerm(
                "Dialog/ショップ/{0}を購入しました",
                new[] { text },
                SystemDialog.TYPE.OK);

            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"ShopItem_CallOnClickEvent_Prefix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
            return true;
        }
    }

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

    /// <summary>
    ///     为商店分类按钮应用本地化
    ///     按钮结构: CategoryButton(root) / Label(UILabel) + Button(UIButton)
    /// </summary>
    private static void LocalizeShopCategoryButton(UIButton button, string categoryName,
        string[] termPrefixes)
    {
        try
        {
            if (button == null || string.IsNullOrEmpty(categoryName))
                return;

            // Button 是 root 的子对象，Label 是 Button 的兄弟节点
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
                $"LocalizeShopCategoryButton error for '{categoryName}' unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    #endregion

    #region ScheduleIcons

    /// <summary>
    ///     替换 OnHoverTaskIcon.SetText，强制使用 I2 本地化获取任务名称
    ///     原方法在 !Product.supportMultiLanguage 时直接使用传入的日文 message
    ///     此 Prefix 始终通过 GetTranslation 获取翻译后的任务名
    /// </summary>
    [HarmonyPatch(typeof(OnHoverTaskIcon), "SetText")]
    [HarmonyPrefix]
    private static bool OnHoverTaskIcon_SetText_Prefix(
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
                return true;

            var component = goNamePlate.GetComponent<UISprite>();

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
            var array = message.Split('\n');
            component.width = labelName.width + 15;
            component.height = array.Length * 33;

            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"OnHoverTaskIcon_SetText_Prefix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
            return true;
        }
    }

    /// <summary>
    ///     在 OnHoverTaskIcon.OnHover 之前，强制刷新文本
    ///     原方法仅在 Product.supportMultiLanguage 为 true 时才在 hover 时重新调用 SetText
    ///     此 Prefix 确保每次 hover 时都调用 SetText 以显示翻译后的文本
    ///     注意：这是一个非跳过的 Prefix（void 返回），原方法仍会执行
    /// </summary>
    [HarmonyPatch(typeof(OnHoverTaskIcon), "OnHover")]
    [HarmonyPrefix]
    private static void OnHoverTaskIcon_OnHover_Prefix(
        OnHoverTaskIcon __instance,
        bool isOver)
    {
        try
        {
            if (!isOver)
                return;

            var traverse = Traverse.Create(__instance);
            var goNamePlate = traverse.Field("m_goNamePlate").GetValue<GameObject>();
            if (goNamePlate == null)
                return;

            var taskName = traverse.Field("taskName").GetValue<string>();
            if (string.IsNullOrEmpty(taskName))
                return;

            // 在原方法执行前调用 SetText（我们的 SetText Prefix 会处理翻译）
            // 原方法中 !supportMultiLanguage 导致 SetText 被跳过，这里补上
            traverse.Method("SetText", new[] { typeof(string), typeof(int) })
                .GetValue(taskName, -1);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"OnHoverTaskIcon_OnHover_Prefix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
        }
    }

    #endregion

    #region StatusPanel

    /// <summary>
    ///     在 StatusCtrl.SetData 之后，确保性格等字段的翻译已应用
    ///     原方法先设置日文文本，再调用 Utility.SetLocalizeTerm（JAT 已 hook 为 forceApply=true）
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

    /// <summary>
    ///     辅助方法：如果 SetLocalizeTerm 未能成功翻译（标签文本仍为日文原值），
    ///     直接通过 GetTranslation 获取翻译并设置
    /// </summary>
    private static void TryLocalizeStatusLabel(UILabel label, string term, string fallback)
    {
        if (label == null || string.IsNullOrEmpty(term))
            return;

        // 只在 SetLocalizeTerm 未能成功翻译时进行回退处理
        if (label.text == fallback || string.IsNullOrEmpty(label.text))
        {
            var translation = LocalizationManager.GetTranslation(term);
            if (!string.IsNullOrEmpty(translation))
                label.text = translation;
        }
    }

    #endregion
}