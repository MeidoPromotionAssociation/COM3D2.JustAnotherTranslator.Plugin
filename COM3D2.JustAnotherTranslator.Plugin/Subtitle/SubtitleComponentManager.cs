using System.Collections.Generic;
using UnityEngine;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle;

/// <summary>
///     字幕组件管理器，负责管理所有字幕组件的生命周期
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

    /// <summary>
    ///     初始化字幕组件管理器
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;

        // 如果在VR模式下，初始化VR头部跟踪
        if (JustAnotherTranslator.IsVrMode)
        {
            InitVRComponents();
        }

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
                LogManager.Debug("VR头部变换 (EyeAnchor) 已找到，字幕头部跟踪已启用/VR head transform (EyeAnchor) found, subtitle head tracking enabled");
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
    ///     创建新的字幕组件
    /// </summary>
    /// <returns>字幕组件接口</returns>
    public static ISubtitleComponent CreateSubtitleComponent()
    {
        if (!_initialized) Initialize();

        // 创建游戏对象
        var gameObject = new GameObject("SubtitleComponent");
        var component = gameObject.AddComponent<SubtitleComponent>();

        // 初始化组件
        component.Initialize(SubtitleConfigManager.Instance.GetCurrentConfig());

        // 如果在VR模式下，设置VR组件
        if (JustAnotherTranslator.IsVrMode && _vrHeadTransform != null)
        {
            component.InitVRComponents(_vrHeadTransform);
        }

        // 添加到活动组件列表
        SubtitleComponents.Add(component);

        LogManager.Debug("新字幕组件已创建/New subtitle component created");
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
        {
            if (kvp.Value == component)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            SubtitleIdMap.Remove(key);
        }

        // 销毁组件
        component.Destroy();

        LogManager.Debug("字幕组件已销毁/Subtitle component destroyed");
    }

    /// <summary>
    ///     销毁所有字幕组件
    /// </summary>
    public static void DestroyAllSubtitleComponents()
    {
        foreach (var component in SubtitleComponents)
        {
            component.Destroy();
        }

        SubtitleComponents.Clear();
        SubtitleIdMap.Clear();
        LogManager.Debug("所有字幕组件已销毁/All subtitle components destroyed");
    }

    /// <summary>
    ///     更新所有字幕组件的配置
    /// </summary>
    /// <param name="config">新的字幕配置</param>
    public static void UpdateAllSubtitleConfigs(SubtitleConfig config)
    {
        // 更新配置管理器
        SubtitleConfigManager.Instance.UpdateConfig(config);
        
        // 配置更新会通过事件通知各组件，无需手动更新
        LogManager.Debug("所有字幕组件配置已更新/All subtitle component configs updated");
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
    ///     显示浮动字幕
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
            subtitleComponent = CreateSubtitleComponent();
            SubtitleIdMap[subtitleId] = subtitleComponent;
        }
        
        // 更新字幕配置（如果需要）
        if (config != null)
        {
            subtitleComponent.UpdateConfig(config);
        }
        
        // 显示字幕
        subtitleComponent.ShowSubtitle(text, speakerName, duration);
        
        LogManager.Debug($"显示浮动字幕/Showing floating subtitle: {speakerName}: {text}");
    }
    
    /// <summary>
    ///     隐藏字幕
    /// </summary>
    /// <param name="subtitleId">字幕ID</param>
    public static void HideSubtitle(string subtitleId)
    {
        if (string.IsNullOrEmpty(subtitleId))
            return;
            
        // 查找是否存在该ID的字幕组件
        if (SubtitleIdMap.TryGetValue(subtitleId, out var subtitleComponent))
        {
            // 隐藏字幕
            subtitleComponent.HideSubtitle();
            
            LogManager.Debug($"隐藏字幕/Hiding subtitle: {subtitleId}");
        }
    }
    
    /// <summary>
    ///     获取说话者的字幕ID
    /// </summary>
    /// <param name="speakerName">说话者名称</param>
    /// <returns>字幕ID</returns>
    public static string GetSpeakerSubtitleId(string speakerName)
    {
        return $"subtitle_{speakerName}";
    }
}