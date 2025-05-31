using System.Collections.Generic;
using UnityEngine;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle;

/// <summary>
///     字幕管理器，用于创建和管理字幕组件
/// </summary>
public static class SubtitleComponentManager
{
    // 字幕组件字典，键为字幕ID
    private static readonly Dictionary<string, SubtitleComponent> Subtitles = new();

    // 存储每个说话者正在使用的字幕ID
    private static readonly Dictionary<string, string> SpeakerSubtitleIds = new();

    // 字幕ID计数器，用于为每个说话者生成唯一ID
    private static int _subtitleIdCounter;

    /// <summary>
    ///     获取说话者专用的字幕ID
    /// </summary>
    /// <param name="speakerName">说话者名称</param>
    /// <returns>说话者专用的字幕ID</returns>
    public static string GetSpeakerSubtitleId(string speakerName)
    {
        if (string.IsNullOrEmpty(speakerName))
            return "Subtitle_JAT_DefaultSubtitle";

        if (!SpeakerSubtitleIds.TryGetValue(speakerName, out var id))
        {
            // 为说话者创建新的字幕ID
            id = $"Subtitle_JAT_Subtitle_For_{speakerName}_{_subtitleIdCounter++}";
            SpeakerSubtitleIds[speakerName] = id;
        }

        return id;
    }

    /// <summary>
    ///     创建字幕组件
    /// </summary>
    /// <param name="id">字幕ID</param>
    /// <param name="config">字幕配置，如果为null则使用默认配置</param>
    /// <returns>创建的字幕组件</returns>
    public static SubtitleComponent CreateSubtitle(string id, SubtitleConfig config)
    {
        // 如果已存在同ID的字幕，先销毁它
        if (Subtitles.TryGetValue(id, out var existingSubtitle))
        {
            Object.Destroy(existingSubtitle.gameObject);
            Subtitles.Remove(id);
        }

        // 创建新的字幕游戏对象
        var subtitleObj = new GameObject(id);
        Object.DontDestroyOnLoad(subtitleObj); // 确保场景切换时不会被销毁

        // 添加字幕组件
        var subtitle = subtitleObj.AddComponent<SubtitleComponent>();

        // 初始化字幕组件
        subtitle.Initialize(config);

        // 添加到字典
        Subtitles[id] = subtitle;

        LogManager.Debug($"Created subtitle with ID: {id}");

        return subtitle;
    }

    /// <summary>
    ///     获取字幕组件
    /// </summary>
    /// <param name="id">字幕ID</param>
    /// <returns>字幕组件，如果不存在则返回null</returns>
    public static SubtitleComponent GetSubtitle(string id)
    {
        if (Subtitles.TryGetValue(id, out var subtitle)) return subtitle;

        LogManager.Debug($"Subtitle with ID {id} not found");
        return null;
    }

    /// <summary>
    ///     获取或创建字幕组件
    /// </summary>
    /// <param name="id">字幕ID</param>
    /// <param name="config">字幕配置，如果为null则使用默认配置</param>
    /// <returns>字幕组件</returns>
    public static SubtitleComponent GetOrCreateSubtitle(string id, SubtitleConfig config)
    {
        var subtitle = GetSubtitle(id);
        if (subtitle is null)
            subtitle = CreateSubtitle(id, config);
        else if (config != null) subtitle.UpdateConfig(config);

        return subtitle;
    }

    /// <summary>
    ///     显示字幕（单个说话者）
    /// </summary>
    /// <param name="id">字幕ID</param>
    /// <param name="text">字幕文本</param>
    /// <param name="speakerName">说话者名称，会以不同颜色显示在文本前</param>
    /// <param name="duration">显示时长（秒），0表示一直显示直到手动隐藏</param>
    /// <param name="config">字幕配置</param>
    public static void ShowSubtitle(string id, string text, string speakerName, float duration,
        SubtitleConfig config)
    {
        var subtitle = GetOrCreateSubtitle(id, config);

        subtitle.ShowSubtitle(text, speakerName, duration);
    }

    /// <summary>
    ///     显示浮动字幕，自动避免重叠
    /// </summary>
    public static void ShowFloatingSubtitle(string text, string speakerName, float duration, SubtitleConfig baseConfig)
    {
        // 获取或创建专用字幕ID
        var subtitleId = GetSpeakerSubtitleId(speakerName);

        // 创建新的配置，初始化和基本配置相同
        var newConfig = CloneConfig(baseConfig);

        // 计算此字幕应该放置的位置，避免与其他活动字幕重叠
        newConfig.VerticalPosition = CalculateNonOverlappingPosition(speakerName, newConfig.BackgroundHeight);

        // 显示字幕
        ShowSubtitle(subtitleId, text, speakerName, duration, newConfig);
    }

    /// <summary>
    ///     计算不重叠的垂直位置
    /// </summary>
    private static float CalculateNonOverlappingPosition(string speakerName, float subtitleHeight)
    {
        // 获取所有活动字幕的位置信息
        var activePositions = new List<VerticalRange>();

        // 遍历所有活动字幕
        foreach (var subtitle in Subtitles.Values)
            if (subtitle.gameObject.activeSelf && subtitle._speakerName != speakerName)
            {
                // 获取此字幕占用的垂直空间
                var position = subtitle.GetVerticalPosition();
                var height = subtitle.GetHeight();
                // TODO 重构多人字幕
                // TODO 描边透明度消失了
                // TODO 字体自定义消失了，策略里面也有，需要审查

                // 归一化为 0-1 范围内的屏幕位置
                float normalizedHeight;
                if (JustAnotherTranslator.IsVrMode &&
                    JustAnotherTranslator.VRSubtitleMode.Value == JustAnotherTranslator.VRSubtitleModeEnum.InSpace)
                    // VR空间模式使用固定的归一化高度
                    normalizedHeight = 0.1f; // 在VR空间中使用固定的垂直间距
                else
                    // 普通模式使用屏幕高度
                    normalizedHeight = height / Screen.height;

                // 添加到活动位置列表
                activePositions.Add(new VerticalRange(position, position + normalizedHeight));
            }

        // 按开始位置排序
        activePositions.Sort((a, b) => a.Start.CompareTo(b.Start));

        // 计算基准位置
        float normalizedSubtitleHeight;
        if (JustAnotherTranslator.IsVrMode &&
            JustAnotherTranslator.VRSubtitleMode.Value == JustAnotherTranslator.VRSubtitleModeEnum.InSpace)
            // VR空间模式使用固定的归一化高度
            normalizedSubtitleHeight = 0.1f;
        else
            // 普通模式使用屏幕高度
            normalizedSubtitleHeight = subtitleHeight / Screen.height;

        // 根据当前字幕类型选择相应的垂直位置
        var basePos = Mathf.Clamp(
            JustAnotherTranslator.GetSubtitleVerticalPosition(JustAnotherTranslator.SubtitleType.Value), 0.01f,
            0.99f - normalizedSubtitleHeight);

        var found = false;
        var resultPos = basePos;

        // 1. 向下查找（优先）
        foreach (var range in activePositions)
        {
            if (range.End + 0.01f < basePos) continue; // 跳过在基准位置之上的
            if (basePos + normalizedSubtitleHeight <= range.Start)
            {
                resultPos = basePos;
                found = true;
                break;
            }

            basePos = range.End + 0.01f;
            if (basePos > 0.99f - normalizedSubtitleHeight) break;
        }

        if (!found)
        {
            // 2. 向上查找
            basePos = Mathf.Clamp(
                JustAnotherTranslator.GetSubtitleVerticalPosition(JustAnotherTranslator.SubtitleType.Value), 0.01f,
                0.99f - normalizedSubtitleHeight);
            for (var i = activePositions.Count - 1; i >= 0; i--)
            {
                var range = activePositions[i];
                if (range.Start - normalizedSubtitleHeight > basePos) continue; // 跳过在基准位置之下的
                var tryPos = range.Start - normalizedSubtitleHeight - 0.01f;
                if (tryPos >= 0.01f && tryPos + normalizedSubtitleHeight <= range.Start)
                {
                    resultPos = tryPos;
                    found = true;
                    break;
                }
            }
        }

        // 确保不越界
        return Mathf.Clamp(resultPos, 0.01f, 0.99f - normalizedSubtitleHeight);
    }

    /// <summary>
    ///     隐藏字幕
    /// </summary>
    public static void HideSubtitle(string id)
    {
        var subtitle = GetSubtitle(id);
        subtitle?.HideSubtitle();
    }

    /// <summary>
    ///     隐藏所有字幕
    /// </summary>
    public static void HideAllSubtitles()
    {
        foreach (var subtitle in Subtitles.Values) subtitle.HideSubtitle();
    }

    /// <summary>
    ///     销毁字幕
    /// </summary>
    public static void DestroySubtitle(string id)
    {
        var subtitle = GetSubtitle(id);
        if (subtitle is null) return;

        Object.Destroy(subtitle.gameObject);
        Subtitles.Remove(id);
    }

    /// <summary>
    ///     销毁所有字幕
    /// </summary>
    public static void DestroyAllSubtitles()
    {
        foreach (var subtitle in Subtitles.Values) Object.Destroy(subtitle.gameObject);

        Subtitles.Clear();
    }

    /// <summary>
    ///     获取所有当前活跃的字幕组件
    /// </summary>
    /// <returns>字幕组件列表</returns>
    public static IEnumerable<SubtitleComponent> GetAllSubtitles()
    {
        return Subtitles.Values;
    }

    /// <summary>
    ///     克隆字幕配置
    /// </summary>
    /// <param name="source">源配置</param>
    /// <returns>新的配置副本</returns>
    private static SubtitleConfig CloneConfig(SubtitleConfig source)
    {
        if (source == null)
            return new SubtitleConfig();

        // 创建配置副本
        var clone = new SubtitleConfig
        {
            // 基本属性
            SubtitleType = source.SubtitleType,
            EnableSpeakerName = source.EnableSpeakerName,

            // 文本样式
            FontName = source.FontName,
            FontSize = source.FontSize,
            TextColor = source.TextColor,
            TextAlignment = source.TextAlignment,

            // 背景样式
            BackgroundColor = source.BackgroundColor,
            BackgroundOpacity = source.BackgroundOpacity,
            BackgroundHeight = source.BackgroundHeight,
            VerticalPosition = source.VerticalPosition,

            // 动画
            EnableAnimation = source.EnableAnimation,
            FadeInDuration = source.FadeInDuration,
            FadeOutDuration = source.FadeOutDuration,

            // 描边
            OutlineEnabled = source.OutlineEnabled,
            OutlineColor = source.OutlineColor,
            OutlineWidth = source.OutlineWidth,

            // VR相关
            VRSubtitleMode = source.VRSubtitleMode,
            VRSubtitleDistance = source.VRSubtitleDistance,
            VRSubtitleVerticalOffset = source.VRSubtitleVerticalOffset,
            VRSubtitleHorizontalOffset = source.VRSubtitleHorizontalOffset,
            VRSubtitleWidth = source.VRSubtitleWidth,
            VRSubtitleScale = source.VRSubtitleScale,

            // 参考分辨率
            ReferenceWidth = source.ReferenceWidth,
            ReferenceHeight = source.ReferenceHeight,
            MatchWidthOrHeight = source.MatchWidthOrHeight
        };

        return clone;
    }

    /// <summary>
    ///     表示一个垂直范围，用于计算字幕位置
    ///     .NET Framework 3.5 不支持 ValueTuple
    /// </summary>
    private class VerticalRange
    {
        public readonly float End;
        public readonly float Start;

        public VerticalRange(float start, float end)
        {
            Start = start;
            End = end;
        }
    }
}