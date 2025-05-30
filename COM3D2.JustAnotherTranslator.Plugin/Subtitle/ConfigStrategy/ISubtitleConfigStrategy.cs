namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle.ConfigStrategy;

/// <summary>
///     字幕配置策略接口
/// </summary>
public interface ISubtitleConfigStrategy
{
    /// <summary>
    ///     创建字幕UI组件
    /// </summary>
    /// <param name="component">字幕组件</param>
    void CreateSubtitleUI(SubtitleComponent component);

    /// <summary>
    ///     应用字幕配置
    /// </summary>
    /// <param name="component">字幕组件</param>
    void ApplyConfig(SubtitleComponent component);

    /// <summary>
    ///     更新字幕配置
    /// </summary>
    /// <param name="component">字幕组件</param>
    /// <param name="config">新的字幕配置</param>
    void UpdateConfig(SubtitleComponent component, SubtitleConfig config);

    /// <summary>
    ///     处理字幕显示
    /// </summary>
    /// <param name="component">字幕组件</param>
    /// <param name="text">要显示的文本</param>
    /// <param name="speakerName">说话者名称，应当可以处理 null</param>
    /// <param name="duration">显示持续时间，0 为一直显示</param>
    void HandleShowSubtitle(SubtitleComponent component, string text, string speakerName, float duration);

    /// <summary>
    ///     处理字幕隐藏
    /// </summary>
    /// <param name="component">字幕组件</param>
    void HandleHideSubtitle(SubtitleComponent component);
}