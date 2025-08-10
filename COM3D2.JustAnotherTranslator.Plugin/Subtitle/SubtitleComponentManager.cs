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
    private static readonly Dictionary<string, ISubtitleComponent>
        SubtitleIdComponentsMap = new(); // 字幕 ID ->字幕组件

    /// 所有种类字幕的配置字典
    private static readonly Dictionary<JustAnotherTranslator.SubtitleTypeEnum, SubtitleConfig>
        SubtitleConfigs = new(); // 字幕类型 -> 字幕配置

    /// 活跃字幕列表
    private static readonly List<SubtitlePositionInfo> ActiveSubtitles = new();

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
    private static ISubtitleComponent CreateSubtitleComponent(string speakerName,
        SubtitleConfig config)
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
                LogManager.Error(
                    "Default subtitle component is null, please report this issue/默认字幕组件为空，请报告此问题");
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

        CalculateNewPosition(subtitleComponent);

        subtitleComponent.ShowSubtitle(text, speakerName, duration);
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

        ActiveSubtitles.RemoveAll(s => s.Id == subtitleId);
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
    public static void CalculateNewPosition(ISubtitleComponent subtitleComponent)
    {
        if (!_initialized)
            Init();

        var config = subtitleComponent.GetConfig();
        if (config == null) return;

        // VR 模式使用专门的空间位置计算
        if (JustAnotherTranslator.IsVrMode)
        {
            switch (config.VRSubtitleMode)
            {
                case JustAnotherTranslator.VRSubtitleModeEnum.OnTablet:
                    CalculateVRTabletPosition(subtitleComponent, config);
                    return;
                case JustAnotherTranslator.VRSubtitleModeEnum.InSpace:
                    CalculateVRSpacePosition(subtitleComponent, config);
                    return;
                default:
                    LogManager.Error(
                        $"Unknown VR mode, please report this issue/未知 VR 模式，请报告此问题 {config.VRSubtitleMode}");
                    return;
            }
        }

        try
        {
            // 获取当前字幕类型的高度
            var subtitleHeight = subtitleComponent.GetSubtitleHeight();
            var currentSubtitleId = subtitleComponent.GetSubtitleId();

            // 查找现有条目以确定初始位置
            var existingEntry = ActiveSubtitles.FirstOrDefault(s => s.Id == currentSubtitleId);
            // 如果已存在条目，尝试放回已记录的位置
            var initialY = existingEntry.Id != null
                ? existingEntry.InitialVerticalPosition
                : config.CurrentVerticalPosition;

            // 在查找新位置之前，删除此字幕的现有条目
            ActiveSubtitles.RemoveAll(s => s.Id == currentSubtitleId);

            var finalY = initialY;

            const int maxSearchDistance = 1080; // 上下最大搜索距离
            const int screenHeight = 1080; // 参考屏幕高度

            // 检查位置是否与任何活动字幕重叠
            // 屏幕模式以左下角为锚点，VerticalPosition 是字幕的“底边”像素
            // 新字幕：从 z 开始向上占 subtitleHeight 像素（从 z 到 z + subtitleHeight）
            // 旧字幕：从 s.VerticalPosition 开始向上占 s.Height 像素（从 s.VerticalPosition 到 s.VerticalPosition + s.Height)
            // 新字幕完全在旧字幕上面：z ≥ s.VerticalPosition + s.Height，取反 z < s.VerticalPosition + s.Height
            // 新字幕完全在旧字幕下面：s.VerticalPosition ≥ z + subtitleHeight 取反 s.VerticalPosition < z + subtitleHeight
            // 因此取反新字幕完全不在旧字幕上面，且新字幕完全不在旧字幕下面，就算重叠
            bool Overlaps(float z) => ActiveSubtitles.Any(s =>
                z < s.VerticalPosition + s.Height &&
                s.VerticalPosition < z + subtitleHeight);

            LogManager.Debug(
                $"Finding available position for {currentSubtitleId}, checking initial position: {initialY}, subtitle height: {subtitleHeight}, overlaps: {Overlaps(initialY)}");

            foreach (var subtitle in ActiveSubtitles)
            {
                LogManager.Debug(
                    $"Active subtitle: {subtitle.Id} height: {subtitle.Height} position: {subtitle.VerticalPosition} initial position: {subtitle.InitialVerticalPosition}");
            }

            if (Overlaps(initialY))
            {
                var foundPosition = false;
                // 先向下搜索可用位置
                for (var offset = 1; offset <= maxSearchDistance; offset++)
                {
                    var testY = initialY - offset;
                    if (testY < 0) break; // 到达屏幕底部

                    if (!Overlaps(testY))
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

                        if (!Overlaps(testY))
                        {
                            finalY = testY;
                            break;
                        }
                    }
            }

            LogManager.Debug(
                $"Calculated subtitle position: {finalY} for subtitle: {currentSubtitleId}");

            // 更新配置和活动字幕列表
            config.CurrentVerticalPosition = finalY;
            subtitleComponent.SetVerticalPosition(finalY);

            ActiveSubtitles.Add(new SubtitlePositionInfo
            {
                Id = currentSubtitleId,
                VerticalPosition = finalY,
                Height = subtitleHeight,
                InitialVerticalPosition = initialY
            });
        }
        catch (Exception ex)
        {
            LogManager.Error(
                $"Calculate subtitle position failed, using default position/计算字幕位置失败，使用默认位置: {ex.Message}\n{ex.StackTrace}");
            config.CurrentVerticalPosition = config.VerticalPosition;
            subtitleComponent.SetVerticalPosition(config.CurrentVerticalPosition);
        }
    }

    /// <summary>
    ///     VR 平板电脑模式的字幕位置计算
    /// </summary>
    /// <param name="subtitleComponent"></param>
    /// <param name="config"></param>
    private static void CalculateVRTabletPosition(ISubtitleComponent subtitleComponent,
        SubtitleConfig config)
    {
        try
        {
            // 将像素高度转换为世界单位（Canvas 在 WorldSpace 模式下缩放 0.001）
            var subtitleHeightWorld = config.VRTabletSubtitleHeight * 0.001f;
            var currentSubtitleId = subtitleComponent.GetSubtitleId();

            // 查找现有条目以确定初始位置
            var existingEntry = ActiveSubtitles.FirstOrDefault(s => s.Id == currentSubtitleId);
            // 如果已存在条目，尝试放回已记录的位置
            var initialZ = existingEntry.Id != null
                ? existingEntry.InitialVerticalPosition
                : config.VRTabletSubtitleVerticalPosition;

            // 在查找新位置之前，删除此字幕的现有条目
            ActiveSubtitles.RemoveAll(s => s.Id == currentSubtitleId);

            var finalZ = initialZ;

            const int maxSteps = 2100; // 最大搜索步数
            var step = subtitleHeightWorld; // 步长 = 字幕世界高度

            // 检查给定位置是否与任何活动字幕重叠
            // 平板电脑模式锚点为中心，即字幕垂直位置设置的是字幕中心的位置
            // 因此两个中心之间的距离 d = Abs(s.VerticalPosition − z)
            // 任意两个字幕中心之间的距离小于设定的字幕高度，则重叠
            bool Overlaps(float z) => ActiveSubtitles.Any(s =>
                Mathf.Abs(s.VerticalPosition - z) < subtitleHeightWorld);
            // Abs(s.VerticalPosition − z) < (subtitleHeightWorld + s.Height) * 0.5f − epsilon
            // epsilon = 1e-6f

            LogManager.Debug(
                $"Checking initial position: {initialZ}, subtitle height: {config.VRTabletSubtitleHeight} world height: {subtitleHeightWorld}, overlaps: {Overlaps(initialZ)}");

            foreach (var subtitle in ActiveSubtitles)
            {
                LogManager.Debug(
                    $"Active subtitle: {subtitle.Id} height: {subtitle.Height} position: {subtitle.VerticalPosition} initial position: {subtitle.InitialVerticalPosition}");
            }

            if (Overlaps(initialZ))
            {
                var found = false;
                // 先向下搜索（Z 轴负方向）
                for (var i = 1; i <= maxSteps; i++)
                {
                    var testZ = initialZ - i * step;
                    if (!Overlaps(testZ))
                    {
                        finalZ = testZ;
                        found = true;
                        break;
                    }
                }

                // 如果向下搜索不到，则向上搜索
                if (!found)
                {
                    for (var i = 1; i <= maxSteps; i++)
                    {
                        var testZ = initialZ + i * step;
                        if (!Overlaps(testZ))
                        {
                            finalZ = testZ;
                            break;
                        }
                    }
                }
            }

            // 更新配置和活动字幕列表
            config.CurrentVerticalPosition = finalZ;
            subtitleComponent.SetVerticalPosition(finalZ);

            ActiveSubtitles.Add(new SubtitlePositionInfo
            {
                Id = currentSubtitleId,
                VerticalPosition = finalZ,
                Height = subtitleHeightWorld,
                InitialVerticalPosition = initialZ
            });

            LogManager.Debug(
                $"Calculated VR tablet subtitle position: {finalZ} for subtitle: {currentSubtitleId}");
        }
        catch (Exception ex)
        {
            LogManager.Error(
                $"Calculate VR tablet subtitle position failed/计算字幕位置失败，使用默认位置:: {ex.Message}\n{ex.StackTrace}");
        }
    }

    /// <summary>
    ///     VR 空间模式的字幕位置计算
    /// </summary>
    /// <param name="subtitleComponent"></param>
    /// <param name="config"></param>
    private static void CalculateVRSpacePosition(ISubtitleComponent subtitleComponent,
        SubtitleConfig config)
    {
        try
        {
            // 计算字幕在视野中的角高（度数），根据字幕高度与距离推算
            // Canvas 在 WorldSpace 模式下采用比例 0.001 => 1000px = 1m
            var subtitleHeightWorld = Mathf.Max(config.VRSpaceSubtitleHeight * 0.001f, 0.001f);
            var distance = Mathf.Max(config.VRSubtitleDistance, 0.001f);
            // 角度 = 2 * atan(h/2d)
            var angularHeightDeg =
                2f * Mathf.Rad2Deg * Mathf.Atan(subtitleHeightWorld / (2f * distance));
            if (angularHeightDeg < 0.1f) angularHeightDeg = 0.1f; // 最小限制

            // 间隔
            var stepDeg = angularHeightDeg;

            var currentSubtitleId = subtitleComponent.GetSubtitleId();

            // 查找已存在条目以获取之前的位置
            var existingEntry = ActiveSubtitles.FirstOrDefault(s => s.Id == currentSubtitleId);
            var initialOffset = existingEntry.Id != null
                ? existingEntry.InitialVerticalPosition
                : config.VRSubtitleVerticalOffset;

            // 删除旧记录
            ActiveSubtitles.RemoveAll(s => s.Id == currentSubtitleId);

            float finalOffset = initialOffset;

            bool Overlaps(float offset) => ActiveSubtitles.Any(s =>
                Mathf.Abs(s.VerticalPosition - offset) < stepDeg * 0.5f);

            if (Overlaps(initialOffset))
            {
                const int maxSteps = 100;
                var found = false;
                // 先向下（更负的角度）
                for (var i = 1; i <= maxSteps; i++)
                {
                    var test = initialOffset - i * stepDeg;
                    if (!Overlaps(test))
                    {
                        finalOffset = test;
                        found = true;
                        break;
                    }
                }

                // 再向上
                if (!found)
                {
                    for (var i = 1; i <= maxSteps; i++)
                    {
                        var test = initialOffset + i * stepDeg;
                        if (!Overlaps(test))
                        {
                            finalOffset = test;
                            break;
                        }
                    }
                }
            }

            // 更新配置并刷新 UI
            config.VRSubtitleVerticalOffset = finalOffset;
            // CurrentVerticalPosition 不使用但保持同步
            config.CurrentVerticalPosition = finalOffset;
            subtitleComponent.UpdateConfig(config);

            // 记录活跃字幕
            ActiveSubtitles.Add(new SubtitlePositionInfo
            {
                Id = currentSubtitleId,
                VerticalPosition = finalOffset,
                Height = angularHeightDeg, // 记录角高，便于调试
                InitialVerticalPosition = initialOffset
            });

            LogManager.Debug(
                $"Calculated VR space subtitle offset: {finalOffset}° for subtitle: {currentSubtitleId}");
        }
        catch (Exception ex)
        {
            LogManager.Error(
                $"Calculate VR space subtitle position failed: {ex.Message}\n{ex.StackTrace}");
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

        var subtitleId = GetSpeakerSubtitleId(component.GetSpeakerName());

        SubtitleIdComponentsMap.Remove(subtitleId);

        component.Destroy();

        ActiveSubtitles.RemoveAll(s => s.Id == subtitleId);

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

        ActiveSubtitles.RemoveAll(s => s.Id == subtitleId);
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

        ActiveSubtitles.Clear();
    }

    /// 活跃字幕的位置信息结构
    private struct SubtitlePositionInfo
    {
        public string Id { get; set; }
        public float VerticalPosition { get; set; }
        public float Height { get; set; }
        public float InitialVerticalPosition { get; set; }
    }
}