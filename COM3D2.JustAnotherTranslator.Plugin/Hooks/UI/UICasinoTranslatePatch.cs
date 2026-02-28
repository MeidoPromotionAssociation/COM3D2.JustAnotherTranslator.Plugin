using System;
using I2.Loc;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using wf;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.UI;

/// <summary>
///     赌场场景相关的本地化补丁
///     覆盖 UIStates（21点结果）、CasinoItemUI/SceneCasinoShop（物品购买）、ExChangeUI（换币汇率）
///     中被 Product.supportMultiLanguage 门控的本地化代码路径
/// </summary>
public static class UICasinoTranslatePatch
{
    #region UIStates

    /// <summary>
    ///     UIStates.CheckResultText Postfix
    ///     原方法中 5 个 if (Product.supportMultiLanguage) 块分别对应不同赌局结果
    ///     在 JP 版中这些块被跳过，导致结果文本始终为日文
    ///     此 Postfix 复现 BjPlayer 状态判断，设置正确的 Localize TermArgs 和 term
    /// </summary>
    [HarmonyPatch(typeof(UIStates), "CheckResultText")]
    [HarmonyPostfix]
    private static void UIStates_CheckResultText_Postfix(UIStates __instance)
    {
        try
        {
            var traverse = Traverse.Create(__instance);

            // 检查是否为早期返回路径（HideResultText 被调用）
            var resultUI = traverse.Field("m_ResultUI").GetValue<GameObject>();
            if (resultUI == null || !resultUI.activeSelf)
                return;

            var getMoneyText = traverse.Field("m_GetMoneyText").GetValue<Text>();
            if (getMoneyText == null)
                return;

            var localize = getMoneyText.GetComponent<Localize>();
            if (localize == null)
                return;

            // 根据 BjPlayer 状态确定正确的 term 和参数值
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

    #endregion

    #region CasinoItemUI

    /// <summary>
    ///     CasinoItemUI.ItemBuy Prefix 替换
    ///     原方法在 !supportMultiLanguage 时使用 m_ItemData.Name（日文）作为对话框参数
    ///     此 Prefix 始终通过 GetTranslation(NameTerm) 获取翻译后的物品名称
    /// </summary>
    [HarmonyPatch(typeof(CasinoItemUI), "ItemBuy")]
    [HarmonyPrefix]
    private static bool CasinoItemUI_ItemBuy_Prefix(CasinoItemUI __instance)
    {
        try
        {
            var itemData = Traverse.Create(__instance).Field("m_ItemData").GetValue<CasinoShopItem>();
            if (itemData == null)
                return true;

            if (!itemData.IsCanBuy)
            {
                GameMain.Instance.SysDlg.ShowFromLanguageTerm(
                    "SceneCasino/ダイアログ/カジノコインが不足しています。", null,
                    SystemDialog.TYPE.OK, null, null);
                return false;
            }

            // 始终尝试获取翻译后的物品名称
            var name = LocalizationManager.GetTranslation(itemData.NameTerm);
            if (string.IsNullOrEmpty(name))
                name = itemData.Name;

            var array = new[] { name, Utility.ConvertMoneyText(itemData.Price) };

            // 创建回调委托：购买确认后调用 SceneCasinoShop.ItemBuy
            var capturedItemData = itemData;
            GameMain.Instance.SysDlg.ShowFromLanguageTerm(
                "SceneCasino/ダイアログ/{0}を購入しますか?", array,
                SystemDialog.TYPE.OK_CANCEL,
                (SystemDialog.OnClick)(() =>
                    KasaSceneMgr<SceneCasinoShop>.Instance.ItemBuy(capturedItemData)),
                null);

            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"CasinoItemUI_ItemBuy_Prefix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
            return true;
        }
    }

    #endregion

    #region SceneCasinoShop

    /// <summary>
    ///     SceneCasinoShop.ItemBuy Prefix 替换
    ///     原方法在 !supportMultiLanguage 时使用 item_data.Name（日文）作为购买完成对话框参数
    ///     此 Prefix 始终通过 GetTranslation(NameTerm) 获取翻译后的物品名称
    /// </summary>
    [HarmonyPatch(typeof(SceneCasinoShop), "ItemBuy")]
    [HarmonyPrefix]
    private static bool SceneCasinoShop_ItemBuy_Prefix(
        SceneCasinoShop __instance,
        CasinoShopItem item_data)
    {
        try
        {
            // 复制原方法的购买逻辑
            var status = GameMain.Instance.CharacterMgr.status;
            status.casinoCoin -= (long)item_data.Price;
            item_data.ItemBuy();

            // 调用私有方法 UpdateUIState
            Traverse.Create(__instance).Method("UpdateUIState").GetValue();

            // 始终尝试获取翻译后的物品名称
            var name = LocalizationManager.GetTranslation(item_data.NameTerm);
            if (string.IsNullOrEmpty(name))
                name = item_data.Name;

            GameMain.Instance.SysDlg.ShowFromLanguageTerm(
                "SceneCasino/ダイアログ/{0}を購入しました",
                new[] { name },
                SystemDialog.TYPE.OK, null, null);

            return false;
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"SceneCasinoShop_ItemBuy_Prefix unknown error, please report this issue/未知错误，请报告此问题 {e.Message}\n{e.StackTrace}");
            return true;
        }
    }

    #endregion

    #region ExChangeUI

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

            // 获取或创建 Localize 组件
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
}
