using UnityEngine;
using UnityEngine.UI;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle.Component;

/// <summary>
///     VR平板字幕组件，在 VR 模式中原有桌面模式 UI 会被放到一个悬浮平板电脑上
/// </summary>
public class VRTabletSubtitleComponent : BaseSubtitleComponent
{
    // OvrTabletScreen, set this via Unity Inspector or find dynamically
    private Transform screenTransform;

    // Public getter for the CanvasScaler from the base class
    public CanvasScaler TabletCanvasScaler => base.CanvasScaler;

    /// <summary>
    ///     创建字幕UI
    /// </summary>
    protected override void CreateSubtitleUI()
    {
        // 在 VR 模式中原有 UI 会被放到一个悬浮平板电脑上，见 OvrTablet
        var canvasObj = new GameObject("JAT_Subtitle_SubtitleCanvas_VR_Tablet");

        var ovrTablet = FindObjectOfType<OvrTablet>();
        if (ovrTablet != null) // Use is not null for Unity objects
        {
            var screenTransform = ovrTablet.transform.Find("Screen");
            if (screenTransform == null) // Use is null
            {
                LogManager.Warning(
                    "OvrTablet Screen not found, subtitles may not display correctly on VR tablet. Parenting to self.");
                canvasObj.transform.SetParent(transform, false);
            }
            else
            {
                canvasObj.transform.SetParent(screenTransform, false);
                canvasObj.layer = screenTransform.gameObject.layer;
            }
        }
        else
        {
            LogManager.Warning("OvrTablet not found. Parenting to self.");
            canvasObj.transform.SetParent(transform, false);
        }

        Canvas = canvasObj.AddComponent<Canvas>();
        Canvas.renderMode = RenderMode.WorldSpace; // Important for display on a 3D object like a tablet
        Canvas.overrideSorting = true;
        Canvas.sortingOrder = 32767;

        var canvasRectTransform = Canvas.GetComponent<RectTransform>();
        canvasRectTransform.localPosition = Vector3.zero;
        canvasRectTransform.localRotation = Quaternion.identity;
        // This scale makes UI elements designed at referenceResolution appear at a reasonable size in world space.
        canvasRectTransform.localScale = new Vector3(0.001f, 0.001f, 0.001f);

        // 添加画布缩放器
        CanvasScaler = Canvas.gameObject.AddComponent<CanvasScaler>();
        CanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        // This referenceResolution is the "virtual screen size" for this subtitle UI on the tablet.
        CanvasScaler.referenceResolution = new Vector2(Config.VRTabletReferenceResolutionX, Config.VRTabletReferenceResolutionY);
        CanvasScaler.matchWidthOrHeight = 0.5f; // Or 0 for width, 1 for height, depending on desired scaling

        // 创建背景面板
        var backgroundObj = new GameObject("JAT_Subtitle_SubtitleBackground");
        backgroundObj.transform.SetParent(Canvas.transform, false);
        BackgroundImage = backgroundObj.AddComponent<Image>();
        // BackgroundImage.color will be set by base.ApplyConfig()

        // 设置背景位置和大小 - Initial setup, will be refined by ApplyConfig
        var backgroundRect = BackgroundImage.rectTransform;
        // Anchors and pivot for bottom-center positioning, width/height set by sizeDelta
        backgroundRect.anchorMin = new Vector2(0.5f, 0f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0f);
        backgroundRect.pivot = new Vector2(0.5f, 0f);
        // Initial size - these values are in UI units of the referenceResolution
        // Config.BackgroundHeight and Config.BackgroundWidth (normalized) will be used in ApplyConfig
        // For now, set a placeholder or use defaults.
        float initialWidth = Config.BackgroundWidth * CanvasScaler.referenceResolution.x - 2 * Config.HorizontalPadding;
        backgroundRect.sizeDelta = new Vector2(initialWidth > 0 ? initialWidth : CanvasScaler.referenceResolution.x * 0.8f, Config.BackgroundHeight > 0 ? Config.BackgroundHeight : 100f);
        // Initial Y position based on Config.VerticalPosition (will become CurrentVerticalPosition in ApplyConfig)
        backgroundRect.anchoredPosition = new Vector2(0, Config.VerticalPosition * CanvasScaler.referenceResolution.y);


        // 创建文本对象
        var textObj = new GameObject("JAT_Subtitle_SubtitleText");
        textObj.transform.SetParent(backgroundRect, false);
        TextComponent = textObj.AddComponent<Text>();

        // 设置文本位置和大小 (stretch within background, with padding)
        var textRect = TextComponent.rectTransform;
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        // Padding will be applied by base.ApplyConfig() or here if needed
        float padding = Config.TextPadding;
        textRect.offsetMin = new Vector2(padding, padding);
        textRect.offsetMax = new Vector2(-padding, -padding);
        // TextComponent properties (alignment, color, font, etc.) will be set by base.ApplyConfig()

        // 设置文本和背景不拦截点击事件
        TextComponent.raycastTarget = false;
        BackgroundImage.raycastTarget = false;

        // 添加描边组件
        Outline = TextComponent.gameObject.AddComponent<Outline>();
        // Outline properties will be set by base.ApplyConfig()
        Outline.enabled = false;

        LogManager.Debug("VRTabletSubtitleComponent Subtitle UI created");
    }

    /// <summary>
    ///     应用配置到UI。覆盖基类方法以处理特定于平板字幕的定位和大小调整。
    /// </summary>
    protected override void ApplyConfig()
    {
        base.ApplyConfig(); // Apply common settings like text, font, colors

        if (Config == null)
        {
            LogManager.Warning("Config is null in VRTabletSubtitleComponent.ApplyConfig. Cannot apply settings.");
            return;
        }

        if (BackgroundImage == null || CanvasScaler == null)
        {
            LogManager.Warning("BackgroundImage or CanvasScaler is null in VRTabletSubtitleComponent.ApplyConfig.");
            return;
        }

        var backgroundRect = BackgroundImage.rectTransform;

        // Use referenceResolution as the effective "screen size" for positioning
        float effectiveCanvasHeight = CanvasScaler.referenceResolution.y;
        float effectiveCanvasWidth = CanvasScaler.referenceResolution.x;

        // Calculate subtitle dimensions based on Config settings
        // BackgroundHeight is in UI units.
        // BackgroundWidth is normalized (0-1), convert to UI units.
        float subtitleUiWidth = Config.BackgroundWidth * effectiveCanvasWidth;
        // Apply horizontal padding if it's meant to be inside the BackgroundWidth
        subtitleUiWidth -= 2 * Config.HorizontalPadding;
        if (subtitleUiWidth <= 0) subtitleUiWidth = effectiveCanvasWidth * 0.8f; // Fallback width

        float subtitleUiHeight = Config.BackgroundHeight;
        if (subtitleUiHeight <= 0) subtitleUiHeight = 100f; // Fallback height

        backgroundRect.sizeDelta = new Vector2(subtitleUiWidth, subtitleUiHeight);

        // Calculate anchored Y position. CurrentVerticalPosition is normalized (0=bottom, 1=top).
        // The pivot is at (0.5, 0), so anchoredPosition.y is the bottom edge of the subtitle.
        float anchoredY = Config.CurrentVerticalPosition * effectiveCanvasHeight;

        // Ensure anchors and pivot are set for bottom-center alignment relative to the parent Canvas.
        // The parent Canvas (JAT_Subtitle_SubtitleCanvas_VR_Tablet) is effectively the "screen" here.
        backgroundRect.anchorMin = new Vector2(0.5f, 0f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0f);
        backgroundRect.pivot = new Vector2(0.5f, 0f); // Pivot at bottom-center of the background image itself

        // Horizontal position is 0 because it's centered by anchors.
        // Vertical position is anchoredY.
        backgroundRect.anchoredPosition = new Vector2(0, anchoredY);

        // Text padding is handled by base.ApplyConfig() if TextRect is set up with offsets,
        // or could be explicitly set here if base.ApplyConfig() doesn't cover it.
        // Assuming TextRect is already set up with offsets (offsetMin/Max) in CreateSubtitleUI.

        LogManager.Debug($"Applied VR Tablet specific subtitle config to {gameObject.name}. AnchoredY: {anchoredY}, Size: {backgroundRect.sizeDelta}");
    }
}