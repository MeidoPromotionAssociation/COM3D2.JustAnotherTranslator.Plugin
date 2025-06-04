using System;
using System.Collections.Generic;
using System.Linq; // Added for Linq operations
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

    // 屏幕底部阈值，低于此值则优先向上查找
    private const float BottomThreshold = 0.2f;

    // 屏幕顶部阈值，高于此值则优先向下查找 (实际上是大部分情况)
    private const float TopThreshold = 0.8f; // (1 - BottomThreshold)

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
        // SubtitleIdComponentsMap[GetSpeakerSubtitleId(speakerName)] = component; // Moved to ShowSubtitle to ensure map only contains active/shown components
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
        SubtitleConfig config = GetSubtitleConfig(subtitleType);

        if (!SubtitleIdComponentsMap.TryGetValue(subtitleId, out var subtitleComponent) ||
            subtitleComponent.GetGameObject() == null) // Check if GameObject is destroyed
        {
            if (subtitleComponent != null &&
                subtitleComponent.GetGameObject() == null) // Component exists but GameObject is destroyed
            {
                SubtitleIdComponentsMap.Remove(subtitleId); // Clean up stale entry
            }

            subtitleComponent = CreateSubtitleComponent(speakerName, config);
            SubtitleIdComponentsMap[subtitleId] = subtitleComponent;
        }
        else
        {
            // 如果字幕类型不同，或配置的期望垂直位置不同，则可能需要重新评估或重新创建
            var currentConfig = subtitleComponent.GetConfig();
            if (currentConfig.SubtitleType != subtitleType || currentConfig.VerticalPosition != config.VerticalPosition)
            {
                // 更新配置，而不是完全销毁和重建，除非类型根本改变
                // 如果只是VerticalPosition变了，让RefreshAllSubtitlePositions处理
                subtitleComponent.UpdateConfig(config); // Update with the new base config
            }
        }

        subtitleComponent.ShowSubtitle(text, speakerName, duration);
        RefreshAllSubtitlePositions(); // Refresh positions after showing a new subtitle
        LogManager.Debug($"Showing subtitle: [{speakerName}] {text}");
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
        {
            subtitleComponent.HideSubtitle();
            // Consider removing from map or marking as inactive for RefreshAllSubtitlePositions
            // For now, RefreshAllSubtitlePositions will filter by IsVisible()
            RefreshAllSubtitlePositions(); // Refresh positions after hiding a subtitle
        }
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
    ///     获取指定类型的字幕配置
    /// </summary>
    /// <param name="type">字幕类型</param>
    /// <returns>字幕配置</returns>
    public static SubtitleConfig GetSubtitleConfig(JustAnotherTranslator.SubtitleTypeEnum type)
    {
        if (!_initialized) Init();
        // Return a copy to prevent unintended modifications to the base config
        return SubtitleConfig.CreateSubtitleConfig(type); // Ensures fresh config based on current settings
    }

    /// <summary>
    ///     更新所有字幕配置
    /// </summary>
    public static void UpdateAllSubtitleConfig()
    {
        if (!_initialized) Init();

        // Re-create base configs
        foreach (JustAnotherTranslator.SubtitleTypeEnum subtitleType in Enum.GetValues(
                     typeof(JustAnotherTranslator.SubtitleTypeEnum)))
        {
            var newBaseConfig = SubtitleConfig.CreateSubtitleConfig(subtitleType);
            SubtitleConfigs[subtitleType] = newBaseConfig;
        }

        // Update existing components with new base configurations
        foreach (var subtitleComponent in SubtitleIdComponentsMap.Values.ToList()) // ToList to allow modification
        {
            if (subtitleComponent.GetGameObject() == null) // Stale component
            {
                SubtitleIdComponentsMap.Remove(subtitleComponent.GetSubtitleId());
                continue;
            }

            var oldConfig = subtitleComponent.GetConfig();
            var newConfigForComponent = SubtitleConfig.CreateSubtitleConfig(oldConfig.SubtitleType); // Get fresh base
            newConfigForComponent.CurrentVerticalPosition =
                oldConfig.CurrentVerticalPosition; // Preserve runtime position for now
            subtitleComponent.UpdateConfig(newConfigForComponent);
            LogManager.Debug($"Updated subtitle config for: {subtitleComponent.GetSubtitleId()}");
        }

        RefreshAllSubtitlePositions(); // Refresh positions after config updates
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
            if (subtitleComponent.GetGameObject() == null) // Stale component
            {
                SubtitleIdComponentsMap.Remove(subtitleId);
                return;
            }

            var oldConfig = subtitleComponent.GetConfig();
            var newConfig = SubtitleConfig.CreateSubtitleConfig(oldConfig.SubtitleType);
            newConfig.CurrentVerticalPosition = oldConfig.CurrentVerticalPosition; // Preserve runtime position for now
            subtitleComponent.UpdateConfig(newConfig);
            RefreshAllSubtitlePositions(); // Refresh positions after config update
            LogManager.Debug($"Updated subtitle config for: {subtitleId}");
        }
    }

    /// <summary>
    ///     销毁所有字幕组件
    /// </summary>
    public static void DestroyAllSubtitleComponents()
    {
        foreach (var component in SubtitleIdComponentsMap.Values.ToList()) // ToList to allow modification
        {
            component?.Destroy();
        }

        SubtitleIdComponentsMap.Clear();
        LogManager.Debug("All subtitle components destroyed");
    }

    // --- New methods for position calculation ---

    /// <summary>
    ///     刷新所有适用字幕组件（屏幕字幕和VR平板字幕）的垂直位置以避免重叠。
    ///     VR空间字幕不受此2D重叠逻辑影响。
    /// </summary>
    public static void RefreshAllSubtitlePositions()
    {
        if (!_initialized) Init(); // 确保已初始化

        var subtitlesToPosition = SubtitleIdComponentsMap.Values
            .Where(comp => comp != null && comp.GetGameObject() != null && comp.IsVisible() &&
                           (comp is ScreenSubtitleComponent || comp is VRTabletSubtitleComponent))
            .OrderByDescending(comp => comp.GetConfig().VerticalPosition) // 首先处理期望位置在顶部的字幕
            .ToList();

        if (!subtitlesToPosition.Any()) return;

        var placedSubtitleBounds = new List<KeyValuePair<float, float>>(); // 存储已放置字幕的Y轴底部和顶部 (归一化)

        foreach (var subtitleComponent in subtitlesToPosition)
        {
            var config = subtitleComponent.GetConfig();
            if (config == null) continue;

            float desiredYNormalized = config.VerticalPosition; // 用户期望的归一化Y位置 (0=底, 1=顶)
            float actualPixelHeight = config.BackgroundHeight; // 字幕的实际像素高度
            float subtitleHeightNormalized; // 字幕的归一化高度

            if (actualPixelHeight <= 0) actualPixelHeight = 100f; // 提供一个默认高度以防配置错误

            if (subtitleComponent is ScreenSubtitleComponent)
            {
                subtitleHeightNormalized = Screen.height > 0 ? actualPixelHeight / Screen.height : 0.1f; // 0.1f 为回退值
            }
            else if (subtitleComponent is VRTabletSubtitleComponent tabletComp)
            {
                if (tabletComp.TabletCanvasScaler != null && tabletComp.TabletCanvasScaler.referenceResolution.y > 0)
                {
                    subtitleHeightNormalized = actualPixelHeight / tabletComp.TabletCanvasScaler.referenceResolution.y;
                }
                else
                {
                    subtitleHeightNormalized = 0.1f; // 回退值
                    LogManager.Warning(
                        $"VRTabletSubtitleComponent {subtitleComponent.GetGameObject().name} missing CanvasScaler or valid referenceResolution.y");
                }
            }
            else
            {
                continue; //理论上不会执行到这里，因为上面已经筛选过类型
            }

            subtitleHeightNormalized = Mathf.Clamp(subtitleHeightNormalized, 0.01f, 1f); //确保高度在合理范围

            float newYNormalized = desiredYNormalized; // 默认为期望的Y值

            // 根据 desiredYNormalized 决定搜索策略
            bool preferSearchDown = desiredYNormalized >= TopThreshold;
            bool preferSearchUp = desiredYNormalized < BottomThreshold;

            float? foundY = null;

            if (preferSearchDown) // 通常用于靠近顶部的字幕
            {
                foundY =
                    SearchPosition(desiredYNormalized, subtitleHeightNormalized, true, placedSubtitleBounds) // 先向下搜索
                    ?? SearchPosition(desiredYNormalized, subtitleHeightNormalized, false,
                        placedSubtitleBounds); // 再向上搜索
            }
            else if (preferSearchUp) // 通常用于靠近底部的字幕
            {
                foundY =
                    SearchPosition(desiredYNormalized, subtitleHeightNormalized, false, placedSubtitleBounds) // 先向上搜索
                    ?? SearchPosition(desiredYNormalized, subtitleHeightNormalized, true,
                        placedSubtitleBounds); // 再向下搜索
            }
            else // 对于中间位置的字幕，或特定偏好未产生结果时
            {
                // 默认：从原始期望位置尝试向下然后向上
                foundY = SearchPosition(desiredYNormalized, subtitleHeightNormalized, true, placedSubtitleBounds)
                         ?? SearchPosition(desiredYNormalized, subtitleHeightNormalized, false, placedSubtitleBounds);
            }

            // 如果仍然未找到，则尝试从屏幕中间搜索作为最后的手段，以避免重叠
            if (foundY == null)
            {
                // 尝试从屏幕中心点（考虑字幕高度后）向下搜索
                foundY = SearchPosition(0.5f, subtitleHeightNormalized, true, placedSubtitleBounds)
                         // 然后尝试从屏幕中心点向上搜索
                         ?? SearchPosition(0.5f - subtitleHeightNormalized, subtitleHeightNormalized, false,
                             placedSubtitleBounds);
            }

            if (foundY.HasValue)
            {
                newYNormalized = foundY.Value;
            }
            // 如果在所有尝试后仍未找到不重叠的位置，它将使用原始的 'desiredYNormalized'，
            // 可能会导致重叠。这是一种回退机制。

            config.CurrentVerticalPosition = newYNormalized;
            subtitleComponent.UpdateConfig(config); // 应用新的 CurrentVerticalPosition

            // 将其边界添加到已放置字幕的列表中
            // Y 是字幕的底部锚点，所以顶部是 Y + 高度
            placedSubtitleBounds.Add(new KeyValuePair<float, float>(newYNormalized,
                newYNormalized + subtitleHeightNormalized));
            // 按 Y 位置排序，可以使重叠检查更快（尽管列表通常很小）
            placedSubtitleBounds = placedSubtitleBounds.OrderBy(b => b.Key).ToList();
        }

        LogManager.Debug($"Refreshed positions for {subtitlesToPosition.Count} applicable subtitles.");
    }

    /// <summary>
    ///     尝试在指定方向上找到一个不重叠的字幕位置。
    /// </summary>
    /// <param name="desiredY">期望的起始Y位置 (0-1, 底部为0, 顶部为1, 代表字幕的底部锚点)。</param>
    /// <param name="itemHeightNormalized">字幕的归一化高度 (0-1, 屏幕/画布高度的百分比)。</param>
    /// <param name="searchDown">true表示向下搜索（Y值减小），false表示向上搜索（Y值增大）。</param>
    /// <param name="existingBounds">已放置字幕的边界列表 (Key=Y-bottom, Value=Y-top, 均为归一化值)。</param>
    /// <returns>找到的无重叠Y位置 (归一化)，如果找不到则返回null。</returns>
    private static float? SearchPosition(float desiredY, float itemHeightNormalized, bool searchDown,
        List<KeyValuePair<float, float>> existingBounds)
    {
        const int maxAttempts = 30; // 限制搜索尝试次数以防止无限循环
        float step = itemHeightNormalized / 4; // 搜索步长，可以调整。步长越小，放置越精细。最小为1像素。
        if (step <= 0) step = 0.01f; // 避免步长为0或负

        for (int i = 0; i < maxAttempts; i++)
        {
            float currentY = searchDown ? desiredY - (i * step) : desiredY + (i * step);

            // 确保 currentY 在屏幕边界内 (0 到 1-itemHeightNormalized)
            // 如果 currentY 是底部锚点，则 currentY + itemHeightNormalized 是顶部。
            // 所以, currentY 必须 >= 0 且 currentY + itemHeightNormalized <= 1
            // 这意味着 currentY <= 1 - itemHeightNormalized

            // 先检查原始currentY是否重叠，如果重叠且超出边界，则后续的Clamp可能无意义
            // 但如果初始位置就在边界外，但又不重叠，则Clamp它可能是有效的
            bool initiallyOutOfBounds = currentY < 0 || currentY > (1f - itemHeightNormalized);

            if (initiallyOutOfBounds)
            {
                // 如果第一次尝试(i=0)就出界了
                if (i == 0)
                {
                    float clampedY = Mathf.Clamp(currentY, 0f, 1f - itemHeightNormalized);
                    if (!CheckOverlap(clampedY, itemHeightNormalized, existingBounds))
                    {
                        return clampedY; // 出界但Clamp后不重叠，则使用
                    }

                    // 如果Clamp后仍然重叠，或者不是第一次尝试就出界，则此方向搜索失败
                    if (searchDown && currentY < 0) return null; // 向下搜索，已低于底部
                    if (!searchDown && currentY > (1f - itemHeightNormalized)) return null; // 向上搜索，已高于顶部
                }
                else
                {
                    // 非首次尝试，如果出界则此方向搜索失败
                    return null;
                }
            }

            // 对仍在界内的currentY进行检查
            currentY = Mathf.Clamp(currentY, 0f, 1f - itemHeightNormalized);

            if (!CheckOverlap(currentY, itemHeightNormalized, existingBounds))
            {
                return currentY; // 找到一个不重叠的位置
            }
        }

        return null; // 未找到位置
    }

    /// <summary>
    ///     检查提议的字幕位置和大小是否与任何现有字幕重叠。
    /// </summary>
    /// <param name="yPosNormalized">提议的字幕底部Y位置 (0-1, 归一化)。</param>
    /// <param name="itemHeightNormalized">提议的字幕高度 (0-1, 归一化)。</param>
    /// <param name="existingBounds">已放置字幕的边界列表 (Key=Y-bottom, Value=Y-top, 均为归一化值)。</param>
    /// <returns>如果重叠则为true，否则为false。</returns>
    private static bool CheckOverlap(float yPosNormalized, float itemHeightNormalized,
        List<KeyValuePair<float, float>> existingBounds)
    {
        float proposedBottom = yPosNormalized;
        float proposedTop = yPosNormalized + itemHeightNormalized;

        // 添加一个小的缓冲区以防止字幕看起来完全接触
        const float buffer = 0.001f; // 0.1% of screen/canvas height

        foreach (var bound in existingBounds)
        {
            float existingBottom = bound.Key;
            float existingTop = bound.Value;

            // 检查重叠：
            // (提议的底部 < (现有顶部 + 缓冲区)) AND (提议的顶部 > (现有底部 - 缓冲区))
            if (proposedBottom < (existingTop + buffer) && proposedTop > (existingBottom - buffer))
            {
                return true; // 检测到重叠
            }
        }

        return false; // 没有重叠
    }
}