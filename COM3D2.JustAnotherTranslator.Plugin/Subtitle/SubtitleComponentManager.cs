using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

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

    // 存储说话者名称和对应的颜色缓存
    private static readonly Dictionary<string, Color> SpeakerColors = new();

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
            id = $"Subtitle_JAT_DefaultSubtitle_For_{speakerName}_{_subtitleIdCounter++}";
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

        LogManager.Warning($"Subtitle with ID {id} not found");
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

        // 获取说话者专属颜色
        var speakerColor = string.IsNullOrEmpty(speakerName)
            ? ColorUtility.ToHtmlStringRGB(new Color(1f, 0.6f, 0.2f))
            : ColorUtility.ToHtmlStringRGB(GetSpeakerColor(speakerName));

        subtitle.Show(text, duration, speakerName, speakerColor, config.EnableSpeakerName);
    }

    /// <summary>
    ///     为特定说话者显示字幕
    /// </summary>
    /// <param name="text">字幕文本</param>
    /// <param name="speakerName">说话者名称</param>
    /// <param name="duration">显示时长（秒），0表示一直显示直到手动隐藏</param>
    /// <param name="verticalOffset">垂直位置偏移，用于错开多个字幕</param>
    /// <param name="config">字幕配置</param>
    public static void ShowSpeakerSubtitle(string text, string speakerName, float duration, float verticalOffset,
        SubtitleConfig config)
    {
        // 获取说话者专用字幕ID
        var subtitleId = GetSpeakerSubtitleId(speakerName);

        // 如果提供了配置，尝试调整垂直位置
        var newConfig = config;
        if (config != null && verticalOffset != 0f)
        {
            // 创建配置副本以避免修改原始配置
            newConfig = config;
            newConfig.VerticalPosition = config.VerticalPosition + verticalOffset;
        }

        // 显示字幕
        ShowSubtitle(subtitleId, text, speakerName, duration, newConfig);
    }

    /// <summary>
    ///     显示浮动字幕，自动避免重叠
    /// </summary>
    public static void ShowFloatingSubtitle(string text, string speakerName, float duration, SubtitleConfig baseConfig)
    {
        // 获取或创建专用字幕ID
        var subtitleId = GetSpeakerSubtitleId(speakerName);

        // 创建新的配置，初始化和基本配置相同
        var newConfig = baseConfig;

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
            if (subtitle.gameObject.activeSelf && subtitle.GetSubtitleId() != GetSpeakerSubtitleId(speakerName))
            {
                // 获取此字幕占用的垂直空间
                var position = subtitle.GetVerticalPosition();
                var height = subtitle.GetHeight();

                // 归一化为 0-1 范围内的屏幕位置
                float normalizedHeight;

                // 判断是否为VR模式
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

        // 判断是否为VR模式
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
    /// <param name="id">字幕ID</param>
    public static void HideSubtitle(string id)
    {
        var subtitle = GetSubtitle(id);
        if (subtitle is null)
            return;

        subtitle.Hide();
    }

    /// <summary>
    ///     隐藏所有字幕
    /// </summary>
    public static void HideAllSubtitles()
    {
        foreach (var subtitle in Subtitles.Values) subtitle.Hide();
    }

    /// <summary>
    ///     更新某个字幕的配置
    /// </summary>
    /// <param name="id">字幕ID</param>
    /// <param name="config">新的配置</param>
    public static void UpdateSubtitleConfig(string id, SubtitleConfig config)
    {
        var subtitle = GetSubtitle(id);
        if (subtitle is null)
            return;

        subtitle.UpdateConfig(config);
    }

    /// <summary>
    ///     更新所有字幕的配置
    /// </summary>
    public static void UpdateAllSubtitles(SubtitleConfig config)
    {
        foreach (var subtitle in Subtitles.Values) subtitle.UpdateConfig(config);

        LogManager.Debug("All subtitles updated");
    }

    /// <summary>
    ///     销毁某个字幕
    /// </summary>
    /// <param name="id">字幕ID</param>
    public static void DestroySubtitle(string id)
    {
        if (Subtitles.TryGetValue(id, out var subtitle))
        {
            Object.Destroy(subtitle.gameObject);
            Subtitles.Remove(id);
            LogManager.Debug($"Destroyed subtitle with ID: {id}");
        }
    }

    /// <summary>
    ///     销毁所有字幕
    /// </summary>
    public static void DestroyAllSubtitles()
    {
        foreach (var subtitle in Subtitles.Values) Object.Destroy(subtitle.gameObject);

        Subtitles.Clear();

        LogManager.Debug("All subtitles destroyed");
    }

    /// <summary>
    ///     获取说话者的专属颜色
    /// </summary>
    /// <param name="speakerName">说话者名称</param>
    /// <returns>专属颜色</returns>
    private static Color GetSpeakerColor(string speakerName)
    {
        // 如果已经为此说话者分配了颜色，则直接返回
        if (SpeakerColors.TryGetValue(speakerName, out var color))
            return color;

        if (string.IsNullOrEmpty(speakerName))
            speakerName = "Unknown";

        // 基于说话者名称生成稳定的哈希值
        var hash = speakerName.GetHashCode();

        // 使用哈希值生成颜色，确保相同名称总是获得相同颜色
        var random = new Random(hash);

        // 生成偏亮的颜色
        color = new Color(
            0.5f + (float)random.NextDouble() * 0.5f, // 0.5-1.0 范围
            0.5f + (float)random.NextDouble() * 0.5f,
            0.5f + (float)random.NextDouble() * 0.5f
        );

        // 保存到缓存
        SpeakerColors[speakerName] = color;

        LogManager.Debug($"Created Color R:{color.r:F2} G:{color.g:F2} B:{color.b:F2} for {speakerName}");

        return color;
    }

    // .NET Framework 3.5 不支持 ValueTuple
    private struct VerticalRange
    {
        public readonly float Start;
        public readonly float End;

        public VerticalRange(float start, float end)
        {
            Start = start;
            End = end;
        }
    }
}