namespace COM3D2.JustAnotherTranslator.Plugin.Utils;

public static class MaidInfo
{
    public static string GetMaidFullName(Maid maid)
    {
        if (JustAnotherTranslator.MaidNameStyle.Value == JustAnotherTranslator.MaidNameStyleEnum.JpStyle)
            return maid.status.fullNameJpStyle;

        return maid.status.fullNameEnStyle;
    }
}