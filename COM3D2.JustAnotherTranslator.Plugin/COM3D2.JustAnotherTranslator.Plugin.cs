using System;
using System.ComponentModel;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using COM3D2.JustAnotherTranslator.Plugin.Subtitle;
using COM3D2.JustAnotherTranslator.Plugin.Translator;
using COM3D2.JustAnotherTranslator.Plugin.Utils;

namespace COM3D2.JustAnotherTranslator.Plugin;

[BepInPlugin("COM3D2.JustAnotherTranslator.Plugin", "COM3D2.JustAnotherTranslator.Plugin", "1.0.0")]
public class JustAnotherTranslator : BaseUnityPlugin
{
    public enum MaidNameStyleEnum
    {
        [Description("JpStyle/日式")] JpStyle,
        [Description("EnStyle/英式")] EnStyle
    }

    public enum SubtitleTypeEnum
    {
        [Description("Base/基础字幕")] Base,
        [Description("Yotogi/夜伽字幕")] Yotogi,
        [Description("ADV/ADV字幕")] Adv,
        [Description("Lyric/歌词字幕")] Lyric
    }

    public enum VRSubtitleModeEnum
    {
        [Description("InSpac/空间字幕")] InSpace,
        [Description("OnTablet/平板字幕")] OnTablet
    }

    public static bool IsVrMode;

    public static ConfigEntry<string> TargetLanguage;
    public static ConfigEntry<bool> EnableTextTranslation;
    public static ConfigEntry<bool> EnableUITranslation;
    public static ConfigEntry<bool> EnableTextureReplace;
    public static ConfigEntry<MaidNameStyleEnum> MaidNameStyle;
    public static ConfigEntry<LogLevel> LogLevelConfig;
    public static ConfigEntry<int> TextureCacheSize;
    public static ConfigEntry<bool> EnableAsyncLoading;

    // 字幕启用相关配置
    public static ConfigEntry<bool> EnableBaseSubtitle;
    public static ConfigEntry<bool> EnableYotogiSubtitle;
    public static ConfigEntry<bool> EnableAdvSubtitle;
    public static ConfigEntry<bool> ForceEnableAdvSubtitle;
    public static ConfigEntry<bool> EnableLyricSubtitle;

    // 基础字幕相关配置
    public static ConfigEntry<bool> EnableBaseSubtitleSpeakerName;
    public static ConfigEntry<string> BaseSubtitleFont;
    public static ConfigEntry<int> BaseSubtitleFontSize;
    public static ConfigEntry<string> BaseSubtitleColor;
    public static ConfigEntry<float> BaseSubtitleOpacity;
    public static ConfigEntry<string> BaseSubtitleBackgroundColor;
    public static ConfigEntry<float> BaseSubtitleBackgroundOpacity;
    public static ConfigEntry<float> BaseSubtitleVerticalPosition;
    public static ConfigEntry<float> BaseSubtitleBackgroundWidth;
    public static ConfigEntry<float> BaseSubtitleBackgroundHeight;
    public static ConfigEntry<bool> EnableBaseSubtitleAnimation;
    public static ConfigEntry<float> BaseSubtitleFadeInDuration;
    public static ConfigEntry<float> BaseSubtitleFadeOutDuration;
    public static ConfigEntry<bool> EnableBaseSubtitleOutline;
    public static ConfigEntry<string> BaseSubtitleOutlineColor;
    public static ConfigEntry<float> BaseSubtitleOutlineOpacity;
    public static ConfigEntry<float> BaseSubtitleOutlineWidth;

    // 夜伽字幕相关配置
    public static ConfigEntry<bool> EnableYotogiSubtitleSpeakerName;
    public static ConfigEntry<string> YotogiSubtitleFont;
    public static ConfigEntry<int> YotogiSubtitleFontSize;
    public static ConfigEntry<string> YotogiSubtitleColor;
    public static ConfigEntry<float> YotogiSubtitleOpacity;
    public static ConfigEntry<string> YotogiSubtitleBackgroundColor;
    public static ConfigEntry<float> YotogiSubtitleBackgroundOpacity;
    public static ConfigEntry<float> YotogiSubtitleVerticalPosition;
    public static ConfigEntry<float> YotogiSubtitleBackgroundWidth;
    public static ConfigEntry<float> YotogiSubtitleBackgroundHeight;
    public static ConfigEntry<bool> EnableYotogiSubtitleAnimation;
    public static ConfigEntry<float> YotogiSubtitleFadeInDuration;
    public static ConfigEntry<float> YotogiSubtitleFadeOutDuration;
    public static ConfigEntry<bool> EnableYotogiSubtitleOutline;
    public static ConfigEntry<string> YotogiSubtitleOutlineColor;
    public static ConfigEntry<float> YotogiSubtitleOutlineOpacity;
    public static ConfigEntry<float> YotogiSubtitleOutlineWidth;

    // ADV字幕相关配置
    public static ConfigEntry<bool> EnableAdvSubtitleSpeakerName;
    public static ConfigEntry<string> AdvSubtitleFont;
    public static ConfigEntry<int> AdvSubtitleFontSize;
    public static ConfigEntry<string> AdvSubtitleColor;
    public static ConfigEntry<float> AdvSubtitleOpacity;
    public static ConfigEntry<string> AdvSubtitleBackgroundColor;
    public static ConfigEntry<float> AdvSubtitleBackgroundOpacity;
    public static ConfigEntry<float> AdvSubtitleVerticalPosition;
    public static ConfigEntry<float> AdvSubtitleBackgroundWidth;
    public static ConfigEntry<float> AdvSubtitleBackgroundHeight;
    public static ConfigEntry<bool> EnableAdvSubtitleAnimation;
    public static ConfigEntry<float> AdvSubtitleFadeInDuration;
    public static ConfigEntry<float> AdvSubtitleFadeOutDuration;
    public static ConfigEntry<bool> EnableAdvSubtitleOutline;
    public static ConfigEntry<string> AdvSubtitleOutlineColor;
    public static ConfigEntry<float> AdvSubtitleOutlineOpacity;
    public static ConfigEntry<float> AdvSubtitleOutlineWidth;

    // 歌词字幕相关配置
    public static ConfigEntry<bool> EnableLyricSubtitleSpeakerName;
    public static ConfigEntry<string> LyricSubtitleFont;
    public static ConfigEntry<int> LyricSubtitleFontSize;
    public static ConfigEntry<string> LyricSubtitleColor;
    public static ConfigEntry<float> LyricSubtitleOpacity;
    public static ConfigEntry<string> LyricSubtitleBackgroundColor;
    public static ConfigEntry<float> LyricSubtitleBackgroundOpacity;
    public static ConfigEntry<float> LyricSubtitleVerticalPosition;
    public static ConfigEntry<float> LyricSubtitleBackgroundWidth;
    public static ConfigEntry<float> LyricSubtitleBackgroundHeight;
    public static ConfigEntry<bool> EnableLyricSubtitleAnimation;
    public static ConfigEntry<float> LyricSubtitleFadeInDuration;
    public static ConfigEntry<float> LyricSubtitleFadeOutDuration;
    public static ConfigEntry<bool> EnableLyricSubtitleOutline;
    public static ConfigEntry<string> LyricSubtitleOutlineColor;
    public static ConfigEntry<float> LyricSubtitleOutlineOpacity;
    public static ConfigEntry<float> LyricSubtitleOutlineWidth;

    // VR字幕相关配置
    public static ConfigEntry<VRSubtitleModeEnum> VRSubtitleMode;
    public static ConfigEntry<float> VRSubtitleDistance;
    public static ConfigEntry<float> VRSubtitleVerticalOffset;
    public static ConfigEntry<float> VRSubtitleHorizontalOffset;
    public static ConfigEntry<float> VRSubtitleWidth;
    public static ConfigEntry<float> VRSubtitleHeight;

    // translation folder path
    public static readonly string TranslationRootPath = Paths.BepInExRootPath + "/JustAnotherTranslator";
    public static string TargetLanguePath;
    public static string TranslationTextPath;
    public static string TranslationTexturePath;

    // 字幕类型
    public static ConfigEntry<SubtitleTypeEnum> SubtitleType;

    private void Awake()
    {
        Logger.LogInfo("COM3D2.JustAnotherTranslator.Plugin is loading/COM3D2.JustAnotherTranslator.Plugin 正在载入");
        Logger.LogInfo(
            "Get update or report bug/获取更新或报告bug: https://github.com/90135/COM3D2.JustAnotherTranslator.Plugin");

        // Init our LogManager with the BepInEx logger
        LogManager.Initialize(Logger);

        IsVrMode = Environment.CommandLine.ToLower().Contains("/vr");

        # region GeneralSettings

        TargetLanguage = Config.Bind("General",
            "TargetLanguage/目标语言",
            "zh-CN",
            "Target Language/目标语言");

        TargetLanguePath = TranslationRootPath + "/" + TargetLanguage.Value;
        TranslationTextPath = TargetLanguePath + "/Text";
        TranslationTexturePath = TargetLanguePath + "/Texture";

        EnableTextTranslation = Config.Bind("General",
            "EnableTextTranslation/启用文本翻译",
            true,
            "Enable Text Translation/启用文本翻译");

        EnableUITranslation = Config.Bind("General",
            "EnableUITranslation/启用 UI 翻译",
            true,
            "Enable UI Translation/启用 UI 翻译");

        EnableTextureReplace = Config.Bind("General",
            "EnableTextureReplace/启用贴图替换",
            true,
            "Enable Texture Replace/启用贴图替换");

        MaidNameStyle = Config.Bind("General",
            "MaidNameStyle/女仆名字样式",
            MaidNameStyleEnum.JpStyle,
            "Maid Name Style, JpStyle is family name first and given name last, English style is opposite, cannot change at runtime/女仆名字样式，日式姓前名后，英式相反。无法在运行时更改");

        // 声明后才能使用日志
        LogLevelConfig = Config.Bind("General",
            "LogLevel/日志级别",
            LogLevel.Info,
            "Log Level, DEBUG will log more information/日志级别，DEBUG 级别将记录详细信息");

        TextureCacheSize = Config.Bind("General",
            "TextureCacheSize/贴图缓存大小",
            20,
            "Texture Cache Size, larger value will use more memory but improve performance/贴图缓存大小，较大的值会使用更多内存但提高性能");

        EnableAsyncLoading = Config.Bind("General",
            "EnableAsyncLoading/启用异步加载",
            true,
            "Enable Async Loading, load translation files in background thread/启用异步加载，在后台线程中加载翻译文件");

        # endregion

        # region SubtitleEnableSettings

        EnableBaseSubtitle = Config.Bind("Subtitle",
            "EnableBaseSubtitle/启用基础字幕",
            true,
            "Enable Base Subtitle, usually Karaoke, Casino, etc Some voices are matched by audio file name, so may not be displayed if text translation is not enabled/启用基础字幕，通常是卡拉OK、赌场等字幕。部分语音按音频文件名匹配，因此未启用文本翻译时可能无法显示");

        EnableYotogiSubtitle = Config.Bind("Subtitle",
            "EnableYotogiSubtitle/启用夜伽字幕",
            true,
            "Enable Yotogi Subtitle/启用夜伽字幕");

        EnableAdvSubtitle = Config.Bind("Subtitle",
            "EnableADVSubtitle/启用ADV字幕",
            true,
            "Enable ADV subtitles. Since ADV scenes have their own subtitles, this setting is only useful in VR mode and is invalid in non-VR mode./启用ADV字幕，由于 ADV 场景自带字幕，因此仅在 VR 模式下有用，非 VR 模式此设置无效");

        ForceEnableAdvSubtitle = Config.Bind("Subtitle",
            "ForceEnableADVSubtitle/强制启用ADV字幕",
            false,
            "Force Enable ADV subtitles, whether in VR mode or not/强制启用ADV字幕，无论是不是 VR 模式");

        EnableLyricSubtitle = Config.Bind("Subtitle",
            "EnableLyricSubtitle/启用歌词字幕",
            true,
            "Enable Lyric Subtitle/启用歌词字幕");

        # endregion

        # region BaseSubtitleSettings

        // TODO 添加字幕对齐方式设置
        // TODO 字幕自动找位

        // 基础字幕相关配置
        EnableBaseSubtitle = Config.Bind("BaseSubtitle",
            "EnableBaseSubtitle/启用基础字幕",
            true,
            "Enable Base Subtitle/启用基础字幕");

        EnableBaseSubtitleSpeakerName = Config.Bind("BaseSubtitle",
            "EnableBaseSubtitleSpeakerName/启用基础字幕显示说话人名",
            true,
            "Enable Base Subtitle Speaker Name/启用基础字幕显示说话人名");

        BaseSubtitleFont = Config.Bind("BaseSubtitle",
            "BaseSubtitleFont/基础字幕字体",
            "Arial",
            "Base Subtitle Font, need to already installed the font on the system/基础字幕字体，需要已经安装在系统中的字体");

        BaseSubtitleFontSize = Config.Bind("BaseSubtitle",
            "BaseSubtitleFontSize/基础字幕字体大小",
            24,
            "Base Subtitle Font Size/基础字幕字体大小");

        BaseSubtitleColor = Config.Bind("BaseSubtitle",
            "BaseSubtitleColor/基础字幕颜色",
            "#FFFFFF",
            "Base Subtitle Color, use hex color code/基础字幕颜色，使用十六进制颜色代码");

        BaseSubtitleOpacity = Config.Bind("BaseSubtitle",
            "BaseSubtitleOpacity/基础字幕不透明度",
            1f,
            "Base Subtitle Opacity (0-1)/基础字幕不透明度（0-1）");

        BaseSubtitleBackgroundColor = Config.Bind("BaseSubtitle",
            "BaseSubtitleBackgroundColor/基础字幕背景颜色",
            "#000000",
            "Base Subtitle Background Color, use hex color code/基础字幕背景颜色，使用十六进制颜色代码");

        BaseSubtitleBackgroundOpacity = Config.Bind("BaseSubtitle",
            "BaseSubtitleBackgroundOpacity/基础字幕背景不透明度",
            0.1f,
            "Base Subtitle Background Opacity (0-1)/基础字幕背景不透明度（0-1）");

        BaseSubtitleVerticalPosition = Config.Bind("BaseSubtitle",
            "BaseSubtitleVerticalPosition/基础字幕垂直位置",
            0.97f,
            "Base Subtitle Vertical Position (0 is bottom, 1 is top, note 0 and 1 will go out of screen)/基础字幕垂直位置（0为底部，1为顶部，注意 0 和 1 会超出屏幕）");

        BaseSubtitleBackgroundWidth = Config.Bind("BaseSubtitle",
            "BaseSubtitleBackgroundWidth/基础字幕背景宽度",
            1f,
            "Base Subtitle Background Width, less than 1 is relative, otherwise is pixel/基础背景幕宽度，小于 1 时为相对比例反之为像素");
        //TODO 其他字幕的描述和默认值

        BaseSubtitleBackgroundHeight = Config.Bind("BaseSubtitle",
            "BaseSubtitleBackgroundHeight/基础字幕背景高度",
            0.015f,
            "Base Subtitle Background Height, less than 1 is relative, otherwise is pixel/基础背景幕高度，小于 1 时为相对比例反之为像素");

        EnableBaseSubtitleAnimation = Config.Bind("BaseSubtitle",
            "BaseSubtitleAnimation/基础字幕动画",
            true,
            "Enable Base Subtitle Animation/启用基础字幕动画");

        BaseSubtitleFadeInDuration = Config.Bind("BaseSubtitle",
            "BaseSubtitleFadeInDuration/基础字幕淡入时长",
            0.5f,
            "Base Subtitle Fade In Duration in seconds/基础字幕淡入时长（秒）");

        BaseSubtitleFadeOutDuration = Config.Bind("BaseSubtitle",
            "BaseSubtitleFadeOutDuration/基础字幕淡出时长",
            0.5f,
            "Base Subtitle Fade Out Duration in seconds/基础字幕淡出时长（秒）");

        EnableBaseSubtitleOutline = Config.Bind("BaseSubtitle",
            "EnableBaseSubtitleOutline/启用基础字幕描边",
            true,
            "Enable Base Subtitle Outline/启用基础字幕描边");

        BaseSubtitleOutlineColor = Config.Bind("BaseSubtitle",
            "BaseSubtitleOutlineColor/基础字幕描边颜色",
            "#000000",
            "Base Subtitle Outline Color, use hex color code/基础字幕描边颜色，使用十六进制颜色代码");

        BaseSubtitleOutlineOpacity = Config.Bind("BaseSubtitle",
            "BaseSubtitleOutlineOpacity/基础字幕描边不透明度",
            0.5f,
            "Base Subtitle Outline Opacity (0-1)/基础字幕描边不透明度（0-1）");

        BaseSubtitleOutlineWidth = Config.Bind("BaseSubtitle",
            "BaseSubtitleOutlineWidth/基础字幕描边宽度",
            1f,
            "Base Subtitle Outline Width in pixels/基础字幕描边宽度（像素）");

        # endregion

        # region YotogiSubtitleSettings

        // 夜伽字幕相关配置
        EnableYotogiSubtitle = Config.Bind("YotogiSubtitle",
            "EnableYotogiSubtitle/启用夜伽字幕",
            true,
            "Enable Yotogi Subtitle/启用夜伽字幕");

        EnableYotogiSubtitleSpeakerName = Config.Bind("YotogiSubtitle",
            "EnableYotogiSubtitleSpeakerName/启用夜伽字幕显示说话人名",
            true,
            "Enable Yotogi Subtitle Speaker Name/启用夜伽字幕显示说话人名");

        YotogiSubtitleFont = Config.Bind("YotogiSubtitle",
            "YotogiSubtitleFont/夜伽字幕字体",
            "Arial",
            "Yotogi Subtitle Font, need to already installed the font on the system/夜伽字幕字体，需要已经安装在系统中的字体");

        YotogiSubtitleFontSize = Config.Bind("YotogiSubtitle",
            "YotogiSubtitleFontSize/夜伽字幕字体大小",
            24,
            "Yotogi Subtitle Font Size/夜伽字幕字体大小");

        YotogiSubtitleColor = Config.Bind("YotogiSubtitle",
            "YotogiSubtitleColor/夜伽字幕颜色",
            "#FFFFFF",
            "Yotogi Subtitle Color, use hex color code/夜伽字幕颜色，使用十六进制颜色代码");

        YotogiSubtitleOpacity = Config.Bind("YotogiSubtitle",
            "YotogiSubtitleOpacity/夜伽字幕不透明度",
            1f,
            "Yotogi Subtitle Opacity (0-1)/夜伽字幕不透明度（0-1）");

        YotogiSubtitleBackgroundColor = Config.Bind("YotogiSubtitle",
            "YotogiSubtitleBackgroundColor/夜伽字幕背景颜色",
            "#000000",
            "Yotogi Subtitle Background Color, use hex color code/夜伽字幕背景颜色，使用十六进制颜色代码");

        YotogiSubtitleBackgroundOpacity = Config.Bind("YotogiSubtitle",
            "YotogiSubtitleBackgroundOpacity/夜伽字幕背景不透明度",
            0.1f,
            "Yotogi Subtitle Background Opacity (0-1)/夜伽字幕背景不透明度（0-1）");

        YotogiSubtitleVerticalPosition = Config.Bind("YotogiSubtitle",
            "YotogiSubtitleVerticalPosition/夜伽字幕垂直位置",
            0.965f,
            "Yotogi Subtitle Vertical Position (0-1, 0 is bottom, 1 is top)/夜伽字幕垂直位置（0-1，0为底部，1为顶部）");

        YotogiSubtitleBackgroundWidth = Config.Bind("YotogiSubtitle",
            "YotogiSubtitleBackgroundWidth/夜伽字幕背景宽度",
            1f,
            "Yotogi Subtitle Background Width in percents/夜伽背景幕宽度（百分比）");

        YotogiSubtitleBackgroundHeight = Config.Bind("YotogiSubtitle",
            "YotogiSubtitleBackgroundHeight/夜伽字幕背景高度",
            0.08f,
            "Yotogi Subtitle Background Height in percents/夜伽背景幕高度（百分比）");

        EnableYotogiSubtitleAnimation = Config.Bind("YotogiSubtitle",
            "YotogiSubtitleAnimation/夜伽字幕动画",
            true,
            "Enable Yotogi Subtitle Animation/启用夜伽字幕动画");

        YotogiSubtitleFadeInDuration = Config.Bind("YotogiSubtitle",
            "YotogiSubtitleFadeInDuration/夜伽字幕淡入时长",
            0.5f,
            "Yotogi Subtitle Fade In Duration in seconds/夜伽字幕淡入时长（秒）");

        YotogiSubtitleFadeOutDuration = Config.Bind("YotogiSubtitle",
            "YotogiSubtitleFadeOutDuration/夜伽字幕淡出时长",
            0.5f,
            "Yotogi Subtitle Fade Out Duration in seconds/夜伽字幕淡出时长（秒）");

        EnableYotogiSubtitleOutline = Config.Bind("YotogiSubtitle",
            "EnableYotogiSubtitleOutline/启用夜伽字幕描边",
            true,
            "Enable Yotogi Subtitle Outline/启用夜伽字幕描边");

        YotogiSubtitleOutlineColor = Config.Bind("YotogiSubtitle",
            "YotogiSubtitleOutlineColor/夜伽字幕描边颜色",
            "#000000",
            "Yotogi Subtitle Outline Color, use hex color code/夜伽字幕描边颜色，使用十六进制颜色代码");

        YotogiSubtitleOutlineOpacity = Config.Bind("YotogiSubtitle",
            "YotogiSubtitleOutlineOpacity/夜伽字幕描边不透明度",
            0.5f,
            "Yotogi Subtitle Outline Opacity (0-1)/夜伽字幕描边不透明度（0-1）");

        YotogiSubtitleOutlineWidth = Config.Bind("YotogiSubtitle",
            "YotogiSubtitleOutlineWidth/夜伽字幕描边宽度",
            1f,
            "Yotogi Subtitle Outline Width in pixels/夜伽字幕描边宽度（像素）");

        # endregion

        # region AdvSubtitleSettings

        // ADV字幕相关配置
        EnableAdvSubtitle = Config.Bind("AdvSubtitle",
            "EnableAdvSubtitle/启用ADV字幕",
            true,
            "Enable ADV Subtitle/启用ADV字幕");

        EnableAdvSubtitleSpeakerName = Config.Bind("AdvSubtitle",
            "EnableAdvSubtitleSpeakerName/启用ADV字幕显示说话人名",
            true,
            "Enable ADV Subtitle Speaker Name/启用ADV字幕显示说话人名");

        AdvSubtitleFont = Config.Bind("AdvSubtitle",
            "AdvSubtitleFont/ADV字幕字体",
            "Arial",
            "ADV Subtitle Font, need to already installed the font on the system/ADV字幕字体，需要已经安装在系统中的字体");

        AdvSubtitleFontSize = Config.Bind("AdvSubtitle",
            "AdvSubtitleFontSize/ADV字幕字体大小",
            24,
            "ADV Subtitle Font Size/ADV字幕字体大小");

        AdvSubtitleColor = Config.Bind("AdvSubtitle",
            "AdvSubtitleColor/ADV字幕颜色",
            "#FFFFFF",
            "ADV Subtitle Color, use hex color code/ADV字幕颜色，使用十六进制颜色代码");

        AdvSubtitleOpacity = Config.Bind("AdvSubtitle",
            "AdvSubtitleOpacity/ADV字幕不透明度",
            1f,
            "ADV Subtitle Opacity (0-1)/ADV字幕不透明度（0-1）");

        AdvSubtitleBackgroundColor = Config.Bind("AdvSubtitle",
            "AdvSubtitleBackgroundColor/ADV字幕背景颜色",
            "#000000",
            "ADV Subtitle Background Color, use hex color code/ADV字幕背景颜色，使用十六进制颜色代码");

        AdvSubtitleBackgroundOpacity = Config.Bind("AdvSubtitle",
            "AdvSubtitleBackgroundOpacity/ADV字幕背景不透明度",
            0.1f,
            "ADV Subtitle Background Opacity (0-1)/ADV字幕背景不透明度（0-1）");

        AdvSubtitleVerticalPosition = Config.Bind("AdvSubtitle",
            "AdvSubtitleVerticalPosition/ADV字幕垂直位置",
            0.965f,
            "ADV Subtitle Vertical Position (0-1, 0 is bottom, 1 is top)/ADV字幕垂直位置（0-1，0为底部，1为顶部）");

        AdvSubtitleBackgroundWidth = Config.Bind("AdvSubtitle",
            "AdvSubtitleBackgroundWidth/ADV字幕背景宽度",
            1f,
            "ADV Subtitle Background Width in percent/ADV背景幕宽度（百分比）");

        AdvSubtitleBackgroundHeight = Config.Bind("AdvSubtitle",
            "AdvSubtitleBackgroundHeight/ADV字幕背景高度",
            0.08f,
            "ADV Subtitle Background Height in percent/ADV背景幕高度（百分比）");

        EnableAdvSubtitleAnimation = Config.Bind("AdvSubtitle",
            "AdvSubtitleAnimation/ADV字幕动画",
            true,
            "Enable ADV Subtitle Animation/启用ADV字幕动画");

        AdvSubtitleFadeInDuration = Config.Bind("AdvSubtitle",
            "AdvSubtitleFadeInDuration/ADV字幕淡入时长",
            0.5f,
            "ADV Subtitle Fade In Duration in seconds/ADV字幕淡入时长（秒）");

        AdvSubtitleFadeOutDuration = Config.Bind("AdvSubtitle",
            "AdvSubtitleFadeOutDuration/ADV字幕淡出时长",
            0.5f,
            "ADV Subtitle Fade Out Duration in seconds/ADV字幕淡出时长（秒）");

        EnableAdvSubtitleOutline = Config.Bind("AdvSubtitle",
            "EnableAdvSubtitleOutline/启用ADV字幕描边",
            true,
            "Enable ADV Subtitle Outline/启用ADV字幕描边");

        AdvSubtitleOutlineColor = Config.Bind("AdvSubtitle",
            "AdvSubtitleOutlineColor/ADV字幕描边颜色",
            "#000000",
            "ADV Subtitle Outline Color, use hex color code/ADV字幕描边颜色，使用十六进制颜色代码");

        AdvSubtitleOutlineOpacity = Config.Bind("AdvSubtitle",
            "AdvSubtitleOutlineOpacity/ADV字幕描边不透明度",
            0.5f,
            "ADV Subtitle Outline Opacity (0-1)/ADV字幕描边不透明度（0-1）");

        AdvSubtitleOutlineWidth = Config.Bind("AdvSubtitle",
            "AdvSubtitleOutlineWidth/ADV字幕描边宽度",
            1f,
            "ADV Subtitle Outline Width in pixels/ADV字幕描边宽度（像素）");

        # endregion

        # region LyricSubtitleSettings

        // 歌词字幕相关配置
        EnableLyricSubtitle = Config.Bind("LyricSubtitle",
            "EnableLyricSubtitle/启用歌词字幕",
            true,
            "Enable Lyric Subtitle/启用歌词字幕");

        EnableLyricSubtitleSpeakerName = Config.Bind("LyricSubtitle",
            "EnableLyricSubtitleSpeakerName/启用歌词字幕显示说话人名",
            true,
            "Enable Lyric Subtitle Speaker Name/启用歌词字幕显示说话人名");

        LyricSubtitleFont = Config.Bind("LyricSubtitle",
            "LyricSubtitleFont/歌词字幕字体",
            "Arial",
            "Lyric Subtitle Font, need to already installed the font on the system/歌词字幕字体，需要已经安装在系统中的字体");

        LyricSubtitleFontSize = Config.Bind("LyricSubtitle",
            "LyricSubtitleFontSize/歌词字幕字体大小",
            24,
            "Lyric Subtitle Font Size/歌词字幕字体大小");

        LyricSubtitleColor = Config.Bind("LyricSubtitle",
            "LyricSubtitleColor/歌词字幕颜色",
            "#FFFFFF",
            "Lyric Subtitle Color, use hex color code/歌词字幕颜色，使用十六进制颜色代码");

        LyricSubtitleOpacity = Config.Bind("LyricSubtitle",
            "LyricSubtitleOpacity/歌词字幕不透明度",
            1f,
            "Lyric Subtitle Opacity (0-1)/歌词字幕不透明度（0-1）");

        LyricSubtitleBackgroundColor = Config.Bind("LyricSubtitle",
            "LyricSubtitleBackgroundColor/歌词字幕背景颜色",
            "#000000",
            "Lyric Subtitle Background Color, use hex color code/歌词字幕背景颜色，使用十六进制颜色代码");

        LyricSubtitleBackgroundOpacity = Config.Bind("LyricSubtitle",
            "LyricSubtitleBackgroundOpacity/歌词字幕背景不透明度",
            0.1f,
            "Lyric Subtitle Background Opacity (0-1)/歌词字幕背景不透明度（0-1）");

        LyricSubtitleVerticalPosition = Config.Bind("LyricSubtitle",
            "LyricSubtitleVerticalPosition/歌词字幕垂直位置",
            0.965f,
            "Lyric Subtitle Vertical Position (0-1, 0 is bottom, 1 is top)/歌词字幕垂直位置（0-1，0为底部，1为顶部）");

        LyricSubtitleBackgroundWidth = Config.Bind("LyricSubtitle",
            "LyricSubtitleBackgroundWidth/歌词字幕背景宽度",
            0.6f,
            "Lyric Subtitle Background Width in percent/歌词背景幕宽度（百分比）");

        LyricSubtitleBackgroundHeight = Config.Bind("LyricSubtitle",
            "LyricSubtitleBackgroundHeight/歌词字幕背景高度",
            0.1f,
            "Lyric Subtitle Background Height in percent/歌词背景幕高度（百分比）");

        EnableLyricSubtitleAnimation = Config.Bind("LyricSubtitle",
            "LyricSubtitleAnimation/歌词字幕动画",
            true,
            "Enable Lyric Subtitle Animation/启用歌词字幕动画");

        LyricSubtitleFadeInDuration = Config.Bind("LyricSubtitle",
            "LyricSubtitleFadeInDuration/歌词字幕淡入时长",
            0.5f,
            "Lyric Subtitle Fade In Duration in seconds/歌词字幕淡入时长（秒）");

        LyricSubtitleFadeOutDuration = Config.Bind("LyricSubtitle",
            "LyricSubtitleFadeOutDuration/歌词字幕淡出时长",
            0.5f,
            "Lyric Subtitle Fade Out Duration in seconds/歌词字幕淡出时长（秒）");

        EnableLyricSubtitleOutline = Config.Bind("LyricSubtitle",
            "EnableLyricSubtitleOutline/启用歌词字幕描边",
            true,
            "Enable Lyric Subtitle Outline/启用歌词字幕描边");

        LyricSubtitleOutlineColor = Config.Bind("LyricSubtitle",
            "LyricSubtitleOutlineColor/歌词字幕描边颜色",
            "#000000",
            "Lyric Subtitle Outline Color, use hex color code/歌词字幕描边颜色，使用十六进制颜色代码");

        LyricSubtitleOutlineOpacity = Config.Bind("LyricSubtitle",
            "LyricSubtitleOutlineOpacity/歌词字幕描边不透明度",
            0.5f,
            "Lyric Subtitle Outline Opacity (0-1)/歌词字幕描边不透明度（0-1）");

        LyricSubtitleOutlineWidth = Config.Bind("LyricSubtitle",
            "LyricSubtitleOutlineWidth/歌词字幕描边宽度",
            1f,
            "Lyric Subtitle Outline Width in pixels/歌词字幕描边宽度（像素）");

        # endregion

        # region VRSubtitleSettings

        // VR悬浮字幕相关配置
        VRSubtitleMode = Config.Bind("VRSubtitle",
            "VRSubtitleMode/VR字幕模式",
            VRSubtitleModeEnum.InSpace,
            "VR Subtitle Mode: InSpace=On Control tablet, OnTablet=Floating in world space following head movement/VR字幕模式：InSpace=字幕在控制平板上，OnTablet=跟随头部运动的世界空间悬浮字幕");

        VRSubtitleDistance = Config.Bind("VRSubtitle",
            "VRSubtitleDistance/VR字幕距离",
            1f,
            "VR Floating Subtitle Distance in meters/VR悬浮字幕距离（米）");

        VRSubtitleVerticalOffset = Config.Bind("VRSubtitle",
            "VRSubtitleVerticalOffset/VR字幕垂直偏移",
            35f,
            "VR Floating Subtitle Vertical Offset in degrees (relative to center of view)/VR悬浮字幕垂直偏移（度，相对于视线中心）");

        VRSubtitleHorizontalOffset = Config.Bind("VRSubtitle",
            "VRSubtitleHorizontalOffset/VR字幕水平偏移",
            0f,
            "VR Floating Subtitle Horizontal Offset in degrees (relative to center of view)/VR悬浮字幕水平偏移（度，相对于视线中心）");

        VRSubtitleWidth = Config.Bind("VRSubtitle",
            "VRSubtitleWidth/VR字幕宽度",
            1f,
            "VR Floating Subtitle Width in meters/VR悬浮字幕宽度（米）");

        VRSubtitleHeight = Config.Bind("VRSubtitle",
            "VRSubtitleHeight/VR字幕高度",
            0.2f,
            "VR Floating Subtitle Height in meters/VR悬浮字幕高度（米）");

        # endregion

        # region SubtitleTypeSettings

        SubtitleType = Config.Bind("Subtitle",
            "SubtitleType/字幕类型",
            SubtitleTypeEnum.Base,
            "Subtitle Type/字幕类型");

        # endregion

        LogManager.Debug($"IsVrMode: {IsVrMode}, CommandLine: {Environment.CommandLine}");

        // Create translation folder
        try
        {
            Directory.CreateDirectory(TranslationRootPath);
            Directory.CreateDirectory(TargetLanguePath);
            Directory.CreateDirectory(TranslationTextPath);
            Directory.CreateDirectory(TranslationTexturePath);
        }
        catch (Exception e)
        {
            LogManager.Error(
                "Create translation folder failed, plugin may not work/创建翻译文件夹失败，插件可能无法运行: " + e.Message);
        }

        // Init modules
        if (EnableTextTranslation.Value)
        {
            LogManager.Info("Text Translation Enabled/文本翻译已启用");
            TextTranslator.Init();
        }
        else
        {
            LogManager.Info("Text Translation Disabled/翻译已禁用");
        }

        if (EnableUITranslation.Value)
        {
            LogManager.Info("UI Translation Enabled/UI 翻译已启用");
            UITranslator.Init();
        }
        else
        {
            LogManager.Info("UI Translation Disabled/UI 翻译已禁用");
        }

        if (EnableTextureReplace.Value)
        {
            LogManager.Info("Texture Replace Enabled/贴图替换已启用");
            TextureReplacer.Init();
        }
        else
        {
            LogManager.Info("Texture Replace Disabled/贴图替换已禁用");
        }

        if (EnableYotogiSubtitle.Value)
        {
            LogManager.Info("Yotogi Subtitle Enabled/夜伽字幕已启用");
            SubtitleManager.Init();
        }
        else
        {
            LogManager.Info("Yotogi Subtitle Disabled/夜伽字幕已禁用");
        }

        if (EnableBaseSubtitle.Value)
        {
            LogManager.Info("Base Subtitle Enabled/基础字幕已启用");
            SubtitleManager.Init();
        }
        else
        {
            LogManager.Info("Base Subtitle Disabled/基础字幕已禁用");
        }

        if (EnableAdvSubtitle.Value)
        {
            LogManager.Info("Adv Subtitle Enabled/ADV字幕已启用");
            SubtitleManager.Init();
        }
        else
        {
            LogManager.Info("Adv Subtitle Disabled/ADV字幕已禁用");
        }


        //TODO 注册部分需要翻新
        // 注册一般配置变更事件
        RegisterGeneralConfigEvents();
        // 注册基础字幕配置变更事件
        RegisterBaseSubtitleConfigEvents();
        // 注册夜伽字幕配置变更事件
        RegisterYotogiSubtitleConfigEvents();
        // 注册ADV字幕配置变更事件
        RegisterAdvSubtitleConfigEvents();
        // 注册VR字幕配置变更事件
        RegisterVRSubtitleConfigEvents();
    }

    private void Start()
    {
        if (EnableTextTranslation.Value)
            // Init XUAT interop
            XUATInterop.Initialize();
    }

    private void OnDestroy()
    {
        TextTranslator.Unload();
        UITranslator.Unload();
        TextureReplacer.Unload();
        SubtitleManager.Unload();
    }

    /// <summary>
    ///     注册一般配置变更事件
    /// </summary>
    private void RegisterGeneralConfigEvents()
    {
        // 注册目标语言变更事件
        TargetLanguage.SettingChanged += (sender, args) =>
        {
            LogManager.Info($"Target language changed to {TargetLanguage.Value}/目标语言已更改为 {TargetLanguage.Value}");

            // 更新翻译路径
            TargetLanguePath = TranslationRootPath + "/" + TargetLanguage.Value;
            TranslationTextPath = TargetLanguePath + "/Text";
            TranslationTexturePath = TargetLanguePath + "/Texture";

            // 创建目录
            try
            {
                Directory.CreateDirectory(TargetLanguePath);
                Directory.CreateDirectory(TranslationTextPath);
                Directory.CreateDirectory(TranslationTexturePath);
            }
            catch (Exception e)
            {
                LogManager.Error($"Create translation folder failed/创建翻译文件夹失败: {e.Message}");
            }

            // 重新加载翻译
            if (EnableTextTranslation.Value)
            {
                TextTranslator.Unload();
                TextTranslator.Init();
            }

            // 重新加载贴图
            if (EnableTextureReplace.Value)
            {
                TextureReplacer.Unload();
                TextureReplacer.Init();
            }
        };

        // 注册文本翻译启用状态变更事件
        EnableTextTranslation.SettingChanged += (sender, args) =>
        {
            if (EnableTextTranslation.Value)
            {
                LogManager.Info("Text Translation Enabled/文本翻译已启用");
                TextTranslator.Init();
                XUATInterop.Initialize();
            }
            else
            {
                LogManager.Info("Text Translation Disabled/文本翻译已禁用");
                TextTranslator.Unload();
            }
        };

        // 注册UI翻译启用状态变更事件
        EnableUITranslation.SettingChanged += (sender, args) =>
        {
            if (EnableUITranslation.Value)
            {
                LogManager.Info("UI Translation Enabled/UI 翻译已启用");
                UITranslator.Init();
            }
            else
            {
                LogManager.Info("UI Translation Disabled/UI 翻译已禁用");
                UITranslator.Unload();
            }
        };

        // 注册贴图替换启用状态变更事件
        EnableTextureReplace.SettingChanged += (sender, args) =>
        {
            if (EnableTextureReplace.Value)
            {
                LogManager.Info("Texture Replace Enabled/贴图替换已启用");
                TextureReplacer.Init();
            }
            else
            {
                LogManager.Info("Texture Replace Disabled/贴图替换已禁用");
                TextureReplacer.Unload();
            }
        };

        // 注册日志级别变更事件
        LogLevelConfig.SettingChanged += (sender, args) =>
        {
            // 不需要重新加载
            LogManager.Info($"Log level changed to {LogLevelConfig.Value}/日志级别已更改为 {LogLevelConfig.Value}");
        };

        // 注册贴图缓存大小变更事件
        TextureCacheSize.SettingChanged += (sender, args) =>
        {
            LogManager.Info(
                $"Texture cache size changed to {TextureCacheSize.Value}/贴图缓存大小已更改为 {TextureCacheSize.Value}");

            // 重新加载贴图替换模块以应用新的缓存大小
            if (EnableTextureReplace.Value)
            {
                TextureReplacer.Unload();
                TextureReplacer.Init();
            }
        };

        // 注册异步加载启用状态变更事件
        EnableAsyncLoading.SettingChanged += (sender, args) =>
        {
            LogManager.Info(
                $"Async loading {(EnableAsyncLoading.Value ? "enabled" : "disabled")}/异步加载{(EnableAsyncLoading.Value ? "已启用" : "已禁用")}");

            // 异步加载设置变更后需要重新加载翻译
            if (EnableTextTranslation.Value)
            {
                TextTranslator.Unload();
                TextTranslator.Init();
            }
        };
    }

    private void RegisterEnableSubtitleEvents()
    {
        EnableBaseSubtitle.SettingChanged += (sender, args) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle Enabled/基础字幕已启用");
                SubtitleManager.Unload();
                SubtitleManager.Init();
            }
            else
            {
                LogManager.Info("Base Subtitle Disabled/基础字幕已禁用");
                SubtitleManager.Unload();
            }
        };
        EnableYotogiSubtitle.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle Enabled/夜伽字幕已启用");
                SubtitleManager.Unload();
                SubtitleManager.Init();
            }
            else
            {
                LogManager.Info("Yotogi Subtitle Disabled/夜伽字幕已禁用");
                SubtitleManager.Unload();
            }
        };

        EnableAdvSubtitle.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("ADV Subtitle Enabled/ADV字幕已启用");
                SubtitleManager.Unload();
                SubtitleManager.Init();
            }
            else
            {
                LogManager.Info("ADV Subtitle Disabled/ADV字幕已禁用");
                SubtitleManager.Unload();
            }
        };


        ForceEnableAdvSubtitle.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("ADV Subtitle Enabled/ADV字幕已启用");
                SubtitleManager.Unload();
                SubtitleManager.Init();
            }
            else
            {
                LogManager.Info("ADV Subtitle Disabled/ADV字幕已禁用");
                SubtitleManager.Unload();
            }
        };

        EnableLyricSubtitle.SettingChanged += (sender, args) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle Enabled/歌词字幕已启用");
                SubtitleManager.Unload();
                SubtitleManager.Init();
            }
            else
            {
                LogManager.Info("Lyric Subtitle Disabled/歌词字幕已禁用");
                SubtitleManager.Unload();
            }
        };
    }

    /// <summary>
    ///     注册夜伽字幕配置变更事件
    /// </summary>
    private void RegisterYotogiSubtitleConfigEvents()
    {
        // 注册字幕启用状态变更事件
        EnableYotogiSubtitle.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle Enabled/夜伽字幕已启用");
                SubtitleManager.Init();
            }
            else
            {
                LogManager.Info("Yotogi Subtitle Disabled/夜伽字幕已禁用");
                SubtitleManager.Unload();
            }
        };

        // 注册字幕说话人名启用状态变更事件
        EnableYotogiSubtitleSpeakerName.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle Speaker Name Enabled/夜伽字幕显示演员名已启用");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕字体变更事件
        YotogiSubtitleFont.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle font changed/夜伽字幕字体已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕字体大小变更事件
        YotogiSubtitleFontSize.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle font size changed/夜伽字幕字体大小已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕颜色变更事件
        YotogiSubtitleColor.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle color changed/夜伽字幕颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕透明度变更事件
        YotogiSubtitleOpacity.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle opacity changed/夜伽字幕透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕背景颜色变更事件
        YotogiSubtitleBackgroundColor.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle background color changed/夜伽字幕背景颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕背景不透明度变更事件
        YotogiSubtitleBackgroundOpacity.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle background opacity changed/夜伽字幕背景不透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕位置变更事件
        YotogiSubtitleVerticalPosition.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle position changed/夜伽字幕位置已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕高度变更事件
        YotogiSubtitleBackgroundHeight.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle height changed/夜伽字幕高度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕动画变更事件
        EnableYotogiSubtitleAnimation.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle animation changed/夜伽字幕动画已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕淡入时长变更事件
        YotogiSubtitleFadeInDuration.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle fade in duration changed/夜伽字幕淡入时长已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕淡出时长变更事件
        YotogiSubtitleFadeOutDuration.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle fade out duration changed/夜伽字幕淡出时长已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边变更事件
        EnableYotogiSubtitleOutline.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle outline changed/夜伽字幕描边已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边颜色变更事件
        YotogiSubtitleOutlineColor.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle outline color changed/夜伽字幕描边颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边不透明度变更事件
        YotogiSubtitleOutlineOpacity.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle outline opacity changed/夜伽字幕描边不透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边宽度变更事件
        YotogiSubtitleOutlineWidth.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle outline width changed/夜伽字幕描边宽度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };
    }

    /// <summary>
    ///     注册基础字幕配置变更事件
    /// </summary>
    private void RegisterBaseSubtitleConfigEvents()
    {
        // 注册字幕启用状态变更事件
        EnableBaseSubtitle.SettingChanged += (sender, args) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle Enabled/基础字幕已启用");
                SubtitleManager.Init();
            }
            else
            {
                LogManager.Info("Base Subtitle Disabled/基础字幕已禁用");
                SubtitleManager.Unload();
            }
        };

        // 注册字幕说话人名启用状态变更事件
        EnableBaseSubtitleSpeakerName.SettingChanged += (sender, args) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle Speaker Name Enabled/基础字幕显示演员名已启用");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕字体变更事件
        BaseSubtitleFont.SettingChanged += (sender, args) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle font changed/基础字幕字体已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕字体大小变更事件
        BaseSubtitleFontSize.SettingChanged += (sender, args) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle font size changed/基础字幕字体大小已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕颜色变更事件
        BaseSubtitleColor.SettingChanged += (sender, args) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle color changed/基础字幕颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕透明度变更事件
        BaseSubtitleOpacity.SettingChanged += (sender, args) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle opacity changed/基础字幕透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕背景颜色变更事件
        BaseSubtitleBackgroundColor.SettingChanged += (sender, args) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle background color changed/基础字幕背景颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕背景不透明度变更事件
        BaseSubtitleBackgroundOpacity.SettingChanged += (sender, args) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle background opacity changed/基础字幕背景不透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕位置变更事件
        BaseSubtitleVerticalPosition.SettingChanged += (sender, args) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle position changed/基础字幕位置已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕高度变更事件
        BaseSubtitleBackgroundHeight.SettingChanged += (sender, args) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle height changed/基础字幕高度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕动画变更事件
        EnableBaseSubtitleAnimation.SettingChanged += (sender, args) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle animation changed/基础字幕动画已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕淡入时长变更事件
        BaseSubtitleFadeInDuration.SettingChanged += (sender, args) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle fade in duration changed/基础字幕淡入时长已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕淡出时长变更事件
        BaseSubtitleFadeOutDuration.SettingChanged += (sender, args) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle fade out duration changed/基础字幕淡出时长已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边变更事件
        EnableBaseSubtitleOutline.SettingChanged += (sender, args) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle outline changed/基础字幕描边已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边颜色变更事件
        BaseSubtitleOutlineColor.SettingChanged += (sender, args) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle outline color changed/基础字幕描边颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边不透明度变更事件
        BaseSubtitleOutlineOpacity.SettingChanged += (sender, args) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle outline opacity changed/基础字幕描边不透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边宽度变更事件
        BaseSubtitleOutlineWidth.SettingChanged += (sender, args) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle outline width changed/基础字幕描边宽度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };
    }

    /// <summary>
    ///     注册VR字幕配置变更事件
    /// </summary>
    private void RegisterVRSubtitleConfigEvents()
    {
        // 注册VR字幕模式变更事件
        VRSubtitleMode.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle VR mode changed/夜伽字幕VR模式已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册VR字幕距离变更事件
        VRSubtitleDistance.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle VR distance changed/夜伽字幕VR距离已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册VR字幕垂直偏移变更事件
        VRSubtitleVerticalOffset.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle VR vertical offset changed/夜伽字幕VR垂直偏移已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册VR字幕水平偏移变更事件
        VRSubtitleHorizontalOffset.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle VR horizontal offset changed/夜伽字幕VR水平偏移已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册VR字幕宽度变更事件
        VRSubtitleWidth.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle VR width changed/夜伽字幕VR宽度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };
    }

    /// <summary>
    ///     注册ADV字幕配置变更事件
    /// </summary>
    private void RegisterAdvSubtitleConfigEvents()
    {
        // 注册字幕启用状态变更事件
        EnableAdvSubtitle.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle Enabled/ADV字幕已启用");
                SubtitleManager.Init();
            }
            else
            {
                LogManager.Info("Adv Subtitle Disabled/ADV字幕已禁用");
                SubtitleManager.Unload();
            }
        };

        // 注册字幕说话人名启用状态变更事件
        EnableAdvSubtitleSpeakerName.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle Speaker Name Enabled/ADV字幕显示演员名已启用");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕字体变更事件
        AdvSubtitleFont.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle font changed/ADV字幕字体已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕字体大小变更事件
        AdvSubtitleFontSize.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle font size changed/ADV字幕字体大小已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕颜色变更事件
        AdvSubtitleColor.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle color changed/ADV字幕颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕透明度变更事件
        AdvSubtitleOpacity.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle opacity changed/ADV字幕透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕背景颜色变更事件
        AdvSubtitleBackgroundColor.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle background color changed/ADV字幕背景颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕背景不透明度变更事件
        AdvSubtitleBackgroundOpacity.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle background opacity changed/ADV字幕背景不透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕位置变更事件
        AdvSubtitleVerticalPosition.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle position changed/ADV字幕位置已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕高度变更事件
        AdvSubtitleBackgroundHeight.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle height changed/ADV字幕高度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕动画变更事件
        EnableAdvSubtitleAnimation.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle animation changed/ADV字幕动画已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕淡入时长变更事件
        AdvSubtitleFadeInDuration.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle fade in duration changed/ADV字幕淡入时长已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕淡出时长变更事件
        AdvSubtitleFadeOutDuration.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle fade out duration changed/ADV字幕淡出时长已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边变更事件
        EnableAdvSubtitleOutline.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle outline changed/ADV字幕描边已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边颜色变更事件
        AdvSubtitleOutlineColor.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle outline color changed/ADV字幕描边颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边不透明度变更事件
        AdvSubtitleOutlineOpacity.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle outline opacity changed/ADV字幕描边不透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边宽度变更事件
        AdvSubtitleOutlineWidth.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle outline width changed/ADV字幕描边宽度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };
    }
}