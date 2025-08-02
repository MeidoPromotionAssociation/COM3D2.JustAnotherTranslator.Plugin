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

[BepInPlugin("Github.MeidoPromotionAssociation.COM3D2.JustAnotherTranslator.Plugin",
    "COM3D2.JustAnotherTranslator.Plugin", "0.0.1")]
public class JustAnotherTranslator : BaseUnityPlugin
{
    // To discover this plugin
    // The Hooks folder is where we interact with the game code
    // The Translator folder is where we load translation data and process data passed in by Hooks
    // The Subtitle folder contains subtitle manager, configuration, components
    // The Utils folder contains helper functions
    // This file is mainly for configuration

    public enum LyricSubtitleTypeEnum
    {
        [Description("OriginalOnly/仅原文")] OriginalOnly,
        [Description("TranslationOnly/仅译文")] TranslationOnly,

        [Description("OriginalAndTranslation/译文和原文")]
        TranslationAndOriginal,

        [Description("OriginalAndTranslation/原文和译文")]
        OriginalAndTranslation
    }

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

    public enum TextAnchorEnum
    {
        [Description("UpperLeft/左上")] UpperLeft,
        [Description("UpperCenter/中上")] UpperCenter,
        [Description("UpperRight/右上")] UpperRight,
        [Description("MiddleLeft/左中")] MiddleLeft,
        [Description("MiddleCenter/中中")] MiddleCenter,
        [Description("MiddleRight/右中")] MiddleRight,
        [Description("LowerLeft/左下")] LowerLeft,
        [Description("LowerCenter/中下")] LowerCenter,
        [Description("LowerRight/右下")] LowerRight
    }

    public enum VRSubtitleModeEnum
    {
        [Description("InSpac/空间字幕")] InSpace,
        [Description("OnTablet/平板字幕")] OnTablet
    }

    public static bool IsVrMode;

    // 提示信息
    private static ConfigEntry<bool> _tip1;
    private static ConfigEntry<bool> _tip2;
    private static ConfigEntry<bool> _tip3;
    private static ConfigEntry<bool> _tip4;

    // 通用配置
    public static ConfigEntry<string> TargetLanguage;
    public static ConfigEntry<bool> EnableTextTranslation;
    public static ConfigEntry<bool> EnableUITranslation;
    public static ConfigEntry<bool> EnableTextureReplace;
    public static ConfigEntry<MaidNameStyleEnum> MaidNameStyle;
    public static ConfigEntry<LogLevel> LogLevelConfig;
    public static ConfigEntry<int> TextureCacheSize;
    public static ConfigEntry<int> UICacheSize;
    public static ConfigEntry<bool> AllowFilesInZipLoadInOrder;

    // 字幕启用相关配置
    public static ConfigEntry<bool> EnableBaseSubtitle;
    public static ConfigEntry<bool> EnableYotogiSubtitle;
    public static ConfigEntry<bool> EnableAdvSubtitle;
    public static ConfigEntry<bool> ForceEnableAdvSubtitle;
    public static ConfigEntry<bool> EnableLyricSubtitle;

    // 基础字幕相关配置
    public static ConfigEntry<bool> EnableBaseSubtitleSpeakerName;
    public static ConfigEntry<TextAnchorEnum> BaseSubtitleTextAlignment;
    public static ConfigEntry<string> BaseSubtitleFont;
    public static ConfigEntry<int> BaseSubtitleFontSize;
    public static ConfigEntry<string> BaseSubtitleColor;
    public static ConfigEntry<float> BaseSubtitleOpacity;
    public static ConfigEntry<string> BaseSubtitleBackgroundColor;
    public static ConfigEntry<float> BaseSubtitleBackgroundOpacity;
    public static ConfigEntry<float> BaseSubtitleHorizontalPosition;
    public static ConfigEntry<float> BaseSubtitleVerticalPosition;
    public static ConfigEntry<float> BaseSubtitleWidth;
    public static ConfigEntry<float> BaseSubtitleHeight;
    public static ConfigEntry<bool> EnableBaseSubtitleAnimation;
    public static ConfigEntry<float> BaseSubtitleFadeInDuration;
    public static ConfigEntry<float> BaseSubtitleFadeOutDuration;
    public static ConfigEntry<bool> EnableBaseSubtitleOutline;
    public static ConfigEntry<string> BaseSubtitleOutlineColor;
    public static ConfigEntry<float> BaseSubtitleOutlineOpacity;
    public static ConfigEntry<float> BaseSubtitleOutlineWidth;

    // 夜伽字幕相关配置
    public static ConfigEntry<bool> EnableYotogiSubtitleSpeakerName;
    public static ConfigEntry<TextAnchorEnum> YotogiSubtitleTextAlignment;
    public static ConfigEntry<string> YotogiSubtitleFont;
    public static ConfigEntry<int> YotogiSubtitleFontSize;
    public static ConfigEntry<string> YotogiSubtitleColor;
    public static ConfigEntry<float> YotogiSubtitleOpacity;
    public static ConfigEntry<string> YotogiSubtitleBackgroundColor;
    public static ConfigEntry<float> YotogiSubtitleBackgroundOpacity;
    public static ConfigEntry<float> YotogiSubtitleHorizontalPosition;
    public static ConfigEntry<float> YotogiSubtitleVerticalPosition;
    public static ConfigEntry<float> YotogiSubtitleWidth;
    public static ConfigEntry<float> YotogiSubtitleHeight;
    public static ConfigEntry<bool> EnableYotogiSubtitleAnimation;
    public static ConfigEntry<float> YotogiSubtitleFadeInDuration;
    public static ConfigEntry<float> YotogiSubtitleFadeOutDuration;
    public static ConfigEntry<bool> EnableYotogiSubtitleOutline;
    public static ConfigEntry<string> YotogiSubtitleOutlineColor;
    public static ConfigEntry<float> YotogiSubtitleOutlineOpacity;
    public static ConfigEntry<float> YotogiSubtitleOutlineWidth;

    // ADV字幕相关配置
    public static ConfigEntry<bool> EnableAdvSubtitleSpeakerName;
    public static ConfigEntry<TextAnchorEnum> AdvSubtitleTextAlignment;
    public static ConfigEntry<string> AdvSubtitleFont;
    public static ConfigEntry<int> AdvSubtitleFontSize;
    public static ConfigEntry<string> AdvSubtitleColor;
    public static ConfigEntry<float> AdvSubtitleOpacity;
    public static ConfigEntry<string> AdvSubtitleBackgroundColor;
    public static ConfigEntry<float> AdvSubtitleBackgroundOpacity;
    public static ConfigEntry<float> AdvSubtitleHorizontalPosition;
    public static ConfigEntry<float> AdvSubtitleVerticalPosition;
    public static ConfigEntry<float> AdvSubtitleWidth;
    public static ConfigEntry<float> AdvSubtitleHeight;
    public static ConfigEntry<bool> EnableAdvSubtitleAnimation;
    public static ConfigEntry<float> AdvSubtitleFadeInDuration;
    public static ConfigEntry<float> AdvSubtitleFadeOutDuration;
    public static ConfigEntry<bool> EnableAdvSubtitleOutline;
    public static ConfigEntry<string> AdvSubtitleOutlineColor;
    public static ConfigEntry<float> AdvSubtitleOutlineOpacity;
    public static ConfigEntry<float> AdvSubtitleOutlineWidth;

    // 歌词字幕相关配置
    public static ConfigEntry<bool> EnableLyricSubtitleSpeakerName;
    public static ConfigEntry<LyricSubtitleTypeEnum> LyricSubtitleType;
    public static ConfigEntry<TextAnchorEnum> LyricSubtitleTextAlignment;
    public static ConfigEntry<string> LyricSubtitleFont;
    public static ConfigEntry<int> LyricSubtitleFontSize;
    public static ConfigEntry<string> LyricSubtitleColor;
    public static ConfigEntry<float> LyricSubtitleOpacity;
    public static ConfigEntry<string> LyricSubtitleBackgroundColor;
    public static ConfigEntry<float> LyricSubtitleBackgroundOpacity;
    public static ConfigEntry<float> LyricSubtitleHorizontalPosition;
    public static ConfigEntry<float> LyricSubtitleVerticalPosition;
    public static ConfigEntry<float> LyricSubtitleWidth;
    public static ConfigEntry<float> LyricSubtitleHeight;
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
    public static ConfigEntry<float> VRInSpaceSubtitleWidth;
    public static ConfigEntry<float> VRInSpaceSubtitleHeight;
    public static ConfigEntry<float> VRSubtitleScale;

    // dump相关配置
    private static ConfigEntry<bool> _dumpTip1;
    private static ConfigEntry<bool> _dumpTip2;
    private static ConfigEntry<bool> _dumpTip3;
    private static ConfigEntry<bool> _dumpTip4;
    public static ConfigEntry<bool> EnableTexturesDump;
    public static ConfigEntry<bool> EnableSpriteDump;
    public static ConfigEntry<bool> EnableTextDump;
    public static ConfigEntry<int> TextDumpThreshold;
    public static ConfigEntry<bool> FlushTextDumpNow;


    // translation folder path
    public static readonly string TranslationRootPath = Path.Combine(Paths.BepInExRootPath, "JustAnotherTranslator");
    public static string TargetLanguePath;
    public static string TranslationTextPath;
    public static string TranslationTexturePath;
    public static string LyricPath;
    public static string UIPath;
    public static string UITextPath;
    public static string UISpritePath;
    public static string DumpPath;
    public static string TextDumpPath;
    public static string TextureDumpPath;
    public static string SpriteDumpPath;

    private void Awake()
    {
        Logger.LogInfo(
            $"{Info.Metadata.Name} {Info.Metadata.Version} is loading/{Info.Metadata.Name} {Info.Metadata.Version} 正在载入");
        Logger.LogInfo(
            "Get update or report bug/获取更新或报告bug: https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin");
        Logger.LogInfo(
            "This plugin does not provide translation data, you need to get it somewhere else/本插件不提供翻译数据，您需要自行获取");

        // Init our LogManager with the BepInEx logger
        LogManager.Initialize(Logger);

        IsVrMode = Environment.CommandLine.ToLower().Contains("/vr");

        # region Tips

        // Tips for people who using ConfigurationManager
        _tip1 = Config.Bind("1Tips",
            "Configuration options tips do not prompt in the game, please open the configuration file to view", true,
            new ConfigDescription("this config do nothing", null, new ConfigurationManagerAttributes { Order = 1000 }));

        _tip2 = Config.Bind("1Tips",
            "configuration file location is COM3D2/BepInEx/config/Github.MeidoPromotionAssociation.COM3D2.JustAnotherTranslator.Plugin.cfg",
            true,
            new ConfigDescription("this config do nothing", null, new ConfigurationManagerAttributes { Order = 1010 }));

        _tip3 = Config.Bind("1Tips",
            "配置选项提示不会在游戏内提示，请打开配置文件查看", true,
            new ConfigDescription("这个配置不做任何事情", null, new ConfigurationManagerAttributes { Order = 1020 }));

        _tip4 = Config.Bind("1Tips",
            "配置文件位于 /COM3D2/BepInEx/config/Github.MeidoPromotionAssociation.COM3D2.JustAnotherTranslator.Plugin.cfg",
            true,
            new ConfigDescription("这个配置不做任何事情", null, new ConfigurationManagerAttributes { Order = 1030 }));

        # endregion

        # region GeneralSettings

        TargetLanguage = Config.Bind("2General",
            "TargetLanguage/目标语言",
            "zh-CN",
            new ConfigDescription(
                "Target Language, only affect the path of reading translation files/目标语言，只控制读取翻译文件的路径", null,
                new ConfigurationManagerAttributes { Order = 2000 }));


        TargetLanguePath = Path.Combine(TranslationRootPath, TargetLanguage.Value);
        TranslationTextPath = Path.Combine(TargetLanguePath, "Text");
        TranslationTexturePath = Path.Combine(TargetLanguePath, "Texture");
        LyricPath = Path.Combine(TargetLanguePath, "Lyric");
        UIPath = Path.Combine(TargetLanguePath, "UI");
        UITextPath = Path.Combine(UIPath, "Text");
        UISpritePath = Path.Combine(UIPath, "Sprite");
        DumpPath = Path.Combine(TranslationRootPath, "Dump");
        TextDumpPath = Path.Combine(DumpPath, "Text");
        TextureDumpPath = Path.Combine(DumpPath, "Texture");
        SpriteDumpPath = Path.Combine(DumpPath, "Sprite");


        EnableTextTranslation = Config.Bind("2General",
            "EnableTextTranslation/启用文本翻译",
            true,
            new ConfigDescription("Enable Text Translation/启用文本翻译", null,
                new ConfigurationManagerAttributes { Order = 2010 }));


        EnableUITranslation = Config.Bind("2General",
            "EnableUITranslation/启用 UI 翻译",
            true,
            new ConfigDescription("Enable UI Translation/启用 UI 翻译", null,
                new ConfigurationManagerAttributes { Order = 2020 }));

        EnableTextureReplace = Config.Bind("2General",
            "EnableTextureReplace/启用贴图替换",
            true,
            new ConfigDescription("Enable Texture Replace/启用贴图替换", null,
                new ConfigurationManagerAttributes { Order = 2030 }));


        AllowFilesInZipLoadInOrder = Config.Bind("2General",
            "AllowFilesInZipLoadInOrder/允许 ZIP 文件内文件按顺序加载",
            false,
            new ConfigDescription(
                "Allow files In zip Load in order, This will lower the loading speed/允许 ZIP 文件内文件按顺序加载，这会降低加载速度", null,
                new ConfigurationManagerAttributes { Order = 2040 }));


        MaidNameStyle = Config.Bind("2General",
            "MaidNameStyle/女仆名字样式",
            MaidNameStyleEnum.JpStyle,
            new ConfigDescription(
                "Maid Name Style, JpStyle is family name first and given name last, English style is opposite, cannot change at runtime/女仆名字样式，日式姓前名后，英式相反。无法在运行时更改",
                null, new ConfigurationManagerAttributes { Order = 2050 }));

        // 声明后才能使用日志
        LogLevelConfig = Config.Bind("2General",
            "LogLevel/日志级别",
            LogLevel.Info,
            new ConfigDescription("Log Level, DEBUG will log more information/日志级别，DEBUG 级别将记录详细信息", null,
                new ConfigurationManagerAttributes { Order = 2060 }));


        TextureCacheSize = Config.Bind("2General",
            "TextureCacheSize/贴图缓存大小",
            30,
            new ConfigDescription(
                "Texture Replace Cache Size, larger value will use more memory but improve performance/贴图替换缓存大小，较大的值会使用更多内存但提高性能",
                null, new ConfigurationManagerAttributes { Order = 2070 }));


        UICacheSize = Config.Bind("2General",
            "UICacheSize/UI缓存大小",
            30,
            new ConfigDescription(
                "UI Sprite Cache Size, larger value will use more memory but improve performance/UI精灵图缓存大小，较大的值会使用更多内存但提高性能",
                null, new ConfigurationManagerAttributes { Order = 2080 }));

        # endregion

        # region SubtitleEnableSettings

        EnableBaseSubtitle = Config.Bind("3Subtitle",
            "EnableBaseSubtitle/启用基础字幕",
            true,
            new ConfigDescription(
                "Enable Base Subtitle, usually Karaoke, Casino, etc Some voices are matched by audio file name, so may not be displayed if text translation is not enabled/启用基础字幕，通常是卡拉OK、赌场等字幕。部分语音按音频文件名匹配，因此未启用文本翻译时可能无法显示",
                null, new ConfigurationManagerAttributes { Order = 3000 }));

        EnableYotogiSubtitle = Config.Bind("3Subtitle",
            "EnableYotogiSubtitle/启用夜伽字幕",
            true,
            new ConfigDescription("Enable Yotogi Subtitle/启用夜伽字幕", null,
                new ConfigurationManagerAttributes { Order = 2010 }));

        EnableAdvSubtitle = Config.Bind("3Subtitle",
            "EnableADVSubtitle/启用ADV字幕",
            true,
            new ConfigDescription(
                "Enable ADV subtitles. Since ADV scenes have their own subtitles, this setting is only useful in VR mode and is invalid in non-VR mode./启用ADV字幕，由于 ADV 场景自带字幕，因此仅在 VR 模式下有用，非 VR 模式此设置无效",
                null, new ConfigurationManagerAttributes { Order = 3010 }));

        ForceEnableAdvSubtitle = Config.Bind("3Subtitle",
            "ForceEnableADVSubtitle/强制启用ADV字幕",
            false,
            new ConfigDescription("Force Enable ADV subtitles, whether in VR mode or not/强制启用ADV字幕，无论是不是 VR 模式", null,
                new ConfigurationManagerAttributes { Order = 3020 }));

        EnableLyricSubtitle = Config.Bind("3Subtitle",
            "EnableLyricSubtitle/启用歌词字幕",
            true,
            new ConfigDescription("Enable Lyric Subtitle/启用歌词字幕", null,
                new ConfigurationManagerAttributes { Order = 3030 }));

        # endregion

        # region BaseSubtitleSettings

        // 基础字幕相关配置
        EnableBaseSubtitleSpeakerName = Config.Bind("4BaseSubtitle",
            "EnableBaseSubtitleSpeakerName/启用基础字幕显示说话人名",
            true,
            new ConfigDescription("Enable Base Subtitle Speaker Name/启用基础字幕显示说话人名", null,
                new ConfigurationManagerAttributes { Order = 4000 }));

        BaseSubtitleFont = Config.Bind("4BaseSubtitle",
            "BaseSubtitleFont/基础字幕字体",
            "Arial",
            new ConfigDescription(
                "Base Subtitle Font, need to already installed the font on the system/基础字幕字体，需要已经安装在系统中的字体", null,
                new ConfigurationManagerAttributes { Order = 4010 }));

        BaseSubtitleFontSize = Config.Bind("4BaseSubtitle",
            "BaseSubtitleFontSize/基础字幕字体大小",
            24,
            new ConfigDescription("Base Subtitle Font Size/基础字幕字体大小", null,
                new ConfigurationManagerAttributes { Order = 4020 }));

        BaseSubtitleTextAlignment = Config.Bind("4BaseSubtitle",
            "BaseSubtitleTextAlignment/基础字幕文本对齐方式",
            TextAnchorEnum.MiddleCenter,
            new ConfigDescription("Base Subtitle Text Alignment/基础字幕文本对齐方式", null,
                new ConfigurationManagerAttributes { Order = 4030 }));

        BaseSubtitleColor = Config.Bind("4BaseSubtitle",
            "BaseSubtitleColor/基础字幕颜色",
            "#FFFFFF",
            new ConfigDescription("Base Subtitle Color, use hex color code/基础字幕颜色，使用十六进制颜色代码", null,
                new ConfigurationManagerAttributes { Order = 4040 }));

        BaseSubtitleOpacity = Config.Bind("4BaseSubtitle",
            "BaseSubtitleOpacity/基础字幕不透明度",
            1f,
            new ConfigDescription("Base Subtitle Opacity (0-1)/基础字幕不透明度（0-1）", null,
                new ConfigurationManagerAttributes { Order = 4050 }));

        BaseSubtitleBackgroundColor = Config.Bind("4BaseSubtitle",
            "BaseSubtitleBackgroundColor/基础字幕背景颜色",
            "#000000",
            new ConfigDescription("Base Subtitle Background Color, use hex color code/基础字幕背景颜色，使用十六进制颜色代码", null,
                new ConfigurationManagerAttributes { Order = 4060 }));

        BaseSubtitleBackgroundOpacity = Config.Bind("4BaseSubtitle",
            "BaseSubtitleBackgroundOpacity/基础字幕背景不透明度",
            0.1f,
            new ConfigDescription("Base Subtitle Background Opacity (0-1)/基础字幕背景不透明度（0-1）", null,
                new ConfigurationManagerAttributes { Order = 4070 }));

        BaseSubtitleWidth = Config.Bind("4BaseSubtitle",
            "BaseSubtitleWidth/基础字幕背景宽度",
            1920f,
            new ConfigDescription(
                "Subtitle Width(pixel), reference resolution is 1920x1080, so set to 1920 will fill the whole screen width/字幕背景宽度(像素)，参考分辨率为1920x1080，因此设置为1920时将填满整个屏幕宽",
                null, new ConfigurationManagerAttributes { Order = 4080 }));

        BaseSubtitleHeight = Config.Bind("4BaseSubtitle",
            "BaseSubtitleHeight/基础字幕背景高度",
            30f,
            new ConfigDescription(
                "Subtitle Height(pixel), reference resolution is 1920x1080, so set to 1080 will fill the whole screen height/字幕背景高度(像素)，参考分辨率为1920x1080，因此设置为1080时将填满整个屏幕高",
                null, new ConfigurationManagerAttributes { Order = 4090 }));

        BaseSubtitleHorizontalPosition = Config.Bind("4BaseSubtitle",
            "BaseSubtitleHorizontalPosition/基础字幕水平位置",
            0f,
            new ConfigDescription(
                "Distance to the left side of the screen (0 is the left, 1920 is the right)/到屏幕左边的距离（0为左边，1920为最右边）",
                null, new ConfigurationManagerAttributes { Order = 4100 }));

        BaseSubtitleVerticalPosition = Config.Bind("4BaseSubtitle",
            "BaseSubtitleVerticalPosition/基础字幕垂直位置",
            1050f,
            new ConfigDescription(
                "Distance to bottom of screen (0 is bottom, 1 is top, note 1080 will go out of screen, should subtract background height)/到屏幕底部的距离（0为底部，1080为顶部，注意1080会超出屏幕，应减去背景高度）",
                null, new ConfigurationManagerAttributes { Order = 4110 }));

        EnableBaseSubtitleAnimation = Config.Bind("4BaseSubtitle",
            "BaseSubtitleAnimation/基础字幕动画",
            true,
            new ConfigDescription("Enable Base Subtitle Animation/启用基础字幕动画", null,
                new ConfigurationManagerAttributes { Order = 4120 }));

        BaseSubtitleFadeInDuration = Config.Bind("4BaseSubtitle",
            "BaseSubtitleFadeInDuration/基础字幕淡入时长",
            0.5f,
            new ConfigDescription("Base Subtitle Fade In Duration in seconds/基础字幕淡入时长（秒）", null,
                new ConfigurationManagerAttributes { Order = 4130 }));

        BaseSubtitleFadeOutDuration = Config.Bind("4BaseSubtitle",
            "BaseSubtitleFadeOutDuration/基础字幕淡出时长",
            0.5f,
            new ConfigDescription("Base Subtitle Fade Out Duration in seconds/基础字幕淡出时长（秒）", null,
                new ConfigurationManagerAttributes { Order = 4140 }));

        EnableBaseSubtitleOutline = Config.Bind("4BaseSubtitle",
            "EnableBaseSubtitleOutline/启用基础字幕描边",
            true,
            new ConfigDescription("Enable Base Subtitle Outline/启用基础字幕描边", null,
                new ConfigurationManagerAttributes { Order = 4150 }));

        BaseSubtitleOutlineColor = Config.Bind("4BaseSubtitle",
            "BaseSubtitleOutlineColor/基础字幕描边颜色",
            "#000000",
            new ConfigDescription("Base Subtitle Outline Color, use hex color code/基础字幕描边颜色，使用十六进制颜色代码", null,
                new ConfigurationManagerAttributes { Order = 4160 }));

        BaseSubtitleOutlineOpacity = Config.Bind("4BaseSubtitle",
            "BaseSubtitleOutlineOpacity/基础字幕描边不透明度",
            0.5f,
            new ConfigDescription("Base Subtitle Outline Opacity (0-1)/基础字幕描边不透明度（0-1）", null,
                new ConfigurationManagerAttributes { Order = 4170 }));

        BaseSubtitleOutlineWidth = Config.Bind("4BaseSubtitle",
            "BaseSubtitleOutlineWidth/基础字幕描边宽度",
            1f,
            new ConfigDescription("Base Subtitle Outline Width in pixels/基础字幕描边宽度（像素）", null,
                new ConfigurationManagerAttributes { Order = 4180 }));

        # endregion

        # region YotogiSubtitleSettings

        // 夜伽字幕相关配置
        EnableYotogiSubtitleSpeakerName = Config.Bind("5YotogiSubtitle",
            "EnableYotogiSubtitleSpeakerName/启用夜伽字幕显示说话人名",
            true,
            new ConfigDescription("Enable Yotogi Subtitle Speaker Name/启用夜伽字幕显示说话人名", null,
                new ConfigurationManagerAttributes { Order = 5000 }));

        YotogiSubtitleFont = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleFont/夜伽字幕字体",
            "Arial",
            new ConfigDescription(
                "Yotogi Subtitle Font, need to already installed the font on the system/夜伽字幕字体，需要已经安装在系统中的字体", null,
                new ConfigurationManagerAttributes { Order = 5010 }));

        YotogiSubtitleFontSize = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleFontSize/夜伽字幕字体大小",
            24,
            new ConfigDescription("Yotogi Subtitle Font Size/夜伽字幕字体大小", null,
                new ConfigurationManagerAttributes { Order = 5020 }));

        YotogiSubtitleTextAlignment = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleTextAlignment/夜伽字幕文本对齐",
            TextAnchorEnum.MiddleCenter,
            new ConfigDescription("Yotogi Subtitle Text Alignment/夜伽字幕文本对齐方式", null,
                new ConfigurationManagerAttributes { Order = 5030 }));

        YotogiSubtitleColor = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleColor/夜伽字幕颜色",
            "#FFFFFF",
            new ConfigDescription("Yotogi Subtitle Color, use hex color code/夜伽字幕颜色，使用十六进制颜色代码", null,
                new ConfigurationManagerAttributes { Order = 5040 }));

        YotogiSubtitleOpacity = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleOpacity/夜伽字幕不透明度",
            1f,
            new ConfigDescription("Yotogi Subtitle Opacity (0-1)/夜伽字幕不透明度（0-1）", null,
                new ConfigurationManagerAttributes { Order = 5050 }));

        YotogiSubtitleBackgroundColor = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleBackgroundColor/夜伽字幕背景颜色",
            "#000000",
            new ConfigDescription("Yotogi Subtitle Background Color, use hex color code/夜伽字幕背景颜色，使用十六进制颜色代码", null,
                new ConfigurationManagerAttributes { Order = 5060 }));

        YotogiSubtitleBackgroundOpacity = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleBackgroundOpacity/夜伽字幕背景不透明度",
            0.1f,
            new ConfigDescription("Yotogi Subtitle Background Opacity (0-1)/夜伽字幕背景不透明度（0-1）", null,
                new ConfigurationManagerAttributes { Order = 5070 }));

        YotogiSubtitleWidth = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleWidth/夜伽字幕背景宽度",
            1920f,
            new ConfigDescription(
                "Subtitle Width(pixel), reference resolution is 1920x1080, so set to 1920 will fill the whole screen width/字幕宽度(像素)，参考分辨率为1920x1080，因此设置为1920时将填满整个屏幕宽",
                null, new ConfigurationManagerAttributes { Order = 5080 }));

        YotogiSubtitleHeight = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleHeight/夜伽字幕背景高度",
            30f,
            new ConfigDescription(
                "Subtitle Height(pixel), reference resolution is 1920x1080, so set to 1080 will fill the whole screen height/字幕高度(像素)，参考分辨率为1920x1080，因此设置为1080时将填满整个屏幕高",
                null, new ConfigurationManagerAttributes { Order = 5090 }));

        YotogiSubtitleHorizontalPosition = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleHorizontalPosition/夜伽字幕水平位置",
            0f,
            new ConfigDescription(
                "Distance to the left side of the screen (0 is the left, 1920 is the right)/到屏幕左边的距离（0为左边，1920为最右边）",
                null, new ConfigurationManagerAttributes { Order = 5100 }));

        YotogiSubtitleVerticalPosition = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleVerticalPosition/夜伽字幕垂直位置",
            1050f,
            new ConfigDescription(
                "Distance to the left side of the screen (0 is the left, 1920 is the right)/到屏幕左边的距离（0为左边，1920为最右边）",
                null, new ConfigurationManagerAttributes { Order = 5110 }));

        EnableYotogiSubtitleAnimation = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleAnimation/夜伽字幕动画",
            true,
            new ConfigDescription("Enable Yotogi Subtitle Animation/启用夜伽字幕动画", null,
                new ConfigurationManagerAttributes { Order = 5120 }));

        YotogiSubtitleFadeInDuration = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleFadeInDuration/夜伽字幕淡入时长",
            0.5f,
            new ConfigDescription("Yotogi Subtitle Fade In Duration in seconds/夜伽字幕淡入时长（秒）", null,
                new ConfigurationManagerAttributes { Order = 5130 }));

        YotogiSubtitleFadeOutDuration = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleFadeOutDuration/夜伽字幕淡出时长",
            0.5f,
            new ConfigDescription("Yotogi Subtitle Fade Out Duration in seconds/夜伽字幕淡出时长（秒）", null,
                new ConfigurationManagerAttributes { Order = 5140 }));

        EnableYotogiSubtitleOutline = Config.Bind("5YotogiSubtitle",
            "EnableYotogiSubtitleOutline/启用夜伽字幕描边",
            true,
            new ConfigDescription("Enable Yotogi Subtitle Outline/启用夜伽字幕描边", null,
                new ConfigurationManagerAttributes { Order = 5150 }));

        YotogiSubtitleOutlineColor = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleOutlineColor/夜伽字幕描边颜色",
            "#000000",
            new ConfigDescription("Yotogi Subtitle Outline Color, use hex color code/夜伽字幕描边颜色，使用十六进制颜色代码", null,
                new ConfigurationManagerAttributes { Order = 5160 }));

        YotogiSubtitleOutlineOpacity = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleOutlineOpacity/夜伽字幕描边不透明度",
            0.5f,
            new ConfigDescription("Yotogi Subtitle Outline Opacity (0-1)/夜伽字幕描边不透明度（0-1）", null,
                new ConfigurationManagerAttributes { Order = 5170 }));

        YotogiSubtitleOutlineWidth = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleOutlineWidth/夜伽字幕描边宽度",
            1f,
            new ConfigDescription("Yotogi Subtitle Outline Width in pixels/夜伽字幕描边宽度（像素）", null,
                new ConfigurationManagerAttributes { Order = 5180 }));

        # endregion

        # region AdvSubtitleSettings

        // ADV字幕相关配置
        EnableAdvSubtitleSpeakerName = Config.Bind("6AdvSubtitle",
            "EnableAdvSubtitleSpeakerName/启用ADV字幕显示说话人名",
            true,
            new ConfigDescription("Enable ADV Subtitle Speaker Name/启用ADV字幕显示说话人名", null,
                new ConfigurationManagerAttributes { Order = 6000 }));

        AdvSubtitleFont = Config.Bind("6AdvSubtitle",
            "AdvSubtitleFont/ADV字幕字体",
            "Arial",
            new ConfigDescription(
                "ADV Subtitle Font, need to already installed the font on the system/ADV字幕字体，需要已经安装在系统中的字体", null,
                new ConfigurationManagerAttributes { Order = 6010 }));

        AdvSubtitleFontSize = Config.Bind("6AdvSubtitle",
            "AdvSubtitleFontSize/ADV字幕字体大小",
            24,
            new ConfigDescription("ADV Subtitle Font Size/ADV字幕字体大小", null,
                new ConfigurationManagerAttributes { Order = 6020 }));

        AdvSubtitleTextAlignment = Config.Bind("6AdvSubtitle",
            "AdvSubtitleTextAlignment/ADV字幕文本对齐方式",
            TextAnchorEnum.MiddleCenter,
            new ConfigDescription("ADV Subtitle Text Alignment/ADV字幕文本对齐方式", null,
                new ConfigurationManagerAttributes { Order = 6030 }));

        AdvSubtitleColor = Config.Bind("6AdvSubtitle",
            "AdvSubtitleColor/ADV字幕颜色",
            "#FFFFFF",
            new ConfigDescription("ADV Subtitle Color, use hex color code/ADV字幕颜色，使用十六进制颜色代码", null,
                new ConfigurationManagerAttributes { Order = 6040 }));

        AdvSubtitleOpacity = Config.Bind("6AdvSubtitle",
            "AdvSubtitleOpacity/ADV字幕不透明度",
            1f,
            new ConfigDescription("ADV Subtitle Opacity (0-1)/ADV字幕不透明度（0-1）", null,
                new ConfigurationManagerAttributes { Order = 6050 }));

        AdvSubtitleBackgroundColor = Config.Bind("6AdvSubtitle",
            "AdvSubtitleBackgroundColor/ADV字幕背景颜色",
            "#000000",
            new ConfigDescription("ADV Subtitle Background Color, use hex color code/ADV字幕背景颜色，使用十六进制颜色代码", null,
                new ConfigurationManagerAttributes { Order = 6060 }));

        AdvSubtitleBackgroundOpacity = Config.Bind("6AdvSubtitle",
            "AdvSubtitleBackgroundOpacity/ADV字幕背景不透明度",
            0.1f,
            new ConfigDescription("ADV Subtitle Background Opacity (0-1)/ADV字幕背景不透明度（0-1）", null,
                new ConfigurationManagerAttributes { Order = 6070 }));

        AdvSubtitleWidth = Config.Bind("6AdvSubtitle",
            "AdvSubtitleWidth/ADV字幕背景宽度",
            1920f,
            new ConfigDescription(
                "Subtitle Width(pixel), reference resolution is 1920x1080, so set to 1920 will fill the whole screen width/字幕宽度(像素)，参考分辨率为1920x1080，因此设置为1920时将填满整个屏幕宽",
                null, new ConfigurationManagerAttributes { Order = 6080 }));

        AdvSubtitleHeight = Config.Bind("6AdvSubtitle",
            "AdvSubtitleHeight/ADV字幕背景高度",
            30f,
            new ConfigDescription(
                "Subtitle Height(pixel), reference resolution is 1920x1080, so set to 1080 will fill the whole screen height/字幕高度(像素)，参考分辨率为1920x1080，因此设置为1080时将填满整个屏幕高",
                null, new ConfigurationManagerAttributes { Order = 6090 }));

        AdvSubtitleHorizontalPosition = Config.Bind("6AdvSubtitle",
            "AdvSubtitleHorizontalPosition/ADV字幕水平位置",
            0f,
            new ConfigDescription(
                "Distance to the left side of the screen (0 is the left, 1920 is the right)/到屏幕左边的距离（0为左边，1920为最右边）",
                null, new ConfigurationManagerAttributes { Order = 6100 }));

        AdvSubtitleVerticalPosition = Config.Bind("6AdvSubtitle",
            "AdvSubtitleVerticalPosition/ADV字幕垂直位置",
            1050f,
            new ConfigDescription(
                "Distance to bottom of screen (0 is bottom, 1 is top, note 1080 will go out of screen, should subtract background height)/到屏幕底部的距离（0为底部，1080为顶部，注意1080会超出屏幕，应减去背景高度）",
                null, new ConfigurationManagerAttributes { Order = 6110 }));

        EnableAdvSubtitleAnimation = Config.Bind("6AdvSubtitle",
            "AdvSubtitleAnimation/ADV字幕动画",
            true,
            new ConfigDescription("Enable ADV Subtitle Animation/启用ADV字幕动画", null,
                new ConfigurationManagerAttributes { Order = 6120 }));

        AdvSubtitleFadeInDuration = Config.Bind("6AdvSubtitle",
            "AdvSubtitleFadeInDuration/ADV字幕淡入时长",
            0.5f,
            new ConfigDescription("ADV Subtitle Fade In Duration in seconds/ADV字幕淡入时长（秒）", null,
                new ConfigurationManagerAttributes { Order = 6130 }));

        AdvSubtitleFadeOutDuration = Config.Bind("6AdvSubtitle",
            "AdvSubtitleFadeOutDuration/ADV字幕淡出时长",
            0.5f,
            new ConfigDescription("ADV Subtitle Fade Out Duration in seconds/ADV字幕淡出时长（秒）", null,
                new ConfigurationManagerAttributes { Order = 6140 }));

        EnableAdvSubtitleOutline = Config.Bind("6AdvSubtitle",
            "EnableAdvSubtitleOutline/启用ADV字幕描边",
            true,
            new ConfigDescription("Enable ADV Subtitle Outline/启用ADV字幕描边", null,
                new ConfigurationManagerAttributes { Order = 6150 }));

        AdvSubtitleOutlineColor = Config.Bind("6AdvSubtitle",
            "AdvSubtitleOutlineColor/ADV字幕描边颜色",
            "#000000",
            new ConfigDescription("ADV Subtitle Outline Color, use hex color code/ADV字幕描边颜色，使用十六进制颜色代码", null,
                new ConfigurationManagerAttributes { Order = 6160 }));

        AdvSubtitleOutlineOpacity = Config.Bind("6AdvSubtitle",
            "AdvSubtitleOutlineOpacity/ADV字幕描边不透明度",
            0.5f,
            new ConfigDescription("ADV Subtitle Outline Opacity (0-1)/ADV字幕描边不透明度（0-1）", null,
                new ConfigurationManagerAttributes { Order = 6170 }));


        AdvSubtitleOutlineWidth = Config.Bind("6AdvSubtitle",
            "AdvSubtitleOutlineWidth/ADV字幕描边宽度",
            1f,
            new ConfigDescription("ADV Subtitle Outline Width in pixels/ADV字幕描边宽度（像素）", null,
                new ConfigurationManagerAttributes { Order = 6180 }));

        # endregion

        # region LyricSubtitleSettings

        // 歌词字幕相关配置
        EnableLyricSubtitleSpeakerName = Config.Bind("7LyricSubtitle",
            "EnableLyricSubtitleSpeakerName/启用歌词字幕显示说话人名",
            false,
            new ConfigDescription(
                "Enable Lyric Subtitle Speaker Name. The song is played as a BGM, the speaker name is always displayed as the dance main maid/启用歌词字幕显示说话人名。歌曲作为BGM形式播放，人名始终显示为舞蹈主女仆",
                null, new ConfigurationManagerAttributes { Order = 7000 }));

        LyricSubtitleType = Config.Bind("7LyricSubtitle",
            "LyricSubtitleType/歌词字幕类型",
            LyricSubtitleTypeEnum.TranslationAndOriginal,
            new ConfigDescription("Lyric Subtitle Type/歌词字幕类型", null,
                new ConfigurationManagerAttributes { Order = 7010 }));

        LyricSubtitleFont = Config.Bind("7LyricSubtitle",
            "LyricSubtitleFont/歌词字幕字体",
            "Arial",
            new ConfigDescription(
                "Lyric Subtitle Font, need to already installed the font on the system/歌词字幕字体，需要已经安装在系统中的字体", null,
                new ConfigurationManagerAttributes { Order = 7020 }));

        LyricSubtitleFontSize = Config.Bind("7LyricSubtitle",
            "LyricSubtitleFontSize/歌词字幕字体大小",
            24,
            new ConfigDescription("Lyric Subtitle Font Size/歌词字幕字体大小", null,
                new ConfigurationManagerAttributes { Order = 7030 }));

        LyricSubtitleTextAlignment = Config.Bind("7LyricSubtitle",
            "LyricSubtitleTextAlignment/歌词字幕文本对齐",
            TextAnchorEnum.MiddleCenter,
            new ConfigDescription("Lyric Subtitle Text Alignment/歌词字幕文本对齐方式", null,
                new ConfigurationManagerAttributes { Order = 7040 }));

        LyricSubtitleColor = Config.Bind("7LyricSubtitle",
            "LyricSubtitleColor/歌词字幕颜色",
            "#FFFFFF",
            new ConfigDescription("Lyric Subtitle Color, use hex color code/歌词字幕颜色，使用十六进制颜色代码", null,
                new ConfigurationManagerAttributes { Order = 7050 }));

        LyricSubtitleOpacity = Config.Bind("7LyricSubtitle",
            "LyricSubtitleOpacity/歌词字幕不透明度",
            1f,
            new ConfigDescription("Lyric Subtitle Opacity (0-1)/歌词字幕不透明度（0-1）", null,
                new ConfigurationManagerAttributes { Order = 7060 }));

        LyricSubtitleBackgroundColor = Config.Bind("7LyricSubtitle",
            "LyricSubtitleBackgroundColor/歌词字幕背景颜色",
            "#000000",
            new ConfigDescription("Lyric Subtitle Background Color, use hex color code/歌词字幕背景颜色，使用十六进制颜色代码", null,
                new ConfigurationManagerAttributes { Order = 7070 }));

        LyricSubtitleBackgroundOpacity = Config.Bind("7LyricSubtitle",
            "LyricSubtitleBackgroundOpacity/歌词字幕背景不透明度",
            0f,
            new ConfigDescription("Lyric Subtitle Background Opacity (0-1)/歌词字幕背景不透明度（0-1）", null,
                new ConfigurationManagerAttributes { Order = 7080 }));

        LyricSubtitleWidth = Config.Bind("7LyricSubtitle",
            "LyricSubtitleWidth/歌词字幕背景宽度",
            1920f,
            new ConfigDescription(
                "Subtitle Width(pixel), reference resolution is 1920x1080, so set to 1920 will fill the whole screen width/字幕宽度(像素），参考分辨率为1920x1080，因此设置为1920时将填满整个屏幕宽",
                null, new ConfigurationManagerAttributes { Order = 7090 }));

        LyricSubtitleHeight = Config.Bind("7LyricSubtitle",
            "LyricSubtitleHeight/歌词字幕背景高度",
            70f,
            new ConfigDescription(
                "Subtitle Height(pixel), reference resolution is 1920x1080, so set to 1080 will fill the whole screen height/字幕高度(像素)，参考分辨率为1920x1080，因此设置为1080时将填满整个屏幕高",
                null, new ConfigurationManagerAttributes { Order = 7100 }));

        LyricSubtitleHorizontalPosition = Config.Bind("7LyricSubtitle",
            "LyricSubtitleHorizontalPosition/歌词字幕水平位置",
            0f,
            new ConfigDescription(
                "Distance to the left side of the screen (0 is the left, 1920 is the right)/到屏幕左边的距离（0为左边，1920为最右边）",
                null, new ConfigurationManagerAttributes { Order = 7110 }));

        LyricSubtitleVerticalPosition = Config.Bind("7LyricSubtitle",
            "LyricSubtitleVerticalPosition/歌词字幕垂直位置",
            200f,
            new ConfigDescription(
                "Distance to bottom of screen (0 is bottom, 1 is top, note 1080 will go out of screen, should subtract background height)/到屏幕底部的距离（0为底部，1080为顶部，注意1080会超出屏幕，应减去背景高度）",
                null, new ConfigurationManagerAttributes { Order = 7120 }));

        EnableLyricSubtitleAnimation = Config.Bind("7LyricSubtitle",
            "LyricSubtitleAnimation/歌词字幕动画",
            true,
            new ConfigDescription("Enable Lyric Subtitle Animation/启用歌词字幕动画", null,
                new ConfigurationManagerAttributes { Order = 7130 }));

        LyricSubtitleFadeInDuration = Config.Bind("7LyricSubtitle",
            "LyricSubtitleFadeInDuration/歌词字幕淡入时长",
            0.5f,
            new ConfigDescription("Lyric Subtitle Fade In Duration in seconds/歌词字幕淡入时长（秒）", null,
                new ConfigurationManagerAttributes { Order = 7140 }));

        LyricSubtitleFadeOutDuration = Config.Bind("7LyricSubtitle",
            "LyricSubtitleFadeOutDuration/歌词字幕淡出时长",
            0.5f,
            new ConfigDescription("Lyric Subtitle Fade Out Duration in seconds/歌词字幕淡出时长（秒）", null,
                new ConfigurationManagerAttributes { Order = 7150 }));

        EnableLyricSubtitleOutline = Config.Bind("7LyricSubtitle",
            "EnableLyricSubtitleOutline/启用歌词字幕描边",
            true,
            new ConfigDescription("Enable Lyric Subtitle Outline/启用歌词字幕描边", null,
                new ConfigurationManagerAttributes { Order = 7160 }));

        LyricSubtitleOutlineColor = Config.Bind("7LyricSubtitle",
            "LyricSubtitleOutlineColor/歌词字幕描边颜色",
            "#000000",
            new ConfigDescription("Lyric Subtitle Outline Color, use hex color code/歌词字幕描边颜色，使用十六进制颜色代码", null,
                new ConfigurationManagerAttributes { Order = 7170 }));

        LyricSubtitleOutlineOpacity = Config.Bind("7LyricSubtitle",
            "LyricSubtitleOutlineOpacity/歌词字幕描边不透明度",
            0.5f,
            new ConfigDescription("Lyric Subtitle Outline Opacity (0-1)/歌词字幕描边不透明度（0-1）", null,
                new ConfigurationManagerAttributes { Order = 7180 }));

        LyricSubtitleOutlineWidth = Config.Bind("7LyricSubtitle",
            "LyricSubtitleOutlineWidth/歌词字幕描边宽度",
            1f,
            new ConfigDescription("Lyric Subtitle Outline Width in pixels/歌词字幕描边宽度（像素）", null,
                new ConfigurationManagerAttributes { Order = 7190 }));

        # endregion

        # region VRSubtitleSettings

        //TODO 重做VR 字幕配置
        //TODO 是序号越大的排序越靠前

        // VR悬浮字幕相关配置
        VRSubtitleMode = Config.Bind("8VRSubtitle",
            "VRSubtitleMode/VR字幕模式",
            VRSubtitleModeEnum.InSpace,
            new ConfigDescription(
                "VR Subtitle Mode: InSpace=On Control tablet, OnTablet=Floating in world space following head movement/VR字幕模式：InSpace=字幕在控制平板上，OnTablet=跟随头部运动的世界空间悬浮字幕",
                null, new ConfigurationManagerAttributes { Order = 8000 }));

        VRSubtitleDistance = Config.Bind("8VRSubtitle",
            "VRSubtitleDistance/VR字幕距离",
            1f,
            new ConfigDescription("VR Floating Subtitle Distance in meters/VR悬浮字幕距离（米）", null,
                new ConfigurationManagerAttributes { Order = 8010 }));

        VRSubtitleVerticalOffset = Config.Bind("8VRSubtitle",
            "VRSubtitleVerticalOffset/VR字幕垂直偏移",
            25f,
            new ConfigDescription(
                "VR Floating Subtitle Vertical Offset in degrees (relative to center of view)/VR悬浮字幕垂直偏移（度，相对于视线中心）",
                null, new ConfigurationManagerAttributes { Order = 8020 }));

        VRSubtitleHorizontalOffset = Config.Bind("8VRSubtitle",
            "VRSubtitleHorizontalOffset/VR字幕水平偏移",
            0f,
            new ConfigDescription(
                "VR Floating Subtitle Horizontal Offset in degrees (relative to center of view)/VR悬浮字幕水平偏移（度，相对于视线中心）",
                null, new ConfigurationManagerAttributes { Order = 8030 }));

        VRInSpaceSubtitleWidth = Config.Bind("8VRSubtitle",
            "VRFloatingSubtitleWidth/VR悬浮字幕宽度",
            1.5f,
            new ConfigDescription("VR Floating Subtitle Width in meters/VR悬浮字幕宽度（米）", null,
                new ConfigurationManagerAttributes { Order = 8040 }));

        VRInSpaceSubtitleHeight = Config.Bind("8VRSubtitle",
            "VRFloatingSubtitleHeight/VR悬浮字幕高度",
            0.04f,
            new ConfigDescription("VR Floating Subtitle Height in meters/VR悬浮字幕高度（米）", null,
                new ConfigurationManagerAttributes { Order = 8050 }));

        VRSubtitleScale = Config.Bind("8VRSubtitle",
            "VRSubtitleScale/VR字幕整体缩放",
            0.8f,
            new ConfigDescription("VR Floating Subtitle Scale/VR悬浮字幕整体缩放比例", null,
                new ConfigurationManagerAttributes { Order = 8060 }));

        # endregion

        # region DumpSettings

        // Tip for people who using ConfigurationManager
        _dumpTip1 = Config.Bind("9Dump",
            "this section is for translators. If you are not planning to change translate file, please do not enable it, it will slow down the game",
            true,
            new ConfigDescription("this config do nothing", null, new ConfigurationManagerAttributes { Order = 9000 }));

        _dumpTip2 = Config.Bind("9Dump",
            "这个部分是为翻译者准备的，如果您不计划改动翻译文件，请不要启用它, 会减慢游戏速度",
            true,
            new ConfigDescription("这个配置不做任何事情", null, new ConfigurationManagerAttributes { Order = 9010 }));

        _dumpTip3 = Config.Bind("9Dump",
            "Turn log level to Debug to make it easier to debug and find problems",
            true,
            new ConfigDescription("this config do nothing", null, new ConfigurationManagerAttributes { Order = 9020 }));

        _dumpTip4 = Config.Bind("9Dump",
            "在通用设置内将日志等级调至 Debug 能让您更方便的调试，以及排查各类不生效原因",
            true,
            new ConfigDescription("这个配置不做任何事情", null, new ConfigurationManagerAttributes { Order = 9030 }));

        EnableTexturesDump = Config.Bind("9Dump",
            "EnableDumpTexture/是否启用纹理导出",
            false,
            new ConfigDescription("Only export textures that have not been replaced/仅导出未替换过的纹理", null,
                new ConfigurationManagerAttributes { Order = 9040 }));

        EnableSpriteDump = Config.Bind("9Dump",
            "EnableDumpSprite/是否启用精灵图导出",
            false,
            new ConfigDescription("Only export sprites that have not been replaced/仅导出未替换过的精灵图", null,
                new ConfigurationManagerAttributes { Order = 9050 }));

        EnableTextDump = Config.Bind("9Dump",
            "EnableDumpText/是否启用文本导出",
            false,
            new ConfigDescription(
                "Only export text that has not been replaced, write out when the threshold is reached or switching scenes or the game correct exits/仅导出未替换过的文本，达到阈值或切换场景或正确退出游戏时写出",
                null, new ConfigurationManagerAttributes { Order = 9060 }));

        TextDumpThreshold = Config.Bind("9Dump",
            "TextDumpThreshold/文本导出阈值",
            20,
            new ConfigDescription("How many lines of text to write out at once/累计多少条文本后写出一次", null,
                new ConfigurationManagerAttributes { Order = 9070 }));

        FlushTextDumpNow = Config.Bind("9Dump",
            "FlushTextDumpNow/立即写出文本",
            false,
            new ConfigDescription(
                "Immediately write out all cached text when the option status changes/立即写出所有已缓存的文本，选项状态变更时立即写出", null,
                new ConfigurationManagerAttributes { Order = 9080 }));

        # endregion

        LogManager.Debug($"IsVrMode: {IsVrMode}, CommandLine: {Environment.CommandLine}");

        // Create translation folder
        try
        {
            Directory.CreateDirectory(TranslationRootPath);
            Directory.CreateDirectory(TargetLanguePath);
            Directory.CreateDirectory(TranslationTextPath);
            Directory.CreateDirectory(TranslationTexturePath);
            Directory.CreateDirectory(LyricPath);
            Directory.CreateDirectory(UIPath);
            Directory.CreateDirectory(UITextPath);
            Directory.CreateDirectory(UISpritePath);
            Directory.CreateDirectory(DumpPath);
            Directory.CreateDirectory(TextDumpPath);
            Directory.CreateDirectory(TextureDumpPath);
            Directory.CreateDirectory(SpriteDumpPath);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"Create translation folder failed, plugin may not work/创建翻译文件夹失败，插件可能无法运行: {e.Message}");
        }

        // Init modules
        if (EnableTextTranslation.Value)
        {
            LogManager.Info("Text Translation Enabled/文本翻译已启用");
            TextTranslateManger.Init();
        }
        else
        {
            LogManager.Info("Text Translation Disabled/翻译已禁用");
        }

        if (EnableUITranslation.Value)
        {
            LogManager.Info("UI Translation Enabled/UI 翻译已启用");
            UITranslateManager.Init();
        }
        else
        {
            LogManager.Info("UI Translation Disabled/UI 翻译已禁用");
        }

        if (EnableTextureReplace.Value)
        {
            LogManager.Info("Texture Replace Enabled/贴图替换已启用");
            TextureReplaceManger.Init();
        }
        else
        {
            LogManager.Info("Texture Replace Disabled/贴图替换已禁用");
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

        if (EnableYotogiSubtitle.Value)
        {
            LogManager.Info("Yotogi Subtitle Enabled/夜伽字幕已启用");
            SubtitleManager.Init();
        }
        else
        {
            LogManager.Info("Yotogi Subtitle Disabled/夜伽字幕已禁用");
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

        if (EnableLyricSubtitle.Value)
        {
            LogManager.Info("Lyric Subtitle Enabled/歌词字幕已启用");
            LyricManger.Init();
        }
        else
        {
            LogManager.Info("Lyric Subtitle Disabled/歌词字幕已禁用");
        }


        // 注册通用变更事件
        RegisterGeneralConfigEvents();
        // 注册字幕启用状态变更事件
        RegisterEnableSubtitleEvents();
        // 注册基础字幕配置变更事件
        RegisterBaseSubtitleConfigEvents();
        // 注册夜伽字幕配置变更事件
        RegisterYotogiSubtitleConfigEvents();
        // 注册ADV字幕配置变更事件
        RegisterAdvSubtitleConfigEvents();
        // 注册歌词字幕配置变更事件
        RegisterLyricSubtitleConfigEvents();
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
        TextTranslateManger.Unload();
        UITranslateManager.Unload();
        TextureReplaceManger.Unload();
        SubtitleManager.Unload();
        LyricManger.Unload();
    }

    # region ConfigEvents

    /// <summary>
    ///     注册通用变更事件
    /// </summary>
    private void RegisterGeneralConfigEvents()
    {
        // 注册目标语言变更事件
        TargetLanguage.SettingChanged += (sender, args) =>
        {
            LogManager.Info($"Target language changed to {TargetLanguage.Value}/目标语言已更改为 {TargetLanguage.Value}");

            // 更新翻译路径
            TargetLanguePath = Path.Combine(TranslationRootPath, TargetLanguage.Value);
            TranslationTextPath = Path.Combine(TargetLanguePath, "Text");
            TranslationTexturePath = Path.Combine(TargetLanguePath, "Texture");
            LyricPath = Path.Combine(TargetLanguePath, "Lyric");
            UIPath = Path.Combine(TargetLanguePath, "UI");
            UITextPath = Path.Combine(UIPath, "Text");
            UISpritePath = Path.Combine(UIPath, "Sprite");
            DumpPath = Path.Combine(TranslationRootPath, "Dump");
            TextDumpPath = Path.Combine(DumpPath, "Text");
            TextureDumpPath = Path.Combine(DumpPath, "Texture");
            SpriteDumpPath = Path.Combine(DumpPath, "Sprite");
            // 创建目录
            try
            {
                Directory.CreateDirectory(TargetLanguePath);
                Directory.CreateDirectory(TranslationTextPath);
                Directory.CreateDirectory(TranslationTexturePath);
                Directory.CreateDirectory(LyricPath);
                Directory.CreateDirectory(UIPath);
                Directory.CreateDirectory(UITextPath);
                Directory.CreateDirectory(UISpritePath);
                Directory.CreateDirectory(DumpPath);
                Directory.CreateDirectory(TextDumpPath);
                Directory.CreateDirectory(TextureDumpPath);
                Directory.CreateDirectory(SpriteDumpPath);
            }
            catch (Exception e)
            {
                LogManager.Error($"Create translation folder failed/创建翻译文件夹失败: {e.Message}");
            }

            // 重新加载翻译
            if (EnableTextTranslation.Value)
            {
                TextTranslateManger.Unload();
                TextTranslateManger.Init();
            }

            // 重新加载贴图
            if (EnableTextureReplace.Value)
            {
                TextureReplaceManger.Unload();
                TextureReplaceManger.Init();
            }

            // 重新加载UI翻译
            if (EnableUITranslation.Value)
            {
                UITranslateManager.Unload();
                UITranslateManager.Init();
            }

            // 重新加载歌词翻译
            if (EnableLyricSubtitle.Value)
            {
                LyricManger.Unload();
                LyricManger.Init();
            }
        };

        // 注册文本翻译启用状态变更事件
        EnableTextTranslation.SettingChanged += (sender, args) =>
        {
            if (EnableTextTranslation.Value)
            {
                LogManager.Info("Text Translation Enabled/文本翻译已启用");
                TextTranslateManger.Init();
                XUATInterop.Initialize();
            }
            else
            {
                LogManager.Info("Text Translation Disabled/文本翻译已禁用");
                TextTranslateManger.Unload();
            }
        };

        // 注册UI翻译启用状态变更事件
        EnableUITranslation.SettingChanged += (sender, args) =>
        {
            if (EnableUITranslation.Value)
            {
                LogManager.Info("UI Translation Enabled/UI 翻译已启用");
                UITranslateManager.Init();
            }
            else
            {
                LogManager.Info("UI Translation Disabled/UI 翻译已禁用");
                UITranslateManager.Unload();
            }
        };

        // 注册贴图替换启用状态变更事件
        EnableTextureReplace.SettingChanged += (sender, args) =>
        {
            if (EnableTextureReplace.Value)
            {
                LogManager.Info("Texture Replace Enabled/贴图替换已启用");
                TextureReplaceManger.Init();
            }
            else
            {
                LogManager.Info("Texture Replace Disabled/贴图替换已禁用");
                TextureReplaceManger.Unload();
            }
        };

        MaidNameStyle.SettingChanged += (sender, args) =>
        {
            LogManager.Info("Not Support change maid name style during runtime/不支持在运行时更改角色名字样式");
        };

        // 注册日志级别变更事件
        LogLevelConfig.SettingChanged += (sender, args) =>
        {
            // 不需要重新加载
            LogManager.Info($"Log level changed to {LogLevelConfig.Value}/日志级别已更改为 {LogLevelConfig.Value}");
            if (LogLevelConfig.Value >= LogLevel.Debug)
                LogManager.Info("some debug patches require a game restart/部分 debug 补丁需要重启游戏生效");
        };

        // 注册贴图缓存大小变更事件
        TextureCacheSize.SettingChanged += (sender, args) =>
        {
            LogManager.Info(
                $"Texture replace cache size changed to {TextureCacheSize.Value}/贴图替换缓存大小已更改为 {TextureCacheSize.Value}");

            // 重新加载贴图替换模块以应用新的缓存大小
            if (EnableTextureReplace.Value)
            {
                TextureReplaceManger.Unload();
                TextureReplaceManger.Init();
            }
        };

        // 注册UI精灵图缓存大小变更事件
        UICacheSize.SettingChanged += (sender, args) =>
        {
            LogManager.Info($"UI Sprite cache size changed to {UICacheSize.Value}/UI精灵图缓存大小已更改为 {UICacheSize.Value}");

            // 重新加载贴图替换模块以应用新的缓存大小
            if (EnableUITranslation.Value)
            {
                UITranslateManager.Unload();
                UITranslateManager.Init();
            }
        };
    }

    /// <summary>
    ///     注册字幕启用状态变更事件
    /// </summary>
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
                LogManager.Info("Force ADV Subtitle Enabled/强制ADV字幕已启用");
                SubtitleManager.Unload();
                SubtitleManager.Init();
            }
            else
            {
                LogManager.Info("Force ADV Subtitle Disabled/强制ADV字幕已禁用");
                SubtitleManager.Unload();
            }
        };

        EnableLyricSubtitle.SettingChanged += (sender, args) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle Enabled/歌词字幕已启用");
                LyricManger.Unload();
                LyricManger.Init();
            }
            else
            {
                LogManager.Info("Lyric Subtitle Disabled/歌词字幕已禁用");
                LyricManger.Unload();
            }
        };
    }

    /// <summary>
    ///     注册基础字幕配置变更事件
    /// </summary>
    private void RegisterBaseSubtitleConfigEvents()
    {
        // 注册字幕说话人名启用状态变更事件
        EnableBaseSubtitleSpeakerName.SettingChanged += (sender, args) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle Speaker Name Enabled/启用基础字幕显示说话人名");
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

        // 注册字幕文本对齐方式变更事件
        BaseSubtitleTextAlignment.SettingChanged += (sender, args) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle text alignment changed/基础字幕文本对齐方式已更改");
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
                LogManager.Info("Base Subtitle vertical position changed/基础字幕垂直位置已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕背景宽度变更事件
        BaseSubtitleWidth.SettingChanged += (sender, args) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle background width changed/基础字幕背景宽度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕高度变更事件
        BaseSubtitleHeight.SettingChanged += (sender, args) =>
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

        // 注册字幕描边启用变更事件
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
    ///     注册夜伽字幕配置变更事件
    /// </summary>
    private void RegisterYotogiSubtitleConfigEvents()
    {
        // 注册字幕说话人名启用状态变更事件
        EnableYotogiSubtitleSpeakerName.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle Speaker Name Enabled/启用夜伽字幕显示说话人名");
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

        // 注册字幕文本对齐方式变更事件
        YotogiSubtitleTextAlignment.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle text alignment changed/夜伽字幕文本对齐方式已更改");
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
                LogManager.Info("Yotogi Subtitle vertical position changed/夜伽字幕垂直位置已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕背景宽度变更事件
        YotogiSubtitleWidth.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle background width changed/夜伽字幕背景宽度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕高度变更事件
        YotogiSubtitleHeight.SettingChanged += (sender, args) =>
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

        // 注册字幕描边启用变更事件
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
    ///     注册Adv字幕配置变更事件
    /// </summary>
    private void RegisterAdvSubtitleConfigEvents()
    {
        // 注册字幕说话人名启用状态变更事件
        EnableAdvSubtitleSpeakerName.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle Speaker Name Enabled/启用Adv字幕显示说话人名");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕字体变更事件
        AdvSubtitleFont.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle font changed/Adv字幕字体已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕字体大小变更事件
        AdvSubtitleFontSize.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle font size changed/Adv字幕字体大小已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕文本对齐方式变更事件
        AdvSubtitleTextAlignment.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle text alignment changed/Adv字幕文本对齐方式已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕颜色变更事件
        AdvSubtitleColor.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle color changed/Adv字幕颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕透明度变更事件
        AdvSubtitleOpacity.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle opacity changed/Adv字幕透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕背景颜色变更事件
        AdvSubtitleBackgroundColor.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle background color changed/Adv字幕背景颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕背景不透明度变更事件
        AdvSubtitleBackgroundOpacity.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle background opacity changed/Adv字幕背景不透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕位置变更事件
        AdvSubtitleVerticalPosition.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle vertical position changed/Adv字幕垂直位置已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕背景宽度变更事件
        AdvSubtitleWidth.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle background width changed/Adv字幕背景宽度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕高度变更事件
        AdvSubtitleHeight.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle height changed/Adv字幕高度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕动画变更事件
        EnableAdvSubtitleAnimation.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle animation changed/Adv字幕动画已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕淡入时长变更事件
        AdvSubtitleFadeInDuration.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle fade in duration changed/Adv字幕淡入时长已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕淡出时长变更事件
        AdvSubtitleFadeOutDuration.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle fade out duration changed/Adv字幕淡出时长已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边启用变更事件
        EnableAdvSubtitleOutline.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle outline changed/Adv字幕描边已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边颜色变更事件
        AdvSubtitleOutlineColor.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle outline color changed/Adv字幕描边颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边不透明度变更事件
        AdvSubtitleOutlineOpacity.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle outline opacity changed/Adv字幕描边不透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边宽度变更事件
        AdvSubtitleOutlineWidth.SettingChanged += (sender, args) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle outline width changed/Adv字幕描边宽度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };
    }

    /// <summary>
    ///     注册歌词字幕配置变更事件
    /// </summary>
    private void RegisterLyricSubtitleConfigEvents()
    {
        // 注册字幕说话人名启用状态变更事件
        EnableLyricSubtitleSpeakerName.SettingChanged += (sender, args) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle Speaker Name Enabled/启用歌词字幕显示说话人名");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕类型变更事件
        LyricSubtitleType.SettingChanged += (sender, args) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle type changed/歌词字幕类型已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕字体变更事件
        LyricSubtitleFont.SettingChanged += (sender, args) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle font changed/歌词字幕字体已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕字体大小变更事件
        LyricSubtitleFontSize.SettingChanged += (sender, args) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle font size changed/歌词字幕字体大小已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕文本对齐方式变更事件
        LyricSubtitleTextAlignment.SettingChanged += (sender, args) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle text alignment changed/歌词字幕文本对齐方式已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕颜色变更事件
        LyricSubtitleColor.SettingChanged += (sender, args) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle color changed/歌词字幕颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕透明度变更事件
        LyricSubtitleOpacity.SettingChanged += (sender, args) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle opacity changed/歌词字幕透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕背景颜色变更事件
        LyricSubtitleBackgroundColor.SettingChanged += (sender, args) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle background color changed/歌词字幕背景颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕背景不透明度变更事件
        LyricSubtitleBackgroundOpacity.SettingChanged += (sender, args) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle background opacity changed/歌词字幕背景不透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕位置变更事件
        LyricSubtitleVerticalPosition.SettingChanged += (sender, args) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle vertical position changed/歌词字幕垂直位置已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕背景宽度变更事件
        LyricSubtitleWidth.SettingChanged += (sender, args) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle background width changed/歌词字幕背景宽度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕高度变更事件
        LyricSubtitleHeight.SettingChanged += (sender, args) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle height changed/歌词字幕高度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕动画变更事件
        EnableLyricSubtitleAnimation.SettingChanged += (sender, args) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle animation changed/歌词字幕动画已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕淡入时长变更事件
        LyricSubtitleFadeInDuration.SettingChanged += (sender, args) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle fade in duration changed/歌词字幕淡入时长已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕淡出时长变更事件
        LyricSubtitleFadeOutDuration.SettingChanged += (sender, args) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle fade out duration changed/歌词字幕淡出时长已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边启用变更事件
        EnableLyricSubtitleOutline.SettingChanged += (sender, args) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle outline changed/歌词字幕描边已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边颜色变更事件
        LyricSubtitleOutlineColor.SettingChanged += (sender, args) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle outline color changed/歌词字幕描边颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边不透明度变更事件
        LyricSubtitleOutlineOpacity.SettingChanged += (sender, args) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle outline opacity changed/歌词字幕描边不透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边宽度变更事件
        LyricSubtitleOutlineWidth.SettingChanged += (sender, args) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle outline width changed/歌词字幕描边宽度已更改");
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
                LogManager.Info("VR Subtitle mode changed/VR字幕模式已更改");
                SubtitleComponentManager.DestroyAllSubtitleComponents();
            }
        };

        // 注册VR字幕距离变更事件
        VRSubtitleDistance.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("VR Floating Subtitle distance changed/VR悬浮字幕距离已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册VR字幕垂直偏移变更事件
        VRSubtitleVerticalOffset.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("VR Floating Subtitle vertical offset changed/VR悬浮字幕垂直偏移已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册VR字幕水平偏移变更事件
        VRSubtitleHorizontalOffset.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("VR Floating Subtitle horizontal offset changed/VR悬浮字幕水平偏移已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册VR悬浮字幕宽度变更事件
        VRInSpaceSubtitleWidth.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("VR Floating Subtitle Width changed/VR悬浮字幕宽度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册VR悬浮字幕高度变更事件
        VRInSpaceSubtitleHeight.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("VR Floating Subtitle Height changed/VR悬浮字幕高度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册VR字幕缩放变更事件
        VRSubtitleScale.SettingChanged += (sender, args) =>
        {
            LogManager.Info("VR Subtitle scale changed/VR字幕缩放已更改");
            SubtitleComponentManager.UpdateAllSubtitleConfig();
        };
    }


    /// <summary>
    ///     注册Dump配置变更事件
    /// </summary>
    private void RegisterDumpConfigEvents()
    {
        // 注册Dump文本启用状态变更事件
        EnableTextDump.SettingChanged += (sender, args) =>
        {
            if (EnableTextDump.Value)
                LogManager.Info("Text dump enabled/启用文本导出");
            else
                LogManager.Info("Text dump disabled/禁用文本导出");
        };

        // 注册Dump纹理启用状态变更事件
        EnableTexturesDump.SettingChanged += (sender, args) =>
        {
            if (EnableTexturesDump.Value)
                LogManager.Info("Texture dump enabled/启用纹理导出");
            else
                LogManager.Info("Texture dump disabled/禁用纹理导出");
        };

        // 注册Dump文本阈值变更事件
        TextDumpThreshold.SettingChanged += (sender, args) =>
        {
            LogManager.Info("Text dump threshold changed/文本导出阈值已更改");
        };


        // 注册Dump文本立即写出变更事件
        FlushTextDumpNow.SettingChanged += (sender, args) =>
        {
            TextTranslateManger.FlushDumpBuffer();
            LogManager.Info("Text dump flushed/文本导出缓存已写出");
        };
    }

    # endregion
}