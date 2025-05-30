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
    public bool OutlineEnabled { get; set; } = false;

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
    public float VRSubtitleHorizontalOffset { get; set; } = 0f;

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
}