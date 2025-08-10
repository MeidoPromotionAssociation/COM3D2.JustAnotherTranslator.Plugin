using System;

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
        try
        {
            if (JustAnotherTranslator.MaidNameStyle.Value ==
                JustAnotherTranslator.MaidNameStyleEnum.JpStyle)
                return maid.status.fullNameJpStyle;

            return maid.status.fullNameEnStyle;
        }
        catch (Exception e)
        {
            // Because there is a stack, it would rather be caught
            LogManager.Error(
                $"GetMaidFullName unknown error, please report this issue/未知错误，请报告此错误 {e.Message}\n{e.StackTrace}");
            return "";
        }
    }
}