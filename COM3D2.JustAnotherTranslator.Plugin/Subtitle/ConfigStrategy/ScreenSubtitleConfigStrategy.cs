using System;
using COM3D2.JustAnotherTranslator.Plugin.Translator;
using UnityEngine;
using UnityEngine.UI;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle.ConfigStrategy;

/// <summary>
///     屏幕空间字幕配置策略
/// </summary>
public class ScreenSubtitleConfigStrategy : BaseSubtitleConfigStrategy
{
    /// <summary>
    ///     创建字幕UI元素
    /// </summary>
    /// <param name="component">字幕组件</param>
    public override void CreateSubtitleUI(SubtitleComponent component)
    {
        LogManager.Debug("Creating screen space subtitle UI");

        // 创建画布
        var canvasObj = new GameObject("Subtitle_JAT_SubtitleCanvas");
        canvasObj.transform.SetParent(component.transform, false);
        
        // 添加Canvas组件
        var canvas = canvasObj.AddComponent<Canvas>();
        component.SetCanvas(canvas);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767; // 确保在最上层显示

        // 添加画布缩放器
        var canvasScaler = canvas.gameObject.AddComponent<CanvasScaler>();
        component.SetCanvasScaler(canvasScaler);
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.matchWidthOrHeight = 0.5f;

        // 添加CanvasGroup用于控制透明度
        var canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
        component.SetCanvasGroup(canvasGroup);

        // 创建背景面板
        var backgroundObj = new GameObject("Subtitle_JAT_SubtitleBackground");
        backgroundObj.transform.SetParent(canvas.transform, false);
        var backgroundImage = backgroundObj.AddComponent<Image>();
        component.SetBackgroundImage(backgroundImage);
        backgroundImage.color = new Color(0, 0, 0, 0.5f); // 半透明黑色背景

        // 设置背景位置和大小
        var backgroundRect = backgroundImage.rectTransform;
        component.SetBackgroundRect(backgroundRect);
        backgroundRect.anchorMin = new Vector2(0, 0);
        backgroundRect.anchorMax = new Vector2(1, 0);
        backgroundRect.pivot = new Vector2(0.5f, 0);
        backgroundRect.sizeDelta = new Vector2(0, 100);
        backgroundRect.anchoredPosition = new Vector2(0, 0);

        // 创建文本对象
        var textObj = new GameObject("Subtitle_JAT_SubtitleText");
        textObj.transform.SetParent(backgroundRect, false);
        var text = textObj.AddComponent<Text>();
        component.SetTextComponent(text);

        // 设置文本位置和大小
        var textRect = text.rectTransform;
        component.SetTextRect(textRect);
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(-40, -20); // 左右上下各留出10像素的边距
        textRect.anchoredPosition = new Vector2(0, 0);

        // 设置默认文本样式
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        // 设置文本和背景不拦截点击事件
        text.raycastTarget = false;
        backgroundImage.raycastTarget = false;

        // 添加描边组件
        var outline = text.gameObject.AddComponent<Outline>();
        component.SetOutline(outline);
        outline.enabled = false;

        LogManager.Debug("Screen space subtitle UI created/字幕UI已创建");
    }

    /// <summary>
    ///     处理字幕显示
    /// </summary>
    /// <param name="component">字幕组件</param>
    /// <param name="text">要显示的文本</param>
    /// <param name="speakerName">说话人名称</param>
    /// <param name="duration">显示持续时间，0 为一直显示</param>
    public override void HandleShowSubtitle(SubtitleComponent component, string text, string speakerName,
        float duration)
    {
        base.HandleShowSubtitle(component, text, speakerName, duration);
        LogManager.Debug($"Screen subtitle shown: {text}");
    }

    /// <summary>
    ///     应用字幕配置
    /// </summary>
    /// <param name="component">字幕组件</param>
    public override void ApplyConfig(SubtitleComponent component)
    {
        var config = component.GetConfig();
        if (config is null)
        {
            LogManager.Warning("Cannot apply config, subtitle config is null");
            return;
        }

        try
        {
            // 设置Canvas缩放器参数
            var canvasScaler = component.GetCanvasScaler();
            canvasScaler.referenceResolution = new Vector2(
                config.ReferenceWidth,
                config.ReferenceHeight);
            canvasScaler.matchWidthOrHeight = config.MatchWidthOrHeight;

            // 加载字体
            var font = Resources.GetBuiltinResource<Font>(config.FontName);
            if (font is null)
            {
                LogManager.Warning($"Font not found: {config.FontName}, using Arial instead");
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            // 设置文本样式
            var text = component.GetTextComponent();
            text.font = font;
            text.fontSize = config.FontSize;
            text.color = config.TextColor;
            text.alignment = config.TextAlignment;

            // 设置背景样式
            var bgColor = config.BackgroundColor;
            bgColor.a = config.BackgroundOpacity; // 设置不透明度
            var backgroundImage = component.GetBackgroundImage();
            backgroundImage.color = bgColor;

            // 设置描边
            var outline = component.GetOutline();
            outline.enabled = config.OutlineEnabled;
            outline.effectColor = config.OutlineColor;
            outline.effectDistance = new Vector2(
                config.OutlineWidth,
                config.OutlineWidth);

            // 设置容器位置
            var verticalPosition = config.VerticalPosition;
            var backgroundRect = component.GetBackgroundRect();
            backgroundRect.anchoredPosition = new Vector2(
                0,
                Screen.height * verticalPosition);

            // 设置背景高度
            backgroundRect.sizeDelta = new Vector2(
                0,
                config.BackgroundHeight);

            LogManager.Debug("Screen space subtitle config applied");
        }
        catch (Exception ex)
        {
            LogManager.Error($"Failed to apply subtitle config: {ex.Message}");
        }
    }

    /// <summary>
    ///     更新字幕UI配置
    /// </summary>
    /// <param name="component">字幕组件</param>
    /// <param name="currentText">当前文本</param>
    /// <param name="currentSpeakerName">当前说话者名称</param>
    public override void UpdateUI(SubtitleComponent component, string currentText, string currentSpeakerName)
    {
        // 更新配置
        ApplyConfig(component);

        // 更新字幕文本
        if (!string.IsNullOrEmpty(currentText))
            SetText(component, currentText, currentSpeakerName);

        LogManager.Debug("Screen space subtitle UI updated");
    }

    /// <summary>
    ///     设置文本内容，处理说话者名称和颜色
    /// </summary>
    protected override void SetText(SubtitleComponent component, string text, string speakerName)
    {
        // 设置文本内容
        if (component.GetConfig().EnableSpeakerName && !string.IsNullOrEmpty(speakerName))
        {
            // 获取说话者专属颜色
            var speakerColor = ColorUtility.ToHtmlStringRGB(GetSpeakerColor(speakerName));

            // 尝试翻译说话者名称
            var translatedSpeakerName = speakerName;
            if (TextTranslator.GetTranslateText(speakerName, out var translated)) translatedSpeakerName = translated;

            var textComponent = component.GetTextComponent();
            textComponent.text = $"<color=#{speakerColor}>{translatedSpeakerName}</color>: {text}";
        }
        else
        {
            var textComponent = component.GetTextComponent();
            textComponent.text = text;
        }

        // 动态调整背景大小以适应文本高度
        if (!string.IsNullOrEmpty(component.GetTextComponent().text) && component.GetBackgroundRect() != null)
        {
            // 获取文本内容的预计高度
            var textGenerator = component.GetTextComponent().cachedTextGenerator;
            if (textGenerator.characterCountVisible == 0)
                component.GetTextComponent().cachedTextGenerator.Populate(component.GetTextComponent().text,
                    component.GetTextComponent().GetGenerationSettings(component.GetTextComponent().rectTransform.rect.size));

            // 计算实际行数与高度
            float textHeight = textGenerator.lineCount * component.GetTextComponent().fontSize;

            // 只有当文本高度超过原始背景高度时才调整背景
            if (textHeight + 10 > component.GetConfig().BackgroundHeight)
                // 调整背景尺寸
                component.GetBackgroundRect().sizeDelta =
                    new Vector2(component.GetBackgroundRect().sizeDelta.x, textHeight + 10); // 文本高度 + 边距
        }
    }
}