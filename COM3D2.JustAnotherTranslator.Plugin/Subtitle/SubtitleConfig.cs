using System;
using UnityEngine;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle;

/// <summary>
///     字幕配置类
/// </summary>
[Serializable]
public class SubtitleConfig
{
    // 是否启用说话人名字显示
    public bool EnableSpeakerName = true;

    // 字幕类型
    public JustAnotherTranslator.SubtitleTypeEnum SubtitleType { get; set; } =
        JustAnotherTranslator.SubtitleTypeEnum.Base;

    // 字体名称 (Unity内置字体资源路径)
    public string FontName { get; set; } = "Arial.ttf";

    // 字体大小
    public int FontSize { get; set; } = 24;

    // 文本对齐方式
    public TextAnchor TextAlignment { get; set; } = TextAnchor.MiddleCenter;

    // 文本颜色
    public Color TextColor { get; set; } = Color.white;

    // 背景颜色
    public Color BackgroundColor { get; set; } = new(0, 0, 0);

    // 背景不透明度（0-1）
    public float BackgroundOpacity { get; set; } = 0.5f;

    // 垂直位置（0-1，0表示底部，1表示顶部）
    public float VerticalPosition { get; set; } = 0.1f;

    // 背景高度
    public float BackgroundHeight { get; set; } = 100;

    // 是否启用动画效果
    public bool EnableAnimation { get; set; } = true;

    // 淡入时长（秒）
    public float FadeInDuration { get; set; } = 0.5f;

    // 淡出时长（秒）
    public float FadeOutDuration { get; set; } = 0.5f;

    // 是否启用描边
    public bool OutlineEnabled { get; set; }

    // 描边颜色
    public Color OutlineColor { get; set; } = Color.black;

    // 描边粗细
    public float OutlineWidth { get; set; } = 1f;

    // VR模式字幕类型
    public JustAnotherTranslator.VRSubtitleModeEnum VRSubtitleMode { get; set; } =
        JustAnotherTranslator.VRSubtitleModeEnum.InSpace;

    // VR悬浮字幕距离（米）
    public float VRSubtitleDistance { get; set; } = 2f;

    // VR悬浮字幕垂直偏移（度，相对于视线中心）
    public float VRSubtitleVerticalOffset { get; set; } = -15f;

    // VR悬浮字幕水平偏移（度，相对于视线中心）
    public float VRSubtitleHorizontalOffset { get; set; }

    // VR悬浮字幕宽度（米）
    public float VRSubtitleWidth { get; set; } = 1f;

    // VR悬浮字幕缩放比例
    public float VRSubtitleScale { get; set; } = 1f;

    // 画布参考宽度
    public float ReferenceWidth { get; set; } = 1920f;

    // 画布参考高度
    public float ReferenceHeight { get; set; } = 1080f;

    // 画布匹配模式（0为宽度优先，1为高度优先，0.5为平衡）
    public float MatchWidthOrHeight { get; set; } = 0.5f;

    /// <summary>
    ///     创建默认配置
    /// </summary>
    /// <returns>默认字幕配置</returns>
    public static SubtitleConfig CreateDefault()
    {
        return new SubtitleConfig();
    }

    /// <summary>
    ///     创建指定类型的默认配置
    /// </summary>
    /// <param name="type">字幕类型</param>
    /// <returns>特定类型的默认字幕配置</returns>
    public static SubtitleConfig CreateDefault(JustAnotherTranslator.SubtitleTypeEnum type)
    {
        var config = new SubtitleConfig { SubtitleType = type };

        // 根据类型调整默认配置
        switch (type)
        {
            case JustAnotherTranslator.SubtitleTypeEnum.Base:
                config.VerticalPosition = 0.8f;
                break;
            case JustAnotherTranslator.SubtitleTypeEnum.Adv:
                config.VerticalPosition = 0.1f;
                break;
            case JustAnotherTranslator.SubtitleTypeEnum.Yotogi:
                config.VerticalPosition = 0.5f;
                break;
            case JustAnotherTranslator.SubtitleTypeEnum.Lyric:
                config.VerticalPosition = 0.9f;
                break;
        }

        return config;
    }

    /// <summary>
    ///     克隆当前配置
    /// </summary>
    /// <returns>当前配置的深度副本</returns>
    public SubtitleConfig Clone()
    {
        // 创建配置副本
        var clone = new SubtitleConfig
        {
            // 基本属性
            SubtitleType = SubtitleType,
            EnableSpeakerName = EnableSpeakerName,

            // 文本样式
            FontName = FontName,
            FontSize = FontSize,
            TextColor = TextColor,
            TextAlignment = TextAlignment,

            // 背景样式
            BackgroundColor = BackgroundColor,
            BackgroundOpacity = BackgroundOpacity,
            BackgroundHeight = BackgroundHeight,
            VerticalPosition = VerticalPosition,

            // 动画
            EnableAnimation = EnableAnimation,
            FadeInDuration = FadeInDuration,
            FadeOutDuration = FadeOutDuration,

            // 描边
            OutlineEnabled = OutlineEnabled,
            OutlineColor = OutlineColor,
            OutlineWidth = OutlineWidth,

            // VR相关
            VRSubtitleMode = VRSubtitleMode,
            VRSubtitleDistance = VRSubtitleDistance,
            VRSubtitleVerticalOffset = VRSubtitleVerticalOffset,
            VRSubtitleHorizontalOffset = VRSubtitleHorizontalOffset,
            VRSubtitleWidth = VRSubtitleWidth,
            VRSubtitleScale = VRSubtitleScale,

            // 参考分辨率
            ReferenceWidth = ReferenceWidth,
            ReferenceHeight = ReferenceHeight,
            MatchWidthOrHeight = MatchWidthOrHeight
        };

        return clone;
    }
}