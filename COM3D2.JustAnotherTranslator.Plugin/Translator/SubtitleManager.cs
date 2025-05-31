using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;
using COM3D2.JustAnotherTranslator.Plugin.Hooks;
using COM3D2.JustAnotherTranslator.Plugin.Subtitle;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace COM3D2.JustAnotherTranslator.Plugin.Translator;

public static class SubtitleManager
{
    private static bool _initialized;
    private static Dictionary<JustAnotherTranslator.SubtitleTypeEnum, SubtitleConfig> _subtitleConfigs;

    // 存储每个Maid的监听协程ID
    private static readonly Dictionary<Maid, string> MaidMonitorCoroutineIds = new();

    // 存储voiceId与文本的映射关系
    public static readonly Dictionary<string, string> VoiceIdToTextMap = new();

    // 当前正在说话的角色
    public static Maid CurrentSpeaker;

    // 当前正在播放的语音ID
    public static string CurrentVoiceId;

    private static Harmony _yotogiSubtitlePatch;

    private static Harmony _advSubtitlePatch;

    private static Harmony _baseVoiceSubtitlePatch;

    private static Harmony _debugPatch;

    private static Harmony _privateMaidTouchSubtitlePatch;

    private static Harmony _vrTouchSubtitlePatch;

    public static void Init()
    {
        if (_initialized) return;

        // 从插件配置中获取字幕配置
        GetConfigFromPluginConfig();

        // 基础字幕补丁
        if (JustAnotherTranslator.EnableBaseSubtitle.Value)
        {
            _baseVoiceSubtitlePatch = Harmony.CreateAndPatchAll(typeof(BaseVoiceSubtitlePatch));
            _privateMaidTouchSubtitlePatch = Harmony.CreateAndPatchAll(typeof(PrivateMaidTouchSubtitlePatch));
            _vrTouchSubtitlePatch = Harmony.CreateAndPatchAll(typeof(VRTouchSubtitlePatch));
        }

        // Yotogi 字幕补丁
        if (JustAnotherTranslator.EnableYotogiSubtitle.Value)
            _yotogiSubtitlePatch = Harmony.CreateAndPatchAll(typeof(YotogiSubtitlePatch));

        // ADV 字幕补丁
        // ADV 模式自带字幕，因此非 VR 模式下几乎没有意义
        if ((JustAnotherTranslator.IsVrMode && JustAnotherTranslator.EnableAdvSubtitle.Value) ||
            JustAnotherTranslator.ForceEnableAdvSubtitle.Value)
            _advSubtitlePatch = Harmony.CreateAndPatchAll(typeof(AdvSubtitlePatch));

        if (JustAnotherTranslator.LogLevelConfig.Value >= LogLevel.Debug)
            _debugPatch = Harmony.CreateAndPatchAll(typeof(DebugPatch));

        SceneManager.sceneUnloaded += OnSceneChange;

        _initialized = true;
    }

    public static void Unload()
    {
        if (!_initialized) return;

        _yotogiSubtitlePatch?.UnpatchSelf();
        _yotogiSubtitlePatch = null;
        _advSubtitlePatch?.UnpatchSelf();
        _advSubtitlePatch = null;
        _baseVoiceSubtitlePatch?.UnpatchSelf();
        _baseVoiceSubtitlePatch = null;
        _debugPatch?.UnpatchSelf();
        _debugPatch = null;
        _privateMaidTouchSubtitlePatch?.UnpatchSelf();
        _privateMaidTouchSubtitlePatch = null;
        _vrTouchSubtitlePatch?.UnpatchSelf();
        _vrTouchSubtitlePatch = null;

        _subtitleConfigs = null;

        SceneManager.sceneUnloaded -= OnSceneChange;

        CleanupAllCoroutines();

        _initialized = false;
    }


    /// <summary>
    ///     更新字幕配置
    /// </summary>
    public static void UpdateSubtitleConfig()
    {
        // 如果尚未初始化，则先初始化
        if (!_initialized) Init();

        // 更新字幕配置
        GetConfigFromPluginConfig();

        // 更新所有字幕的配置（直接在组件上应用新配置）
        foreach (var subtitle in SubtitleComponentManager.GetAllSubtitles())
        {
            // 根据字幕类型获取对应的配置
            var config = _subtitleConfigs[JustAnotherTranslator.SubtitleType.Value];

            subtitle.UpdateConfig(config);
        }

        LogManager.Info("All subtitle configs updated/所有字幕配置已更新");
    }


    // 显示字幕
    private static void ShowSubtitle(string text, Maid maid)
    {
        if (string.IsNullOrEmpty(text) || maid is null)
            return;

        var speakerName = MaidInfo.GetMaidFullName(maid);

        // 获取对应字幕类型的配置
        var config = _subtitleConfigs[JustAnotherTranslator.SubtitleType.Value];

        SubtitleComponentManager.ShowFloatingSubtitle(text, speakerName, 0f, config);

        LogManager.Debug($"Showing subtitle for {speakerName}: {text}");
    }


    // 隐藏字幕
    private static void HideSubtitle(Maid maid)
    {
        if (maid is null)
            return;

        var speakerName = MaidInfo.GetMaidFullName(maid);
        SubtitleComponentManager.HideSubtitle(SubtitleComponentManager.GetSpeakerSubtitleId(speakerName));

        LogManager.Debug($"Hiding subtitle for {speakerName}");
    }


    // 从插件配置中获取字幕配置
    private static void GetConfigFromPluginConfig()
    {
        _subtitleConfigs = new Dictionary<JustAnotherTranslator.SubtitleTypeEnum, SubtitleConfig>
        {
            {
                JustAnotherTranslator.SubtitleTypeEnum.Base,
                CreateSubtitleConfig(JustAnotherTranslator.SubtitleTypeEnum.Base)
            },
            {
                JustAnotherTranslator.SubtitleTypeEnum.Yotogi,
                CreateSubtitleConfig(JustAnotherTranslator.SubtitleTypeEnum.Yotogi)
            },
            {
                JustAnotherTranslator.SubtitleTypeEnum.ADV,
                CreateSubtitleConfig(JustAnotherTranslator.SubtitleTypeEnum.ADV)
            }
        };
    }

    // 从插件配置中创建字幕配置
    private static SubtitleConfig CreateSubtitleConfig(JustAnotherTranslator.SubtitleTypeEnum subtitleType)
    {
        // 初始化字幕配置
        var config = new SubtitleConfig
        {
            SubtitleType = subtitleType,

            // 基本设置
            EnableSpeakerName = subtitleType switch
            {
                JustAnotherTranslator.SubtitleTypeEnum.Base =>
                    JustAnotherTranslator.EnableBaseSubtitleSpeakerName.Value,
                JustAnotherTranslator.SubtitleTypeEnum.Yotogi => JustAnotherTranslator.EnableYotogiSubtitleSpeakerName
                    .Value,
                JustAnotherTranslator.SubtitleTypeEnum.ADV => JustAnotherTranslator.EnableAdvSubtitleSpeakerName.Value,
                _ => false
            },

            // 文本样式
            FontName = "Arial.ttf", // 使用Unity内置字体 //TODO 支持自定义字体
            FontSize = subtitleType switch
            {
                JustAnotherTranslator.SubtitleTypeEnum.Base => JustAnotherTranslator.BaseSubtitleFontSize.Value,
                JustAnotherTranslator.SubtitleTypeEnum.Yotogi => JustAnotherTranslator.YotogiSubtitleFontSize.Value,
                JustAnotherTranslator.SubtitleTypeEnum.ADV => JustAnotherTranslator.AdvSubtitleFontSize.Value,
                _ => 24
            },
            TextColor = subtitleType switch
            {
                JustAnotherTranslator.SubtitleTypeEnum.Base =>
                    ParseColor(JustAnotherTranslator.BaseSubtitleColor.Value),
                JustAnotherTranslator.SubtitleTypeEnum.Yotogi => ParseColor(JustAnotherTranslator.YotogiSubtitleColor
                    .Value),
                JustAnotherTranslator.SubtitleTypeEnum.ADV => ParseColor(JustAnotherTranslator.AdvSubtitleColor.Value),
                _ => Color.white
            },
            TextAlignment = TextAnchor.MiddleCenter,

            // 背景样式
            BackgroundColor = subtitleType switch
            {
                JustAnotherTranslator.SubtitleTypeEnum.Base => ParseColor(JustAnotherTranslator
                    .BaseSubtitleBackgroundColor.Value),
                JustAnotherTranslator.SubtitleTypeEnum.Yotogi => ParseColor(JustAnotherTranslator
                    .YotogiSubtitleBackgroundColor.Value),
                JustAnotherTranslator.SubtitleTypeEnum.ADV => ParseColor(JustAnotherTranslator
                    .AdvSubtitleBackgroundColor.Value),
                _ => Color.black
            },
            BackgroundOpacity = subtitleType switch
            {
                JustAnotherTranslator.SubtitleTypeEnum.Base =>
                    JustAnotherTranslator.BaseSubtitleBackgroundOpacity.Value,
                JustAnotherTranslator.SubtitleTypeEnum.Yotogi => JustAnotherTranslator.YotogiSubtitleBackgroundOpacity
                    .Value,
                JustAnotherTranslator.SubtitleTypeEnum.ADV => JustAnotherTranslator.AdvSubtitleBackgroundOpacity.Value,
                _ => 0.5f
            },
            BackgroundHeight = subtitleType switch
            {
                JustAnotherTranslator.SubtitleTypeEnum.Base => JustAnotherTranslator.BaseSubtitleBackgroundHeight.Value,
                JustAnotherTranslator.SubtitleTypeEnum.Yotogi => JustAnotherTranslator.YotogiSubtitleBackgroundHeight
                    .Value,
                JustAnotherTranslator.SubtitleTypeEnum.ADV => JustAnotherTranslator.AdvSubtitleBackgroundHeight.Value,
                _ => 0.1f
            },
            VerticalPosition = subtitleType switch
            {
                JustAnotherTranslator.SubtitleTypeEnum.Base => JustAnotherTranslator.BaseSubtitleVerticalPosition.Value,
                JustAnotherTranslator.SubtitleTypeEnum.Yotogi => JustAnotherTranslator.YotogiSubtitleVerticalPosition
                    .Value,
                JustAnotherTranslator.SubtitleTypeEnum.ADV => JustAnotherTranslator.AdvSubtitleVerticalPosition.Value,
                _ => 0.85f
            },

            // 动画效果
            EnableAnimation = subtitleType switch
            {
                JustAnotherTranslator.SubtitleTypeEnum.Base => JustAnotherTranslator.BaseSubtitleAnimation.Value,
                JustAnotherTranslator.SubtitleTypeEnum.Yotogi => JustAnotherTranslator.YotogiSubtitleAnimation.Value,
                JustAnotherTranslator.SubtitleTypeEnum.ADV => JustAnotherTranslator.AdvSubtitleAnimation.Value,
                _ => true
            },
            FadeInDuration = subtitleType switch
            {
                JustAnotherTranslator.SubtitleTypeEnum.Base => JustAnotherTranslator.BaseSubtitleFadeInDuration.Value,
                JustAnotherTranslator.SubtitleTypeEnum.Yotogi => JustAnotherTranslator.YotogiSubtitleFadeInDuration
                    .Value,
                JustAnotherTranslator.SubtitleTypeEnum.ADV => JustAnotherTranslator.AdvSubtitleFadeInDuration.Value,
                _ => 0.3f
            },
            FadeOutDuration = subtitleType switch
            {
                JustAnotherTranslator.SubtitleTypeEnum.Base => JustAnotherTranslator.BaseSubtitleFadeOutDuration.Value,
                JustAnotherTranslator.SubtitleTypeEnum.Yotogi => JustAnotherTranslator.YotogiSubtitleFadeOutDuration
                    .Value,
                JustAnotherTranslator.SubtitleTypeEnum.ADV => JustAnotherTranslator.AdvSubtitleFadeOutDuration.Value,
                _ => 0.3f
            },

            // 描边效果
            OutlineEnabled = subtitleType switch
            {
                JustAnotherTranslator.SubtitleTypeEnum.Base => JustAnotherTranslator.EnableBaseSubtitleOutline.Value,
                JustAnotherTranslator.SubtitleTypeEnum.Yotogi =>
                    JustAnotherTranslator.EnableYotogiSubtitleOutline.Value,
                JustAnotherTranslator.SubtitleTypeEnum.ADV => JustAnotherTranslator.EnableAdvSubtitleOutline.Value,
                _ => true
            },
            OutlineColor = subtitleType switch
            {
                JustAnotherTranslator.SubtitleTypeEnum.Base => ParseColor(JustAnotherTranslator.BaseSubtitleOutlineColor
                    .Value),
                JustAnotherTranslator.SubtitleTypeEnum.Yotogi => ParseColor(JustAnotherTranslator
                    .YotogiSubtitleOutlineColor.Value),
                JustAnotherTranslator.SubtitleTypeEnum.ADV => ParseColor(JustAnotherTranslator.AdvSubtitleOutlineColor
                    .Value),
                _ => Color.black
            },
            OutlineWidth = subtitleType switch
            {
                JustAnotherTranslator.SubtitleTypeEnum.Base => JustAnotherTranslator.BaseSubtitleOutlineWidth.Value,
                JustAnotherTranslator.SubtitleTypeEnum.Yotogi => JustAnotherTranslator.YotogiSubtitleOutlineWidth.Value,
                JustAnotherTranslator.SubtitleTypeEnum.ADV => JustAnotherTranslator.AdvSubtitleOutlineWidth.Value,
                _ => 2f
            },

            // VR相关设置
            VRSubtitleMode = JustAnotherTranslator.VRSubtitleMode.Value,
            VRSubtitleDistance = JustAnotherTranslator.VRSubtitleDistance.Value,
            VRSubtitleVerticalOffset = JustAnotherTranslator.VRSubtitleVerticalOffset.Value,
            VRSubtitleHorizontalOffset = JustAnotherTranslator.VRSubtitleHorizontalOffset.Value,
            VRSubtitleWidth = JustAnotherTranslator.VRSubtitleWidth.Value,

            // 参考分辨率
            ReferenceWidth = 1920,
            ReferenceHeight = 1080,
            MatchWidthOrHeight = 0.5f
        };

        return config;
    }

    // 启动Maid监听协程
    public static void StartMaidMonitoringCoroutine(Maid maid)
    {
        if (maid is null)
            return;

        // MaidCafe 模式游戏自带字幕，且由于其脚本编写是一整个音频文件，通过 wait 等待播放，因此无法简单可靠获取文本，应当交由游戏自行处理
        // 见 stream001_movie_0001.ks
        // 见 SetStreamingMessageText(LocalizationString value)
        if (MaidCafeManagerHelper.IsStreamingPart())
        {
            LogManager.Debug("MaidCafe mode detected, skipping subtitle display");
            StopMaidMonitoringCoroutine(maid);
            return;
        }

        // 如果已经有协程在运行，则不再创建新的协程
        if (MaidMonitorCoroutineIds.ContainsKey(maid) && !string.IsNullOrEmpty(MaidMonitorCoroutineIds[maid]))
            return;

        // 启动新的协程监控Maid的语音播放状态
        var coroutineId = CoroutineManager.LaunchCoroutine(MonitorMaidVoicePlayback(maid));
        MaidMonitorCoroutineIds[maid] = coroutineId;

        LogManager.Debug($"Started monitoring coroutine for Maid: {MaidInfo.GetMaidFullName(maid)}, ID: {coroutineId}");
    }

    // 停止Maid监听协程
    public static void StopMaidMonitoringCoroutine(Maid maid)
    {
        if (maid is null || !MaidMonitorCoroutineIds.ContainsKey(maid))
            return;

        var coroutineId = MaidMonitorCoroutineIds[maid];
        if (!string.IsNullOrEmpty(coroutineId)) CoroutineManager.StopCoroutine(coroutineId);

        MaidMonitorCoroutineIds.Remove(maid);

        LogManager.Debug($"Stopped monitoring coroutine for Maid: {MaidInfo.GetMaidFullName(maid)}");
    }

    // 监控Maid语音播放状态的协程
    public static IEnumerator MonitorMaidVoicePlayback(Maid maid)
    {
        var foundText = false;
        var lastPlayingVoiceId = string.Empty;

        LogManager.Debug($"MonitorMaidVoicePlayback started for: {MaidInfo.GetMaidFullName(maid)}");

        while (maid is not null && maid.Visible)
        {
            // 检查maid和AudioMan是否为null
            if (maid.AudioMan is null)
            {
                LogManager.Debug("Maid AudioMan is null, waiting...");
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            var isPlaying = maid.AudioMan.isPlay();
            var currentFileName = maid.AudioMan.FileName;
            var currentVoiceId = !string.IsNullOrEmpty(currentFileName)
                ? Path.GetFileNameWithoutExtension(currentFileName)
                : string.Empty;

            // 检测语音切换
            var voiceChanged = currentVoiceId != lastPlayingVoiceId;

            // 检测播放状态变化
            if (isPlaying)
            {
                // 如果语音ID发生变化，重置foundText状态
                if (voiceChanged)
                {
                    foundText = false;
                    // 如果上一个语音还没结束就切换了，先隐藏之前的字幕
                    if (!string.IsNullOrEmpty(lastPlayingVoiceId))
                        LogManager.Debug(
                            $"Voice changed from {lastPlayingVoiceId} to {currentVoiceId}, hiding previous subtitle");
                    LogManager.Debug(
                        $"Maid {MaidInfo.GetMaidFullName(maid)} is now playing new voice: {currentVoiceId}");
                }

                // 只有未找到文本时才尝试查找和显示
                if (!foundText)
                {
                    // 检查是否有对应的文本
                    if (!string.IsNullOrEmpty(currentVoiceId) && VoiceIdToTextMap.ContainsKey(currentVoiceId))
                    {
                        var text = VoiceIdToTextMap[currentVoiceId];
                        LogManager.Debug(
                            $"Found text for voice {currentVoiceId}: {text}, showing subtitle (form text cache)");

                        // 显示字幕
                        ShowSubtitle(text, maid);
                        foundText = true;
                    }
                    // 尝试直接按 voiceId 获取翻译
                    else if (TextTranslator.GetTranslateText(currentVoiceId, out var translateText))
                    {
                        VoiceIdToTextMap[currentVoiceId] = translateText;
                        LogManager.Debug(
                            $"Found text for voice {currentVoiceId}: {translateText}, showing subtitle (form text translator)");
                        ShowSubtitle(translateText, maid);
                        foundText = true;
                    }
                    else
                    {
                        LogManager.Debug($"No text found for voice {currentVoiceId}, waiting...");
                    }
                }
            }
            else
            {
                // 语音停止播放
                // 语音可能重复播放，等待一段时间后再检查
                if (foundText)
                {
                    yield return new WaitForSeconds(0.1f);
                    if (currentVoiceId == lastPlayingVoiceId)
                    {
                        LogManager.Debug($"Voice {lastPlayingVoiceId} stopped playing, hiding subtitle");
                        HideSubtitle(maid);
                        foundText = false;
                    }
                    else
                    {
                        LogManager.Debug(
                            $"Voice {lastPlayingVoiceId} stopped playing, but current voice is {currentVoiceId}, not hiding subtitle");
                    }
                }
            }

            // 更新上次播放的语音ID
            lastPlayingVoiceId = currentVoiceId;

            // 未找到文本时更快检查
            if (isPlaying && !foundText)
                yield return new WaitForSeconds(0.05f);
            else
                yield return new WaitForSeconds(0.1f);
        }

        // Maid不可见或为null，停止协程
        if (maid is null || !maid.Visible)
        {
            if (maid is not null) HideSubtitle(maid);

            MaidMonitorCoroutineIds.Remove(maid);
            // 重新创建字幕以重新排序
            UpdateSubtitleConfig();
        }

        LogManager.Debug($"MonitorMaidVoicePlayback ended for Maid {MaidInfo.GetMaidFullName(maid)}");
    }

    // 清理所有协程和资源
    public static void CleanupAllCoroutines()
    {
        foreach (var pair in MaidMonitorCoroutineIds)
        {
            if (pair.Key is not null)
                HideSubtitle(pair.Key);

            if (!string.IsNullOrEmpty(pair.Value))
                CoroutineManager.StopCoroutine(pair.Value);
        }

        MaidMonitorCoroutineIds.Clear();
        VoiceIdToTextMap.Clear();
        CurrentSpeaker = null;
        CurrentVoiceId = null;

        LogManager.Debug("All coroutines and resources cleaned up");
    }

    private static void OnSceneChange(Scene scene)
    {
        LogManager.Debug("OnSceneChange called");
        CleanupAllCoroutines();
    }

    // 将颜色字符串解析为Color对象
    private static Color ParseColor(string colorString)
    {
        Color color;
        if (ColorUtility.TryParseHtmlString(colorString, out color))
            return color;
        return Color.white; // 默认颜色
    }
}