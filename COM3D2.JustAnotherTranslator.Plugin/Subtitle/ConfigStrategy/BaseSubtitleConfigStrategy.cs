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
    protected Dictionary<string, Color> SpeakerColors = new();

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
        var textComponent = component.GetTextComponent();
        if (textComponent is null)
        {
            LogManager.Error("Subtitle text component is null/字幕文本组件为空");
            return;
        }

        if (string.IsNullOrEmpty(text))
            return;

        // 停止正在运行的动画
        component.StopCoroutine(component.StartFade(0, 0, 0));

        // 处理文本显示（包括说话者名称和颜色）
        SetText(component, text, speakerName);

        // 显示字幕
        component.SetActive(true);

        // 获取配置
        var config = component.GetConfig();

        // 动画处理
        if (config.EnableAnimation && config.FadeInDuration > 0)
        {
            // 从透明开始
            component.SetCanvasGroupAlpha(0);

            // 启动淡入动画
            component.StartFade(0, 1, config.FadeInDuration);

            // 如果设置了持续时间，设置自动隐藏
            if (duration > 0) component.StartCoroutine(AutoHide(component, duration));
        }
        else
        {
            // 不使用动画，直接显示
            component.SetCanvasGroupAlpha(1);

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
        var config = component.GetConfig();
        
        // 如果启用了动画效果，播放淡出动画
        if (config.EnableAnimation && config.FadeOutDuration > 0)
            // 启动淡出动画
            component.StartFade(
                component.GetCanvasGroupAlpha(), 
                0, 
                config.FadeOutDuration,
                () => component.SetActive(false)
            );
        else
            // 不使用动画，直接隐藏
            component.SetActive(false);
    }

    /// <summary>
    ///     更新字幕配置
    /// </summary>
    /// <param name="component">字幕组件</param>
    /// <param name="config">新的字幕配置</param>
    public virtual void UpdateConfig(SubtitleComponent component, SubtitleConfig config)
    {
        // 保存当前文本内容
        var currentText = component.GetText();
        var currentSpeakerName = component.GetSpeakerName();

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
        var config = component.GetConfig();
        
        // 设置文本内容
        if (config.EnableSpeakerName && !string.IsNullOrEmpty(speakerName))
        {
            // 获取说话者专属颜色
            var speakerColor = ColorUtility.ToHtmlStringRGB(GetSpeakerColor(speakerName));

            // 尝试翻译说话者名称
            var translatedSpeakerName = speakerName;
            if (TextTranslator.GetTranslateText(speakerName, out var translated)) translatedSpeakerName = translated;

            // 设置文本内容
            // 无需处理 XUAT 互操作以及翻译，因为获取时已经被翻译了
            component.SetText($"<color=#{speakerColor}>{translatedSpeakerName}</color>: {text}");
        }
        else
        {
            component.SetText(text);
        }

        // 动态调整背景大小以适应文本高度
        var textComponent = component.GetTextComponent();
        var backgroundRect = component.GetBackgroundRect();
        
        if (textComponent != null && !string.IsNullOrEmpty(textComponent.text) && backgroundRect != null)
        {
            // 获取文本内容的预计高度
            var textGenerator = textComponent.cachedTextGenerator;
            if (textGenerator.characterCountVisible == 0)
                textComponent.cachedTextGenerator.Populate(textComponent.text,
                    textComponent.GetGenerationSettings(textComponent.rectTransform.rect.size));

            // 计算实际行数与高度
            float textHeight = textGenerator.lineCount * textComponent.fontSize;

            // 只有当文本高度超过原始背景高度时才调整背景
            if (textHeight + 10 > config.BackgroundHeight)
                // 调整背景尺寸
                component.SetBackgroundSize(new Vector2(backgroundRect.sizeDelta.x, textHeight + 10)); // 文本高度 + 边距
        }
    }

    /// <summary>
    ///     自动隐藏字幕
    /// </summary>
    protected IEnumerator AutoHide(SubtitleComponent component, float delay)
    {
        var config = component.GetConfig();
        
        // 等待指定时间
        yield return new WaitForSeconds(delay);

        // 如果启用了动画效果，播放淡出动画
        if (config.EnableAnimation && config.FadeOutDuration > 0)
        {
            // 启动淡出动画
            component.StartFade(1, 0, config.FadeOutDuration, () => component.SetActive(false));
        }
        else
        {
            // 不使用动画，直接隐藏
            component.SetActive(false);
        }
    }

    /// <summary>
    ///     获取说话者的专属颜色
    /// </summary>
    protected Color GetSpeakerColor(string speakerName)
    {
        if (string.IsNullOrEmpty(speakerName))
            return Color.white;

        // 如果已经有颜色，直接返回
        if (SpeakerColors.TryGetValue(speakerName, out var color))
            return color;

        // 为说话者生成一个固定的颜色（基于名称的哈希值，确保同一角色总是获得相同颜色）
        var random = new Random(speakerName.GetHashCode());
        // 生成明亮的颜色，避免太深或太浅
        var h = (float)random.NextDouble(); // 色相
        var s = 0.7f + (float)random.NextDouble() * 0.3f; // 饱和度（0.7-1.0，保证鲜艳）
        var v = 0.8f + (float)random.NextDouble() * 0.2f; // 亮度（0.8-1.0，保证明亮）

        // 转换为RGB
        Color newColor = Color.HSVToRGB(h, s, v);

        // 存储并返回
        SpeakerColors[speakerName] = newColor;
        return newColor;
    }
}