using System;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using UnityEngine;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle;

/// <summary>
///     字幕配置类
/// </summary>
[Serializable]
public class SubtitleConfig
{
    /// <summary>
    ///     从插件配置中创建字幕配置
    /// </summary>
    public static SubtitleConfig CreateSubtitleConfig(JustAnotherTranslator.SubtitleTypeEnum subtitleType)
    {
        // 初始化字幕配置
        var config = new SubtitleConfig
        {
            // 字幕类型
            SubtitleType = subtitleType,

            // 是否启用说话人名称
            EnableSpeakerName = GetSubtitleTypeConfig(
                subtitleType,
                () => JustAnotherTranslator.EnableBaseSubtitleSpeakerName.Value,
                () => JustAnotherTranslator.EnableYotogiSubtitleSpeakerName.Value,
                () => JustAnotherTranslator.EnableAdvSubtitleSpeakerName.Value,
                () => JustAnotherTranslator.EnableLyricSubtitleSpeakerName.Value,
                false),

            // 字体
            Font = GetSubtitleTypeConfig(
                subtitleType,
                () => GetFontByName(JustAnotherTranslator.BaseSubtitleFont.Value,
                    JustAnotherTranslator.BaseSubtitleFontSize.Value),
                () => GetFontByName(JustAnotherTranslator.YotogiSubtitleFont.Value,
                    JustAnotherTranslator.YotogiSubtitleFontSize.Value),
                () => GetFontByName(JustAnotherTranslator.AdvSubtitleFont.Value,
                    JustAnotherTranslator.AdvSubtitleFontSize.Value),
                () => GetFontByName(JustAnotherTranslator.LyricSubtitleFont.Value,
                    JustAnotherTranslator.LyricSubtitleFontSize.Value),
                null),

            // 字体大小
            FontSize = GetSubtitleTypeConfig(
                subtitleType,
                () => JustAnotherTranslator.BaseSubtitleFontSize.Value,
                () => JustAnotherTranslator.YotogiSubtitleFontSize.Value,
                () => JustAnotherTranslator.AdvSubtitleFontSize.Value,
                () => JustAnotherTranslator.LyricSubtitleFontSize.Value,
                24),

            // 文本对齐方式
            TextAlignment = GetSubtitleTypeConfig(
                subtitleType,
                () => ConvertTextAnchorEnum(JustAnotherTranslator.BaseSubtitleTextAlignment.Value),
                () => ConvertTextAnchorEnum(JustAnotherTranslator.YotogiSubtitleTextAlignment.Value),
                () => ConvertTextAnchorEnum(JustAnotherTranslator.AdvSubtitleTextAlignment.Value),
                () => ConvertTextAnchorEnum(JustAnotherTranslator.LyricSubtitleTextAlignment.Value),
                ConvertTextAnchorEnum(JustAnotherTranslator.TextAnchorEnum.MiddleCenter)),

            // 文本颜色
            TextColor = GetSubtitleTypeConfig(
                subtitleType,
                () => ParseColor(JustAnotherTranslator.BaseSubtitleColor.Value,
                    JustAnotherTranslator.BaseSubtitleOpacity.Value),
                () => ParseColor(JustAnotherTranslator.YotogiSubtitleColor.Value,
                    JustAnotherTranslator.YotogiSubtitleOpacity.Value),
                () => ParseColor(JustAnotherTranslator.AdvSubtitleColor.Value,
                    JustAnotherTranslator.AdvSubtitleOpacity.Value),
                () => ParseColor(JustAnotherTranslator.LyricSubtitleColor.Value,
                    JustAnotherTranslator.LyricSubtitleOpacity.Value),
                Color.white),

            // 背景颜色
            BackgroundColor = GetSubtitleTypeConfig(
                subtitleType,
                () => ParseColor(JustAnotherTranslator.BaseSubtitleBackgroundColor.Value,
                    JustAnotherTranslator.BaseSubtitleBackgroundOpacity.Value),
                () => ParseColor(JustAnotherTranslator.YotogiSubtitleBackgroundColor.Value,
                    JustAnotherTranslator.YotogiSubtitleBackgroundOpacity.Value),
                () => ParseColor(JustAnotherTranslator.AdvSubtitleBackgroundColor.Value,
                    JustAnotherTranslator.AdvSubtitleBackgroundOpacity.Value),
                () => ParseColor(JustAnotherTranslator.LyricSubtitleBackgroundColor.Value,
                    JustAnotherTranslator.LyricSubtitleBackgroundOpacity.Value),
                new Color(0, 0, 0, 0.5f)),

            // 是否启用描边
            EnableOutline = GetSubtitleTypeConfig(
                subtitleType,
                () => JustAnotherTranslator.EnableBaseSubtitleOutline.Value,
                () => JustAnotherTranslator.EnableYotogiSubtitleOutline.Value,
                () => JustAnotherTranslator.EnableAdvSubtitleOutline.Value,
                () => JustAnotherTranslator.EnableLyricSubtitleOutline.Value,
                false),

            // 描边颜色
            OutlineColor = GetSubtitleTypeConfig(
                subtitleType,
                () => ParseColor(JustAnotherTranslator.BaseSubtitleOutlineColor.Value,
                    JustAnotherTranslator.BaseSubtitleOutlineOpacity.Value),
                () => ParseColor(JustAnotherTranslator.YotogiSubtitleOutlineColor.Value,
                    JustAnotherTranslator.YotogiSubtitleOutlineOpacity.Value),
                () => ParseColor(JustAnotherTranslator.AdvSubtitleOutlineColor.Value,
                    JustAnotherTranslator.AdvSubtitleOutlineOpacity.Value),
                () => ParseColor(JustAnotherTranslator.LyricSubtitleOutlineColor.Value,
                    JustAnotherTranslator.LyricSubtitleOutlineOpacity.Value),
                Color.black),

            // 描边粗细
            OutlineWidth = GetSubtitleTypeConfig(
                subtitleType,
                () => JustAnotherTranslator.BaseSubtitleOutlineWidth.Value,
                () => JustAnotherTranslator.YotogiSubtitleOutlineWidth.Value,
                () => JustAnotherTranslator.AdvSubtitleOutlineWidth.Value,
                () => JustAnotherTranslator.LyricSubtitleOutlineWidth.Value,
                1f),

            // 水平位置
            HorizontalPosition = GetSubtitleTypeConfig(
                subtitleType,
                () => JustAnotherTranslator.BaseSubtitleHorizontalPosition.Value,
                () => JustAnotherTranslator.YotogiSubtitleHorizontalPosition.Value,
                () => JustAnotherTranslator.AdvSubtitleHorizontalPosition.Value,
                () => JustAnotherTranslator.LyricSubtitleHorizontalPosition.Value,
                0f),

            // 垂直位置
            VerticalPosition = GetSubtitleTypeConfig(
                subtitleType,
                () => JustAnotherTranslator.BaseSubtitleVerticalPosition.Value,
                () => JustAnotherTranslator.YotogiSubtitleVerticalPosition.Value,
                () => JustAnotherTranslator.AdvSubtitleVerticalPosition.Value,
                () => JustAnotherTranslator.LyricSubtitleVerticalPosition.Value,
                1f),

            // 字幕宽度
            SubtitleWidth = GetSubtitleTypeConfig(
                subtitleType,
                () => JustAnotherTranslator.BaseSubtitleWidth.Value,
                () => JustAnotherTranslator.YotogiSubtitleWidth.Value,
                () => JustAnotherTranslator.AdvSubtitleWidth.Value,
                () => JustAnotherTranslator.LyricSubtitleWidth.Value,
                1f),

            // 字幕高度
            SubtitleHeight = GetSubtitleTypeConfig(
                subtitleType,
                () => JustAnotherTranslator.BaseSubtitleHeight.Value,
                () => JustAnotherTranslator.YotogiSubtitleHeight.Value,
                () => JustAnotherTranslator.AdvSubtitleHeight.Value,
                () => JustAnotherTranslator.LyricSubtitleHeight.Value,
                0.1f),

            // 是否启用动画效果
            EnableAnimation = GetSubtitleTypeConfig(
                subtitleType,
                () => JustAnotherTranslator.EnableBaseSubtitleAnimation.Value,
                () => JustAnotherTranslator.EnableYotogiSubtitleAnimation.Value,
                () => JustAnotherTranslator.EnableAdvSubtitleAnimation.Value,
                () => JustAnotherTranslator.EnableLyricSubtitleAnimation.Value,
                true),

            // 淡入时长
            FadeInDuration = GetSubtitleTypeConfig(
                subtitleType,
                () => JustAnotherTranslator.BaseSubtitleFadeInDuration.Value,
                () => JustAnotherTranslator.YotogiSubtitleFadeInDuration.Value,
                () => JustAnotherTranslator.AdvSubtitleFadeInDuration.Value,
                () => JustAnotherTranslator.LyricSubtitleFadeInDuration.Value,
                0.5f),

            // 淡出时长
            FadeOutDuration = GetSubtitleTypeConfig(
                subtitleType,
                () => JustAnotherTranslator.BaseSubtitleFadeOutDuration.Value,
                () => JustAnotherTranslator.YotogiSubtitleFadeOutDuration.Value,
                () => JustAnotherTranslator.AdvSubtitleFadeOutDuration.Value,
                () => JustAnotherTranslator.LyricSubtitleFadeOutDuration.Value,
                0.5f),

            // VR 字幕模式
            VRSubtitleMode = JustAnotherTranslator.VRSubtitleMode.Value,

            // VR悬浮字幕距离（米）
            VRSubtitleDistance = JustAnotherTranslator.VRSubtitleDistance.Value,

            // VR悬浮字幕垂直偏移（度，相对于视线中心）
            VRSubtitleVerticalOffset = JustAnotherTranslator.VRSubtitleVerticalOffset.Value,

            // VR悬浮字幕水平偏移（度，相对于视线中心）
            VRSubtitleHorizontalOffset = JustAnotherTranslator.VRSubtitleHorizontalOffset.Value,

            // VR悬浮字幕背景宽度
            VRSubtitleWidth = JustAnotherTranslator.VRInSpaceSubtitleWidth.Value,

            // VR悬浮字幕高度
            VRSubtitleHeight = JustAnotherTranslator.VRInSpaceSubtitleHeight.Value
        };

        return config;
    }

    /// <summary>
    ///     获取指定字幕类型的配置值
    /// </summary>
    private static T GetSubtitleTypeConfig<T>(
        JustAnotherTranslator.SubtitleTypeEnum type,
        Func<T> baseConfig,
        Func<T> yotogiConfig,
        Func<T> advConfig,
        Func<T> lyricConfig,
        T defaultValue)
    {
        return type switch
        {
            JustAnotherTranslator.SubtitleTypeEnum.Base => baseConfig(),
            JustAnotherTranslator.SubtitleTypeEnum.Yotogi => yotogiConfig(),
            JustAnotherTranslator.SubtitleTypeEnum.Adv => advConfig(),
            JustAnotherTranslator.SubtitleTypeEnum.Lyric => lyricConfig(),
            _ => defaultValue
        };
    }

    /// <summary>
    ///     解析颜色字符串
    /// </summary>
    /// <param name="colorStr">颜色字符串</param>
    /// <returns>颜色</returns>
    private static Color ParseColor(string colorStr)
    {
        try
        {
            if (ColorUtility.TryParseHtmlString(colorStr, out var color)) return color;
            LogManager.Warning($"Failed to parse color: {colorStr}, using default color/解析颜色失败: {colorStr}，使用默认颜色");
        }
        catch
        {
            LogManager.Warning($"Failed to parse color: {colorStr}, using default color/解析颜色失败: {colorStr}，使用默认颜色");
        }

        return Color.white;
    }


    /// <summary>
    ///     解析颜色字符串
    /// </summary>
    /// <param name="colorStr">颜色字符串</param>
    /// <param name="alpha">透明度</param>
    /// <returns>颜色</returns>
    private static Color ParseColor(string colorStr, float alpha)
    {
        try
        {
            if (ColorUtility.TryParseHtmlString(colorStr, out var color))
            {
                color.a = alpha;
                return color;
            }

            LogManager.Warning($"Failed to parse color: {colorStr}, using default color/解析颜色失败: {colorStr}，使用默认颜色");
        }
        catch
        {
            LogManager.Warning($"Failed to parse color: {colorStr}, using default color/解析颜色失败: {colorStr}，使用默认颜色");
        }

        return Color.white;
    }

    /// <summary>
    ///     获取字体
    ///     如果字体不存在，使用默认字体
    /// </summary>
    /// <param name="name">字体名称</param>
    /// <param name="size">字体大小</param>
    /// <returns>字体</returns>
    private static Font GetFontByName(string name, int size)
    {
        if (name == "Arial" || name == "Arial.ttf") return Resources.GetBuiltinResource<Font>("Arial.ttf");
        try
        {
            var font = Font.CreateDynamicFontFromOSFont(name, size);
            if (font is null)
            {
                LogManager.Warning($"Failed to load font: {name}, using default font/无法加载字体：{name}。使用默认字体");
                return Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return font;
        }
        catch (Exception e)
        {
            LogManager.Warning($"Failed to load font: {name}, using default font{e.Message}/无法加载字体：{name}。使用默认字体");
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }

    /// <summary>
    ///     将TextAnchorEnum转换为TextAnchor
    ///     只是为了安全性
    /// </summary>
    /// <param name="textAnchorEnum"></param>
    /// <returns></returns>
    private static TextAnchor ConvertTextAnchorEnum(JustAnotherTranslator.TextAnchorEnum textAnchorEnum)
    {
        return textAnchorEnum switch
        {
            JustAnotherTranslator.TextAnchorEnum.UpperLeft => TextAnchor.UpperLeft,
            JustAnotherTranslator.TextAnchorEnum.UpperCenter => TextAnchor.UpperCenter,
            JustAnotherTranslator.TextAnchorEnum.UpperRight => TextAnchor.UpperRight,
            JustAnotherTranslator.TextAnchorEnum.MiddleLeft => TextAnchor.MiddleLeft,
            JustAnotherTranslator.TextAnchorEnum.MiddleCenter => TextAnchor.MiddleCenter,
            JustAnotherTranslator.TextAnchorEnum.MiddleRight => TextAnchor.MiddleRight,
            JustAnotherTranslator.TextAnchorEnum.LowerLeft => TextAnchor.LowerLeft,
            JustAnotherTranslator.TextAnchorEnum.LowerCenter => TextAnchor.LowerCenter,
            JustAnotherTranslator.TextAnchorEnum.LowerRight => TextAnchor.LowerRight,
            _ => TextAnchor.MiddleCenter
        };
    }

    # region Config

    /// 字幕类型
    public JustAnotherTranslator.SubtitleTypeEnum SubtitleType { get; set; } =
        JustAnotherTranslator.SubtitleTypeEnum.Base;

    /// 是否启用说话人名字显示
    public bool EnableSpeakerName { get; set; } = true;

    /// 字体
    public Font Font { get; set; } = Resources.GetBuiltinResource<Font>("Arial.ttf");

    /// 字体大小
    public int FontSize { get; set; } = 24;

    /// 文本对齐方式
    public TextAnchor TextAlignment { get; set; } = TextAnchor.MiddleCenter;

    /// 文本颜色
    public Color TextColor { get; set; } = Color.white;

    /// 背景颜色
    public Color BackgroundColor { get; set; } = new(0, 0, 0, 0.1f);

    /// 是否启用描边
    public bool EnableOutline { get; set; }

    /// 描边颜色
    public Color OutlineColor { get; set; } = new(0, 0, 0, 0.5f);

    /// 描边粗细
    public float OutlineWidth { get; set; } = 1f;

    /// 水平位置
    public float HorizontalPosition { get; set; }

    /// 垂直位置
    public float VerticalPosition { get; set; } = 1050f;

    /// 当前垂直位置
    public float CurrentVerticalPosition { get; set; } = 1050f;

    /// 字幕宽度
    public float SubtitleWidth { get; set; } = 1920f;

    /// 背景高度
    public float SubtitleHeight { get; set; } = 30f;

    /// 是否启用动画效果
    public bool EnableAnimation { get; set; } = true;

    /// 淡入时长（秒）
    public float FadeInDuration { get; set; } = 0.5f;

    /// 淡出时长（秒）
    public float FadeOutDuration { get; set; } = 0.5f;

    /// VR模式字幕类型
    public JustAnotherTranslator.VRSubtitleModeEnum VRSubtitleMode { get; set; } =
        JustAnotherTranslator.VRSubtitleModeEnum.InSpace;

    /// VR悬浮字幕距离（米）
    public float VRSubtitleDistance { get; set; } = 1f;

    /// VR悬浮字幕垂直偏移（度，相对于视线中心）
    public float VRSubtitleVerticalOffset { get; set; } = -15f;

    /// VR悬浮字幕水平偏移（度，相对于视线中心）
    public float VRSubtitleHorizontalOffset { get; set; }

    /// VR悬浮字幕宽度
    public float VRSubtitleWidth { get; set; } = 1000f;

    /// VR悬浮字幕高度
    public float VRSubtitleHeight { get; set; } = 30f;

    # endregion
}