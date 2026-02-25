using System.Collections.Generic;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle;

/// <summary>
///     单个说话人的颜色配置
/// </summary>
public class SpeakerColorConfig
{
    /// <summary>
    ///     各字幕类型的自定义颜色配置，key 为 SubtitleTypeEnum.ToString()
    ///     (如 "Base", "Yotogi", "Adv", "Lyric")
    /// </summary>
    public Dictionary<string, SubtitleColorEntry> SubtitleColors { get; set; } = new();
}

/// <summary>
///     单种字幕类型的颜色配置
/// </summary>
public class SubtitleColorEntry
{
    /// <summary>
    ///     说话人名称颜色 (hex, 如 "#FCD5DE")，不受 Enabled 开关影响
    /// </summary>
    public string SpeakerColor { get; set; } = "#FFFFFF";

    /// <summary>
    ///     是否启用此字幕类型的自定义颜色（仅影响文本/背景/描边颜色，不影响 SpeakerColor）
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    ///     文本颜色 (hex, 如 "#FFFFFF")
    /// </summary>
    public string TextColor { get; set; } = "#FFFFFF";

    /// <summary>
    ///     文本不透明度 (0-1)
    /// </summary>
    public float TextOpacity { get; set; } = 1.0f;

    /// <summary>
    ///     背景颜色 (hex, 如 "#000000")
    /// </summary>
    public string BackgroundColor { get; set; } = "#000000";

    /// <summary>
    ///     背景不透明度 (0-1)
    /// </summary>
    public float BackgroundOpacity { get; set; } = 0.1f;

    /// <summary>
    ///     描边颜色 (hex, 如 "#000000")
    /// </summary>
    public string OutlineColor { get; set; } = "#000000";

    /// <summary>
    ///     描边不透明度 (0-1)
    /// </summary>
    public float OutlineOpacity { get; set; } = 0.5f;
}