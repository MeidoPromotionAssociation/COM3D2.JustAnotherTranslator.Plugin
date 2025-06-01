using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle;

/// <summary>
///     字幕组件管理器，负责管理所有字幕组件的生命周期和配置
/// </summary>
public static class SubtitleComponentManager
{
    // 活动字幕组件列表
    private static readonly List<ISubtitleComponent> SubtitleComponents = new();

    // 字幕ID映射，用于跟踪说话者与字幕组件的关系
    private static readonly Dictionary<string, ISubtitleComponent> SubtitleIdMap = new();

    // 是否已初始化
    private static bool _initialized;

    // VR头部变换
    private static Transform _vrHeadTransform;

    // 字幕配置字典
    private static Dictionary<JustAnotherTranslator.SubtitleTypeEnum, SubtitleConfig> _subtitleConfigs;

    /// <summary>
    ///     初始化字幕组件管理器
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;

        // 从插件配置中获取字幕配置
        GetConfigFromPluginConfig();

        // 如果在VR模式下，初始化VR头部跟踪
        if (JustAnotherTranslator.IsVrMode) InitVRComponents();

        _initialized = true;
        LogManager.Debug("字幕组件管理器已初始化/Subtitle component manager initialized");
    }

    /// <summary>
    ///     初始化VR组件
    /// </summary>
    private static void InitVRComponents()
    {
        // 查找 OvrMgr 的 EyeAnchor
        if (GameMain.Instance is not null && GameMain.Instance.OvrMgr is not null)
        {
            _vrHeadTransform = GameMain.Instance.OvrMgr.EyeAnchor;
            if (_vrHeadTransform is not null)
            {
                LogManager.Debug(
                    "VR头部变换 (EyeAnchor) 已找到，字幕头部跟踪已启用/VR head transform (EyeAnchor) found, subtitle head tracking enabled");
                return;
            }
        }

        // 如果无法通过GameMain.Instance.OvrMgr获取，尝试直接查找OvrMgr
        var ovrMgr = Object.FindObjectOfType<OvrMgr>();
        if (ovrMgr is not null && ovrMgr.EyeAnchor is not null)
        {
            _vrHeadTransform = ovrMgr.EyeAnchor;
            LogManager.Debug(
                "通过FindObjectOfType<OvrMgr>()找到VR头部变换，字幕头部跟踪已启用/VR head transform found through FindObjectOfType<OvrMgr>(), subtitle head tracking enabled");
        }
        else
        {
            LogManager.Warning(
                "找不到VR头部变换，头部字幕跟踪将无法工作/VR head transform not found, head tracking will not work");
        }
    }

    /// <summary>
    ///     获取VR头部变换
    /// </summary>
    /// <returns>VR头部变换</returns>
    public static Transform GetVRHeadTransform()
    {
        return _vrHeadTransform;
    }

    /// <summary>
    ///     根据配置创建合适类型的字幕组件
    /// </summary>
    /// <param name="config">字幕配置</param>
    /// <returns>字幕组件</returns>
    private static ISubtitleComponent CreateSubtitleComponent(SubtitleConfig config)
    {
        if (!_initialized) Initialize();

        // 创建游戏对象
        var gameObject = new GameObject("SubtitleComponent");
        ISubtitleComponent component;

        // 根据配置类型创建不同的字幕组件
        if (JustAnotherTranslator.IsVrMode)
        {
            // VR模式下根据VR字幕类型创建不同组件
            switch (config.VRSubtitleType)
            {
                case JustAnotherTranslator.VRSubtitleTypeEnum.Space:
                    component = gameObject.AddComponent<VRSpaceSubtitleComponent>();
                    LogManager.Debug("已创建VR空间字幕组件/Created VR space subtitle component");
                    break;
                case JustAnotherTranslator.VRSubtitleTypeEnum.Tablet:
                    component = gameObject.AddComponent<VRTabletSubtitleComponent>();
                    LogManager.Debug("已创建VR平板字幕组件/Created VR tablet subtitle component");
                    break;
                default:
                    component = gameObject.AddComponent<ScreenSubtitleComponent>();
                    LogManager.Debug("已创建屏幕字幕组件(VR默认)/Created screen subtitle component (VR default)");
                    break;
            }
        }
        else
        {
            // 非VR模式只使用屏幕字幕
            component = gameObject.AddComponent<ScreenSubtitleComponent>();
            LogManager.Debug("已创建屏幕字幕组件/Created screen subtitle component");
        }

        // 初始化组件
        component.Initialize(config);

        // 添加到活动组件列表
        SubtitleComponents.Add(component);

        return component;
    }

    /// <summary>
    ///     销毁字幕组件
    /// </summary>
    /// <param name="component">要销毁的字幕组件</param>
    public static void DestroySubtitleComponent(ISubtitleComponent component)
    {
        if (component is null) return;

        // 从活动组件列表中移除
        SubtitleComponents.Remove(component);

        // 从ID映射中移除
        var keysToRemove = new List<string>();
        foreach (var kvp in SubtitleIdMap)
            if (kvp.Value == component)
                keysToRemove.Add(kvp.Key);

        foreach (var key in keysToRemove) SubtitleIdMap.Remove(key);

        // 销毁组件
        component.Destroy();

        LogManager.Debug("字幕组件已销毁/Subtitle component destroyed");
    }

    /// <summary>
    ///     销毁所有字幕组件
    /// </summary>
    public static void DestroyAllSubtitleComponents()
    {
        foreach (var component in SubtitleComponents) component.Destroy();

        SubtitleComponents.Clear();
        SubtitleIdMap.Clear();
        LogManager.Debug("所有字幕组件已销毁/All subtitle components destroyed");
    }

    /// <summary>
    ///     更新所有字幕组件的配置
    /// </summary>
    public static void UpdateAllSubtitleConfigs()
    {
        if (!_initialized) Initialize();

        // 获取当前字幕类型的配置
        var config = _subtitleConfigs[JustAnotherTranslator.SubtitleType.Value];

        // 更新所有字幕组件的配置
        foreach (var component in SubtitleComponents) component.UpdateConfig(config);

        LogManager.Debug("所有字幕组件配置已更新/All subtitle component configs updated");
    }

    /// <summary>
    ///     更新指定字幕组件的配置
    /// </summary>
    /// <param name="component">字幕组件</param>
    /// <param name="config">新的字幕配置</param>
    public static void UpdateSubtitleConfig(ISubtitleComponent component, SubtitleConfig config)
    {
        if (component is null) return;

        component.UpdateConfig(config);
    }

    /// <summary>
    ///     获取活动字幕组件数量
    /// </summary>
    /// <returns>活动字幕组件数量</returns>
    public static int GetActiveSubtitleComponentCount()
    {
        return SubtitleComponents.Count;
    }

    /// <summary>
    ///     获取所有字幕组件列表
    /// </summary>
    /// <returns>字幕组件列表</returns>
    public static List<ISubtitleComponent> GetAllSubtitles()
    {
        return new List<ISubtitleComponent>(SubtitleComponents);
    }

    /// <summary>
    ///     显示字幕
    /// </summary>
    /// <param name="text">字幕文本</param>
    /// <param name="speakerName">说话者名称</param>
    /// <param name="duration">显示时长，0为无限</param>
    public static void ShowSubtitle(string text, string speakerName, float duration = 0f)
    {
        if (string.IsNullOrEmpty(text))
            return;

        if (!_initialized) Initialize();

        // 获取当前字幕类型的配置
        var config = _subtitleConfigs[JustAnotherTranslator.SubtitleType.Value];

        // 生成字幕ID
        var subtitleId = GetSpeakerSubtitleId(speakerName);

        // 查找是否已存在该ID的字幕组件
        if (!SubtitleIdMap.TryGetValue(subtitleId, out var subtitleComponent))
        {
            // 不存在则创建新组件
            subtitleComponent = CreateSubtitleComponent(config);
            SubtitleIdMap[subtitleId] = subtitleComponent;
        }

        // 显示字幕
        subtitleComponent.ShowSubtitle(text, speakerName, duration);

        LogManager.Debug($"显示字幕：[{speakerName}] {text}/Showing subtitle: [{speakerName}] {text}");
    }

    /// <summary>
    ///     为指定Maid显示字幕
    /// </summary>
    /// <param name="text">字幕文本</param>
    /// <param name="maid">Maid对象</param>
    /// <param name="duration">显示时长，0为无限</param>
    public static void ShowSubtitle(string text, Maid maid, float duration = 0f)
    {
        if (string.IsNullOrEmpty(text) || maid is null)
            return;

        var speakerName = MaidInfo.GetMaidFullName(maid);
        ShowSubtitle(text, speakerName, duration);
    }

    /// <summary>
    ///     显示浮动字幕（向后兼容方法）
    /// </summary>
    /// <param name="text">字幕文本</param>
    /// <param name="speakerName">说话者名称</param>
    /// <param name="duration">显示时长，0为无限</param>
    /// <param name="config">字幕配置</param>
    public static void ShowFloatingSubtitle(string text, string speakerName, float duration, SubtitleConfig config)
    {
        if (string.IsNullOrEmpty(text))
            return;

        // 生成字幕ID
        var subtitleId = GetSpeakerSubtitleId(speakerName);

        // 查找是否已存在该ID的字幕组件
        if (!SubtitleIdMap.TryGetValue(subtitleId, out var subtitleComponent))
        {
            // 不存在则创建新组件
            subtitleComponent = CreateSubtitleComponent(config);
            SubtitleIdMap[subtitleId] = subtitleComponent;
        }

        // 显示字幕
        subtitleComponent.ShowSubtitle(text, speakerName, duration);

        LogManager.Debug($"显示浮动字幕：[{speakerName}] {text}/Showing floating subtitle: [{speakerName}] {text}");
    }

    /// <summary>
    ///     隐藏字幕
    /// </summary>
    /// <param name="subtitleId">字幕ID</param>
    public static void HideSubtitle(string subtitleId)
    {
        if (string.IsNullOrEmpty(subtitleId))
            return;

        if (SubtitleIdMap.TryGetValue(subtitleId, out var subtitleComponent))
        {
            subtitleComponent.HideSubtitle();
            LogManager.Debug($"隐藏字幕：{subtitleId}/Hiding subtitle: {subtitleId}");
        }
    }

    /// <summary>
    ///     隐藏特定Maid的字幕
    /// </summary>
    /// <param name="maid">Maid对象</param>
    public static void HideSubtitle(Maid maid)
    {
        if (maid is null)
            return;

        var speakerName = MaidInfo.GetMaidFullName(maid);
        var subtitleId = GetSpeakerSubtitleId(speakerName);
        HideSubtitle(subtitleId);
    }

    /// <summary>
    ///     从说话者名称获取字幕ID
    /// </summary>
    /// <param name="speakerName">说话者名称</param>
    /// <returns>字幕ID</returns>
    public static string GetSpeakerSubtitleId(string speakerName)
    {
        return $"speaker_{speakerName}";
    }

    /// <summary>
    ///     从插件配置中获取字幕配置
    /// </summary>
    private static void GetConfigFromPluginConfig()
    {
        _subtitleConfigs = new Dictionary<JustAnotherTranslator.SubtitleTypeEnum, SubtitleConfig>
        {
            {
                JustAnotherTranslator.SubtitleTypeEnum.Base,
                CreateSubtitleConfig(JustAnotherTranslator.SubtitleTypeEnum.Base)
            },
            {
                JustAnotherTranslator.SubtitleTypeEnum.Yotogi,
                CreateSubtitleConfig(JustAnotherTranslator.SubtitleTypeEnum.Yotogi)
            },
            {
                JustAnotherTranslator.SubtitleTypeEnum.Adv,
                CreateSubtitleConfig(JustAnotherTranslator.SubtitleTypeEnum.Adv)
            }
        };
    }

    /// <summary>
    ///     获取当前字幕配置
    /// </summary>
    /// <returns>当前字幕配置</returns>
    public static SubtitleConfig GetCurrentSubtitleConfig()
    {
        if (!_initialized) Initialize();
        return _subtitleConfigs[JustAnotherTranslator.SubtitleType.Value];
    }

    /// <summary>
    ///     获取指定类型的字幕配置
    /// </summary>
    /// <param name="type">字幕类型</param>
    /// <returns>字幕配置</returns>
    public static SubtitleConfig GetSubtitleConfig(JustAnotherTranslator.SubtitleTypeEnum type)
    {
        if (!_initialized) Initialize();
        return _subtitleConfigs[type];
    }

    /// <summary>
    ///     获取指定字幕类型的配置值
    /// </summary>
    private static T GetSubtitleTypeConfig<T>(
        JustAnotherTranslator.SubtitleTypeEnum type,
        Func<T> baseConfig,
        Func<T> yotogiConfig,
        Func<T> advConfig,
        T defaultValue)
    {
        return type switch
        {
            JustAnotherTranslator.SubtitleTypeEnum.Base => baseConfig(),
            JustAnotherTranslator.SubtitleTypeEnum.Yotogi => yotogiConfig(),
            JustAnotherTranslator.SubtitleTypeEnum.Adv => advConfig(),
            _ => defaultValue
        };
    }

    /// <summary>
    ///     从插件配置中创建字幕配置
    /// </summary>
    private static SubtitleConfig CreateSubtitleConfig(JustAnotherTranslator.SubtitleTypeEnum subtitleType)
    {
        // 初始化字幕配置
        var config = new SubtitleConfig
        {
            SubtitleType = subtitleType,

            // 基本设置
            EnableSpeakerName = GetSubtitleTypeConfig(
                subtitleType,
                () => JustAnotherTranslator.EnableBaseSubtitleSpeakerName.Value,
                () => JustAnotherTranslator.EnableYotogiSubtitleSpeakerName.Value,
                () => JustAnotherTranslator.EnableAdvSubtitleSpeakerName.Value,
                false),

            // 文本样式
            FontName = "Arial.ttf", // 使用Unity内置字体 //TODO 支持自定义字体
            FontSize = GetSubtitleTypeConfig(
                subtitleType,
                () => JustAnotherTranslator.BaseSubtitleFontSize.Value,
                () => JustAnotherTranslator.YotogiSubtitleFontSize.Value,
                () => JustAnotherTranslator.AdvSubtitleFontSize.Value,
                24),
            TextColor = GetSubtitleTypeConfig(
                subtitleType,
                () => ParseColor(JustAnotherTranslator.BaseSubtitleTextColor.Value),
                () => ParseColor(JustAnotherTranslator.YotogiSubtitleTextColor.Value),
                () => ParseColor(JustAnotherTranslator.AdvSubtitleTextColor.Value),
                Color.white),
            SpeakerNameColor = GetSubtitleTypeConfig(
                subtitleType,
                () => ParseColor(JustAnotherTranslator.BaseSubtitleSpeakerNameColor.Value),
                () => ParseColor(JustAnotherTranslator.YotogiSubtitleSpeakerNameColor.Value),
                () => ParseColor(JustAnotherTranslator.AdvSubtitleSpeakerNameColor.Value),
                Color.yellow),

            // 背景样式
            BackgroundColor = GetSubtitleTypeConfig(
                subtitleType,
                () => ParseColor(JustAnotherTranslator.BaseSubtitleBackgroundColor.Value),
                () => ParseColor(JustAnotherTranslator.YotogiSubtitleBackgroundColor.Value),
                () => ParseColor(JustAnotherTranslator.AdvSubtitleBackgroundColor.Value),
                new Color(0, 0, 0, 0.5f)),

            // 位置和大小
            PositionX = GetSubtitleTypeConfig(
                subtitleType,
                () => JustAnotherTranslator.BaseSubtitlePositionX.Value,
                () => JustAnotherTranslator.YotogiSubtitlePositionX.Value,
                () => JustAnotherTranslator.AdvSubtitlePositionX.Value,
                0),
            PositionY = GetSubtitleTypeConfig(
                subtitleType,
                () => JustAnotherTranslator.BaseSubtitlePositionY.Value,
                () => JustAnotherTranslator.YotogiSubtitlePositionY.Value,
                () => JustAnotherTranslator.AdvSubtitlePositionY.Value,
                100),
            Width = GetSubtitleTypeConfig(
                subtitleType,
                () => JustAnotherTranslator.BaseSubtitleWidth.Value,
                () => JustAnotherTranslator.YotogiSubtitleWidth.Value,
                () => JustAnotherTranslator.AdvSubtitleWidth.Value,
                800),

            // 描边设置
            OutlineColor = Color.black,
            OutlineWidth = 1f,

            // VR相关设置
            VRSubtitleType = JustAnotherTranslator.VRSubtitleType.Value,
            VRDistance = JustAnotherTranslator.VRSubtitleDistance.Value,
            VRTabletPositionX = JustAnotherTranslator.VRTabletPositionX.Value,
            VRTabletPositionY = JustAnotherTranslator.VRTabletPositionY.Value,
            VRTabletPositionZ = JustAnotherTranslator.VRTabletPositionZ.Value,
            VRTabletRotationX = JustAnotherTranslator.VRTabletRotationX.Value,
            VRTabletRotationY = JustAnotherTranslator.VRTabletRotationY.Value,
            VRTabletRotationZ = JustAnotherTranslator.VRTabletRotationZ.Value
        };

        return config;
    }

    /// <summary>
    ///     解析颜色字符串
    /// </summary>
    private static Color ParseColor(string colorStr)
    {
        try
        {
            var parts = colorStr.Split(',');
            if (parts.Length >= 3)
            {
                var r = float.Parse(parts[0]) / 255f;
                var g = float.Parse(parts[1]) / 255f;
                var b = float.Parse(parts[2]) / 255f;
                var a = parts.Length >= 4 ? float.Parse(parts[3]) / 255f : 1f;
                return new Color(r, g, b, a);
            }
        }
        catch
        {
            LogManager.Warning($"解析颜色失败: {colorStr}，使用默认颜色/Failed to parse color: {colorStr}, using default color");
        }

        return Color.white;
    }
}