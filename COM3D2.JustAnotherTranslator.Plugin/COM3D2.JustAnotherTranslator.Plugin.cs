using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using COM3D2.JustAnotherTranslator.Plugin.Translator;
using COM3D2.JustAnotherTranslator.Plugin.Utils;

namespace COM3D2.JustAnotherTranslator.Plugin;

[BepInPlugin("COM3D2.JustAnotherTranslator.Plugin", "COM3D2.JustAnotherTranslator.Plugin", "1.0.0")]
public class JustAnotherTranslator : BaseUnityPlugin
{
    public static ConfigEntry<string> TargetLanguage;
    public static ConfigEntry<bool> EnableTextTranslation;
    public static ConfigEntry<bool> EnableUITranslation;
    public static ConfigEntry<bool> EnableTextureReplace;
    public static ConfigEntry<LogLevel> LogLevelConfig;
    public static ConfigEntry<int> TextureCacheSize;
    public static ConfigEntry<bool> EnableAsyncLoading;

    // 字幕相关配置
    public static ConfigEntry<bool> EnableYotogiSubtitle;
    public static ConfigEntry<bool> EnableYotogiSubtitleSpeakerName;
    public static ConfigEntry<string> YotogiSubtitleFont;
    public static ConfigEntry<int> YotogiSubtitleFontSize;
    public static ConfigEntry<string> YotogiSubtitleColor;
    public static ConfigEntry<float> YotogiSubtitleOpacity;
    public static ConfigEntry<string> YotogiSubtitleBackgroundColor;
    public static ConfigEntry<float> YotogiSubtitleBackgroundOpacity;
    public static ConfigEntry<float> YotogiSubtitlePosition;
    public static ConfigEntry<float> YotogiSubtitleBackgroundHeight;
    public static ConfigEntry<bool> YotogiSubtitleAnimation;
    public static ConfigEntry<float> YotogiSubtitleFadeInDuration;
    public static ConfigEntry<float> YotogiSubtitleFadeOutDuration;
    public static ConfigEntry<bool> EnableYotogiSubtitleOutline;
    public static ConfigEntry<string> YotogiSubtitleOutlineColor;
    public static ConfigEntry<float> YotogiSubtitleOutlineOpacity;
    public static ConfigEntry<float> YotogiSubtitleOutlineWidth;

    // translation folder path
    public static readonly string TranslationRootPath = Paths.BepInExRootPath + "/JustAnotherTranslator";
    public static string TargetLanguePath;
    public static string TranslationTextPath;
    public static string TranslationTexturePath;

    private void Awake()
    {
        // Initialize our LogManager with the BepInEx logger
        LogManager.Initialize(Logger);

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

        YotogiSubtitlePosition = Config.Bind("YotogiSubtitle",
            "YotogiSubtitlePosition/夜伽字幕位置",
            0.965f,
            "Yotogi Subtitle Vertical Position (0-1, 0 is bottom, 1 is top)/夜伽字幕垂直位置（0-1，0为底部，1为顶部）");

        YotogiSubtitleBackgroundHeight = Config.Bind("YotogiSubtitle",
            "YotogiSubtitleBackgroundHeight/夜伽字幕背景高度",
            40f,
            "Yotogi Subtitle Background Height in pixels/夜伽背景幕高度（像素）");

        YotogiSubtitleAnimation = Config.Bind("YotogiSubtitle",
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


        // Initialize modules
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
            YotogiSubtitleManager.Init();
        }
        else
        {
            LogManager.Info("Yotogi Subtitle Disabled/夜伽字幕已禁用");
        }

        // 注册一般配置变更事件
        RegisterGeneralConfigEvents();
        // 注册夜伽字幕配置变更事件
        RegisterYotogiSubtitleConfigEvents();
    }


    private void Start()
    {
        if (EnableTextTranslation.Value)
            // Initialize XUAT interop
            XUATInterop.Initialize();
    }

    private void OnDestroy()
    {
        TextTranslator.Unload();
        UITranslator.Unload();
        TextureReplacer.Unload();
        YotogiSubtitleManager.Unload();
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


    /// <summary>
    ///     注册字幕配置变更事件
    /// </summary>
    private void RegisterYotogiSubtitleConfigEvents()
    {
        // 注册字幕启用状态变更事件
        EnableYotogiSubtitle.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle Enabled/夜伽字幕已启用");
                YotogiSubtitleManager.Init();
            }
            else
            {
                LogManager.Info("Yotogi Subtitle Disabled/夜伽字幕已禁用");
                YotogiSubtitleManager.Unload();
            }
        };

        // 注册字幕说话人名启用状态变更事件
        EnableYotogiSubtitleSpeakerName.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle Speaker Name Enabled/夜伽字幕显示演员名已启用");
                YotogiSubtitleManager.UpdateSubtitleConfig();
            }
        };


        // 注册字幕字体变更事件
        YotogiSubtitleFont.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle font changed/夜伽字幕字体已更改");
                YotogiSubtitleManager.UpdateSubtitleConfig();
            }
        };

        // 注册字幕字体大小变更事件
        YotogiSubtitleFontSize.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle font size changed/夜伽字幕字体大小已更改");
                YotogiSubtitleManager.UpdateSubtitleConfig();
            }
        };

        // 注册字幕颜色变更事件
        YotogiSubtitleColor.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle color changed/夜伽字幕颜色已更改");
                YotogiSubtitleManager.UpdateSubtitleConfig();
            }
        };

        // 注册字幕透明度变更事件
        YotogiSubtitleOpacity.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle opacity changed/夜伽字幕透明度已更改");
                YotogiSubtitleManager.UpdateSubtitleConfig();
            }
        };

        // 注册字幕背景颜色变更事件
        YotogiSubtitleBackgroundColor.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle background color changed/夜伽字幕背景颜色已更改");
                YotogiSubtitleManager.UpdateSubtitleConfig();
            }
        };

        // 注册字幕背景不透明度变更事件
        YotogiSubtitleBackgroundOpacity.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle background opacity changed/夜伽字幕背景不透明度已更改");
                YotogiSubtitleManager.UpdateSubtitleConfig();
            }
        };

        // 注册字幕位置变更事件
        YotogiSubtitlePosition.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle position changed/夜伽字幕位置已更改");
                YotogiSubtitleManager.UpdateSubtitleConfig();
            }
        };

        // 注册字幕高度变更事件
        YotogiSubtitleBackgroundHeight.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle height changed/夜伽字幕高度已更改");
                YotogiSubtitleManager.UpdateSubtitleConfig();
            }
        };

        // 注册字幕动画变更事件
        YotogiSubtitleAnimation.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle animation changed/夜伽字幕动画已更改");
                YotogiSubtitleManager.UpdateSubtitleConfig();
            }
        };

        // 注册字幕淡入时长变更事件
        YotogiSubtitleFadeInDuration.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle fade in duration changed/夜伽字幕淡入时长已更改");
                YotogiSubtitleManager.UpdateSubtitleConfig();
            }
        };

        // 注册字幕淡出时长变更事件
        YotogiSubtitleFadeOutDuration.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle fade out duration changed/夜伽字幕淡出时长已更改");
                YotogiSubtitleManager.UpdateSubtitleConfig();
            }
        };

        // 注册字幕描边变更事件
        EnableYotogiSubtitleOutline.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle outline changed/夜伽字幕描边已更改");
                YotogiSubtitleManager.UpdateSubtitleConfig();
            }
        };

        // 注册字幕描边颜色变更事件
        YotogiSubtitleOutlineColor.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle outline color changed/夜伽字幕描边颜色已更改");
                YotogiSubtitleManager.UpdateSubtitleConfig();
            }
        };

        // 注册字幕描边不透明度变更事件
        YotogiSubtitleOutlineOpacity.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle outline opacity changed/夜伽字幕描边不透明度已更改");
                YotogiSubtitleManager.UpdateSubtitleConfig();
            }
        };

        // 注册字幕描边宽度变更事件
        YotogiSubtitleOutlineWidth.SettingChanged += (sender, args) =>
        {
            if (EnableYotogiSubtitle.Value)
            {
                LogManager.Info("Yotogi Subtitle outline width changed/夜伽字幕描边宽度已更改");
                YotogiSubtitleManager.UpdateSubtitleConfig();
            }
        };
    }
}