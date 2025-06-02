using UnityEngine;

namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle.Component;

/// <summary>
///     字幕组件接口，定义所有字幕组件必须实现的方法
/// </summary>
public interface ISubtitleComponent
{
    /// <summary>
    ///     初始化字幕组件
    /// </summary>
    /// <param name="config">字幕配置</param>
    void Init(SubtitleConfig config);

    /// <summary>
    ///     显示字幕
    /// </summary>
    /// <param name="text">要显示的文本</param>
    /// <param name="speakerName">说话者名称，可为空</param>
    /// <param name="duration">显示持续时间，0表示持续显示</param>
    void ShowSubtitle(string text, string speakerName, float duration);

    /// <summary>
    ///     隐藏字幕
    /// </summary>
    void HideSubtitle(bool skipAnimation = false);

    /// <summary>
    ///     更新字幕配置
    /// </summary>
    /// <param name="config">新的字幕配置</param>
    void UpdateConfig(SubtitleConfig config);

    /// <summary>
    ///     获取当前字幕配置
    /// </summary>
    /// <returns>当前字幕配置</returns>
    SubtitleConfig GetConfig();

    /// <summary>
    ///     获取当前显示的文本
    /// </summary>
    /// <returns>当前显示的文本</returns>
    string GetText();

    /// <summary>
    ///     获取当前说话者名称
    /// </summary>
    /// <returns>当前说话者名称</returns>
    string GetSpeakerName();

    /// <summary>
    ///     获取字幕ID
    /// </summary>
    /// <returns>字幕ID</returns>
    string GetSubtitleId();

    /// <summary>
    ///     检查字幕是否可见
    /// </summary>
    /// <returns>字幕是否可见</returns>
    bool IsVisible();

    /// <summary>
    ///     销毁字幕组件
    /// </summary>
    void Destroy();

    /// <summary>
    ///     获取游戏对象
    /// </summary>
    /// <returns>游戏对象</returns>
    GameObject GetGameObject();
}