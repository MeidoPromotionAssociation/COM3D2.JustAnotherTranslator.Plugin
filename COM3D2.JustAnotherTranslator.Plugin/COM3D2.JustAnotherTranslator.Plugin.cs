using System;
using System.ComponentModel;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using COM3D2.JustAnotherTranslator.Plugin.Manger;
using COM3D2.JustAnotherTranslator.Plugin.Subtitle;
using COM3D2.JustAnotherTranslator.Plugin.Utils;

namespace COM3D2.JustAnotherTranslator.Plugin;

[BepInPlugin("Github.MeidoPromotionAssociation.COM3D2.JustAnotherTranslator.Plugin",
    "COM3D2.JustAnotherTranslator.Plugin", "0.2.1")]
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

    public enum SubtitleSearchDirectionEnum
    {
        [Description("DownFirst/先向下搜索")] DownFirst,
        [Description("UpFirst/先向上搜索")] UpFirst
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
        [Description("Space/空间字幕")] Space,
        [Description("Tablet/平板字幕")] Tablet
    }

    public static bool IsVrMode;

    // 提示信息
    private static ConfigEntry<bool> _tip1;
    private static ConfigEntry<bool> _tip2;
    private static ConfigEntry<bool> _tip3;
    private static ConfigEntry<bool> _tip4;

    // 通用配置
    public static ConfigEntry<string> TargetLanguage;
    public static ConfigEntry<bool> EnableGeneralTextTranslation;
    public static ConfigEntry<bool> EnableUITextTranslation;
    public static ConfigEntry<bool> EnableUITextExtraPatch;
    public static ConfigEntry<bool> EnableUISpriteReplace;
    public static ConfigEntry<bool> EnableTextureReplace;
    public static ConfigEntry<MaidNameStyleEnum> MaidNameStyle;
    public static ConfigEntry<LogLevel> LogLevelConfig;
    public static ConfigEntry<bool> AllowFilesInZipLoadInOrder;
    public static ConfigEntry<KeyboardShortcut> ReloadTranslateResourceShortcut;

    // 字幕启用相关配置
    public static ConfigEntry<bool> EnableBaseSubtitle;
    public static ConfigEntry<bool> EnableYotogiSubtitle;
    public static ConfigEntry<bool> EnableAdvSubtitle;
    public static ConfigEntry<bool> ForceEnableAdvSubtitle;
    public static ConfigEntry<bool> EnableLyricSubtitle;
    public static ConfigEntry<SubtitleSearchDirectionEnum> SubtitleSearchDirection;

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
    public static ConfigEntry<float> VRSpaceSubtitleWidth;
    public static ConfigEntry<float> VRSpaceSubtitleHeight;
    public static ConfigEntry<float> VRSpaceSubtitleDistance;
    public static ConfigEntry<float> VRSpaceSubtitleVerticalOffset;
    public static ConfigEntry<float> VRSpaceSubtitleHorizontalOffset;
    public static ConfigEntry<float> VRSpaceSubtitleTextSizeMultiplier;
    public static ConfigEntry<float> VRSpaceSubtitleOutlineScaleFactor;
    public static ConfigEntry<bool> VRSpaceSubtitlePixelPerfect;
    public static ConfigEntry<float> VRSpaceSubtitleFollowSmoothness;
    public static ConfigEntry<float> VRTabletSubtitleWidth;
    public static ConfigEntry<float> VRTabletSubtitleHeight;
    public static ConfigEntry<float> VRTabletSubtitleVerticalPosition;
    public static ConfigEntry<float> VRTabletSubtitleHorizontalPosition;
    public static ConfigEntry<float> VRTabletSubtitleTextSizeMultiplier;
    public static ConfigEntry<float> VRTabletSubtitleOutlineScaleFactor;
    public static ConfigEntry<bool> VRTabletSubtitlePixelPerfect;

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
    public static ConfigEntry<bool> EnableDumpDanceInfo;
    public static ConfigEntry<bool> EnableTermDump;
    public static ConfigEntry<int> TermDumpThreshold;
    public static ConfigEntry<bool> FlushTermDumpNow;
    public static ConfigEntry<bool> PrintOSFont;

    // patch相关配置
    private static ConfigEntry<bool> _fixerTip1;
    private static ConfigEntry<bool> _fixerTip2;
    public static ConfigEntry<bool> EnableMaidCafeDlcLineBreakCommentFix;
    public static ConfigEntry<bool> EnableUIFontReplace;
    public static ConfigEntry<string> UIFont;
    public static ConfigEntry<bool> EnableNoneCjkFix;

    // Translation folder path
    public static readonly string TranslationRootPath =
        Path.Combine(Paths.BepInExRootPath, "JustAnotherTranslator");

    public static string TargetLanguePath;
    public static string TranslationTextPath;
    public static string TextureReplacePath;
    public static string LyricPath;
    public static string UIPath;
    public static string DumpUIPath;
    public static string UITextPath;
    public static string UISpritePath;
    public static string DumpPath;
    public static string TextDumpPath;
    public static string TextureDumpPath;
    public static string SpriteDumpPath;
    public static string TermDumpPath;
    public static string SubtitleColorsConfigPath;

    private void Awake()
    {
        Logger.LogInfo(
            $"{Info.Metadata.Name} {Info.Metadata.Version} is loading/{Info.Metadata.Name} {Info.Metadata.Version} 正在载入");
        Logger.LogInfo(
            "Get update or report bug/获取更新或报告bug: https://github.com/MeidoPromotionAssociation/COM3D2.JustAnotherTranslator.Plugin");
        Logger.LogInfo(
            "This plugin does not provide translation data, you need to get it somewhere else/本插件不提供翻译数据，您需要自行获取");

        // Init our LogManager with the BepInEx logger
        LogManager.Init(Logger);

        IsVrMode = Environment.CommandLine.ToLower().Contains("/vr");

        # region Tips

        // Tips for people who using ConfigurationManager
        _tip1 = Config.Bind("1Tips",
            "Configuration options tips do not prompt in the game, please open the configuration file to view",
            true,
            new ConfigDescription("this config do nothing", null,
                new ConfigurationManagerAttributes { Order = 990 }));

        _tip2 = Config.Bind("1Tips",
            "configuration file location is COM3D2/BepInEx/config/Github.MeidoPromotionAssociation.COM3D2.JustAnotherTranslator.Plugin.cfg",
            true,
            new ConfigDescription("this config do nothing", null,
                new ConfigurationManagerAttributes { Order = 980 }));

        _tip3 = Config.Bind("1Tips",
            "配置选项提示不会在游戏内提示，请打开配置文件查看", true,
            new ConfigDescription("这个配置不做任何事情", null,
                new ConfigurationManagerAttributes { Order = 970 }));

        _tip4 = Config.Bind("1Tips",
            "配置文件位于 /COM3D2/BepInEx/config/Github.MeidoPromotionAssociation.COM3D2.JustAnotherTranslator.Plugin.cfg",
            true,
            new ConfigDescription("这个配置不做任何事情", null,
                new ConfigurationManagerAttributes { Order = 960 }));

        # endregion

        # region GeneralSettings

        TargetLanguage = Config.Bind("2General",
            "TargetLanguage/目标语言",
            "zh-CN",
            new ConfigDescription(
                "Target Language, only affect the path of reading translation files/目标语言，只控制读取翻译文件的路径",
                null,
                new ConfigurationManagerAttributes { Order = 1990 }));

        TargetLanguePath = Path.Combine(TranslationRootPath, TargetLanguage.Value);
        TranslationTextPath = Path.Combine(TargetLanguePath, "Text");
        TextureReplacePath = Path.Combine(TargetLanguePath, "Texture");
        LyricPath = Path.Combine(TargetLanguePath, "Lyric");
        UIPath = Path.Combine(TargetLanguePath, "UI");
        UITextPath = Path.Combine(UIPath, "Text");
        UISpritePath = Path.Combine(UIPath, "Sprite");
        DumpPath = Path.Combine(TranslationRootPath, "Dump");
        DumpUIPath = Path.Combine(DumpPath, "UI");
        TextDumpPath = Path.Combine(DumpPath, "Text");
        TextureDumpPath = Path.Combine(DumpPath, "Texture");
        SpriteDumpPath = Path.Combine(DumpUIPath, "Sprite");
        TermDumpPath = Path.Combine(DumpUIPath, "Text");
        SubtitleColorsConfigPath = Path.Combine(TranslationRootPath, "SubtitleColors.json");

        EnableGeneralTextTranslation = Config.Bind("2General",
            "EnableGeneralTextTranslation/启用通用文本翻译",
            true,
            new ConfigDescription("Enable Text Translation/启用文本翻译", null,
                new ConfigurationManagerAttributes { Order = 1980 }));

        EnableTextureReplace = Config.Bind("2General",
            "EnableTextureReplace/启用纹理替换",
            true,
            new ConfigDescription("Enable Texture Replace/启用纹理替换", null,
                new ConfigurationManagerAttributes { Order = 1970 }));

        EnableUITextTranslation = Config.Bind("2General",
            "EnableUITextTranslation/启用UI文本翻译",
            true,
            new ConfigDescription("Enable UI Translation/启用 UI 文本翻译", null,
                new ConfigurationManagerAttributes { Order = 1960 }));

        EnableUISpriteReplace = Config.Bind("2General",
            "EnableUISpriteReplace/启用UI精灵图替换",
            true,
            new ConfigDescription("Enable UI Translation/启用 UI 精灵图替换", null,
                new ConfigurationManagerAttributes { Order = 1950 }));

        EnableUITextExtraPatch = Config.Bind("2General",
            "EnableUITextExtraPatch/启用UI文本翻译额外补丁",
            true,
            new ConfigDescription(
                "Enable patches that cover UI translations not translated by the UI translation module depending on whether the game is a multilingual version/启用用于覆盖因游戏是否为多语言版本而未被 UI 翻译模块翻译的补丁",
                null,
                new ConfigurationManagerAttributes { Order = 1940 }));

        AllowFilesInZipLoadInOrder = Config.Bind("2General",
            "AllowFilesInZipLoadInOrder/允许 ZIP 文件内文件按顺序加载",
            false,
            new ConfigDescription(
                "Allow files In zip Load in order, This will lower the loading speed/允许 ZIP 文件内文件按顺序加载，这会降低加载速度",
                null,
                new ConfigurationManagerAttributes { Order = 1940 }));

        MaidNameStyle = Config.Bind("2General",
            "MaidNameStyle/女仆名字样式",
            MaidNameStyleEnum.JpStyle,
            new ConfigDescription(
                "Maid Name Style, JpStyle is family name first and given name last, English style is opposite, cannot change at runtime/女仆名字样式，日式姓前名后，英式相反。无法在运行时更改",
                null, new ConfigurationManagerAttributes { Order = 1930 }));

        ReloadTranslateResourceShortcut = Config.Bind("2General",
            "ReloadTranslateResource/重载翻译资源",
            new KeyboardShortcut(),
            new ConfigDescription(
                "Press this shortcut to hot reload all translation resources/按下此快捷键来热重载所有翻译资源",
                null, new ConfigurationManagerAttributes { Order = 1920 }));

        // 声明后才能使用日志
        LogLevelConfig = Config.Bind("2General",
            "LogLevel/日志级别",
            LogLevel.Info,
            new ConfigDescription("Log Level, DEBUG will log more information/日志级别，DEBUG 级别将记录详细信息",
                null,
                new ConfigurationManagerAttributes { Order = 1910 }));

        # endregion

        # region SubtitleEnableSettings

        EnableBaseSubtitle = Config.Bind("3Subtitle",
            "EnableBaseSubtitle/启用基础字幕",
            true,
            new ConfigDescription(
                "Enable Base Subtitle, usually Karaoke, Casino, etc Some voices are matched by audio file name, so may not be displayed if text translation is not enabled/启用基础字幕，通常是卡拉OK、赌场等字幕。部分语音按音频文件名匹配，因此未启用文本翻译时可能无法显示",
                null, new ConfigurationManagerAttributes { Order = 2990 }));

        EnableYotogiSubtitle = Config.Bind("3Subtitle",
            "EnableYotogiSubtitle/启用夜伽字幕",
            true,
            new ConfigDescription("Enable Yotogi Subtitle/启用夜伽字幕", null,
                new ConfigurationManagerAttributes { Order = 2980 }));

        EnableAdvSubtitle = Config.Bind("3Subtitle",
            "EnableADVSubtitle/启用ADV字幕",
            false,
            new ConfigDescription(
                "Enable ADV subtitles. Since ADV scenes have their own subtitles, this setting is only useful in VR mode and is invalid in non-VR mode./启用ADV字幕，由于 ADV 场景自带字幕，因此仅在 VR 模式下有用，非 VR 模式此设置无效",
                null, new ConfigurationManagerAttributes { Order = 2970 }));

        ForceEnableAdvSubtitle = Config.Bind("3Subtitle",
            "ForceEnableADVSubtitle/强制启用ADV字幕",
            false,
            new ConfigDescription(
                "Force Enable ADV subtitles, whether in VR mode or not/强制启用ADV字幕，无论是不是 VR 模式", null,
                new ConfigurationManagerAttributes { Order = 2960 }));

        EnableLyricSubtitle = Config.Bind("3Subtitle",
            "EnableLyricSubtitle/启用歌词字幕",
            true,
            new ConfigDescription("Enable Lyric Subtitle/启用歌词字幕", null,
                new ConfigurationManagerAttributes { Order = 2950 }));

        SubtitleSearchDirection = Config.Bind("3Subtitle",
            "SubtitleSearchDirection/字幕搜索方向",
            SubtitleSearchDirectionEnum.DownFirst,
            new ConfigDescription(
                "Default search direction when finding available subtitle positions/查找可用字幕位置时的默认搜索方向",
                null, new ConfigurationManagerAttributes { Order = 2940 }));

        # endregion

        # region BaseSubtitleSettings

        // 基础字幕相关配置
        EnableBaseSubtitleSpeakerName = Config.Bind("4BaseSubtitle",
            "EnableBaseSubtitleSpeakerName/启用基础字幕显示说话人名",
            true,
            new ConfigDescription("Enable Base Subtitle Speaker Name/启用基础字幕显示说话人名", null,
                new ConfigurationManagerAttributes { Order = 3990 }));

        BaseSubtitleFont = Config.Bind("4BaseSubtitle",
            "BaseSubtitleFont/基础字幕字体",
            "Arial",
            new ConfigDescription(
                "Base Subtitle Font, need to already installed the font on the system/基础字幕字体，需要已经安装在系统中的字体",
                null,
                new ConfigurationManagerAttributes { Order = 3980 }));

        BaseSubtitleFontSize = Config.Bind("4BaseSubtitle",
            "BaseSubtitleFontSize/基础字幕字体大小",
            24,
            new ConfigDescription("Base Subtitle Font Size/基础字幕字体大小", null,
                new ConfigurationManagerAttributes { Order = 3970 }));

        BaseSubtitleTextAlignment = Config.Bind("4BaseSubtitle",
            "BaseSubtitleTextAlignment/基础字幕文本对齐方式",
            TextAnchorEnum.MiddleCenter,
            new ConfigDescription("Base Subtitle Text Alignment/基础字幕文本对齐方式", null,
                new ConfigurationManagerAttributes { Order = 3960 }));

        BaseSubtitleColor = Config.Bind("4BaseSubtitle",
            "BaseSubtitleColor/基础字幕颜色",
            "#FFFFFF",
            new ConfigDescription("Base Subtitle Color, use hex color code/基础字幕颜色，使用十六进制颜色代码", null,
                new ConfigurationManagerAttributes { Order = 3950 }));

        BaseSubtitleOpacity = Config.Bind("4BaseSubtitle",
            "BaseSubtitleOpacity/基础字幕不透明度",
            1f,
            new ConfigDescription("Base Subtitle Opacity (0-1)/基础字幕不透明度（0-1）", null,
                new ConfigurationManagerAttributes { Order = 3940 }));

        BaseSubtitleBackgroundColor = Config.Bind("4BaseSubtitle",
            "BaseSubtitleBackgroundColor/基础字幕背景颜色",
            "#000000",
            new ConfigDescription(
                "Base Subtitle Background Color, use hex color code/基础字幕背景颜色，使用十六进制颜色代码", null,
                new ConfigurationManagerAttributes { Order = 3930 }));

        BaseSubtitleBackgroundOpacity = Config.Bind("4BaseSubtitle",
            "BaseSubtitleBackgroundOpacity/基础字幕背景不透明度",
            0.1f,
            new ConfigDescription("Base Subtitle Background Opacity (0-1)/基础字幕背景不透明度（0-1）", null,
                new ConfigurationManagerAttributes { Order = 3820 }));

        BaseSubtitleWidth = Config.Bind("4BaseSubtitle",
            "BaseSubtitleWidth/基础字幕背景宽度",
            1920f,
            new ConfigDescription(
                "Subtitle Width(pixel), reference resolution is 1920x1080, so set to 1920 will fill the whole screen width/字幕背景宽度(像素)，参考分辨率为1920x1080，因此设置为1920时将填满整个屏幕宽",
                null, new ConfigurationManagerAttributes { Order = 3910 }));

        BaseSubtitleHeight = Config.Bind("4BaseSubtitle",
            "BaseSubtitleHeight/基础字幕背景高度",
            30f,
            new ConfigDescription(
                "Subtitle Height(pixel), reference resolution is 1920x1080, so set to 1080 will fill the whole screen height/字幕背景高度(像素)，参考分辨率为1920x1080，因此设置为1080时将填满整个屏幕高",
                null, new ConfigurationManagerAttributes { Order = 3900 }));

        BaseSubtitleHorizontalPosition = Config.Bind("4BaseSubtitle",
            "BaseSubtitleHorizontalPosition/基础字幕水平位置",
            0f,
            new ConfigDescription(
                "Distance to the left side of the screen (0 is the left, 1920 is the right)/到屏幕左边的距离（0为左边，1920为最右边）",
                null, new ConfigurationManagerAttributes { Order = 3890 }));

        BaseSubtitleVerticalPosition = Config.Bind("4BaseSubtitle",
            "BaseSubtitleVerticalPosition/基础字幕垂直位置",
            1050f,
            new ConfigDescription(
                "Distance to bottom of screen (0 is bottom, 1 is top, note 1080 will go out of screen, should subtract background height)/到屏幕底部的距离（0为底部，1080为顶部，注意1080会超出屏幕，应减去背景高度）",
                null, new ConfigurationManagerAttributes { Order = 3880 }));

        EnableBaseSubtitleAnimation = Config.Bind("4BaseSubtitle",
            "BaseSubtitleAnimation/基础字幕动画",
            true,
            new ConfigDescription("Enable Base Subtitle Animation/启用基础字幕动画", null,
                new ConfigurationManagerAttributes { Order = 3870 }));

        BaseSubtitleFadeInDuration = Config.Bind("4BaseSubtitle",
            "BaseSubtitleFadeInDuration/基础字幕淡入时长",
            0.5f,
            new ConfigDescription("Base Subtitle Fade In Duration in seconds/基础字幕淡入时长（秒）", null,
                new ConfigurationManagerAttributes { Order = 3860 }));

        BaseSubtitleFadeOutDuration = Config.Bind("4BaseSubtitle",
            "BaseSubtitleFadeOutDuration/基础字幕淡出时长",
            0.5f,
            new ConfigDescription("Base Subtitle Fade Out Duration in seconds/基础字幕淡出时长（秒）", null,
                new ConfigurationManagerAttributes { Order = 3850 }));

        EnableBaseSubtitleOutline = Config.Bind("4BaseSubtitle",
            "EnableBaseSubtitleOutline/启用基础字幕描边",
            true,
            new ConfigDescription("Enable Base Subtitle Outline/启用基础字幕描边", null,
                new ConfigurationManagerAttributes { Order = 3840 }));

        BaseSubtitleOutlineColor = Config.Bind("4BaseSubtitle",
            "BaseSubtitleOutlineColor/基础字幕描边颜色",
            "#000000",
            new ConfigDescription(
                "Base Subtitle Outline Color, use hex color code/基础字幕描边颜色，使用十六进制颜色代码", null,
                new ConfigurationManagerAttributes { Order = 3830 }));

        BaseSubtitleOutlineOpacity = Config.Bind("4BaseSubtitle",
            "BaseSubtitleOutlineOpacity/基础字幕描边不透明度",
            0.5f,
            new ConfigDescription("Base Subtitle Outline Opacity (0-1)/基础字幕描边不透明度（0-1）", null,
                new ConfigurationManagerAttributes { Order = 3820 }));

        BaseSubtitleOutlineWidth = Config.Bind("4BaseSubtitle",
            "BaseSubtitleOutlineWidth/基础字幕描边宽度",
            1f,
            new ConfigDescription("Base Subtitle Outline Width in pixels/基础字幕描边宽度（像素）", null,
                new ConfigurationManagerAttributes { Order = 3810 }));

        # endregion

        # region YotogiSubtitleSettings

        // 夜伽字幕相关配置
        EnableYotogiSubtitleSpeakerName = Config.Bind("5YotogiSubtitle",
            "EnableYotogiSubtitleSpeakerName/启用夜伽字幕显示说话人名",
            true,
            new ConfigDescription("Enable Yotogi Subtitle Speaker Name/启用夜伽字幕显示说话人名", null,
                new ConfigurationManagerAttributes { Order = 4990 }));

        YotogiSubtitleFont = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleFont/夜伽字幕字体",
            "Arial",
            new ConfigDescription(
                "Yotogi Subtitle Font, need to already installed the font on the system/夜伽字幕字体，需要已经安装在系统中的字体",
                null,
                new ConfigurationManagerAttributes { Order = 4980 }));

        YotogiSubtitleFontSize = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleFontSize/夜伽字幕字体大小",
            24,
            new ConfigDescription("Yotogi Subtitle Font Size/夜伽字幕字体大小", null,
                new ConfigurationManagerAttributes { Order = 4970 }));

        YotogiSubtitleTextAlignment = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleTextAlignment/夜伽字幕文本对齐",
            TextAnchorEnum.MiddleCenter,
            new ConfigDescription("Yotogi Subtitle Text Alignment/夜伽字幕文本对齐方式", null,
                new ConfigurationManagerAttributes { Order = 4960 }));

        YotogiSubtitleColor = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleColor/夜伽字幕颜色",
            "#FFFFFF",
            new ConfigDescription("Yotogi Subtitle Color, use hex color code/夜伽字幕颜色，使用十六进制颜色代码",
                null,
                new ConfigurationManagerAttributes { Order = 4950 }));

        YotogiSubtitleOpacity = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleOpacity/夜伽字幕不透明度",
            1f,
            new ConfigDescription("Yotogi Subtitle Opacity (0-1)/夜伽字幕不透明度（0-1）", null,
                new ConfigurationManagerAttributes { Order = 4940 }));

        YotogiSubtitleBackgroundColor = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleBackgroundColor/夜伽字幕背景颜色",
            "#000000",
            new ConfigDescription(
                "Yotogi Subtitle Background Color, use hex color code/夜伽字幕背景颜色，使用十六进制颜色代码", null,
                new ConfigurationManagerAttributes { Order = 4930 }));

        YotogiSubtitleBackgroundOpacity = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleBackgroundOpacity/夜伽字幕背景不透明度",
            0.1f,
            new ConfigDescription("Yotogi Subtitle Background Opacity (0-1)/夜伽字幕背景不透明度（0-1）", null,
                new ConfigurationManagerAttributes { Order = 4920 }));

        YotogiSubtitleWidth = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleWidth/夜伽字幕背景宽度",
            1920f,
            new ConfigDescription(
                "Subtitle Width(pixel), reference resolution is 1920x1080, so set to 1920 will fill the whole screen width/字幕宽度(像素)，参考分辨率为1920x1080，因此设置为1920时将填满整个屏幕宽",
                null, new ConfigurationManagerAttributes { Order = 4910 }));

        YotogiSubtitleHeight = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleHeight/夜伽字幕背景高度",
            30f,
            new ConfigDescription(
                "Subtitle Height(pixel), reference resolution is 1920x1080, so set to 1080 will fill the whole screen height/字幕高度(像素)，参考分辨率为1920x1080，因此设置为1080时将填满整个屏幕高",
                null, new ConfigurationManagerAttributes { Order = 4900 }));

        YotogiSubtitleHorizontalPosition = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleHorizontalPosition/夜伽字幕水平位置",
            0f,
            new ConfigDescription(
                "Distance to the left side of the screen (0 is the left, 1920 is the right)/到屏幕左边的距离（0为左边，1920为最右边）",
                null, new ConfigurationManagerAttributes { Order = 4890 }));

        YotogiSubtitleVerticalPosition = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleVerticalPosition/夜伽字幕垂直位置",
            1050f,
            new ConfigDescription(
                "Distance to the left side of the screen (0 is the left, 1920 is the right)/到屏幕左边的距离（0为左边，1920为最右边）",
                null, new ConfigurationManagerAttributes { Order = 4880 }));

        EnableYotogiSubtitleAnimation = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleAnimation/夜伽字幕动画",
            true,
            new ConfigDescription("Enable Yotogi Subtitle Animation/启用夜伽字幕动画", null,
                new ConfigurationManagerAttributes { Order = 4870 }));

        YotogiSubtitleFadeInDuration = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleFadeInDuration/夜伽字幕淡入时长",
            0.5f,
            new ConfigDescription("Yotogi Subtitle Fade In Duration in seconds/夜伽字幕淡入时长（秒）", null,
                new ConfigurationManagerAttributes { Order = 4860 }));

        YotogiSubtitleFadeOutDuration = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleFadeOutDuration/夜伽字幕淡出时长",
            0.5f,
            new ConfigDescription("Yotogi Subtitle Fade Out Duration in seconds/夜伽字幕淡出时长（秒）", null,
                new ConfigurationManagerAttributes { Order = 4850 }));

        EnableYotogiSubtitleOutline = Config.Bind("5YotogiSubtitle",
            "EnableYotogiSubtitleOutline/启用夜伽字幕描边",
            true,
            new ConfigDescription("Enable Yotogi Subtitle Outline/启用夜伽字幕描边", null,
                new ConfigurationManagerAttributes { Order = 4840 }));

        YotogiSubtitleOutlineColor = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleOutlineColor/夜伽字幕描边颜色",
            "#000000",
            new ConfigDescription(
                "Yotogi Subtitle Outline Color, use hex color code/夜伽字幕描边颜色，使用十六进制颜色代码", null,
                new ConfigurationManagerAttributes { Order = 4830 }));

        YotogiSubtitleOutlineOpacity = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleOutlineOpacity/夜伽字幕描边不透明度",
            0.5f,
            new ConfigDescription("Yotogi Subtitle Outline Opacity (0-1)/夜伽字幕描边不透明度（0-1）", null,
                new ConfigurationManagerAttributes { Order = 4820 }));

        YotogiSubtitleOutlineWidth = Config.Bind("5YotogiSubtitle",
            "YotogiSubtitleOutlineWidth/夜伽字幕描边宽度",
            1f,
            new ConfigDescription("Yotogi Subtitle Outline Width in pixels/夜伽字幕描边宽度（像素）", null,
                new ConfigurationManagerAttributes { Order = 4810 }));

        # endregion

        # region AdvSubtitleSettings

        // ADV字幕相关配置
        EnableAdvSubtitleSpeakerName = Config.Bind("6AdvSubtitle",
            "EnableAdvSubtitleSpeakerName/启用ADV字幕显示说话人名",
            true,
            new ConfigDescription("Enable ADV Subtitle Speaker Name/启用ADV字幕显示说话人名", null,
                new ConfigurationManagerAttributes { Order = 5990 }));

        AdvSubtitleFont = Config.Bind("6AdvSubtitle",
            "AdvSubtitleFont/ADV字幕字体",
            "Arial",
            new ConfigDescription(
                "ADV Subtitle Font, need to already installed the font on the system/ADV字幕字体，需要已经安装在系统中的字体",
                null,
                new ConfigurationManagerAttributes { Order = 5980 }));

        AdvSubtitleFontSize = Config.Bind("6AdvSubtitle",
            "AdvSubtitleFontSize/ADV字幕字体大小",
            24,
            new ConfigDescription("ADV Subtitle Font Size/ADV字幕字体大小", null,
                new ConfigurationManagerAttributes { Order = 5970 }));

        AdvSubtitleTextAlignment = Config.Bind("6AdvSubtitle",
            "AdvSubtitleTextAlignment/ADV字幕文本对齐方式",
            TextAnchorEnum.MiddleCenter,
            new ConfigDescription("ADV Subtitle Text Alignment/ADV字幕文本对齐方式", null,
                new ConfigurationManagerAttributes { Order = 5960 }));

        AdvSubtitleColor = Config.Bind("6AdvSubtitle",
            "AdvSubtitleColor/ADV字幕颜色",
            "#FFFFFF",
            new ConfigDescription("ADV Subtitle Color, use hex color code/ADV字幕颜色，使用十六进制颜色代码", null,
                new ConfigurationManagerAttributes { Order = 5950 }));

        AdvSubtitleOpacity = Config.Bind("6AdvSubtitle",
            "AdvSubtitleOpacity/ADV字幕不透明度",
            1f,
            new ConfigDescription("ADV Subtitle Opacity (0-1)/ADV字幕不透明度（0-1）", null,
                new ConfigurationManagerAttributes { Order = 5940 }));

        AdvSubtitleBackgroundColor = Config.Bind("6AdvSubtitle",
            "AdvSubtitleBackgroundColor/ADV字幕背景颜色",
            "#000000",
            new ConfigDescription(
                "ADV Subtitle Background Color, use hex color code/ADV字幕背景颜色，使用十六进制颜色代码", null,
                new ConfigurationManagerAttributes { Order = 5930 }));

        AdvSubtitleBackgroundOpacity = Config.Bind("6AdvSubtitle",
            "AdvSubtitleBackgroundOpacity/ADV字幕背景不透明度",
            0.1f,
            new ConfigDescription("ADV Subtitle Background Opacity (0-1)/ADV字幕背景不透明度（0-1）", null,
                new ConfigurationManagerAttributes { Order = 5920 }));

        AdvSubtitleWidth = Config.Bind("6AdvSubtitle",
            "AdvSubtitleWidth/ADV字幕背景宽度",
            1920f,
            new ConfigDescription(
                "Subtitle Width(pixel), reference resolution is 1920x1080, so set to 1920 will fill the whole screen width/字幕宽度(像素)，参考分辨率为1920x1080，因此设置为1920时将填满整个屏幕宽",
                null, new ConfigurationManagerAttributes { Order = 5910 }));

        AdvSubtitleHeight = Config.Bind("6AdvSubtitle",
            "AdvSubtitleHeight/ADV字幕背景高度",
            30f,
            new ConfigDescription(
                "Subtitle Height(pixel), reference resolution is 1920x1080, so set to 1080 will fill the whole screen height/字幕高度(像素)，参考分辨率为1920x1080，因此设置为1080时将填满整个屏幕高",
                null, new ConfigurationManagerAttributes { Order = 5900 }));

        AdvSubtitleHorizontalPosition = Config.Bind("6AdvSubtitle",
            "AdvSubtitleHorizontalPosition/ADV字幕水平位置",
            0f,
            new ConfigDescription(
                "Distance to the left side of the screen (0 is the left, 1920 is the right)/到屏幕左边的距离（0为左边，1920为最右边）",
                null, new ConfigurationManagerAttributes { Order = 5890 }));

        AdvSubtitleVerticalPosition = Config.Bind("6AdvSubtitle",
            "AdvSubtitleVerticalPosition/ADV字幕垂直位置",
            1050f,
            new ConfigDescription(
                "Distance to bottom of screen (0 is bottom, 1 is top, note 1080 will go out of screen, should subtract background height)/到屏幕底部的距离（0为底部，1080为顶部，注意1080会超出屏幕，应减去背景高度）",
                null, new ConfigurationManagerAttributes { Order = 5880 }));

        EnableAdvSubtitleAnimation = Config.Bind("6AdvSubtitle",
            "AdvSubtitleAnimation/ADV字幕动画",
            true,
            new ConfigDescription("Enable ADV Subtitle Animation/启用ADV字幕动画", null,
                new ConfigurationManagerAttributes { Order = 5870 }));

        AdvSubtitleFadeInDuration = Config.Bind("6AdvSubtitle",
            "AdvSubtitleFadeInDuration/ADV字幕淡入时长",
            0.5f,
            new ConfigDescription("ADV Subtitle Fade In Duration in seconds/ADV字幕淡入时长（秒）", null,
                new ConfigurationManagerAttributes { Order = 5860 }));

        AdvSubtitleFadeOutDuration = Config.Bind("6AdvSubtitle",
            "AdvSubtitleFadeOutDuration/ADV字幕淡出时长",
            0.5f,
            new ConfigDescription("ADV Subtitle Fade Out Duration in seconds/ADV字幕淡出时长（秒）", null,
                new ConfigurationManagerAttributes { Order = 5850 }));

        EnableAdvSubtitleOutline = Config.Bind("6AdvSubtitle",
            "EnableAdvSubtitleOutline/启用ADV字幕描边",
            true,
            new ConfigDescription("Enable ADV Subtitle Outline/启用ADV字幕描边", null,
                new ConfigurationManagerAttributes { Order = 5840 }));

        AdvSubtitleOutlineColor = Config.Bind("6AdvSubtitle",
            "AdvSubtitleOutlineColor/ADV字幕描边颜色",
            "#000000",
            new ConfigDescription(
                "ADV Subtitle Outline Color, use hex color code/ADV字幕描边颜色，使用十六进制颜色代码", null,
                new ConfigurationManagerAttributes { Order = 5830 }));

        AdvSubtitleOutlineOpacity = Config.Bind("6AdvSubtitle",
            "AdvSubtitleOutlineOpacity/ADV字幕描边不透明度",
            0.5f,
            new ConfigDescription("ADV Subtitle Outline Opacity (0-1)/ADV字幕描边不透明度（0-1）", null,
                new ConfigurationManagerAttributes { Order = 5820 }));


        AdvSubtitleOutlineWidth = Config.Bind("6AdvSubtitle",
            "AdvSubtitleOutlineWidth/ADV字幕描边宽度",
            1f,
            new ConfigDescription("ADV Subtitle Outline Width in pixels/ADV字幕描边宽度（像素）", null,
                new ConfigurationManagerAttributes { Order = 5810 }));

        # endregion

        # region LyricSubtitleSettings

        // 歌词字幕相关配置
        EnableLyricSubtitleSpeakerName = Config.Bind("7LyricSubtitle",
            "EnableLyricSubtitleSpeakerName/启用歌词字幕显示说话人名",
            false,
            new ConfigDescription(
                "Enable Lyric Subtitle Speaker Name. The song is played as a BGM, the speaker name is always displayed as the dance main maid/启用歌词字幕显示说话人名。歌曲作为BGM形式播放，人名始终显示为舞蹈主女仆",
                null, new ConfigurationManagerAttributes { Order = 6990 }));

        LyricSubtitleType = Config.Bind("7LyricSubtitle",
            "LyricSubtitleType/歌词字幕类型",
            LyricSubtitleTypeEnum.TranslationAndOriginal,
            new ConfigDescription("Lyric Subtitle Type/歌词字幕类型", null,
                new ConfigurationManagerAttributes { Order = 6980 }));

        LyricSubtitleFont = Config.Bind("7LyricSubtitle",
            "LyricSubtitleFont/歌词字幕字体",
            "Arial",
            new ConfigDescription(
                "Lyric Subtitle Font, need to already installed the font on the system/歌词字幕字体，需要已经安装在系统中的字体",
                null,
                new ConfigurationManagerAttributes { Order = 6970 }));

        LyricSubtitleFontSize = Config.Bind("7LyricSubtitle",
            "LyricSubtitleFontSize/歌词字幕字体大小",
            24,
            new ConfigDescription("Lyric Subtitle Font Size/歌词字幕字体大小", null,
                new ConfigurationManagerAttributes { Order = 6960 }));

        LyricSubtitleTextAlignment = Config.Bind("7LyricSubtitle",
            "LyricSubtitleTextAlignment/歌词字幕文本对齐",
            TextAnchorEnum.MiddleCenter,
            new ConfigDescription("Lyric Subtitle Text Alignment/歌词字幕文本对齐方式", null,
                new ConfigurationManagerAttributes { Order = 6950 }));

        LyricSubtitleColor = Config.Bind("7LyricSubtitle",
            "LyricSubtitleColor/歌词字幕颜色",
            "#FFFFFF",
            new ConfigDescription("Lyric Subtitle Color, use hex color code/歌词字幕颜色，使用十六进制颜色代码",
                null,
                new ConfigurationManagerAttributes { Order = 6940 }));

        LyricSubtitleOpacity = Config.Bind("7LyricSubtitle",
            "LyricSubtitleOpacity/歌词字幕不透明度",
            1f,
            new ConfigDescription("Lyric Subtitle Opacity (0-1)/歌词字幕不透明度（0-1）", null,
                new ConfigurationManagerAttributes { Order = 6930 }));

        LyricSubtitleBackgroundColor = Config.Bind("7LyricSubtitle",
            "LyricSubtitleBackgroundColor/歌词字幕背景颜色",
            "#000000",
            new ConfigDescription(
                "Lyric Subtitle Background Color, use hex color code/歌词字幕背景颜色，使用十六进制颜色代码", null,
                new ConfigurationManagerAttributes { Order = 6920 }));

        LyricSubtitleBackgroundOpacity = Config.Bind("7LyricSubtitle",
            "LyricSubtitleBackgroundOpacity/歌词字幕背景不透明度",
            0f,
            new ConfigDescription("Lyric Subtitle Background Opacity (0-1)/歌词字幕背景不透明度（0-1）", null,
                new ConfigurationManagerAttributes { Order = 6910 }));

        LyricSubtitleWidth = Config.Bind("7LyricSubtitle",
            "LyricSubtitleWidth/歌词字幕背景宽度",
            1920f,
            new ConfigDescription(
                "Subtitle Width(pixel), reference resolution is 1920x1080, so set to 1920 will fill the whole screen width/字幕宽度(像素），参考分辨率为1920x1080，因此设置为1920时将填满整个屏幕宽",
                null, new ConfigurationManagerAttributes { Order = 6900 }));

        LyricSubtitleHeight = Config.Bind("7LyricSubtitle",
            "LyricSubtitleHeight/歌词字幕背景高度",
            70f,
            new ConfigDescription(
                "Subtitle Height(pixel), reference resolution is 1920x1080, so set to 1080 will fill the whole screen height/字幕高度(像素)，参考分辨率为1920x1080，因此设置为1080时将填满整个屏幕高",
                null, new ConfigurationManagerAttributes { Order = 6890 }));

        LyricSubtitleHorizontalPosition = Config.Bind("7LyricSubtitle",
            "LyricSubtitleHorizontalPosition/歌词字幕水平位置",
            0f,
            new ConfigDescription(
                "Distance to the left side of the screen (0 is the left, 1920 is the right)/到屏幕左边的距离（0为左边，1920为最右边）",
                null, new ConfigurationManagerAttributes { Order = 6980 }));

        LyricSubtitleVerticalPosition = Config.Bind("7LyricSubtitle",
            "LyricSubtitleVerticalPosition/歌词字幕垂直位置",
            200f,
            new ConfigDescription(
                "Distance to bottom of screen (0 is bottom, 1 is top, note 1080 will go out of screen, should subtract background height)/到屏幕底部的距离（0为底部，1080为顶部，注意1080会超出屏幕，应减去背景高度）",
                null, new ConfigurationManagerAttributes { Order = 6870 }));

        EnableLyricSubtitleAnimation = Config.Bind("7LyricSubtitle",
            "LyricSubtitleAnimation/歌词字幕动画",
            true,
            new ConfigDescription("Enable Lyric Subtitle Animation/启用歌词字幕动画", null,
                new ConfigurationManagerAttributes { Order = 6860 }));

        LyricSubtitleFadeInDuration = Config.Bind("7LyricSubtitle",
            "LyricSubtitleFadeInDuration/歌词字幕淡入时长",
            0.5f,
            new ConfigDescription("Lyric Subtitle Fade In Duration in seconds/歌词字幕淡入时长（秒）", null,
                new ConfigurationManagerAttributes { Order = 6850 }));

        LyricSubtitleFadeOutDuration = Config.Bind("7LyricSubtitle",
            "LyricSubtitleFadeOutDuration/歌词字幕淡出时长",
            0.5f,
            new ConfigDescription("Lyric Subtitle Fade Out Duration in seconds/歌词字幕淡出时长（秒）", null,
                new ConfigurationManagerAttributes { Order = 6840 }));

        EnableLyricSubtitleOutline = Config.Bind("7LyricSubtitle",
            "EnableLyricSubtitleOutline/启用歌词字幕描边",
            true,
            new ConfigDescription("Enable Lyric Subtitle Outline/启用歌词字幕描边", null,
                new ConfigurationManagerAttributes { Order = 6830 }));

        LyricSubtitleOutlineColor = Config.Bind("7LyricSubtitle",
            "LyricSubtitleOutlineColor/歌词字幕描边颜色",
            "#000000",
            new ConfigDescription(
                "Lyric Subtitle Outline Color, use hex color code/歌词字幕描边颜色，使用十六进制颜色代码", null,
                new ConfigurationManagerAttributes { Order = 6820 }));

        LyricSubtitleOutlineOpacity = Config.Bind("7LyricSubtitle",
            "LyricSubtitleOutlineOpacity/歌词字幕描边不透明度",
            0.5f,
            new ConfigDescription("Lyric Subtitle Outline Opacity (0-1)/歌词字幕描边不透明度（0-1）", null,
                new ConfigurationManagerAttributes { Order = 6810 }));

        LyricSubtitleOutlineWidth = Config.Bind("7LyricSubtitle",
            "LyricSubtitleOutlineWidth/歌词字幕描边宽度",
            1f,
            new ConfigDescription("Lyric Subtitle Outline Width in pixels/歌词字幕描边宽度（像素）", null,
                new ConfigurationManagerAttributes { Order = 6800 }));

        # endregion

        # region VRSubtitleSettings

        // VR空间字幕相关配置
        VRSubtitleMode = Config.Bind("8VRSubtitle",
            "VRSubtitleMode/VR字幕模式",
            VRSubtitleModeEnum.Tablet,
            new ConfigDescription(
                "VR Subtitle Mode: Tablet=On Control tablet, Space=Floating in world space following head movement/VR字幕模式：Space=跟随头部运动的世界空间悬浮字幕上，Tablet=字幕在控制平板上",
                null, new ConfigurationManagerAttributes { Order = 7990 }));

        VRSpaceSubtitleWidth = Config.Bind("8VRSubtitle",
            "VRSpaceSubtitleWidth/VR空间字幕宽度",
            510f,
            new ConfigDescription(
                "VR Space Subtitle Width(1000 unit=1 meter)/VR空间字幕宽度（1000单位=1米）", null,
                new ConfigurationManagerAttributes { Order = 7980 }));

        VRSpaceSubtitleHeight = Config.Bind("8VRSubtitle",
            "VRSpaceSubtitleHeight/VR空间字幕高度",
            15f,
            new ConfigDescription(
                "VR Space Subtitle Height(1000 unit=1 meter)/VR空间字幕高度（1000单位=1米）", null,
                new ConfigurationManagerAttributes { Order = 7970 }));

        VRSpaceSubtitleDistance = Config.Bind("8VRSubtitle",
            "VRSpaceSubtitleDistance/VR空间字幕距离",
            0.5f,
            new ConfigDescription(
                "VR Space Subtitle Distance from center of view in meters/VR空间字幕距离视线中心有多远（米）", null,
                new ConfigurationManagerAttributes { Order = 7960 }));

        VRSpaceSubtitleVerticalOffset = Config.Bind("8VRSubtitle",
            "VRSpaceSubtitleVerticalOffset/VR空间字幕垂直偏移",
            20f,
            new ConfigDescription(
                "VR Space Subtitle Vertical Offset in degrees, positive value to the bottom (relative to center of view)/VR空间字幕垂直偏移，正值向下（度，相对于视线中心）",
                null, new ConfigurationManagerAttributes { Order = 7950 }));

        VRSpaceSubtitleHorizontalOffset = Config.Bind("8VRSubtitle",
            "VRSpaceSubtitleHorizontalOffset/VR空间字幕水平偏移",
            0f,
            new ConfigDescription(
                "VR Space Subtitle Horizontal Offset in degrees, positive value to the right (relative to center of view)/VR空间字幕水平偏移，正值向右（度，相对于视线中心）",
                null, new ConfigurationManagerAttributes { Order = 7940 }));

        VRSpaceSubtitleTextSizeMultiplier = Config.Bind("8VRSubtitle",
            "VRSpaceSubtitleTextSizeMultiplier/VR空间字幕文字大小倍率",
            14f,
            new ConfigDescription("VR Space Subtitle Text Size Multiplier/VR空间字幕文本大小倍数", null,
                new ConfigurationManagerAttributes { Order = 7930 }));

        VRSpaceSubtitleOutlineScaleFactor = Config.Bind("8VRSubtitle",
            "VRSpaceSubtitleOutlineScaleFactor/VR空间字幕描边缩放因子",
            2.4f,
            new ConfigDescription(
                "VR Space Subtitle Outline Scale Factor, if outline shadow appears, try increasing this value/VR空间字幕描边缩放因子，若出现描边重影请尝试调高此值",
                null,
                new ConfigurationManagerAttributes { Order = 7920 }));

        VRSpaceSubtitlePixelPerfect = Config.Bind("8VRSubtitle",
            "VRSpaceSubtitlePixelPerfect/VR空间字幕像素完美",
            false,
            new ConfigDescription(
                "VR Space Subtitle Pixel Perfect(whether to perform anti-aliasing)/VR空间字幕像素完美（是否进行抗锯齿处理）",
                null,
                new ConfigurationManagerAttributes { Order = 7910 }));

        VRSpaceSubtitleFollowSmoothness = Config.Bind("8VRSubtitle",
            "VRSpaceSubtitleFollowSmoothness/VR空间字幕跟随平滑度",
            3f,
            new ConfigDescription(
                "VR Space Subtitle Follow Smoothness, the higher the value, the faster it follows(set to 5 is about 0.6s to catch 95% of movement)/VR空间字幕跟随平滑度，数值越大跟踪越迅速（设置为 5 则约 0.6s 追上 95% 位移）",
                null,
                new ConfigurationManagerAttributes { Order = 7900 }));

        // VR平板电脑字幕相关配置
        VRTabletSubtitleWidth = Config.Bind("8VRSubtitle",
            "VRTabletSubtitleWidth/VR平板电脑字幕宽度",
            500f,
            new ConfigDescription(
                "VR Tablet Subtitle Width(1000 unit = 1 meter)/VR平板电脑字幕宽度（1000单位=1米）", null,
                new ConfigurationManagerAttributes { Order = 7890 }));

        VRTabletSubtitleHeight = Config.Bind("8VRSubtitle",
            "VRTabletSubtitleHeight/VR平板电脑字幕高度",
            10f,
            new ConfigDescription(
                "VR Tablet Subtitle Height(1000 unit = 1 meter)/VR平板电脑字幕高度（1000单位=1米）", null,
                new ConfigurationManagerAttributes { Order = 7880 }));

        VRTabletSubtitleVerticalPosition = Config.Bind("8VRSubtitle",
            "VRTabletSubtitleVerticalPosition/VR平板电脑字幕垂直位置",
            0.17f,
            new ConfigDescription(
                "VR Tablet Subtitle Vertical Position in meter, 0 is VR tablet center, the larger the value the higher/VR平板电脑字幕垂直位置（米），0 为平板电脑中央，数值越大越往上",
                null,
                new ConfigurationManagerAttributes { Order = 7870 }));

        VRTabletSubtitleHorizontalPosition = Config.Bind("8VRSubtitle",
            "VRTabletSubtitleHorizontalPosition/VR平板电脑字幕水平位置",
            0f,
            new ConfigDescription(
                "VR Tablet Subtitle Horizontal Position in meters, 0 is VR tablet center, the larger the value the right/VR平板电脑字幕水平位置（米），0 为平板电脑中央，数值越大越往右",
                null,
                new ConfigurationManagerAttributes { Order = 7860 }));

        VRTabletSubtitleTextSizeMultiplier = Config.Bind("8VRSubtitle",
            "VRTabletSubtitleTextSizeMultiplier/VR平板电脑字幕文字大小倍率",
            12f,
            new ConfigDescription("VR Tablet Subtitle Text Size Multiplier/VR平板电脑字幕文字大小倍率", null,
                new ConfigurationManagerAttributes { Order = 7850 }));

        VRTabletSubtitleOutlineScaleFactor = Config.Bind("8VRSubtitle",
            "VRTabletSubtitleOutlineScaleFactor/VR平板电脑字幕描边缩放因子",
            2.8f,
            new ConfigDescription(
                "VR Tablet Subtitle Outline Scale Factor, if outline shadow appears, try increasing this value/VR平板电脑字幕描边缩放因子，若出现描边重影请尝试调高此值",
                null,
                new ConfigurationManagerAttributes { Order = 7840 }));

        VRTabletSubtitlePixelPerfect = Config.Bind("8VRSubtitle",
            "VRTabletSubtitlePixelPerfect/VR平板电脑字幕像素完美",
            false,
            new ConfigDescription(
                "VR Tablet Subtitle Pixel Perfect(whether to perform anti-aliasing)/VR平板电脑字幕像素完美（是否进行抗锯齿处理）",
                null,
                new ConfigurationManagerAttributes { Order = 7830 }));

        # endregion

        # region DumpSettings

        // Tip for people who using ConfigurationManager
        _dumpTip1 = Config.Bind("9Dump",
            "this section is for translators. If you are not planning to change translate file, please do not enable it, it will slow down the game",
            true,
            new ConfigDescription("this config do nothing", null,
                new ConfigurationManagerAttributes { Order = 8990 }));

        _dumpTip2 = Config.Bind("9Dump",
            "这个部分是为翻译者准备的，如果您不计划改动翻译文件，请不要启用它, 会减慢游戏速度",
            true,
            new ConfigDescription("这个配置不做任何事情", null,
                new ConfigurationManagerAttributes { Order = 8980 }));

        _dumpTip3 = Config.Bind("9Dump",
            "Turn log level to Debug to make it easier to debug and find problems",
            true,
            new ConfigDescription("this config do nothing", null,
                new ConfigurationManagerAttributes { Order = 8970 }));

        _dumpTip4 = Config.Bind("9Dump",
            "在通用设置内将日志等级调至 Debug 能让您更方便的调试，以及排查各类不生效原因",
            true,
            new ConfigDescription("这个配置不做任何事情", null,
                new ConfigurationManagerAttributes { Order = 8960 }));

        EnableTexturesDump = Config.Bind("9Dump",
            "EnableDumpTexture/是否启用纹理转储",
            false,
            new ConfigDescription("Only export textures that have not been replaced/仅转储未替换过的纹理",
                null,
                new ConfigurationManagerAttributes { Order = 8950 }));

        EnableSpriteDump = Config.Bind("9Dump",
            "EnableDumpSprite/是否启用精灵图转储",
            false,
            new ConfigDescription(
                "Only export sprites that have not been replaced, if atlas has been replaced, the sprite from the replaced atlas will be dump/仅转储未替换过的精灵图，若是Atlas图集被替换过，则会转储替换过Atlas上的精灵图",
                null,
                new ConfigurationManagerAttributes { Order = 8940 }));

        EnableTextDump = Config.Bind("9Dump",
            "EnableDumpText/是否启用文本转储",
            false,
            new ConfigDescription(
                "Only export text that has not been replaced, write out when the threshold is reached or switching scenes or the game correct exits/仅转储未替换过的文本，达到阈值或切换场景或正确退出游戏时写出",
                null, new ConfigurationManagerAttributes { Order = 8930 }));

        TextDumpThreshold = Config.Bind("9Dump",
            "TextDumpThreshold/文本转储阈值",
            20,
            new ConfigDescription("How many lines of text to write out at once/累计多少条文本后写出一次", null,
                new ConfigurationManagerAttributes { Order = 8920 }));

        FlushTextDumpNow = Config.Bind("9Dump",
            "FlushTextDumpNow/立即写出文本",
            false,
            new ConfigDescription(
                "Immediately write out all cached text when the option status changes/立即写出所有已缓存的文本，选项状态变更时立即写出",
                null,
                new ConfigurationManagerAttributes { Order = 8910 }));

        EnableDumpDanceInfo = Config.Bind("9Dump",
            "EnableDumpDanceInfo/是否启用舞蹈信息转储",
            true,
            new ConfigDescription(
                "Dump dance info to danceInfo.csv(after playing a dance)/播放舞蹈后在 danceInfos.csv 内输出舞蹈信息",
                null,
                new ConfigurationManagerAttributes { Order = 8900 }));

        EnableTermDump = Config.Bind("9Dump",
            "EnableDumpTerm/是否启用Term转储",
            false,
            new ConfigDescription(
                "Only export term that has not translated, write out when the threshold is reached or switching scenes or the game correct exits/仅转储未替换过的term，达到阈值或切换场景或正确退出游戏时写出",
                null, new ConfigurationManagerAttributes { Order = 8890 }));

        TermDumpThreshold = Config.Bind("9Dump",
            "TermDumpThreshold/Term转储阈值",
            20,
            new ConfigDescription("How many lines of term to write out at once/累计多少条term后写出一次",
                null,
                new ConfigurationManagerAttributes { Order = 8880 }));

        FlushTermDumpNow = Config.Bind("9Dump",
            "FlushTermDumpNow/立即写出Term",
            false,
            new ConfigDescription(
                "Immediately write out all cached term when the option status changes/立即写出所有已缓存的term，选项状态变更时立即写出",
                null,
                new ConfigurationManagerAttributes { Order = 8870 }));

        PrintOSFont = Config.Bind("9Dump",
            "PrintOSFont/打印OS字体",
            false,
            new ConfigDescription(
                "Prints all installed and available fonts in the system to the console and logs, and prints them immediately when the option status changes/将系统内已安装且可用的字体打印到控制台和日志，选项状态变更时立即打印",
                null,
                new ConfigurationManagerAttributes { Order = 8860 }));

        # endregion

        # region FixerSettings

        // Tip for people who using ConfigurationManager
        _fixerTip1 = Config.Bind("10Fixer",
            "This section is used to disable or enable some fixes. If you dont know what you are doing, please do not touch it",
            true,
            new ConfigDescription("this config do nothing", null,
                new ConfigurationManagerAttributes { Order = 9990 }));

        _fixerTip2 = Config.Bind("10Fixer",
            "这一部分用于禁用或启用一些修复补丁，如果你不知道你在做什么，请不要动",
            true,
            new ConfigDescription("this config do nothing", null,
                new ConfigurationManagerAttributes { Order = 9980 }));

        EnableMaidCafeDlcLineBreakCommentFix = Config.Bind("10Fixer",
            "EnableMaidCafeDlcLineBreakCommentFix/启用女仆咖啡厅DLC的弹幕不移动的修复",
            true,
            new ConfigDescription(
                "Enabled a fix for the Maid Cafe DLC's bullet chat not moving/启用女仆咖啡厅DLC的弹幕不移动的修复",
                null,
                new ConfigurationManagerAttributes { Order = 9970 }));

        EnableNoneCjkFix = Config.Bind("10Fixer",
            "EnableNoneCJKFix/启用非CJK语言修复",
            false,
            new ConfigDescription(
                "Fix potential text compression and stretching issues. If your target language is not in Chinese or Korean, please enable this option/修复可能存在的文字压缩和拉伸问题，如果您的目标语言不是中日韩语言，请启用此选项",
                null, new ConfigurationManagerAttributes { Order = 9960 }));

        EnableUIFontReplace = Config.Bind("10Fixer",
            "EnableUIFontReplace/启用UI字体替换",
            false,
            new ConfigDescription(
                "Enable UI Font Replace/启用 UI 字体替换",
                null, new ConfigurationManagerAttributes { Order = 9950 }));

        UIFont = Config.Bind("10Fixer",
            "UIFont/UI字体",
            "Noto Sans CJK SC",
            new ConfigDescription(
                "This is used to replace the font and requires a font that is already installed on your system. The game's built-in font is NotoSansCJKjp-DemiLight./用于替换字体，需要已经安装在系统中的字体，游戏内置字体为 NotoSansCJKjp-DemiLight",
                null, new ConfigurationManagerAttributes { Order = 9940 }));

        # endregion

        LogManager.Debug($"IsVrMode: {IsVrMode}, CommandLine: {Environment.CommandLine}");

        // Create the translation folder
        try
        {
            Directory.CreateDirectory(TranslationRootPath);
            Directory.CreateDirectory(TargetLanguePath);
            Directory.CreateDirectory(TranslationTextPath);
            Directory.CreateDirectory(TextureReplacePath);
            Directory.CreateDirectory(LyricPath);
            Directory.CreateDirectory(UIPath);
            Directory.CreateDirectory(DumpUIPath);
            Directory.CreateDirectory(UITextPath);
            Directory.CreateDirectory(UISpritePath);
            Directory.CreateDirectory(DumpPath);
            Directory.CreateDirectory(TextDumpPath);
            Directory.CreateDirectory(TextureDumpPath);
            Directory.CreateDirectory(SpriteDumpPath);
            Directory.CreateDirectory(TermDumpPath);
            if (!File.Exists(SubtitleColorsConfigPath))
                File.WriteAllText(SubtitleColorsConfigPath, "{}");
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"Create translation folder failed, plugin may not work/创建翻译文件夹失败，插件可能无法运行: {e.Message}");
        }

        // Init modules
        if (EnableGeneralTextTranslation.Value)
        {
            LogManager.Info("General Text Translation Enabled/通用文本翻译已启用");
            TextTranslateManger.Init();
        }
        else
        {
            LogManager.Info("Text Translation Disabled/翻译已禁用");
        }

        if (EnableUITextTranslation.Value)
        {
            LogManager.Info("UI Translation Enabled/UI 翻译已启用");
            UITranslateManager.Init();
        }
        else
        {
            LogManager.Info("UI Translation Disabled/UI 翻译已禁用");
        }

        if (EnableUITextExtraPatch.Value)
        {
            LogManager.Info("UI Translation Extra Patch Enabled/UI 翻译额外补丁已启用");
            UITranslateManager.Init();
        }
        else
        {
            LogManager.Info("UI Translation Extra Patch Disabled/UI 翻译额外补丁已禁用");
        }

        if (EnableUISpriteReplace.Value)
        {
            LogManager.Info("UI Sprite Replace Enabled/UI 精灵图替换已启用");
            UITranslateManager.Init();
        }
        else
        {
            LogManager.Info("UI Sprite Replace Disabled/UI 精灵图替换已禁用");
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

        if (EnableMaidCafeDlcLineBreakCommentFix.Value)
        {
            LogManager.Info("MaidCafeDlcLineBreakCommentFix Enabled/女仆咖啡厅DLC的弹幕不移动的修复已启用");
            FixerManger.Init();
        }
        else
        {
            LogManager.Info("MaidCafeDlcLineBreakCommentFix Disabled/女仆咖啡厅DLC的弹幕不移动的修复已禁用");
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
        // 注册Dump配置变更事件
        RegisterDumpConfigEvents();
        // 注册修复补丁配置变更事件
        RegisterFixerConfigEvents();
    }

    private void Start()
    {
        if (EnableGeneralTextTranslation.Value)
            // Init XUAT interop
            XUATInterop.Init();
    }

    private void Update()
    {
        if (ReloadTranslateResourceShortcut.Value.IsPressed())
        {
            LogManager.Info("Reloading all translation resources.../正在重载所有翻译资源...");

            if (EnableGeneralTextTranslation.Value)
                TextTranslateManger.Reload();

            if (EnableTextureReplace.Value)
                TextureReplaceManger.Reload();

            if (EnableUITextTranslation.Value || EnableUISpriteReplace.Value)
                UITranslateManager.Reload();

            if (EnableBaseSubtitle.Value || EnableYotogiSubtitle.Value || EnableAdvSubtitle.Value)
                SubtitleManager.Reload();
        }
    }

    private void OnDestroy()
    {
        TextTranslateManger.Unload();
        UITranslateManager.Unload();
        TextureReplaceManger.Unload();
        SubtitleManager.Unload();
        LyricManger.Unload();
        FixerManger.Unload();
        XUATInterop.Unload();
    }

    # region RegisterGeneralConfigEvents

    /// <summary>
    ///     注册通用变更事件
    /// </summary>
    private void RegisterGeneralConfigEvents()
    {
        // 注册目标语言变更事件
        TargetLanguage.SettingChanged += (_, _) =>
        {
            LogManager.Info(
                $"Target language changed to {TargetLanguage.Value}/目标语言已更改为 {TargetLanguage.Value}");
            LogManager.Warning(
                "Change this option at runtime may cause problems/运行时修改此选项可能会导致问题，请重启游戏");

            // 更新翻译路径
            TargetLanguePath = Path.Combine(TranslationRootPath, TargetLanguage.Value);
            TranslationTextPath = Path.Combine(TargetLanguePath, "Text");
            TextureReplacePath = Path.Combine(TargetLanguePath, "Texture");
            LyricPath = Path.Combine(TargetLanguePath, "Lyric");
            UIPath = Path.Combine(TargetLanguePath, "UI");
            DumpUIPath = Path.Combine(TranslationRootPath, "DumpUI");
            UITextPath = Path.Combine(UIPath, "Text");
            UISpritePath = Path.Combine(UIPath, "Sprite");
            DumpPath = Path.Combine(TranslationRootPath, "Dump");
            TextDumpPath = Path.Combine(DumpPath, "Text");
            TextureDumpPath = Path.Combine(DumpPath, "Texture");
            SpriteDumpPath = Path.Combine(DumpUIPath, "Sprite");
            TermDumpPath = Path.Combine(DumpUIPath, "UI");
            SubtitleColorsConfigPath = Path.Combine(TranslationRootPath, "SubtitleColors.json");
            // 创建目录
            try
            {
                Directory.CreateDirectory(TargetLanguePath);
                Directory.CreateDirectory(TranslationTextPath);
                Directory.CreateDirectory(TextureReplacePath);
                Directory.CreateDirectory(LyricPath);
                Directory.CreateDirectory(UIPath);
                Directory.CreateDirectory(DumpUIPath);
                Directory.CreateDirectory(UITextPath);
                Directory.CreateDirectory(UISpritePath);
                Directory.CreateDirectory(DumpPath);
                Directory.CreateDirectory(TextDumpPath);
                Directory.CreateDirectory(TextureDumpPath);
                Directory.CreateDirectory(SpriteDumpPath);
                Directory.CreateDirectory(TermDumpPath);
                if (!File.Exists(SubtitleColorsConfigPath))
                    File.WriteAllText(SubtitleColorsConfigPath, "{}");
            }
            catch (Exception e)
            {
                LogManager.Error($"Create translation folder failed/创建翻译文件夹失败: {e.Message}");
            }

            // 重新加载翻译
            if (EnableGeneralTextTranslation.Value)
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
            if (EnableUITextTranslation.Value)
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
        EnableGeneralTextTranslation.SettingChanged += (_, _) =>
        {
            if (EnableGeneralTextTranslation.Value)
            {
                LogManager.Info("Text Translation Enabled/文本翻译已启用");
                TextTranslateManger.Init();
                XUATInterop.Init();
            }
            else
            {
                LogManager.Info("Text Translation Disabled/文本翻译已禁用");
                TextTranslateManger.Unload();
                XUATInterop.Unload();
            }
        };

        // 注册UI文本翻译启用状态变更事件
        EnableUITextTranslation.SettingChanged += (_, _) =>
        {
            if (EnableUITextTranslation.Value)
            {
                LogManager.Info("UI Translation Enabled/UI 文本翻译已启用");
                UITranslateManager.Unload();
                UITranslateManager.Init();
            }
            else
            {
                LogManager.Info("UI Translation Disabled/UI 文本翻译已禁用");
                UITranslateManager.Unload();
                UITranslateManager.Init();
            }
        };

        EnableUITextExtraPatch.SettingChanged += (_, _) =>
        {
            if (EnableUITextExtraPatch.Value)
            {
                LogManager.Info("UI Translation Extra Patch Enabled/UI 文本翻译额外补丁已启用");
                UITranslateManager.Unload();
                UITranslateManager.Init();
            }
            else
            {
                LogManager.Info("UI Translation Extra Patch Disabled/UI 文本翻译额外补丁已禁用");
                UITranslateManager.Unload();
                UITranslateManager.Init();
            }
        };

        // 注册UI精灵图替换启用状态变更事件
        EnableUISpriteReplace.SettingChanged += (_, _) =>
        {
            if (EnableUITextTranslation.Value)
            {
                LogManager.Info("UI Sprite Replace Enabled/UI 精灵图替换已启用");
                UITranslateManager.Unload();
                UITranslateManager.Init();
            }
            else
            {
                LogManager.Info("UI Sprite Replace Disabled/UI 精灵图替换已禁用");
                UITranslateManager.Unload();
                UITranslateManager.Init();
            }
        };

        // 注册贴图替换启用状态变更事件
        EnableTextureReplace.SettingChanged += (_, _) =>
        {
            if (EnableTextureReplace.Value)
            {
                LogManager.Info("Texture Replace Enabled/贴图替换已启用");
                TextureReplaceManger.Unload();
                TextureReplaceManger.Init();
            }
            else
            {
                LogManager.Info("Texture Replace Disabled/贴图替换已禁用");
                TextureReplaceManger.Unload();
                TextureReplaceManger.Init();
            }
        };

        MaidNameStyle.SettingChanged += (_, _) =>
        {
            LogManager.Warning(
                "Not Support change maid name style during runtime, please restart game/不支持在运行时更改角色名字样式，请重启游戏");
        };

        ReloadTranslateResourceShortcut.SettingChanged += (_, _) =>
        {
            LogManager.Info("Reload translate resource shortcut changed/翻译资源快捷键已更改");
        };

        // 注册日志级别变更事件
        LogLevelConfig.SettingChanged += (_, _) =>
        {
            // 不需要重新加载
            LogManager.Info(
                $"Log level changed to {LogLevelConfig.Value}/日志级别已更改为 {LogLevelConfig.Value}");
            if (LogLevelConfig.Value >= LogLevel.Debug)
                LogManager.Info("some debug patches require a game restart/部分 debug 补丁需要重启游戏生效");
        };
    }

    # endregion

    # region RegisterEnableSubtitleEvents

    /// <summary>
    ///     注册字幕启用状态变更事件
    /// </summary>
    private void RegisterEnableSubtitleEvents()
    {
        EnableBaseSubtitle.SettingChanged += (_, _) =>
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
                SubtitleManager.Init();
            }
        };

        EnableYotogiSubtitle.SettingChanged += (_, _) =>
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
                SubtitleManager.Init();
            }
        };

        EnableAdvSubtitle.SettingChanged += (_, _) =>
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
                SubtitleManager.Init();
            }
        };


        ForceEnableAdvSubtitle.SettingChanged += (_, _) =>
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
                SubtitleManager.Init();
            }
        };

        EnableLyricSubtitle.SettingChanged += (_, _) =>
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

        SubtitleSearchDirection.SettingChanged += (_, _) =>
        {
            LogManager.Info(
                $"Subtitle search direction changed to {SubtitleSearchDirection.Value}/字幕搜索方向已更改为 {SubtitleSearchDirection.Value}");
            SubtitleComponentManager.DestroyAllSubtitleComponents();
        };
    }

    # endregion

    # region RegisterBaseSubtitleConfigEvents

    /// <summary>
    ///     注册基础字幕配置变更事件
    /// </summary>
    private void RegisterBaseSubtitleConfigEvents()
    {
        // 注册字幕说话人名启用状态变更事件
        EnableBaseSubtitleSpeakerName.SettingChanged += (_, _) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle Speaker Name Enabled/启用基础字幕显示说话人名");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕字体变更事件
        BaseSubtitleFont.SettingChanged += (_, _) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle font changed/基础字幕字体已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕字体大小变更事件
        BaseSubtitleFontSize.SettingChanged += (_, _) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle font size changed/基础字幕字体大小已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕文本对齐方式变更事件
        BaseSubtitleTextAlignment.SettingChanged += (_, _) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle text alignment changed/基础字幕文本对齐方式已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕颜色变更事件
        BaseSubtitleColor.SettingChanged += (_, _) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle color changed/基础字幕颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕透明度变更事件
        BaseSubtitleOpacity.SettingChanged += (_, _) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle opacity changed/基础字幕透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕背景颜色变更事件
        BaseSubtitleBackgroundColor.SettingChanged += (_, _) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle background color changed/基础字幕背景颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕背景不透明度变更事件
        BaseSubtitleBackgroundOpacity.SettingChanged += (_, _) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle background opacity changed/基础字幕背景不透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕位置变更事件
        BaseSubtitleVerticalPosition.SettingChanged += (_, _) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info(
                    "Base Subtitle vertical position changed, subtitle destroyed, re-trigger subtitle to view change/基础字幕垂直位置已更改，字幕已销毁，重新触发字幕以查看更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
                SubtitleComponentManager.DestroyAllSubtitleComponents();
            }
        };

        // 注册字幕背景宽度变更事件
        BaseSubtitleWidth.SettingChanged += (_, _) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info(
                    "Base Subtitle background width changed, subtitle destroyed, re-trigger subtitle to view change/基础字幕宽度已更改，字幕已销毁，重新触发字幕以查看更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
                SubtitleComponentManager.DestroyAllSubtitleComponents();
            }
        };

        // 注册字幕高度变更事件
        BaseSubtitleHeight.SettingChanged += (_, _) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle height changed/基础字幕高度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
                SubtitleComponentManager.DestroyAllSubtitleComponents();
            }
        };

        // 注册字幕动画变更事件
        EnableBaseSubtitleAnimation.SettingChanged += (_, _) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle animation changed/基础字幕动画已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕淡入时长变更事件
        BaseSubtitleFadeInDuration.SettingChanged += (_, _) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle fade in duration changed/基础字幕淡入时长已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕淡出时长变更事件
        BaseSubtitleFadeOutDuration.SettingChanged += (_, _) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle fade out duration changed/基础字幕淡出时长已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边启用变更事件
        EnableBaseSubtitleOutline.SettingChanged += (_, _) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle outline changed/基础字幕描边已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边颜色变更事件
        BaseSubtitleOutlineColor.SettingChanged += (_, _) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle outline color changed/基础字幕描边颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边不透明度变更事件
        BaseSubtitleOutlineOpacity.SettingChanged += (_, _) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle outline opacity changed/基础字幕描边不透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边宽度变更事件
        BaseSubtitleOutlineWidth.SettingChanged += (_, _) =>
        {
            if (EnableBaseSubtitle.Value)
            {
                LogManager.Info("Base Subtitle outline width changed/基础字幕描边宽度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };
    }

    # endregion

    # region RegisterYotogiSubtitleConfigEvents

    /// <summary>
    ///     注册夜伽字幕配置变更事件
    /// </summary>
    private void RegisterYotogiSubtitleConfigEvents()
    {
        // 注册字幕说话人名启用状态变更事件
        EnableYotogiSubtitleSpeakerName.SettingChanged += (_, _) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle Speaker Name Enabled/启用夜伽字幕显示说话人名");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕字体变更事件
        YotogiSubtitleFont.SettingChanged += (_, _) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle font changed/夜伽字幕字体已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕字体大小变更事件
        YotogiSubtitleFontSize.SettingChanged += (_, _) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle font size changed/夜伽字幕字体大小已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕文本对齐方式变更事件
        YotogiSubtitleTextAlignment.SettingChanged += (_, _) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle text alignment changed/夜伽字幕文本对齐方式已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕颜色变更事件
        YotogiSubtitleColor.SettingChanged += (_, _) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle color changed/夜伽字幕颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕透明度变更事件
        YotogiSubtitleOpacity.SettingChanged += (_, _) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle opacity changed/夜伽字幕透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕背景颜色变更事件
        YotogiSubtitleBackgroundColor.SettingChanged += (_, _) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle background color changed/夜伽字幕背景颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕背景不透明度变更事件
        YotogiSubtitleBackgroundOpacity.SettingChanged += (_, _) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle background opacity changed/夜伽字幕背景不透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕位置变更事件
        YotogiSubtitleVerticalPosition.SettingChanged += (_, _) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle vertical position changed/夜伽字幕垂直位置已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕宽度变更事件
        YotogiSubtitleWidth.SettingChanged += (_, _) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info(
                    "Yotogi Subtitle background width changed, subtitle destroyed, re-trigger subtitle to view change/夜伽字幕宽度已更改，字幕已销毁，重新触发字幕以查看更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
                SubtitleComponentManager.DestroyAllSubtitleComponents();
            }
        };

        // 注册字幕高度变更事件
        YotogiSubtitleHeight.SettingChanged += (_, _) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info(
                    "Yotogi Subtitle height changed, subtitle destroyed, re-trigger subtitle to view change/夜伽字幕高度已更改，字幕已销毁，重新触发字幕以查看更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
                SubtitleComponentManager.DestroyAllSubtitleComponents();
            }
        };

        // 注册字幕动画变更事件
        EnableYotogiSubtitleAnimation.SettingChanged += (_, _) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle animation changed/夜伽字幕动画已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕淡入时长变更事件
        YotogiSubtitleFadeInDuration.SettingChanged += (_, _) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle fade in duration changed/夜伽字幕淡入时长已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕淡出时长变更事件
        YotogiSubtitleFadeOutDuration.SettingChanged += (_, _) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle fade out duration changed/夜伽字幕淡出时长已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边启用变更事件
        EnableYotogiSubtitleOutline.SettingChanged += (_, _) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle outline changed/夜伽字幕描边已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边颜色变更事件
        YotogiSubtitleOutlineColor.SettingChanged += (_, _) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle outline color changed/夜伽字幕描边颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边不透明度变更事件
        YotogiSubtitleOutlineOpacity.SettingChanged += (_, _) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle outline opacity changed/夜伽字幕描边不透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边宽度变更事件
        YotogiSubtitleOutlineWidth.SettingChanged += (_, _) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle outline width changed/夜伽字幕描边宽度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };
    }

    # endregion

    # region RegisterAdvSubtitleConfigEvents

    /// <summary>
    ///     注册Adv字幕配置变更事件
    /// </summary>
    private void RegisterAdvSubtitleConfigEvents()
    {
        // 注册字幕说话人名启用状态变更事件
        EnableAdvSubtitleSpeakerName.SettingChanged += (_, _) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle Speaker Name Enabled/启用Adv字幕显示说话人名");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕字体变更事件
        AdvSubtitleFont.SettingChanged += (_, _) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle font changed/Adv字幕字体已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕字体大小变更事件
        AdvSubtitleFontSize.SettingChanged += (_, _) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle font size changed/Adv字幕字体大小已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕文本对齐方式变更事件
        AdvSubtitleTextAlignment.SettingChanged += (_, _) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle text alignment changed/Adv字幕文本对齐方式已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕颜色变更事件
        AdvSubtitleColor.SettingChanged += (_, _) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle color changed/Adv字幕颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕透明度变更事件
        AdvSubtitleOpacity.SettingChanged += (_, _) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle opacity changed/Adv字幕透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕背景颜色变更事件
        AdvSubtitleBackgroundColor.SettingChanged += (_, _) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle background color changed/Adv字幕背景颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕背景不透明度变更事件
        AdvSubtitleBackgroundOpacity.SettingChanged += (_, _) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle background opacity changed/Adv字幕背景不透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕位置变更事件
        AdvSubtitleVerticalPosition.SettingChanged += (_, _) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle vertical position changed/Adv字幕垂直位置已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕背景宽度变更事件
        AdvSubtitleWidth.SettingChanged += (_, _) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info(
                    "Adv Subtitle background width changed, subtitle destroyed, re-trigger subtitle to view change/Adv字幕宽度已更改，字幕已销毁，重新触发字幕以查看更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
                SubtitleComponentManager.DestroyAllSubtitleComponents();
            }
        };

        // 注册字幕高度变更事件
        AdvSubtitleHeight.SettingChanged += (_, _) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info(
                    "Adv Subtitle height changed, subtitle destroyed, re-trigger subtitle to view change/Adv字幕高度已更改，字幕已销毁，重新触发字幕以查看更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
                SubtitleComponentManager.DestroyAllSubtitleComponents();
            }
        };

        // 注册字幕动画变更事件
        EnableAdvSubtitleAnimation.SettingChanged += (_, _) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle animation changed/Adv字幕动画已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕淡入时长变更事件
        AdvSubtitleFadeInDuration.SettingChanged += (_, _) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle fade in duration changed/Adv字幕淡入时长已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕淡出时长变更事件
        AdvSubtitleFadeOutDuration.SettingChanged += (_, _) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle fade out duration changed/Adv字幕淡出时长已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边启用变更事件
        EnableAdvSubtitleOutline.SettingChanged += (_, _) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle outline changed/Adv字幕描边已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边颜色变更事件
        AdvSubtitleOutlineColor.SettingChanged += (_, _) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle outline color changed/Adv字幕描边颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边不透明度变更事件
        AdvSubtitleOutlineOpacity.SettingChanged += (_, _) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle outline opacity changed/Adv字幕描边不透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边宽度变更事件
        AdvSubtitleOutlineWidth.SettingChanged += (_, _) =>
        {
            if (EnableAdvSubtitle.Value)
            {
                LogManager.Info("Adv Subtitle outline width changed/Adv字幕描边宽度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };
    }

    # endregion

    # region RegisterLyricSubtitleConfigEvents

    /// <summary>
    ///     注册歌词字幕配置变更事件
    /// </summary>
    private void RegisterLyricSubtitleConfigEvents()
    {
        // 注册字幕说话人名启用状态变更事件
        EnableLyricSubtitleSpeakerName.SettingChanged += (_, _) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle Speaker Name Enabled/启用歌词字幕显示说话人名");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕类型变更事件
        LyricSubtitleType.SettingChanged += (_, _) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle type changed/歌词字幕类型已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕字体变更事件
        LyricSubtitleFont.SettingChanged += (_, _) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle font changed/歌词字幕字体已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕字体大小变更事件
        LyricSubtitleFontSize.SettingChanged += (_, _) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle font size changed/歌词字幕字体大小已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕文本对齐方式变更事件
        LyricSubtitleTextAlignment.SettingChanged += (_, _) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle text alignment changed/歌词字幕文本对齐方式已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕颜色变更事件
        LyricSubtitleColor.SettingChanged += (_, _) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle color changed/歌词字幕颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕透明度变更事件
        LyricSubtitleOpacity.SettingChanged += (_, _) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle opacity changed/歌词字幕透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕背景颜色变更事件
        LyricSubtitleBackgroundColor.SettingChanged += (_, _) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle background color changed/歌词字幕背景颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕背景不透明度变更事件
        LyricSubtitleBackgroundOpacity.SettingChanged += (_, _) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle background opacity changed/歌词字幕背景不透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕位置变更事件
        LyricSubtitleVerticalPosition.SettingChanged += (_, _) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle vertical position changed/歌词字幕垂直位置已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕背景宽度变更事件
        LyricSubtitleWidth.SettingChanged += (_, _) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info(
                    "Lyric Subtitle background width changed, subtitle destroyed, re-trigger subtitle to view change/歌词字幕宽度已更改，字幕已销毁，重新触发字幕以查看更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
                SubtitleComponentManager.DestroyAllSubtitleComponents();
            }
        };

        // 注册字幕高度变更事件
        LyricSubtitleHeight.SettingChanged += (_, _) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info(
                    "Lyric Subtitle height changed, subtitle destroyed, re-trigger subtitle to view change/歌词字幕高度已更改，字幕已销毁，重新触发字幕以查看更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
                SubtitleComponentManager.DestroyAllSubtitleComponents();
            }
        };

        // 注册字幕动画变更事件
        EnableLyricSubtitleAnimation.SettingChanged += (_, _) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle animation changed/歌词字幕动画已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕淡入时长变更事件
        LyricSubtitleFadeInDuration.SettingChanged += (_, _) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle fade in duration changed/歌词字幕淡入时长已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕淡出时长变更事件
        LyricSubtitleFadeOutDuration.SettingChanged += (_, _) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle fade out duration changed/歌词字幕淡出时长已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边启用变更事件
        EnableLyricSubtitleOutline.SettingChanged += (_, _) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle outline changed/歌词字幕描边已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边颜色变更事件
        LyricSubtitleOutlineColor.SettingChanged += (_, _) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle outline color changed/歌词字幕描边颜色已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边不透明度变更事件
        LyricSubtitleOutlineOpacity.SettingChanged += (_, _) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle outline opacity changed/歌词字幕描边不透明度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };

        // 注册字幕描边宽度变更事件
        LyricSubtitleOutlineWidth.SettingChanged += (_, _) =>
        {
            if (EnableLyricSubtitle.Value)
            {
                LogManager.Info("Lyric Subtitle outline width changed/歌词字幕描边宽度已更改");
                SubtitleComponentManager.UpdateAllSubtitleConfig();
            }
        };
    }

    # endregion

    # region RegisterVRSubtitleConfigEvents

    /// <summary>
    ///     注册VR字幕配置变更事件
    /// </summary>
    private void RegisterVRSubtitleConfigEvents()
    {
        // 注册VR字幕模式变更事件
        VRSubtitleMode.SettingChanged += (_, _) =>
        {
            LogManager.Info(
                "VR Subtitle mode changed, subtitle destroyed, re-trigger subtitle to view change/VR字幕模式已更改，字幕已销毁，重新触发字幕以查看更改");
            SubtitleComponentManager.UpdateAllSubtitleConfig();
            SubtitleComponentManager.DestroyAllSubtitleComponents();
        };

        // 注册VR空间字幕宽度变更事件
        VRSpaceSubtitleWidth.SettingChanged += (_, _) =>
        {
            LogManager.Info(
                "VR Floating Subtitle Width changed, subtitle destroyed, re-trigger subtitle to view change/VR空间字幕宽度已更改，字幕已销毁，重新触发字幕以查看更改");
            SubtitleComponentManager.UpdateAllSubtitleConfig();
            SubtitleComponentManager.DestroyAllSubtitleComponents();
        };

        // 注册VR空间字幕高度变更事件
        VRSpaceSubtitleHeight.SettingChanged += (_, _) =>
        {
            LogManager.Info(
                "VR Floating Subtitle Height changed, subtitle destroyed, re-trigger subtitle to view change/VR空间字幕高度已更改，字幕已销毁，重新触发字幕以查看更改");
            SubtitleComponentManager.UpdateAllSubtitleConfig();
            SubtitleComponentManager.DestroyAllSubtitleComponents();
        };

        // 注册VR字幕距离变更事件
        VRSpaceSubtitleDistance.SettingChanged += (_, _) =>
        {
            LogManager.Info("VR Floating Subtitle distance changed/VR空间字幕距离已更改");
            SubtitleComponentManager.UpdateAllSubtitleConfig();
        };

        // 注册VR字幕垂直偏移变更事件
        VRSpaceSubtitleVerticalOffset.SettingChanged += (_, _) =>
        {
            LogManager.Info("VR Floating Subtitle vertical offset changed/VR空间字幕垂直偏移已更改");
            SubtitleComponentManager.UpdateAllSubtitleConfig();
        };

        // 注册VR字幕水平偏移变更事件
        VRSpaceSubtitleHorizontalOffset.SettingChanged += (_, _) =>
        {
            LogManager.Info("VR Floating Subtitle horizontal offset changed/VR空间字幕水平偏移已更改");
            SubtitleComponentManager.UpdateAllSubtitleConfig();
        };

        // 注册VR字幕文字大小倍数变更事件
        VRSpaceSubtitleTextSizeMultiplier.SettingChanged += (_, _) =>
        {
            LogManager.Info("VR Floating Subtitle text size multiplier changed/VR空间字幕文字大小倍数已更改");
            SubtitleComponentManager.UpdateAllSubtitleConfig();
        };

        // 注册VR字幕描边缩放因子变更事件
        VRSpaceSubtitleOutlineScaleFactor.SettingChanged += (_, _) =>
        {
            LogManager.Info("VR Floating Subtitle text size multiplier changed/VR空间字幕描边缩放因子已更改");
            SubtitleComponentManager.UpdateAllSubtitleConfig();
        };

        // 注册VR字幕像素完美变更事件
        VRSpaceSubtitlePixelPerfect.SettingChanged += (_, _) =>
        {
            LogManager.Info("VR Floating Subtitle pixel perfect changed/VR空间字幕像素完美已更改");
            SubtitleComponentManager.UpdateAllSubtitleConfig();
        };

        // 注册VR字幕跟随平滑度变更事件
        VRSpaceSubtitleFollowSmoothness.SettingChanged += (_, _) =>
        {
            LogManager.Info("VR Floating Subtitle follow smoothness changed/VR空间字幕跟随平滑度已更改");
            SubtitleComponentManager.UpdateAllSubtitleConfig();
        };

        // 注册VR平板字幕宽度变更事件
        VRTabletSubtitleWidth.SettingChanged += (_, _) =>
        {
            LogManager.Info(
                "VR Floating Subtitle Width changed, subtitle destroyed, re-trigger subtitle to view change/VR平板电脑字幕宽度已更改，字幕已销毁，重新触发字幕以查看更改");
            SubtitleComponentManager.UpdateAllSubtitleConfig();
            SubtitleComponentManager.DestroyAllSubtitleComponents();
        };

        // 注册VR平板字幕高度变更事件
        VRTabletSubtitleHeight.SettingChanged += (_, _) =>
        {
            LogManager.Info(
                "VR Floating Subtitle Height changed, subtitle destroyed, re-trigger subtitle to view change/VR平板电脑字幕高度已更改，字幕已销毁，重新触发字幕以查看更改");
            SubtitleComponentManager.UpdateAllSubtitleConfig();
            SubtitleComponentManager.DestroyAllSubtitleComponents();
        };

        // 注册VR平板字幕垂直位置变更事件
        VRTabletSubtitleVerticalPosition.SettingChanged += (_, _) =>
        {
            LogManager.Info("VR Floating Subtitle Vertical Position changed/VR平板电脑字幕垂直位置已更改");
            SubtitleComponentManager.UpdateAllSubtitleConfig();
        };

        // 注册VR平板字幕水平位置变更事件
        VRTabletSubtitleHorizontalPosition.SettingChanged += (_, _) =>
        {
            LogManager.Info("VR Floating Subtitle Horizontal Position changed/VR平板电脑字幕水平位置已更改");
            SubtitleComponentManager.UpdateAllSubtitleConfig();
        };

        // 注册VR平板字幕文字大小倍数变更事件
        VRTabletSubtitleTextSizeMultiplier.SettingChanged += (_, _) =>
        {
            LogManager.Info("VR Floating Subtitle Text Size Multiplier changed/VR平板电脑字幕文字大小倍数已更改");
            SubtitleComponentManager.UpdateAllSubtitleConfig();
        };

        // 注册VR平板字幕描边缩放因子变更事件
        VRTabletSubtitleOutlineScaleFactor.SettingChanged += (_, _) =>
        {
            LogManager.Info("VR Floating Subtitle Text Size Multiplier changed/VR平板电脑字幕描边缩放因子已更改");
            SubtitleComponentManager.UpdateAllSubtitleConfig();
        };

        // 注册VR平板字幕描边宽度变更事件
        VRTabletSubtitlePixelPerfect.SettingChanged += (_, _) =>
        {
            LogManager.Info("VR Floating Subtitle Pixel Perfect changed/VR平板电脑字幕像素完美已更改");
            SubtitleComponentManager.UpdateAllSubtitleConfig();
        };
    }

    # endregion

    # region RegisterDumpConfigEvents

    /// <summary>
    ///     注册Dump配置变更事件
    /// </summary>
    private void RegisterDumpConfigEvents()
    {
        // 注册Dump文本启用状态变更事件
        EnableTextDump.SettingChanged += (_, _) =>
        {
            if (EnableTextDump.Value)
                LogManager.Info("Text dump enabled/启用文本转储");
            else
                LogManager.Info("Text dump disabled/禁用文本转储");
        };

        // 注册Dump文本阈值变更事件
        TextDumpThreshold.SettingChanged += (_, _) =>
        {
            LogManager.Info("Text dump threshold changed/文本转储阈值已更改");
        };

        // 注册Dump文本立即写出变更事件
        FlushTextDumpNow.SettingChanged += (_, _) =>
        {
            TextTranslateManger.FlushDumpBuffer();
            LogManager.Info("Text dump flushed/文本转储缓存已写出");
        };

        // 注册Dump纹理启用状态变更事件
        EnableTexturesDump.SettingChanged += (_, _) =>
        {
            if (EnableTexturesDump.Value)
                LogManager.Info("Texture dump enabled/启用纹理转储");
            else
                LogManager.Info("Texture dump disabled/禁用纹理转储");
        };

        // 注册DumpSprite启用状态变更事件
        EnableSpriteDump.SettingChanged += (_, _) =>
        {
            if (EnableSpriteDump.Value)
                LogManager.Info("Sprite dump enabled/启用Sprite转储");
            else
                LogManager.Info("Sprite dump disabled/禁用Sprite转储");
        };

        // 注册Dump舞蹈信息变更事件
        EnableDumpDanceInfo.SettingChanged += (_, _) =>
        {
            if (EnableDumpDanceInfo.Value)
                LogManager.Info(
                    "Dance info dump enabled, will be exported when playing dances/启用舞蹈信息转储，播放舞蹈时会转储舞蹈信息");
            else
                LogManager.Info("Dance info dump disabled/禁用舞蹈信息转储");
        };

        // 注册DumpTerm启用状态变更事件
        EnableTermDump.SettingChanged += (_, _) =>
        {
            if (EnableTermDump.Value || EnableUITextTranslation.Value)
            {
                UITranslateManager.Unload();
                UITranslateManager.Init();
                LogManager.Info(
                    "Term dump enabled, will be exported when playing dances/启用Term转储");
            }
            else if (!EnableTermDump.Value && EnableUITextTranslation.Value)
            {
                UITranslateManager.Unload();
                UITranslateManager.Init();
                LogManager.Info("Term dump disabled/禁用term转储");
            }
        };

        // 注册DumpTerm阈值变更事件
        TermDumpThreshold.SettingChanged += (_, _) =>
        {
            LogManager.Info("Term dump threshold changed/Term转储阈值已更改");
        };

        // 注册DumpTerm立即写出变更事件
        FlushTermDumpNow.SettingChanged += (_, _) =>
        {
            UITranslateManager.FlushTermDumpBuffer();
            LogManager.Info("Term dump flushed/Term转储缓存已写出");
        };

        // 注册打印OS字体变更事件
        PrintOSFont.SettingChanged += (_, _) => { FontTool.PrintOSInstalledFontNames(); };
    }

    # endregion

    # region RegisterFixerConfigEvents

    /// <summary>
    ///     注册修复补丁配置变更事件
    /// </summary>
    private void RegisterFixerConfigEvents()
    {
        // 注册修复补丁启用状态变更事件
        EnableMaidCafeDlcLineBreakCommentFix.SettingChanged += (_, _) =>
        {
            if (EnableMaidCafeDlcLineBreakCommentFix.Value)
            {
                LogManager.Info(
                    "Maid Cafe DLC Line Break Comment Fix enabled/女仆咖啡厅DLC的弹幕不移动的修复已启用");
                FixerManger.Unload();
                FixerManger.Init();
            }
            else
            {
                LogManager.Info(
                    "Maid Cafe DLC Line Break Comment Fix disabled/女仆咖啡厅DLC的弹幕不移动的修复已禁用");
                FixerManger.Unload();
                FixerManger.Init();
            }
        };

        EnableNoneCjkFix.SettingChanged += (_, _) =>
        {
            if (EnableNoneCjkFix.Value)
            {
                LogManager.Info("None CJK Fix enabled/非CJK语言修复已启用");
                FixerManger.Unload();
                FixerManger.Init();
            }
            else
            {
                LogManager.Info("None CJK Fix disabled/非CJK语言修复已禁用");
                FixerManger.Unload();
                FixerManger.Init();
            }
        };

        // 注册 UI 字体替换启用状态变更事件
        EnableUIFontReplace.SettingChanged += (_, _) =>
        {
            if (EnableUIFontReplace.Value)
            {
                LogManager.Info("UI font replace enabled/UI字体替换已启用");
                FixerManger.Unload();
                FixerManger.Init();
            }
            else
            {
                LogManager.Info("UI font replace disabled/UI字体替换已禁用");
                FixerManger.Unload();
                FixerManger.Init();
            }
        };

        // 注册 UI 字体变更事件
        UIFont.SettingChanged += (_, _) => { LogManager.Info("UI font changed/UI 字体已变更"); };
    }

    # endregion
}