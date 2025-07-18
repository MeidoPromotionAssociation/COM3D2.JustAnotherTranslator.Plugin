using System;
using System.Collections.Generic;
using System.Linq;
using COM3D2.JustAnotherTranslator.Plugin.Subtitle.Component;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using UnityEngine;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle;

/// <summary>
///     字幕组件管理器，负责管理所有字幕组件的生命周期和配置
/// </summary>
public static class SubtitleComponentManager
{
    // 槽位相关常量
    private const float BottomMargin = 0.00f; // 底部边距（屏幕高度的3%）

    private const float Spacing = 0.00f; // 槽位之间的间距（屏幕高度的%）

    // 是否已初始化
    private static bool _initialized;

    // 字幕 ID 映射，用于跟踪说话者与字幕组件的关系
    private static readonly Dictionary<string, ISubtitleComponent> SubtitleIdComponentsMap = new(); // 字幕 ID，字幕组件

    // 所有种类字幕的配置字典
    private static readonly Dictionary<JustAnotherTranslator.SubtitleTypeEnum, SubtitleConfig> SubtitleConfigs = new();

    // 槽位列表
    private static readonly List<SlotInfo> Slots = new();

    /// <summary>
    ///     初始化字幕组件管理器
    /// </summary>
    public static void Init()
    {
        if (_initialized) return;

        // 初始化字幕配置字典
        foreach (JustAnotherTranslator.SubtitleTypeEnum subtitleType in Enum.GetValues(
                     typeof(JustAnotherTranslator.SubtitleTypeEnum)))
        {
            var config = SubtitleConfig.CreateSubtitleConfig(subtitleType);
            LogManager.Debug($"Init subtitle config: {subtitleType}");
            if (config == null)
            {
                LogManager.Error(
                    $"Failed to create subtitle config: {subtitleType}, please report this issue/创建 {subtitleType} 的字幕配置失败，请报告此问题");
                continue;
            }

            SubtitleConfigs[subtitleType] = config;
        }

        _initialized = true;
        LogManager.Debug("Subtitle component manager initialized");
    }

    /// <summary>
    ///     根据配置创建合适类型的字幕组件
    /// </summary>
    /// <param name="speakerName">说话者名称</param>
    /// <param name="config">字幕配置</param>
    /// <returns>字幕组件</returns>
    private static ISubtitleComponent CreateSubtitleComponent(string speakerName, SubtitleConfig config)
    {
        if (!_initialized)
            Init();

        if (config == null)
        {
            LogManager.Warning(
                "Subtitle config is null, using default subtitle component, please report this issue/字幕配置为空，使用默认字幕组件，请报告此问题");
            config = SubtitleConfigs[JustAnotherTranslator.SubtitleTypeEnum.Base];
            if (config == null)
            {
                LogManager.Error("Default subtitle component is null, please report this issue/默认字幕组件为空，请报告此问题");
                return null;
            }
        }

        var gameObject = new GameObject(GetSpeakerSubtitleId(speakerName));
        ISubtitleComponent component;

        if (JustAnotherTranslator.IsVrMode)
        {
            switch (JustAnotherTranslator.VRSubtitleMode.Value)
            {
                case JustAnotherTranslator.VRSubtitleModeEnum.InSpace:
                    component = gameObject.AddComponent<VRSpaceSubtitleComponent>();
                    LogManager.Debug("Created VR space subtitle component");
                    break;
                case JustAnotherTranslator.VRSubtitleModeEnum.OnTablet:
                    component = gameObject.AddComponent<VRTabletSubtitleComponent>();
                    LogManager.Debug("Created VR tablet subtitle component");
                    break;
                default:
                    component = gameObject.AddComponent<VRSpaceSubtitleComponent>();
                    LogManager.Warning("Created VR space subtitle component, why? (default)");
                    break;
            }
        }
        else
        {
            component = gameObject.AddComponent<ScreenSubtitleComponent>();
            LogManager.Debug("Created screen subtitle component");
        }

        component.Init(config);

        SubtitleIdComponentsMap[GetSpeakerSubtitleId(speakerName)] = component;

        return component;
    }

    /// <summary>
    ///     显示字幕
    /// </summary>
    /// <param name="text">字幕文本</param>
    /// <param name="speakerName">说话者名称</param>
    /// <param name="duration">显示时长，0为无限</param>
    /// <param name="subtitleType">字幕类型</param>
    public static void ShowSubtitle(string text, string speakerName, float duration,
        JustAnotherTranslator.SubtitleTypeEnum subtitleType)
    {
        var subtitleConfig = GetSubtitleConfig(subtitleType);
        if (subtitleConfig == null)
        {
            LogManager.Error(
                $"Subtitle config for {subtitleType} not found, please report this issue/找不到 {subtitleType} 的字幕配置，请报告此问题");
            return;
        }

        if (string.IsNullOrEmpty(text))
            return;

        if (!_initialized)
            Init();

        var subtitleId = GetSpeakerSubtitleId(speakerName);

        if (!SubtitleIdComponentsMap.TryGetValue(subtitleId, out var subtitleComponent))
        {
            subtitleComponent = CreateSubtitleComponent(speakerName, subtitleConfig);
            SubtitleIdComponentsMap[subtitleId] = subtitleComponent;
        }
        else
        {
            // 如果字幕类型不同，重新创建
            if (subtitleComponent.GetConfig().SubtitleType != subtitleType)
            {
                subtitleComponent.Destroy();
                SubtitleIdComponentsMap.Remove(subtitleId);
                subtitleComponent = CreateSubtitleComponent(speakerName, subtitleConfig);
                SubtitleIdComponentsMap[subtitleId] = subtitleComponent;
            }
        }

        CalculateNewPosition(subtitleComponent, subtitleType);

        subtitleComponent.ShowSubtitle(text, speakerName, duration);

        LogManager.Debug($"Try to show subtitle: [{speakerName}] {text}");
    }

    /// <summary>
    ///     隐藏特定 ID 的字幕
    /// </summary>
    /// <param name="subtitleId">字幕ID</param>
    public static void HideSubtitleById(string subtitleId)
    {
        if (string.IsNullOrEmpty(subtitleId))
            return;

        if (SubtitleIdComponentsMap.TryGetValue(subtitleId, out var subtitleComponent))
            subtitleComponent.HideSubtitle();

        var slotToRemove = Slots.FirstOrDefault(s => s.SubtitleId == subtitleId);
        if (slotToRemove != null) Slots.Remove(slotToRemove);
    }

    /// <summary>
    ///     隐藏特定 speakerName 的字幕
    /// </summary>
    /// <param name="speakerName">说话者名称</param>
    public static void HideSubtitleBySpeakerName(string speakerName)
    {
        var subtitleId = GetSpeakerSubtitleId(speakerName);
        HideSubtitleById(subtitleId);
    }

    /// <summary>
    ///     从说话者名称获取字幕ID
    /// </summary>
    /// <param name="speakerName">说话者名称</param>
    /// <returns>字幕ID</returns>
    public static string GetSpeakerSubtitleId(string speakerName)
    {
        if (string.IsNullOrEmpty(speakerName))
            return "JAT_SubtitleComponent_For_Default";

        return $"JAT_SubtitleComponent_For_{speakerName}";
    }


    /// <summary>
    ///     计算字幕可用位置
    /// </summary>
    public static void CalculateNewPosition(ISubtitleComponent subtitleComponent,
        JustAnotherTranslator.SubtitleTypeEnum subtitleType)
    {
        if (!_initialized)
            Init();

        var config = subtitleComponent.GetConfig();
        if (config == null) return;


        // VR Space 模式使用专门的空间位置计算
        if (JustAnotherTranslator.IsVrMode && config.VRSubtitleMode == JustAnotherTranslator.VRSubtitleModeEnum.InSpace)
        {
            CalculateVRSpacePosition(subtitleComponent, config);
            return;
        }

        try
        {
            // 获取当前字幕类型的高度
            var subtitleHeight = GetSubtitleTypeHeight(subtitleType) * 2; // 我不知道为什么实际高度是 2 倍
            var newPosition = 0f;
            var foundSlot = false;
            var currentSubtitleId = subtitleComponent.GetSubtitleId();

            // 检查是否已经存在相同ID的字幕
            var existingSlot = Slots.FirstOrDefault(s => s.SubtitleId == currentSubtitleId);

            if (existingSlot != null)
            {
                // 如果存在相同ID的字幕，重用其位置
                newPosition = existingSlot.Bottom;
                foundSlot = true;
                Slots.Remove(existingSlot);
            }
            // 如果没有相同ID的字幕，则查找新位置
            else if (Slots.Count == 0)
            {
                newPosition = config.VerticalPosition; // 注意设置为 1 时会超出屏幕
                foundSlot = true;
            }
            else
            {
                LogManager.Debug(
                    $"Finding position for {currentSubtitleId}. Current slots: {Slots.Count}, StartPos: {config.VerticalPosition}, StackMode: {(config.VerticalPosition > 0.5f ? "Downward" : "Upward")}, Height: {subtitleHeight}");

                // 判断堆叠方向：只有非常底部的字幕会向上堆叠
                var stackDownward = config.VerticalPosition > 0.1f;

                // 按位置排序
                Slots.Sort((a, b) => stackDownward
                    ? (b.Bottom + b.Height).CompareTo(a.Bottom + a.Height) // 从上到下
                    : a.Bottom.CompareTo(b.Bottom)); // 从下到上

                // 根据堆叠方向查找可用位置
                if (stackDownward)
                {
                    // 向下堆叠：从用户设置位置开始向下查找
                    var startPosition = Mathf.Min(config.VerticalPosition, 1f - subtitleHeight);
                    newPosition = FindDownwardPosition(startPosition, subtitleHeight);
                    foundSlot = newPosition >= 0;
                }
                else
                {
                    // 向上堆叠：从用户设置位置开始向上查找
                    var startPosition = Mathf.Max(0, config.VerticalPosition - subtitleHeight);
                    newPosition = FindUpwardPosition(startPosition, subtitleHeight);
                    foundSlot = newPosition >= 0;
                }
            }

            // 如果找到了合适的位置，添加新槽位
            if (foundSlot)
            {
                var newSlot = new SlotInfo
                {
                    SubtitleId = currentSubtitleId,
                    Subtitle = subtitleComponent,
                    Bottom = newPosition,
                    Height = subtitleHeight
                };

                Slots.Add(newSlot);
                config.CurrentVerticalPosition = newPosition;
                subtitleComponent.SetVerticalPosition(newPosition);

                LogManager.Debug(
                    $"Subtitle {currentSubtitleId} placed at {newPosition}, height: {subtitleHeight}");
            }
            else
            {
                // 如果没有足够空间，移除第一个字幕
                if (Slots.Count > 0)
                {
                    var oldestSubtitle = Slots[0];
                    Slots.RemoveAt(0);
                    DestroySubtitleComponentBySubtitleId(oldestSubtitle.SubtitleId);
                    CalculateNewPosition(subtitleComponent, subtitleType); // 递归调用以重新计算位置
                    return;
                }

                // 如果还是无法放置，使用默认位置
                config.CurrentVerticalPosition = config.VerticalPosition;
                subtitleComponent.SetVerticalPosition(newPosition);
                LogManager.Warning(
                    $"Subtitle placement failed, using default position/字幕放置失败，使用默认位置: {currentSubtitleId}");
            }
        }
        catch (Exception ex)
        {
            LogManager.Error($"Calculate subtitle position error/计算字幕位置错误: {ex.Message}\n{ex.StackTrace}");
            config.CurrentVerticalPosition = config.VerticalPosition;
            subtitleComponent.UpdateConfig(config);
        }
    }

    private static void CalculateVRSpacePosition(ISubtitleComponent subtitleComponent, SubtitleConfig config)
    {
        //TODO
    }

    /// <summary>
    ///     查找向下可用位置
    /// </summary>
    /// <param name="startPosition"></param>
    /// <param name="subtitleHeight"></param>
    /// <returns>位置</returns>
    private static float FindDownwardPosition(float startPosition, float subtitleHeight)
    {
        // 获取所有已占用的区域，按底部位置排序
        var occupiedRanges = Slots
            .Select(s => new { Top = s.Bottom + s.Height, s.Bottom })
            .OrderBy(r => r.Bottom)
            .ToList();

        // 检查起始位置是否可用
        var isStartPositionValid = true;
        foreach (var range in occupiedRanges)
            if (startPosition < range.Top && startPosition + subtitleHeight > range.Bottom)
            {
                isStartPositionValid = false;
                break;
            }

        if (isStartPositionValid && startPosition + subtitleHeight <= 1f) return startPosition;

        // 检查顶部是否有足够空间
        if (occupiedRanges.Count == 0 || 1f - occupiedRanges[0].Top >= subtitleHeight + Spacing)
            return 1f - subtitleHeight;

        // 检查槽位之间的空间
        for (var i = 0; i < occupiedRanges.Count - 1; i++)
        {
            var currentBottom = occupiedRanges[i].Bottom;
            var nextTop = occupiedRanges[i + 1].Top; // 下一个槽位的顶部
            var availableSpace = currentBottom - nextTop;

            if (availableSpace >= subtitleHeight + Spacing) return currentBottom - subtitleHeight;
        }

        // 检查底部空间
        var lastBottom = occupiedRanges.Last().Bottom;
        if (lastBottom - BottomMargin >= subtitleHeight + Spacing) return lastBottom - subtitleHeight - Spacing;

        return -1; // 没有找到合适位置
    }

    /// <summary>
    ///     向上查找可用位置
    /// </summary>
    /// <param name="startPosition"></param>
    /// <param name="subtitleHeight"></param>
    /// <returns>位置</returns>
    private static float FindUpwardPosition(float startPosition, float subtitleHeight)
    {
        // 获取所有已占用的区域，按顶部位置降序排序
        var occupiedRanges = Slots
            .Select(s => new { Top = s.Bottom + s.Height, s.Bottom })
            .OrderByDescending(r => r.Top)
            .ToList();

        // 检查起始位置是否可用
        var isStartPositionValid = true;
        foreach (var range in occupiedRanges)
            if (startPosition < range.Top && startPosition + subtitleHeight > range.Bottom)
            {
                isStartPositionValid = false;
                break;
            }

        if (isStartPositionValid && startPosition >= 0) return startPosition;

        // 检查底部是否有足够空间
        if (occupiedRanges.Count == 0 || occupiedRanges[0].Bottom - BottomMargin >= subtitleHeight + Spacing)
            return Mathf.Max(0, BottomMargin);

        // 检查槽位之间的空间
        for (var i = 0; i < occupiedRanges.Count - 1; i++)
        {
            var currentTop = occupiedRanges[i].Top;
            var nextBottom = occupiedRanges[i + 1].Bottom + occupiedRanges[i + 1].Top;
            var availableSpace = nextBottom - currentTop;

            if (availableSpace >= subtitleHeight + Spacing) return currentTop + Spacing;
        }

        // 检查顶部空间
        var lastTop = occupiedRanges.Last().Top;
        if (1f - lastTop >= subtitleHeight + Spacing) return lastTop + Spacing;

        return -1; // 没有找到合适位置
    }


    /// <summary>
    ///     获取指定字幕类型的设置高度
    /// </summary>
    /// <param name="type"></param>
    /// <returns>高度</returns>
    private static float GetSubtitleTypeHeight(JustAnotherTranslator.SubtitleTypeEnum type)
    {
        SubtitleConfigs.TryGetValue(type, out var config);
        if (config == null) return 0f;

        if (config.BackgroundHeight >= 1.0f) return config.BackgroundHeight / Screen.height;

        return config.BackgroundHeight;
    }


    /// <summary>
    ///     获取指定类型的字幕配置
    /// </summary>
    /// <param name="type">字幕类型</param>
    /// <returns>字幕配置</returns>
    public static SubtitleConfig GetSubtitleConfig(JustAnotherTranslator.SubtitleTypeEnum type)
    {
        if (!_initialized) Init();
        return SubtitleConfigs[type];
    }

    /// <summary>
    ///     更新所有字幕配置
    /// </summary>
    public static void UpdateAllSubtitleConfig()
    {
        foreach (JustAnotherTranslator.SubtitleTypeEnum subtitleType in Enum.GetValues(
                     typeof(JustAnotherTranslator.SubtitleTypeEnum)))
        {
            var config = SubtitleConfig.CreateSubtitleConfig(subtitleType);
            SubtitleConfigs[subtitleType] = config;
        }

        foreach (var subtitleComponent in SubtitleIdComponentsMap.Values)
        {
            var oldConfig = subtitleComponent.GetConfig();

            subtitleComponent.UpdateConfig(SubtitleConfigs[oldConfig.SubtitleType]);

            LogManager.Debug($"Update subtitle config: {subtitleComponent.GetSubtitleId()}");
        }
    }

    /// <summary>
    ///     更新指定说话者的字幕配置
    /// </summary>
    /// <param name="speakerName">说话者名称</param>
    public static void UpdateSubtitleConfigBySpeakerName(string speakerName)
    {
        var subtitleId = GetSpeakerSubtitleId(speakerName);
        if (SubtitleIdComponentsMap.TryGetValue(subtitleId, out var subtitleComponent))
        {
            var oldConfig = subtitleComponent.GetConfig();

            subtitleComponent.UpdateConfig(SubtitleConfigs[oldConfig.SubtitleType]);

            LogManager.Debug($"Update subtitle config: {subtitleId}");
        }
    }

    /// <summary>
    ///     销毁字幕组件
    /// </summary>
    /// <param name="component">要销毁的字幕组件</param>
    public static void DestroySubtitleComponent(ISubtitleComponent component)
    {
        if (component is null)
            return;

        SubtitleIdComponentsMap.Remove(GetSpeakerSubtitleId(component.GetSpeakerName()));

        component.Destroy();

        var slotToRemove = Slots.FirstOrDefault(s => s.Subtitle == component);
        if (slotToRemove != null) Slots.Remove(slotToRemove);

        LogManager.Debug("Subtitle component destroyed");
    }

    /// <summary>
    ///     根据字幕ID销毁字幕组件
    /// </summary>
    /// <param name="subtitleId">字幕ID</param>
    public static void DestroySubtitleComponentBySubtitleId(string subtitleId)
    {
        if (SubtitleIdComponentsMap.TryGetValue(subtitleId, out var subtitleComponent))
        {
            SubtitleIdComponentsMap.Remove(subtitleId);
            subtitleComponent.Destroy();
        }

        var slotToRemove = Slots.FirstOrDefault(s => s.SubtitleId == subtitleId);
        if (slotToRemove != null) Slots.Remove(slotToRemove);
    }

    /// <summary>
    ///     销毁所有字幕组件
    /// </summary>
    public static void DestroyAllSubtitleComponents()
    {
        foreach (var component in SubtitleIdComponentsMap.Values)
            component.Destroy();

        SubtitleIdComponentsMap.Clear();
        LogManager.Debug("All subtitle components destroyed");

        Slots.Clear();
    }

    /// <summary>
    ///     记录每个字幕槽位的信息
    /// </summary>
    private class SlotInfo
    {
        public string SubtitleId { get; set; }
        public ISubtitleComponent Subtitle { get; set; }
        public float Bottom { get; set; } // 槽位底部位置（相对于屏幕底部的距离）
        public float Height { get; set; } // 槽位高度（屏幕比例）
    }
}