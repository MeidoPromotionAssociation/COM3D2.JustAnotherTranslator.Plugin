using COM3D2.JustAnotherTranslator.Plugin.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle.Component;

/// <summary>
///     屏幕字幕组件，在游戏屏幕上显示字幕
/// </summary>
public class ScreenSubtitleComponent : BaseSubtitleComponent
{
    /// <summary>
    ///     创建字幕UI
    /// </summary>
    protected override void CreateSubtitleUI()
    {
        // 创建画布
        var canvasObj = new GameObject("JAT_Subtitle_SubtitleCanvas");
        canvasObj.transform.SetParent(transform, false);
        CanvasComponents = canvasObj.AddComponent<Canvas>();
        CanvasComponents.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasComponents.sortingOrder = 32767; // 确保在最上层显示

        // 添加画布缩放器
        CanvasScalerComponents = CanvasComponents.gameObject.AddComponent<CanvasScaler>();
        CanvasScalerComponents.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        CanvasScalerComponents.referenceResolution = new Vector2(1920, 1080);
        CanvasScalerComponents.matchWidthOrHeight = 0.5f;

        // 创建背景面板
        var backgroundObj = new GameObject("JAT_Subtitle_SubtitleBackground");
        backgroundObj.transform.SetParent(CanvasComponents.transform, false);
        BackgroundImageComponents = backgroundObj.AddComponent<Image>();
        BackgroundImageComponents.color = new Color(0, 0, 0, 0.5f); // 半透明黑色背景

        // 设置背景位置和大小
        var backgroundRect = BackgroundImageComponents.rectTransform;
        backgroundRect.anchorMin = new Vector2(0, 0);
        backgroundRect.anchorMax = new Vector2(1, 0);
        backgroundRect.pivot = new Vector2(0.5f, 0);
        backgroundRect.sizeDelta = new Vector2(0, 100);
        backgroundRect.anchoredPosition = new Vector2(0, 0);

        // 创建文本对象
        var textObj = new GameObject("JAT_Subtitle_SubtitleText");
        textObj.transform.SetParent(backgroundRect, false);
        TextComponent = textObj.AddComponent<Text>();

        // 设置文本位置和大小
        var textRect = TextComponent.rectTransform;
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(-40, -20); // 左右上下各留出10像素的边距
        textRect.anchoredPosition = new Vector2(0, 0);

        // 设置默认文本样式
        TextComponent.alignment = TextAnchor.MiddleCenter;
        TextComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
        TextComponent.verticalOverflow = VerticalWrapMode.Truncate;

        // 设置文本和背景不拦截点击事件
        TextComponent.raycastTarget = false;
        BackgroundImageComponents.raycastTarget = false;

        // 添加描边组件
        OutlineComponents = TextComponent.gameObject.AddComponent<Outline>();
        OutlineComponents.enabled = false;

        LogManager.Debug("ScreenSubtitleComponent Subtitle UI created");
    }
}