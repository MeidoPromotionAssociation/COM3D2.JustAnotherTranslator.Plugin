namespace COM3D2.JustAnotherTranslator.Plugin.Subtitle.ConfigStrategy;

/// <summary>
///     字幕配置策略工厂
/// </summary>
public static class SubtitleConfigStrategyFactory
{
    /// <summary>
    ///     根据配置创建适当的字幕配置策略
    /// </summary>
    /// <param name="config">字幕配置</param>
    /// <returns>字幕配置策略</returns>
    public static ISubtitleConfigStrategy CreateStrategy(SubtitleConfig config)
    {
        // 根据是否在VR模式下切换策略
        if (JustAnotherTranslator.IsVrMode)
            // 根据配置的VR字幕模式返回相应策略
            return config.VRSubtitleMode switch
            {
                JustAnotherTranslator.VRSubtitleModeEnum.InSpace => new VRSpaceSubtitleConfigStrategy(),
                JustAnotherTranslator.VRSubtitleModeEnum.OnTablet => new VRTabletSubtitleConfigStrategy(),
                _ => new VRSpaceSubtitleConfigStrategy()
            };

        // 非VR模式下始终使用屏幕策略
        return new ScreenSubtitleConfigStrategy();
    }
}