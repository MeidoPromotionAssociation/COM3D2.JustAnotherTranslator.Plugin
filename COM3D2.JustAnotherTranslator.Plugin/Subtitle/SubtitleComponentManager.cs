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
    // 是否已初始化
    private static bool _initialized;

    // VR头部变换
    private static Transform _vrHeadTransform;

    // 字幕ID映射，用于跟踪说话者与字幕组件的关系
    private static readonly Dictionary<string, ISubtitleComponent> SubtitleComponentsMap = new(); // 说话者，字幕组件

    // 所有种类字幕的配置字典
    private static readonly Dictionary<JustAnotherTranslator.SubtitleTypeEnum, SubtitleConfig> SubtitleConfigs = new();


    /// <summary>
    ///     初始化字幕组件管理器
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;

        // 初始化字幕配置字典
        foreach (JustAnotherTranslator.SubtitleTypeEnum subtitleType in Enum.GetValues(
                     typeof(JustAnotherTranslator.SubtitleTypeEnum)))
        {
            var config = SubtitleConfig.CreateSubtitleConfig(subtitleType);
            SubtitleConfigs[subtitleType] = config;
        }

        if (JustAnotherTranslator.IsVrMode) InitVRComponents();

        _initialized = true;
        LogManager.Debug("Subtitle component manager initialized");
    }

    /// <summary>
    ///     初始化VR组件
    /// </summary>
    private static void InitVRComponents()
    {
        if (_vrHeadTransform is not null)
            return;

        // 查找 OvrMgr 的 EyeAnchor
        if (GameMain.Instance is not null && GameMain.Instance.OvrMgr is not null)
        {
            _vrHeadTransform = GameMain.Instance.OvrMgr.EyeAnchor;
            if (_vrHeadTransform is not null)
            {
                LogManager.Debug(
                    "VR head transform (EyeAnchor) found, subtitle head tracking enabled");
                return;
            }
        }

        // 如果无法通过GameMain.Instance.OvrMgr获取，尝试直接查找OvrMgr
        var ovrMgr = Object.FindObjectOfType<OvrMgr>();
        if (ovrMgr is not null && ovrMgr.EyeAnchor is not null)
        {
            _vrHeadTransform = ovrMgr.EyeAnchor;
            LogManager.Debug(
                "VR head transform found through FindObjectOfType<OvrMgr>(), subtitle head tracking enabled");
        }
        else
        {
            LogManager.Warning(
                "找不到VR头部变换，头部字幕跟踪将无法工作/VR head transform not found, head tracking will not work");
        }
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
            Initialize();

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

        component.Initialize(config);

        SubtitleComponentsMap[GetSpeakerSubtitleId(speakerName)] = component;

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
            Initialize();

        var subtitleId = GetSpeakerSubtitleId(speakerName);

        if (!SubtitleComponentsMap.TryGetValue(subtitleId, out var subtitleComponent))
        {
            subtitleComponent = CreateSubtitleComponent(speakerName, GetSubtitleConfig(subtitleType));
            SubtitleComponentsMap[subtitleId] = subtitleComponent;
        }

        subtitleComponent.ShowSubtitle(text, speakerName, duration);

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

        if (SubtitleComponentsMap.TryGetValue(subtitleId, out var subtitleComponent))
        {
            subtitleComponent.HideSubtitle();
            LogManager.Debug($"Hiding subtitle: {subtitleId}");
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
        if (!_initialized) Initialize();
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

        foreach (var subtitleComponent in SubtitleComponentsMap.Values)
        {
            var oldConfig = subtitleComponent.GetConfig();

            subtitleComponent.UpdateConfig(SubtitleConfigs[oldConfig.SubtitleType]);

            LogManager.Debug($"Update subtitle config: {subtitleComponent.GetSubtitleId()}");
        }
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


    public static void UpdateSubtitleConfig(string speakerName)
    {
        var subtitleId = GetSpeakerSubtitleId(speakerName);
        if (SubtitleComponentsMap.TryGetValue(subtitleId, out var subtitleComponent))
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

        SubtitleComponentsMap.Remove(GetSpeakerSubtitleId(component.GetSpeakerName()));

        component.Destroy();

        LogManager.Debug("Subtitle component destroyed");
    }

    /// <summary>
    ///     销毁所有字幕组件
    /// </summary>
    public static void DestroyAllSubtitleComponents()
    {
        foreach (var component in SubtitleComponentsMap.Values)
            component.Destroy();

        SubtitleComponentsMap.Clear();
        LogManager.Debug("All subtitle components destroyed");
    }

    /// <summary>
    ///     获取VR头部变换
    /// </summary>
    /// <returns>VR头部变换</returns>
    public static Transform GetVRHeadTransform()
    {
        return _vrHeadTransform;
    }
}