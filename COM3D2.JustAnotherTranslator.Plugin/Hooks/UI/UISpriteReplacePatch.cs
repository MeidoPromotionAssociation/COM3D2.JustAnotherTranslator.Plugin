using System;
using COM3D2.JustAnotherTranslator.Plugin.Manger;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.UI;

/// <summary>
///     Hooks into various UI components to apply translations
/// </summary>
public static class UISpriteReplacePatch
{
    /// <summary>
    ///     挂钩到 UIButton.SetSprite 中以拦截精灵变化并应用替换
    ///     当 L2 Localization 翻译图片时，最后会得到 SpriteName 然后此方法会被调用
    ///     当然，不通过 L2 Localization 翻译时也会调用
    /// </summary>
    [HarmonyPatch(typeof(UIButton), "SetSprite", typeof(string))]
    [HarmonyPostfix]
    public static void UIButton_SetSprite_Postfix(UIButton __instance, string sp)
    {
        try
        {
            if (__instance == null || string.IsNullOrEmpty(sp)) return;

            LogManager.Debug($"UIButton_SetSprite_Postfix called with sp: {sp}");

            // 检查 atlas 是否已被 JAT 替换为相同的精灵图
            // 注意：不能用 sprite.spriteName == sp 判断，因为这是 Postfix，
            // NGUI 原方法已经将 spriteName 更新为 sp，即使 JAT atlas 中不含该 sprite
            var sprite = __instance.tweenTarget?.GetComponent<UISprite>() ??
                         __instance.GetComponent<UISprite>();
            if (sprite != null && sprite.atlas != null
                               && sprite.atlas.name ==
                               $"JAT_ReplacementAtlas_{sp}") // ProcessSpriteReplacementWithNewAtlas 中会设置的 atlas 名称
            {
                // 已经替换为相同的精灵图，跳过
                LogManager.Debug(
                    $"UIButton_SetSprite_Postfix {__instance.name} atlas.name is JAT_ReplacementAtlas_{sp} skipped");
                return;
            }

            // 进行精灵图替换
            UITranslateManager.ProcessSpriteReplacementWithNewAtlas(__instance, sp);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"UIButton_SetSprite_Postfix unknown error, please report this issue/未知错误，请报告此错误 {e.Message}\n{e.StackTrace}");
        }
    }

    // /// <summary>
    // ///     此方法用于获取 UIWidget 的纹理
    // ///     对于 UI 来说，图片是一整张 UIAtlas，而替换整个 atlas 会有游戏更新后 atlas 布局改变的问题
    // ///     因此我们直接替换组件的子类 UISprite 的贴图，达到只替换 sprite 的目的
    // ///     __result 通常是整张 Atlas，不能直接替换
    // ///
    // ///     弃用，因为替换后 UI 坐标不同，导致替换后显示不正确
    // /// </summary>
    // [HarmonyPatch(typeof(UIWidget), "mainTexture", MethodType.Getter)]
    // [HarmonyPostfix]
    // private static void UIWidget_get_mainTexture_Postfix(UIWidget __instance, ref UnityEngine.Texture __result)
    // {
    //     if (UITranslator.TryGetSpriteReplacement(__instance, out var customTexture))
    //     {
    //         if (customTexture != null)
    //         {
    //             __result = customTexture;
    //             LogManager.Debug($"UIWidget_get_mainTexture_Postfix replaced: {customTexture.name}");
    //         }
    //     }
    // }
}