using System;
using COM3D2.JustAnotherTranslator.Plugin.Translator;
using UnityEngine;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle;

/// <summary>
///     字幕配置管理器，集中管理所有字幕相关配置
/// </summary>
public class SubtitleConfigManager
{
    /// <summary>
    ///     当配置变更时触发的事件
    /// </summary>
    public event Action<SubtitleConfig> ConfigChanged;

    private SubtitleConfig _currentConfig;

    // 单例实现
    private static SubtitleConfigManager _instance;

    /// <summary>
    ///     获取SubtitleConfigManager的单例实例
    /// </summary>
    public static SubtitleConfigManager Instance => _instance ??= new SubtitleConfigManager();

    private SubtitleConfigManager()
    {
        // 初始化默认配置
        _currentConfig = new SubtitleConfig();
        LoadConfig();
    }

    /// <summary>
    ///     获取当前配置（返回副本以防止外部修改）
    /// </summary>
    /// <returns>当前配置的副本</returns>
    public SubtitleConfig GetCurrentConfig()
    {
        // 这里需要克隆配置对象以防止外部修改
        return _currentConfig.Clone();
    }

    /// <summary>
    ///     更新整个配置
    /// </summary>
    /// <param name="newConfig">新的配置对象</param>
    public void UpdateConfig(SubtitleConfig newConfig)
    {
        if (newConfig is null)
        {
            LogManager.Warning("尝试更新的字幕配置为空/Subtitle config to update is null");
            return;
        }

        // 验证配置有效性
        if (!ValidateConfig(newConfig))
        {
            LogManager.Warning("字幕配置验证失败/Subtitle config validation failed");
            return;
        }

        // 更新配置
        _currentConfig = newConfig.Clone();
        
        // 保存配置
        SaveConfig();
        
        // 通知所有监听者
        ConfigChanged?.Invoke(_currentConfig);
        
        LogManager.Debug("字幕配置已更新/Subtitle config updated");
    }

    /// <summary>
    ///     更新特定配置项
    /// </summary>
    /// <typeparam name="T">配置项类型</typeparam>
    /// <param name="propertyName">配置项名称</param>
    /// <param name="value">新值</param>
    public void UpdateConfigItem<T>(string propertyName, T value)
    {
        if (string.IsNullOrEmpty(propertyName))
        {
            LogManager.Warning("配置项名称为空/Property name is empty");
            return;
        }

        // 获取当前配置的副本
        var configCopy = _currentConfig.Clone();

        // 使用反射更新特定配置项
        var property = typeof(SubtitleConfig).GetProperty(propertyName);
        if (property is null)
        {
            LogManager.Warning($"配置项 {propertyName} 不存在/Property {propertyName} does not exist");
            return;
        }

        try
        {
            property.SetValue(configCopy, value, null);
        }
        catch (Exception ex)
        {
            LogManager.Error($"更新配置项失败/Failed to update config item: {ex.Message}");
            return;
        }

        // 验证配置有效性
        if (!ValidateConfig(configCopy))
        {
            LogManager.Warning($"配置项 {propertyName} 验证失败/Config item {propertyName} validation failed");
            return;
        }

        // 更新配置
        _currentConfig = configCopy;
        
        // 保存配置
        SaveConfig();
        
        // 通知所有监听者
        ConfigChanged?.Invoke(_currentConfig);
        
        LogManager.Debug($"字幕配置项 {propertyName} 已更新/Subtitle config item {propertyName} updated");
    }

    /// <summary>
    ///     加载配置
    /// </summary>
    private void LoadConfig()
    {
        try
        {
            // 从文件或游戏配置加载
            // 这里可以使用PlayerPrefs或自定义的配置系统
            // 暂时使用默认配置
            _currentConfig = SubtitleConfig.CreateDefault();
            
            LogManager.Debug("字幕配置已加载/Subtitle config loaded");
        }
        catch (Exception ex)
        {
            LogManager.Error($"加载字幕配置失败/Failed to load subtitle config: {ex.Message}");
            _currentConfig = SubtitleConfig.CreateDefault();
        }
    }

    /// <summary>
    ///     保存配置
    /// </summary>
    private void SaveConfig()
    {
        try
        {
            // 保存到文件或游戏配置
            // 这里可以使用PlayerPrefs或自定义的配置系统
            // 暂时不实现实际保存
            
            LogManager.Debug("字幕配置已保存/Subtitle config saved");
        }
        catch (Exception ex)
        {
            LogManager.Error($"保存字幕配置失败/Failed to save subtitle config: {ex.Message}");
        }
    }

    /// <summary>
    ///     验证配置有效性
    /// </summary>
    /// <param name="config">要验证的配置</param>
    /// <returns>配置是否有效</returns>
    private bool ValidateConfig(SubtitleConfig config)
    {
        if (config is null) return false;

        // 验证字体大小
        if (config.FontSize <= 0)
        {
            LogManager.Warning("字体大小必须大于0/Font size must be greater than 0");
            return false;
        }

        // 验证背景高度
        if (config.BackgroundHeight <= 0)
        {
            LogManager.Warning("背景高度必须大于0/Background height must be greater than 0");
            return false;
        }

        // 验证参考分辨率
        if (config.ReferenceWidth <= 0 || config.ReferenceHeight <= 0)
        {
            LogManager.Warning("参考分辨率必须大于0/Reference resolution must be greater than 0");
            return false;
        }

        // 验证VR字幕宽度和缩放
        if (config.VRSubtitleWidth <= 0 || config.VRSubtitleScale <= 0)
        {
            LogManager.Warning("VR字幕宽度和缩放必须大于0/VR subtitle width and scale must be greater than 0");
            return false;
        }

        return true;
    }
}
