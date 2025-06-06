using System;
using System.Collections.Generic;
using System.Linq;
using COM3D2.JustAnotherTranslator.Plugin.Subtitle.Component;
using UnityEngine;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle;

/// <summary>
///     字幕组件管理器，负责管理所有字幕组件的生命周期和配置
/// </summary>
public static class SubtitleComponentManager
{
    // 是否已初始化
    private static bool _initialized;

    // 字幕 ID 映射，用于跟踪说话者与字幕组件的关系
    private static readonly Dictionary<string, ISubtitleComponent> SubtitleIdComponentsMap = new(); // 字幕 ID，字幕组件

    // 所有种类字幕的配置字典
    private static readonly Dictionary<JustAnotherTranslator.SubtitleTypeEnum, SubtitleConfig> SubtitleConfigs = new();

    // 槽位相关常量
    private const float BOTTOM_MARGIN = 0.03f; // 底部边距（屏幕高度的3%）
    private const float SPACING = 0.00f; // 槽位之间的间距（屏幕高度的%）

    // 槽位列表
    private static readonly List<SlotInfo> _slots = new();

    // 记录每个槽位的信息
    private class SlotInfo
    {
        public string SubtitleId { get; set; }
        public ISubtitleComponent Subtitle { get; set; }
        public float Bottom { get; set; } // 槽位底部位置（相对于屏幕底部的距离）
        public float Height { get; set; } // 槽位高度（屏幕比例）
    }

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
        if (string.IsNullOrEmpty(text))
            return;

        if (!_initialized)
            Init();

        var subtitleId = GetSpeakerSubtitleId(speakerName);

        if (!SubtitleIdComponentsMap.TryGetValue(subtitleId, out var subtitleComponent))
        {
            subtitleComponent = CreateSubtitleComponent(speakerName, GetSubtitleConfig(subtitleType));
            SubtitleIdComponentsMap[subtitleId] = subtitleComponent;
        }
        else
        {
            // 如果字幕类型不同，重新创建
            if (subtitleComponent.GetConfig().SubtitleType != subtitleType)
            {
                subtitleComponent.Destroy();
                SubtitleIdComponentsMap.Remove(subtitleId);
                subtitleComponent = CreateSubtitleComponent(speakerName, GetSubtitleConfig(subtitleType));
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

        try
        {
            // 获取当前字幕类型的高度
            float subtitleHeight = GetSubtitleTypeHeight(subtitleType) * 2; // 我不知道为什么实际占用高度是 2 倍
            float newPosition = 0f;
            bool foundSlot = false;

            // 检查是否已经存在相同ID的字幕
            var existingSlot = _slots.FirstOrDefault(s => s.SubtitleId == subtitleComponent.GetSubtitleId());

            if (existingSlot != null)
            {
                // 如果存在相同ID的字幕，重用其位置
                newPosition = existingSlot.Bottom;
                foundSlot = true;

                // 移除旧槽位
                _slots.Remove(existingSlot);
            }
            // 如果没有相同说话者的活跃字幕，则查找新位置
            else if (_slots.Count == 0)
            {
                // 如果没有其他字幕，放在最顶部
                newPosition = 1f - subtitleHeight;
                foundSlot = true;
            }
            else
            {
                // 按顶部位置降序排序（从高到低）
                _slots.Sort((a, b) => (b.Bottom + b.Height).CompareTo(a.Bottom + a.Height));

                // 检查顶部是否有足够空间
                var topSlot = _slots[0];
                float topSlotTop = topSlot.Bottom + topSlot.Height;
                float availableSpace = 1f - topSlotTop;

                if (availableSpace >= subtitleHeight + SPACING)
                {
                    // 顶部有足够空间
                    newPosition = 1f - subtitleHeight;
                    foundSlot = true;
                }
                else
                {
                    // 检查其他槽位之间的空间
                    for (int i = 0; i < _slots.Count - 1; i++)
                    {
                        float currentBottom = _slots[i].Bottom;
                        float nextTop = _slots[i + 1].Bottom + _slots[i + 1].Height;

                        if (currentBottom - nextTop >= subtitleHeight + SPACING)
                        {
                            // 找到足够的空间
                            newPosition = currentBottom - SPACING - subtitleHeight;
                            foundSlot = true;
                            break;
                        }
                    }

                    // 检查底部是否有足够空间
                    if (!foundSlot)
                    {
                        float bottomSlotBottom = _slots.Min(s => s.Bottom);
                        if (bottomSlotBottom >= BOTTOM_MARGIN + subtitleHeight + SPACING)
                        {
                            // 底部有足够空间
                            newPosition = bottomSlotBottom - SPACING - subtitleHeight;
                            foundSlot = true;
                        }
                    }
                }
            }

            // 如果找到了合适的位置，添加新槽位
            if (foundSlot)
            {
                var newSlot = new SlotInfo
                {
                    SubtitleId = subtitleComponent.GetSubtitleId(),
                    Subtitle = subtitleComponent,
                    Bottom = newPosition,
                    Height = subtitleHeight
                };

                _slots.Add(newSlot);

                // 更新字幕位置
                config.CurrentVerticalPosition = newPosition;
                subtitleComponent.SetVerticalPosition(newPosition);

                LogManager.Debug(
                    $"Subtitle {subtitleComponent.GetSubtitleId()} placed at {newPosition}, height: {subtitleHeight}");
            }
            else
            {
                // 如果还是无法放置，使用默认位置
                config.CurrentVerticalPosition = config.VerticalPosition;
                subtitleComponent.UpdateConfig(config);
                LogManager.Info(
                    $"Subtitle placement failed, using default position/字幕无法放置，使用默认位置: {subtitleComponent.GetSubtitleId()}");
            }
        }
        catch (Exception ex)
        {
            LogManager.Error($"Calculate subtitle position error/计算字幕位置时出错: {ex.Message}\n{ex.StackTrace}");
            config.CurrentVerticalPosition = config.VerticalPosition;
            subtitleComponent.UpdateConfig(config);
        }
    }

    // 获取指定类型字幕的固定高度
    private static float GetSubtitleTypeHeight(JustAnotherTranslator.SubtitleTypeEnum type)
    {
        SubtitleConfigs.TryGetValue(type, out var config);
        if (config == null)
        {
            return 0f;
        }

        if (config.BackgroundHeight >= 1.0f)
        {
            return config.BackgroundHeight / Screen.height;
        }

        return config.BackgroundHeight;
    }

    // 移除字幕的方法
    public static void RemoveSubtitle(ISubtitleComponent subtitleComponent)
    {
        var slotToRemove = _slots.FirstOrDefault(s => s.Subtitle == subtitleComponent);
        if (slotToRemove != null)
        {
            _slots.Remove(slotToRemove);
            LogManager.Debug($"移除字幕 {subtitleComponent.GetSubtitleId()}");
        }
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

        LogManager.Debug("Subtitle component destroyed");
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
    }
}