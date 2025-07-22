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
    /// 是否已初始化
    private static bool _initialized;

    /// 字幕 ID 映射，用于跟踪说话者与字幕组件的关系
    private static readonly Dictionary<string, ISubtitleComponent> SubtitleIdComponentsMap = new(); // 字幕 ID ->字幕组件

    /// 所有种类字幕的配置字典
    private static readonly Dictionary<JustAnotherTranslator.SubtitleTypeEnum, SubtitleConfig>
        SubtitleConfigs = new(); // 字幕类型 -> 字幕配置

    /// 活跃字幕列表
    private static readonly List<SubtitlePositionInfo> _activeSubtitles = new();

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

        var subtitleID = GetSpeakerSubtitleId(speakerName);

        var gameObject = new GameObject(subtitleID);
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

        component.Init(config, subtitleID);

        SubtitleIdComponentsMap[subtitleID] = component;

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

        _activeSubtitles.RemoveAll(s => s.Id == subtitleId);
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
            var subtitleHeight = subtitleComponent.GetHeight();
            var currentSubtitleId = subtitleComponent.GetSubtitleId();

            // 在查找新位置之前，删除此字幕的现有条目
            _activeSubtitles.RemoveAll(s => s.Id == currentSubtitleId);

            var initialY = config.VerticalPosition;
            var finalY = initialY;

            const int maxSearchDistance = 1080; // 上下最大搜索距离
            const int screenHeight = 1080; // 参考屏幕高度

            // 检查初始位置是否与任何活动字幕重叠
            var overlaps = _activeSubtitles.Any(s =>
                initialY < s.VerticalPosition + s.Height && s.VerticalPosition < initialY + subtitleHeight);

            if (overlaps)
            {
                var foundPosition = false;
                // 先向下搜索可用位置
                for (var offset = 1; offset <= maxSearchDistance; offset++)
                {
                    var testY = initialY - offset;
                    if (testY < 0) break; // 到达屏幕底部

                    if (!_activeSubtitles.Any(s =>
                            testY < s.VerticalPosition + s.Height && s.VerticalPosition < testY + subtitleHeight))
                    {
                        finalY = testY;
                        foundPosition = true;
                        break;
                    }
                }

                // 如果向下搜索不到，则向上搜索
                if (!foundPosition)
                    for (var offset = 1; offset <= maxSearchDistance; offset++)
                    {
                        var testY = initialY + offset;
                        if (testY + subtitleHeight > screenHeight) break; // 到达屏幕顶部

                        if (!_activeSubtitles.Any(s =>
                                testY < s.VerticalPosition + s.Height && s.VerticalPosition < testY + subtitleHeight))
                        {
                            finalY = testY;
                            foundPosition = true;
                            break;
                        }
                    }
            }

            LogManager.Debug($"Calculated subtitle position: {finalY} for subtitle: {currentSubtitleId}");
            // 更新配置和活动字幕列表
            config.CurrentVerticalPosition = finalY;
            subtitleComponent.SetVerticalPosition(finalY);

            _activeSubtitles.Add(new SubtitlePositionInfo
            {
                Id = currentSubtitleId,
                VerticalPosition = finalY,
                Height = subtitleHeight
            });
        }
        catch (Exception ex)
        {
            LogManager.Error(
                $"Calculate subtitle position error, using default position/计算字幕位置错误，使用默认位置: {ex.Message}\n{ex.StackTrace}");
            config.CurrentVerticalPosition = config.VerticalPosition;
            subtitleComponent.UpdateConfig(config);
        }
    }

    private static void CalculateVRSpacePosition(ISubtitleComponent subtitleComponent, SubtitleConfig config)
    {
        //TODO
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

        var subtitleId = GetSpeakerSubtitleId(component.GetSpeakerName());

        SubtitleIdComponentsMap.Remove(subtitleId);

        component.Destroy();

        _activeSubtitles.RemoveAll(s => s.Id == subtitleId);

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

        _activeSubtitles.RemoveAll(s => s.Id == subtitleId);
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

        _activeSubtitles.Clear();
    }

    /// 活跃字幕的位置信息结构
    private struct SubtitlePositionInfo
    {
        public string Id { get; set; }
        public float VerticalPosition { get; set; }
        public float Height { get; set; }
    }
}