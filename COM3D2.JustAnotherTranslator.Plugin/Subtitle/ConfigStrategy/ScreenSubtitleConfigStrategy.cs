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
        component._canvas = canvasObj.AddComponent<Canvas>();
        component._canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        component._canvas.sortingOrder = 32767; // 确保在最上层显示

        // 添加画布缩放器
        component._canvasScaler = component._canvas.gameObject.AddComponent<CanvasScaler>();
        component._canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        component._canvasScaler.referenceResolution = new Vector2(1920, 1080);
        component._canvasScaler.matchWidthOrHeight = 0.5f;

        // 添加CanvasGroup用于控制透明度
        component._canvasGroup = component._canvas.gameObject.AddComponent<CanvasGroup>();

        // 创建背景面板
        var backgroundObj = new GameObject("Subtitle_JAT_SubtitleBackground");
        backgroundObj.transform.SetParent(component._canvas.transform, false);
        component._backgroundImage = backgroundObj.AddComponent<Image>();
        component._backgroundImage.color = new Color(0, 0, 0, 0.5f); // 半透明黑色背景

        // 设置背景位置和大小
        component._backgroundRect = component._backgroundImage.rectTransform;
        component._backgroundRect.anchorMin = new Vector2(0, 0);
        component._backgroundRect.anchorMax = new Vector2(1, 0);
        component._backgroundRect.pivot = new Vector2(0.5f, 0);
        component._backgroundRect.sizeDelta = new Vector2(0, 100);
        component._backgroundRect.anchoredPosition = new Vector2(0, 0);

        // 创建文本对象
        var textObj = new GameObject("Subtitle_JAT_SubtitleText");
        textObj.transform.SetParent(component._backgroundRect, false);
        component._text = textObj.AddComponent<Text>();

        // 设置文本位置和大小
        component._textRect = component._text.rectTransform;
        component._textRect.anchorMin = new Vector2(0, 0);
        component._textRect.anchorMax = new Vector2(1, 1);
        component._textRect.pivot = new Vector2(0.5f, 0.5f);
        component._textRect.sizeDelta = new Vector2(-40, -20); // 左右上下各留出10像素的边距
        component._textRect.anchoredPosition = new Vector2(0, 0);

        // 设置默认文本样式
        component._text.alignment = TextAnchor.MiddleCenter;
        component._text.horizontalOverflow = HorizontalWrapMode.Wrap;
        component._text.verticalOverflow = VerticalWrapMode.Overflow;

        // 设置文本和背景不拦截点击事件
        component._text.raycastTarget = false;
        component._backgroundImage.raycastTarget = false;

        // 添加描边组件
        component._outline = component._text.gameObject.AddComponent<Outline>();
        component._outline.enabled = false;

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
    ///     设置文本内容，处理说话者名称和颜色
    /// </summary>
    protected override void SetText(SubtitleComponent component, string text, string speakerName)
    {
        // 设置文本内容
        if (component._config.EnableSpeakerName && !string.IsNullOrEmpty(speakerName))
        {
            // 获取说话者专属颜色
            var speakerColor = ColorUtility.ToHtmlStringRGB(GetSpeakerColor(speakerName));

            // 尝试翻译说话者名称
            var translatedSpeakerName = speakerName;
            if (TextTranslator.GetTranslateText(speakerName, out var translated)) translatedSpeakerName = translated;

            component._text.text = $"<color=#{speakerColor}>{translatedSpeakerName}</color>: {text}";
        }
        else
        {
            component._text.text = text;
        }

        // 动态调整背景大小以适应文本高度
        if (!string.IsNullOrEmpty(component._text.text) && component._backgroundRect != null)
        {
            // 获取文本内容的预计高度
            var textGenerator = component._text.cachedTextGenerator;
            if (textGenerator.characterCountVisible == 0)
                component._text.cachedTextGenerator.Populate(component._text.text,
                    component._text.GetGenerationSettings(component._text.rectTransform.rect.size));

            // 计算实际行数与高度
            float textHeight = textGenerator.lineCount * component._text.fontSize;

            // 只有当文本高度超过原始背景高度时才调整背景
            if (textHeight + 10 > component._config.BackgroundHeight)
                // 调整背景尺寸
                component._backgroundRect.sizeDelta =
                    new Vector2(component._backgroundRect.sizeDelta.x, textHeight + 10); // 文本高度 + 边距
        }
    }


    /// <summary>
    ///     应用字幕配置
    /// </summary>
    /// <param name="component">字幕组件</param>
    public override void ApplyConfig(SubtitleComponent component)
    {
        if (component._config is null)
        {
            LogManager.Warning("Cannot apply config, subtitle config is null");
            return;
        }

        try
        {
            // 设置Canvas缩放器参数
            component._canvasScaler.referenceResolution = new Vector2(
                component._config.ReferenceWidth,
                component._config.ReferenceHeight);
            component._canvasScaler.matchWidthOrHeight = component._config.MatchWidthOrHeight;

            // 加载字体
            var font = Resources.GetBuiltinResource<Font>(component._config.FontName);
            if (font is null)
            {
                LogManager.Warning($"Font not found: {component._config.FontName}, using Arial instead");
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            // 设置文本样式
            component._text.font = font;
            component._text.fontSize = component._config.FontSize;
            component._text.color = component._config.TextColor;
            component._text.alignment = component._config.TextAlignment;

            // 设置背景样式
            var bgColor = component._config.BackgroundColor;
            bgColor.a = component._config.BackgroundOpacity; // 设置不透明度
            component._backgroundImage.color = bgColor;

            // 设置描边
            component._outline.enabled = component._config.OutlineEnabled;
            component._outline.effectColor = component._config.OutlineColor;
            component._outline.effectDistance = new Vector2(
                component._config.OutlineWidth,
                component._config.OutlineWidth);

            // 设置容器位置
            var verticalPosition = component._config.VerticalPosition;
            component._backgroundRect.anchoredPosition = new Vector2(
                0,
                Screen.height * verticalPosition);

            // 设置背景高度
            component._backgroundRect.sizeDelta = new Vector2(
                0,
                component._config.BackgroundHeight);

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
        // 应用新配置
        ApplyConfig(component);

        // 如果当前有显示文本，则使用新配置重新显示
        if (!string.IsNullOrEmpty(currentText))
            HandleShowSubtitle(component, currentText, currentSpeakerName, 0);
    }

    /// <summary>
    ///     更新字幕配置
    /// </summary>
    /// <param name="component">字幕组件</param>
    /// <param name="config">新的字幕配置</param>
    public override void UpdateConfig(SubtitleComponent component, SubtitleConfig config)
    {
        // 调用基类方法
        base.UpdateConfig(component, config);

        LogManager.Debug("Screen subtitle config updated");
    }
}