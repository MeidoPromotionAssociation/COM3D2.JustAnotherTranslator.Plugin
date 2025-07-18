namespace COM3D2.JustAnotherTranslator.Plugin.Utils;

/// <summary>
///     获取 Maid 信息
/// </summary>
public static class MaidInfo
{
    /// <summary>
    ///     获取 Maid 的全名，根据配置获取日式或者英式
    /// </summary>
    /// <param name="maid"></param>
    /// <returns></returns>
    public static string GetMaidFullName(Maid maid)
    {
        if (JustAnotherTranslator.MaidNameStyle.Value == JustAnotherTranslator.MaidNameStyleEnum.JpStyle)
            return maid.status.fullNameJpStyle;

        return maid.status.fullNameEnStyle;
    }
}