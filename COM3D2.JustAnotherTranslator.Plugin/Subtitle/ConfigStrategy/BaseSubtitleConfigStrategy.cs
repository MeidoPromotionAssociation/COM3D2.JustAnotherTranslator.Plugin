using System.Collections;
using System.Collections.Generic;
using COM3D2.JustAnotherTranslator.Plugin.Translator;
using UnityEngine;
using Random = System.Random;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle.ConfigStrategy;

/// <summary>
///     字幕配置策略的抽象基类，包含通用实现
/// </summary>
public abstract class BaseSubtitleConfigStrategy : ISubtitleConfigStrategy
{
    internal Dictionary<string, Color> SpeakerColors = new();

    /// <summary>
    ///     创建字幕UI
    /// </summary>
    /// <param name="component">字幕组件</param>
    public abstract void CreateSubtitleUI(SubtitleComponent component);

    /// <summary>
    ///     应用字幕配置
    /// </summary>
    /// <param name="component">字幕组件</param>
    public abstract void ApplyConfig(SubtitleComponent component);

    /// <summary>
    ///     处理字幕显示
    /// </summary>
    /// <param name="component">字幕组件</param>
    /// <param name="text">要显示的文本</param>
    /// <param name="speakerName">说话人名称</param>
    /// <param name="duration">显示持续时间，0 为一直显示</param>
    public virtual void HandleShowSubtitle(SubtitleComponent component, string text, string speakerName, float duration)
    {
        if (component._text is null)
        {
            LogManager.Error("Subtitle text component is null/字幕文本组件为空");
            return;
        }

        if (string.IsNullOrEmpty(text))
            return;

        component._speakerName = speakerName;

        // 停止正在运行的动画
        if (component._animationCoroutine != null)
        {
            component.StopCoroutine(component._animationCoroutine);
            component._animationCoroutine = null;
        }

        // 处理文本显示（包括说话者名称和颜色）
        SetText(component, text, speakerName);

        // 显示字幕
        component.gameObject.SetActive(true);

        // 动画处理
        if (component._config.EnableAnimation && component._config.FadeInDuration > 0)
        {
            // 从透明开始
            component._canvasGroup.alpha = 0;

            // 启动淡入动画
            component._animationCoroutine = component.StartCoroutine(
                component.FadeCanvasAlpha(0, 1, component._config.FadeInDuration));

            // 如果设置了持续时间，设置自动隐藏
            if (duration > 0) component.StartCoroutine(AutoHide(component, duration));
        }
        else
        {
            // 不使用动画，直接显示
            component._canvasGroup.alpha = 1;

            // 如果设置了持续时间，设置自动隐藏
            if (duration > 0) component.StartCoroutine(AutoHide(component, duration));
        }

        // 子类可能需要额外处理，所以这里不记录日志
    }

    /// <summary>
    ///     处理字幕隐藏
    /// </summary>
    /// <param name="component">字幕组件</param>
    public virtual void HandleHideSubtitle(SubtitleComponent component)
    {
        // 停止正在运行的动画
        if (component._animationCoroutine != null)
        {
            component.StopCoroutine(component._animationCoroutine);
            component._animationCoroutine = null;
        }

        // 如果启用了动画效果，播放淡出动画
        if (component._config.EnableAnimation && component._config.FadeOutDuration > 0)
            // 启动淡出动画
            component._animationCoroutine = component.StartCoroutine(
                component.FadeCanvasAlpha(component._canvasGroup.alpha, 0, component._config.FadeOutDuration,
                    () => component.gameObject.SetActive(false)));
        else
            // 不使用动画，直接隐藏
            component.gameObject.SetActive(false);

        // 清除说话人名称
        if (component._speakerName != null) component._speakerName = string.Empty;
    }

    /// <summary>
    ///     更新字幕配置
    /// </summary>
    /// <param name="component">字幕组件</param>
    /// <param name="config">新的字幕配置</param>
    public virtual void UpdateConfig(SubtitleComponent component, SubtitleConfig config)
    {
        // 保存当前文本内容
        var currentText = component._text?.text ?? string.Empty;
        var currentSpeakerName = component._speakerName ?? string.Empty;

        // 更新配置
        component._config = config;

        // 更新UI
        UpdateUI(component, currentText, currentSpeakerName);
    }

    /// <summary>
    ///     更新字幕UI配置
    /// </summary>
    /// <param name="component">字幕组件</param>
    /// <param name="currentText">当前文本</param>
    /// <param name="currentSpeakerName">当前说话者名称</param>
    public abstract void UpdateUI(SubtitleComponent component, string currentText, string currentSpeakerName);

    /// <summary>
    ///     设置文本内容，处理说话者名称和颜色
    /// </summary>
    protected virtual void SetText(SubtitleComponent component, string text, string speakerName)
    {
        // 设置文本内容
        if (component._config.EnableSpeakerName && !string.IsNullOrEmpty(speakerName))
        {
            // 获取说话者专属颜色
            var speakerColor = ColorUtility.ToHtmlStringRGB(GetSpeakerColor(speakerName));

            // 尝试翻译说话者名称
            var translatedSpeakerName = speakerName;
            if (TextTranslator.GetTranslateText(speakerName, out var translated)) translatedSpeakerName = translated;

            // 设置文本内容
            // 无需处理 XUAT 互操作以及翻译，因为获取时已经被翻译了
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
    ///     自动隐藏字幕
    /// </summary>
    protected IEnumerator AutoHide(SubtitleComponent component, float delay)
    {
        // 等待指定时间
        yield return new WaitForSeconds(delay);

        // 如果启用了动画效果，播放淡出动画
        if (component._config.EnableAnimation && component._config.FadeOutDuration > 0)
        {
            // 启动淡出动画
            if (component._animationCoroutine != null) component.StopCoroutine(component._animationCoroutine);

            component._animationCoroutine = component.StartCoroutine(
                component.FadeCanvasAlpha(1, 0, component._config.FadeOutDuration,
                    () => component.gameObject.SetActive(false)));
        }
        else
        {
            // 不使用动画，直接隐藏
            component.gameObject.SetActive(false);
        }
    }

    /// <summary>
    ///     获取说话者专属颜色
    /// </summary>
    protected virtual Color GetSpeakerColor(string speakerName)
    {
        if (string.IsNullOrEmpty(speakerName))
            return new Color(1f, 0.6f, 0.2f); // 默认橙色

        if (SpeakerColors.TryGetValue(speakerName, out var color))
            return color;

        // 为说话者生成一个唯一的颜色
        var random = new Random(speakerName.GetHashCode());
        color = new Color(
            0.5f + (float)random.NextDouble() * 0.5f, // 0.5-1.0 生成较亮的颜色
            0.5f + (float)random.NextDouble() * 0.5f,
            0.5f + (float)random.NextDouble() * 0.5f
        );

        SpeakerColors[speakerName] = color;
        return color;
    }
}