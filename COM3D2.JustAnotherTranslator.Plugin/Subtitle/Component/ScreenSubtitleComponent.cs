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
        Canvas = canvasObj.AddComponent<Canvas>();
        Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        Canvas.sortingOrder = 32767; // 确保在最上层显示

        // 添加画布缩放器
        CanvasScaler = Canvas.gameObject.AddComponent<CanvasScaler>();
        CanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        CanvasScaler.referenceResolution = new Vector2(1920, 1080); // Standard HD resolution
        CanvasScaler.matchWidthOrHeight = 0.5f; // Balance scaling between width and height

        // 创建背景面板
        var backgroundObj = new GameObject("JAT_Subtitle_SubtitleBackground");
        backgroundObj.transform.SetParent(Canvas.transform, false);
        BackgroundImage = backgroundObj.AddComponent<Image>();
        // Initial color, will be overridden by Config
        BackgroundImage.color = new Color(0, 0, 0, 0.5f);

        // 设置背景位置和大小锚点及轴心
        var backgroundRect = BackgroundImage.rectTransform;
        // Anchors set to stretch horizontally and be at the bottom vertically.
        backgroundRect.anchorMin = new Vector2(0, 0);
        backgroundRect.anchorMax = new Vector2(1, 0);
        // Pivot at its own bottom-center. This means anchoredPosition.y refers to its bottom edge.
        backgroundRect.pivot = new Vector2(0.5f, 0);
         // sizeDelta.x = 0 means width is determined by anchors. Initial height.
        backgroundRect.sizeDelta = new Vector2(0, 100);
        // Initial position (centered horizontally, at screen bottom vertically).
        backgroundRect.anchoredPosition = new Vector2(0, 0);

        // 创建文本对象
        var textObj = new GameObject("JAT_Subtitle_SubtitleText");
        textObj.transform.SetParent(backgroundRect, false);
        TextComponent = textObj.AddComponent<Text>();

        // 设置文本位置和大小 (stretch within background)
        var textRect = TextComponent.rectTransform;
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        // Padding inside the background
        textRect.offsetMin = new Vector2(10, 10); // Left, Bottom padding
        textRect.offsetMax = new Vector2(-10, -10); // Right, Top padding (negative values)
        // textRect.sizeDelta = new Vector2(-40, -20); // Alternative to offsetMin/Max for padding
        textRect.anchoredPosition = new Vector2(0, 0);


        // 设置默认文本样式 (will be overridden by Config)
        TextComponent.alignment = TextAnchor.MiddleCenter;
        TextComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
        TextComponent.verticalOverflow = VerticalWrapMode.Overflow;

        // 设置文本和背景不拦截点击事件
        TextComponent.raycastTarget = false;
        BackgroundImage.raycastTarget = false;

        // 添加描边组件 (will be configured by ApplyConfig)
        Outline = TextComponent.gameObject.AddComponent<Outline>();
        Outline.enabled = false;

        LogManager.Debug("ScreenSubtitleComponent Subtitle UI created");
    }

    /// <summary>
    ///     应用字幕配置到UI元素，特别是屏幕位置和大小。
    /// </summary>
    protected override void ApplyConfig()
    {
        base.ApplyConfig(); // Apply common settings from BaseSubtitleComponent

        if (Config == null || BackgroundImage == null || CanvasScaler == null)
        {
            LogManager.Warning("Cannot apply screen subtitle config: Config, BackgroundImage, or CanvasScaler is null.");
            return;
        }

        var backgroundRect = BackgroundImage.rectTransform;
        // Use reference resolution for consistent scaling if CanvasScaler is set to ScaleWithScreenSize
        float refScreenWidth = CanvasScaler.referenceResolution.x;
        float refScreenHeight = CanvasScaler.referenceResolution.y;

        // --- Apply Background Width ---
        // The backgroundRect anchors are (0,0) to (1,0), meaning it spans the full width of its parent (Canvas) by default.
        // Config.BackgroundWidth (0-1) determines the percentage of this width to actually use.
        // We achieve this by setting horizontal offsets (padding).
        float horizontalPadding = (1f - Config.BackgroundWidth) * refScreenWidth / 2f;
        backgroundRect.offsetMin = new Vector2(horizontalPadding, backgroundRect.offsetMin.y); // Left padding
        backgroundRect.offsetMax = new Vector2(-horizontalPadding, backgroundRect.offsetMax.y); // Right padding
        // backgroundRect.sizeDelta.x remains 0, as width is controlled by anchors + offsets.
        // backgroundRect.anchoredPosition.x remains 0, as pivot is 0.5f and it's centered within the new effective width.

        // --- Apply Background Height ---
        // Config.BackgroundHeight is a normalized value (0-1) representing percentage of screen height.
        float subtitlePixelHeight = Config.BackgroundHeight * refScreenHeight;
        // Since pivot.y is 0 (bottom) and anchors are at the bottom, sizeDelta.y directly sets the height.
        backgroundRect.sizeDelta = new Vector2(backgroundRect.sizeDelta.x, subtitlePixelHeight);

        // --- Apply Vertical Position ---
        // Config.CurrentVerticalPosition is normalized (0=bottom of screen, 1=top of screen)
        // and represents the Y-coordinate of the subtitle's BOTTOM edge.
        // Since backgroundRect.pivot.y is 0 (bottom) and anchors are at the screen bottom,
        // anchoredPosition.y directly sets this bottom edge's position.
        float anchoredY = Config.CurrentVerticalPosition * refScreenHeight;
        backgroundRect.anchoredPosition = new Vector2(backgroundRect.anchoredPosition.x, anchoredY);

        // LogManager.Debug($"Applied ScreenSubtitle Config: ID={GetSubtitleId()}, Speaker={GetSpeakerName()}, Text='{GetText().Substring(0, Math.Min(GetText().Length,20))}', DesiredY={Config.VerticalPosition}, CurrentY={Config.CurrentVerticalPosition}, Height={Config.BackgroundHeight}, Width={Config.BackgroundWidth}");
    }
}