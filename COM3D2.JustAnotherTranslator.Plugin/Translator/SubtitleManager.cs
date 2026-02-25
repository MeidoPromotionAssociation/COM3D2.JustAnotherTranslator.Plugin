using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;
using COM3D2.JustAnotherTranslator.Plugin.Hooks.Subtitle;
using COM3D2.JustAnotherTranslator.Plugin.Subtitle;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;

namespace COM3D2.JustAnotherTranslator.Plugin.Translator;

/// <summary>
///     字幕管理器，负责管理语音监控和补丁应用
/// </summary>
public static class SubtitleManager
{
    private static bool _initialized;

    /// 存储每个 Maid 的监听协程 ID
    private static readonly Dictionary<Maid, string>
        MaidMonitorCoroutineIds = new(); // maid -> coroutineId

    /// 存储 voiceId 与文本的映射关系
    private static readonly Dictionary<string, string> VoiceIdToTextMap = new(); // voiceId -> text

    /// 储存说话者颜色及字幕颜色配置
    private static Dictionary<string, SpeakerColorConfig>
        _colorConfig = new(); // maid name -> config

    /// 当前正在说话的角色
    private static Maid _currentSpeaker;

    /// 当前正在播放的语音ID
    private static string _currentVoiceId;

    /// 当前字幕类型
    private static JustAnotherTranslator.SubtitleTypeEnum _currentSubtitleType;

    /// 各种补丁实例
    private static Harmony _yotogiSubtitlePatch;

    private static Harmony _advSubtitlePatch;
    private static Harmony _baseVoiceSubtitlePatch;
    private static Harmony _debugPatch;
    private static Harmony _privateMaidTouchSubtitlePatch;
    private static Harmony _vrTouchSubtitlePatch;

    /// <summary>
    ///     初始化字幕管理器
    /// </summary>
    public static void Init()
    {
        if (_initialized) return;

        // 初始化字幕组件管理器
        SubtitleComponentManager.Init();

        LoadColorConfig();

        // 应用补丁
        ApplySubtitlePatches();

        // 注册场景变更事件
        SceneManager.sceneUnloaded += OnSceneUnloaded;

        _initialized = true;
    }

    /// <summary>
    ///     卸载字幕管理器
    /// </summary>
    public static void Unload()
    {
        if (!_initialized) return;

        // 卸载字幕补丁
        UnloadAllSubtitlePatches();

        // 取消事件订阅
        SceneManager.sceneUnloaded -= OnSceneUnloaded;

        // 清理颜色配置，注意放在 CleanResources 会导致场景切换时被清理
        _colorConfig.Clear();

        // 清理资源
        CleanResources();

        _initialized = false;
    }

    /// <summary>
    ///     场景切换时的处理
    /// </summary>
    private static void OnSceneUnloaded(Scene scene)
    {
        try
        {
            LogManager.Debug($"Scene {scene.name} - {scene.buildIndex}, unloaded  cleaning up");
            CleanResources();
        }
        catch (Exception e)
        {
            LogManager.Error($"Error during cleanup scene resources/清理场景资源失败: {e.Message}");
        }
    }


    /// <summary>
    ///     应用字幕补丁
    /// </summary>
    private static void ApplySubtitlePatches()
    {
        // 基础字幕补丁
        if (JustAnotherTranslator.EnableBaseSubtitle.Value)
        {
            _baseVoiceSubtitlePatch = Harmony.CreateAndPatchAll(typeof(BaseVoiceSubtitlePatch),
                "github.meidopromotionassociation.com3d2.justanothertranslator.plugin.hooks.subtitle.basevoicesubtitlepatch");
            _privateMaidTouchSubtitlePatch = Harmony.CreateAndPatchAll(
                typeof(PrivateMaidTouchSubtitlePatch),
                "github.meidopromotionassociation.com3d2.justanothertranslator.plugin.hooks.subtitle.privatemaidthouchsubtitlepatch");
            _vrTouchSubtitlePatch = Harmony.CreateAndPatchAll(typeof(VRTouchSubtitlePatch),
                "github.meidopromotionassociation.com3d2.justanothertranslator.plugin.hooks.subtitle.vrtouchsubtitlepatch");
        }

        // Yotogi 字幕补丁
        if (JustAnotherTranslator.EnableYotogiSubtitle.Value)
            _yotogiSubtitlePatch = Harmony.CreateAndPatchAll(typeof(YotogiSubtitlePatch),
                "github.meidopromotionassociation.com3d2.justanothertranslator.plugin.hooks.subtitle.yotogisubtitlepatch");

        // ADV 字幕补丁
        // ADV 模式自带字幕，因此非 VR 模式下几乎没有意义
        if ((JustAnotherTranslator.IsVrMode && JustAnotherTranslator.EnableAdvSubtitle.Value) ||
            JustAnotherTranslator.ForceEnableAdvSubtitle.Value)
            _advSubtitlePatch = Harmony.CreateAndPatchAll(typeof(AdvSubtitlePatch),
                "github.meidopromotionassociation.com3d2.justanothertranslator.plugin.hooks.subtitle.advsubtitlepatch");

        // 调试补丁
        if (JustAnotherTranslator.LogLevelConfig.Value >= LogLevel.Debug)
            _debugPatch = Harmony.CreateAndPatchAll(typeof(SubtitleDebugPatch),
                "github.meidopromotionassociation.com3d2.justanothertranslator.plugin.hooks.subtitle.subtitledebugpatch");
    }


    /// <summary>
    ///     卸载字幕补丁
    /// </summary>
    private static void UnloadAllSubtitlePatches()
    {
        var patches = new[]
        {
            _yotogiSubtitlePatch,
            _advSubtitlePatch,
            _baseVoiceSubtitlePatch,
            _debugPatch,
            _privateMaidTouchSubtitlePatch,
            _vrTouchSubtitlePatch
        };

        foreach (var patch in patches) patch?.UnpatchSelf();

        _yotogiSubtitlePatch = null;
        _advSubtitlePatch = null;
        _baseVoiceSubtitlePatch = null;
        _debugPatch = null;
        _privateMaidTouchSubtitlePatch = null;
        _vrTouchSubtitlePatch = null;
    }

    /// <summary>
    ///     清理资源
    /// </summary>
    private static void CleanResources()
    {
        // 清理所有 Maid 监听协程
        CleanupAllMaidMonitorCoroutines();

        // 销毁所有字幕
        SubtitleComponentManager.DestroyAllSubtitleComponents();

        // 清空映射
        VoiceIdToTextMap.Clear();
        _currentSpeaker = null;
        _currentVoiceId = null;
    }


    /// <summary>
    ///     启动Maid监听协程
    /// </summary>
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
        if (MaidMonitorCoroutineIds.ContainsKey(maid) &&
            !string.IsNullOrEmpty(MaidMonitorCoroutineIds[maid]))
            return;

        // 启动新的协程监控 Maid 的语音播放状态
        try
        {
            var coroutineId = CoroutineManager.LaunchCoroutine(MonitorMaidVoicePlayback(maid));
            MaidMonitorCoroutineIds[maid] = coroutineId;
            LogManager.Debug(
                $"Started monitoring coroutine for Maid: {MaidInfo.GetMaidFullName(maid)}, ID: {coroutineId}");
        }
        catch (Exception ex)
        {
            LogManager.Error(
                $"Failed to start monitoring coroutine for Maid: {MaidInfo.GetMaidFullName(maid)}, {ex.Message}");
        }
    }

    /// <summary>
    ///     停止某个 Maid 的监听协程
    /// </summary>
    public static void StopMaidMonitoringCoroutine(Maid maid)
    {
        if (maid is null || !MaidMonitorCoroutineIds.ContainsKey(maid))
            return;

        var coroutineId = MaidMonitorCoroutineIds[maid];
        if (!string.IsNullOrEmpty(coroutineId)) CoroutineManager.StopCoroutine(coroutineId);

        MaidMonitorCoroutineIds.Remove(maid);

        LogManager.Debug(
            $"Stopped monitoring coroutine for Maid: {MaidInfo.GetMaidFullName(maid)}");
    }


    /// <summary>
    ///     清理所有 Maid 监听协程
    /// </summary>
    private static void CleanupAllMaidMonitorCoroutines()
    {
        // 清理所有Maid监听协程
        if (MaidMonitorCoroutineIds.Count > 0)
        {
            foreach (var id in MaidMonitorCoroutineIds.Values) CoroutineManager.StopCoroutine(id);

            MaidMonitorCoroutineIds.Clear();
        }
    }


    /// <summary>
    ///     监控 Maid 语音播放状态的协程
    /// </summary>
    public static IEnumerator MonitorMaidVoicePlayback(Maid maid)
    {
        var foundText = false;
        var lastPlayingVoiceId = string.Empty;
        var speakerName = MaidInfo.GetMaidFullName(maid);

        LogManager.Debug($"MonitorMaidVoicePlayback started for: {speakerName}");

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
                    // 如果上一个语音还没结束就切换了
                    if (!string.IsNullOrEmpty(lastPlayingVoiceId))
                        LogManager.Debug(
                            $"Voice changed from {lastPlayingVoiceId} to {currentVoiceId}");
                    //SubtitleComponentManager.HideSubtitleBySpeakerName(speakerName);
                    LogManager.Debug(
                        $"Maid {speakerName} is now playing new voice: {currentVoiceId}");
                }

                // 只有未找到文本时才尝试查找和显示
                if (!foundText)
                {
                    // 检查是否有对应的文本
                    if (!string.IsNullOrEmpty(currentVoiceId) &&
                        VoiceIdToTextMap.ContainsKey(currentVoiceId))
                    {
                        var text = VoiceIdToTextMap[currentVoiceId];
                        LogManager.Debug(
                            $"Found text for voice {currentVoiceId} -> {text}, showing subtitle (form text cache), currentSubtitleType={_currentSubtitleType}");

                        // 显示字幕
                        SubtitleComponentManager.ShowSubtitle(text, speakerName, 0,
                            _currentSubtitleType);
                        foundText = true;
                    }
                    // 尝试直接按 voiceId 获取翻译
                    else if (TextTranslateManger.GetTranslateText(currentVoiceId,
                                 out var translateText))
                    {
                        VoiceIdToTextMap[currentVoiceId] = translateText;

                        LogManager.Debug(
                            $"Found text for voice {currentVoiceId} -> {translateText}, showing subtitle (form text translator), currentSubtitleType={_currentSubtitleType}");
                        SubtitleComponentManager.ShowSubtitle(translateText, speakerName, 0,
                            _currentSubtitleType);
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
                        LogManager.Debug(
                            $"Voice {lastPlayingVoiceId} stopped playing, hiding subtitle");
                        SubtitleComponentManager.HideSubtitleBySpeakerName(speakerName);
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
        if (maid is null)
        {
            SubtitleComponentManager.HideSubtitleBySpeakerName(speakerName);
            // 销毁重新创建以重新排序
            SubtitleComponentManager.UpdateAllSubtitleConfig();
            LogManager.Debug($"MonitorMaidVoicePlayback ended for Maid {speakerName}");
            yield break;
        }

        if (!maid.Visible)
        {
            SubtitleComponentManager.HideSubtitleBySpeakerName(speakerName);
            MaidMonitorCoroutineIds.Remove(maid);
            SubtitleComponentManager.UpdateSubtitleConfigBySpeakerName(speakerName);
        }

        LogManager.Debug($"MonitorMaidVoicePlayback ended for Maid {speakerName}");
    }


    /// <summary>
    ///     获取说话者的专属颜色
    /// </summary>
    /// <param name="speakerName">说话者名称</param>
    /// <param name="subtitleType">字幕类型</param>
    /// <returns>专属颜色</returns>
    public static Color GetSpeakerColor(string speakerName,
        JustAnotherTranslator.SubtitleTypeEnum subtitleType)
    {
        LogManager.Debug($"Creating Color for {speakerName}");

        if (speakerName == null) speakerName = "";

        // 确保说话人条目存在
        if (!_colorConfig.ContainsKey(speakerName))
        {
            var newEntry = CreateDefaultSpeakerEntry(speakerName);
            _colorConfig[speakerName] = newEntry;
            SaveColorConfig();
        }

        var entry = _colorConfig[speakerName];
        var typeKey = subtitleType.ToString();

        // 从对应字幕类型的条目中读取 SpeakerColor
        if (entry.SubtitleColors.TryGetValue(typeKey, out var colorEntry) &&
            !string.IsNullOrEmpty(colorEntry.SpeakerColor))
        {
            if (ColorUtility.TryParseHtmlString(colorEntry.SpeakerColor, out var loadedColor))
            {
                LogManager.Debug(
                    $"Loaded Color R:{loadedColor.r:F2} G:{loadedColor.g:F2} B:{loadedColor.b:F2} for {speakerName} ({typeKey})");
                return loadedColor;
            }

            LogManager.Warning(
                $"Invalid speaker color config for {speakerName} ({typeKey}): {colorEntry.SpeakerColor}");
        }

        // 兜底：使用哈希生成颜色
        var random = new Random(speakerName.GetHashCode());
        var color = new Color(
            0.5f + (float)random.NextDouble() * 0.5f,
            0.5f + (float)random.NextDouble() * 0.5f,
            0.5f + (float)random.NextDouble() * 0.5f
        );

        LogManager.Debug(
            $"Created fallback Color R:{color.r:F2} G:{color.g:F2} B:{color.b:F2} for {speakerName}");

        return color;
    }

    /// <summary>
    ///     获取说话者在指定字幕类型下的自定义颜色配置
    /// </summary>
    /// <param name="speakerName">说话者名称</param>
    /// <param name="subtitleType">字幕类型</param>
    /// <returns>自定义颜色配置，不存在则返回 null</returns>
    public static SubtitleColorEntry GetSubtitleColorEntry(
        string speakerName, JustAnotherTranslator.SubtitleTypeEnum subtitleType)
    {
        if (speakerName == null) speakerName = "";

        if (!_colorConfig.TryGetValue(speakerName, out var entry))
            return null;

        var typeKey = subtitleType.ToString();
        return entry.SubtitleColors.TryGetValue(typeKey, out var colorEntry) ? colorEntry : null;
    }

    /// <summary>
    ///     创建说话者的默认颜色条目（含说话人颜色和全部字幕类型的默认颜色）
    /// </summary>
    /// <param name="speakerName">说话者名称</param>
    /// <returns>默认颜色配置</returns>
    private static SpeakerColorConfig CreateDefaultSpeakerEntry(string speakerName)
    {
        // 使用哈希值生成颜色，确保相同名称总是获得相同颜色
        // 具体哈希方式不重要，也不需要强一致，因此直接使用GetHashCode
        var random = new Random(speakerName.GetHashCode());

        // 生成偏亮的颜色
        var color = new Color(
            0.5f + (float)random.NextDouble() * 0.5f, // 0.5-1.0 范围
            0.5f + (float)random.NextDouble() * 0.5f,
            0.5f + (float)random.NextDouble() * 0.5f
        );

        var speakerColorHex = $"#{ColorUtility.ToHtmlStringRGB(color)}";

        var entry = new SpeakerColorConfig
        {
            SubtitleColors = new Dictionary<string, SubtitleColorEntry>()
        };

        // 为每种字幕类型创建默认颜色配置
        foreach (JustAnotherTranslator.SubtitleTypeEnum type in
                 Enum.GetValues(typeof(JustAnotherTranslator.SubtitleTypeEnum)))
            entry.SubtitleColors[type.ToString()] =
                CreateDefaultSubtitleColorEntry(type, speakerColorHex);

        return entry;
    }

    /// <summary>
    ///     为指定字幕类型创建默认的颜色配置条目，从 BepInEx 配置读取当前默认值
    /// </summary>
    private static SubtitleColorEntry CreateDefaultSubtitleColorEntry(
        JustAnotherTranslator.SubtitleTypeEnum type, string speakerColorHex)
    {
        return type switch
        {
            JustAnotherTranslator.SubtitleTypeEnum.Base => new SubtitleColorEntry
            {
                SpeakerColor = speakerColorHex,
                TextColor = JustAnotherTranslator.BaseSubtitleColor.Value,
                TextOpacity = JustAnotherTranslator.BaseSubtitleOpacity.Value,
                BackgroundColor = JustAnotherTranslator.BaseSubtitleBackgroundColor.Value,
                BackgroundOpacity = JustAnotherTranslator.BaseSubtitleBackgroundOpacity.Value,
                OutlineColor = JustAnotherTranslator.BaseSubtitleOutlineColor.Value,
                OutlineOpacity = JustAnotherTranslator.BaseSubtitleOutlineOpacity.Value
            },
            JustAnotherTranslator.SubtitleTypeEnum.Yotogi => new SubtitleColorEntry
            {
                SpeakerColor = speakerColorHex,
                TextColor = JustAnotherTranslator.YotogiSubtitleColor.Value,
                TextOpacity = JustAnotherTranslator.YotogiSubtitleOpacity.Value,
                BackgroundColor = JustAnotherTranslator.YotogiSubtitleBackgroundColor.Value,
                BackgroundOpacity = JustAnotherTranslator.YotogiSubtitleBackgroundOpacity.Value,
                OutlineColor = JustAnotherTranslator.YotogiSubtitleOutlineColor.Value,
                OutlineOpacity = JustAnotherTranslator.YotogiSubtitleOutlineOpacity.Value
            },
            JustAnotherTranslator.SubtitleTypeEnum.Adv => new SubtitleColorEntry
            {
                SpeakerColor = speakerColorHex,
                TextColor = JustAnotherTranslator.AdvSubtitleColor.Value,
                TextOpacity = JustAnotherTranslator.AdvSubtitleOpacity.Value,
                BackgroundColor = JustAnotherTranslator.AdvSubtitleBackgroundColor.Value,
                BackgroundOpacity = JustAnotherTranslator.AdvSubtitleBackgroundOpacity.Value,
                OutlineColor = JustAnotherTranslator.AdvSubtitleOutlineColor.Value,
                OutlineOpacity = JustAnotherTranslator.AdvSubtitleOutlineOpacity.Value
            },
            JustAnotherTranslator.SubtitleTypeEnum.Lyric => new SubtitleColorEntry
            {
                SpeakerColor = speakerColorHex,
                TextColor = JustAnotherTranslator.LyricSubtitleColor.Value,
                TextOpacity = JustAnotherTranslator.LyricSubtitleOpacity.Value,
                BackgroundColor = JustAnotherTranslator.LyricSubtitleBackgroundColor.Value,
                BackgroundOpacity = JustAnotherTranslator.LyricSubtitleBackgroundOpacity.Value,
                OutlineColor = JustAnotherTranslator.LyricSubtitleOutlineColor.Value,
                OutlineOpacity = JustAnotherTranslator.LyricSubtitleOutlineOpacity.Value
            },
            _ => new SubtitleColorEntry { SpeakerColor = speakerColorHex }
        };
    }

    /// <summary>
    ///     加载颜色配置文件。如果配置文件不存在，则创建一个新的空配置文件。
    ///     如果加载失败，将记录警告并初始化一个空的颜色配置。
    /// </summary>
    private static void LoadColorConfig()
    {
        try
        {
            if (!File.Exists(JustAnotherTranslator.SubtitleColorsConfigPath))
            {
                _colorConfig = new Dictionary<string, SpeakerColorConfig>();
                SaveColorConfig();
                return;
            }

            var json = File.ReadAllText(JustAnotherTranslator.SubtitleColorsConfigPath);
            var loaded =
                JsonConvert.DeserializeObject<Dictionary<string, SpeakerColorConfig>>(json);
            _colorConfig = loaded ?? new Dictionary<string, SpeakerColorConfig>();
            LogManager.Debug($"Loaded color config: {json}");
        }
        catch (Exception e)
        {
            _colorConfig = new Dictionary<string, SpeakerColorConfig>();
            LogManager.Warning($"Failed to load color config/加载颜色配置失败: {e.Message}");
        }
    }

    /// <summary>
    ///     将颜色配置保存到配置文件中。
    /// </summary>
    private static void SaveColorConfig()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_colorConfig, Formatting.Indented);
            File.WriteAllText(JustAnotherTranslator.SubtitleColorsConfigPath, json);
            LogManager.Debug($"Saved color config: {json}");
        }
        catch (Exception e)
        {
            LogManager.Warning($"Failed to save color config/保存颜色配置失败: {e.Message}");
        }
    }


    # region Setters

    /// <summary>
    ///     由声音 Patch 调用，用于设置文本和 voiceId 的映射
    /// </summary>
    public static void SetVoiceTextMapping(string voiceId, string text, string callBy)
    {
        if (string.IsNullOrEmpty(voiceId) || string.IsNullOrEmpty(text))
            return;

        // 直接更新映射
        VoiceIdToTextMap[voiceId] = text;

        LogManager.Debug(
            $"SetVoiceText called by {callBy}, create a mapping: voiceId={voiceId}, text={text}");
    }


    /// <summary>
    ///     由声音 Patch 调用，用于设置文本和 voiceId 的映射
    ///     使用 CurrentVoiceId
    /// </summary>
    public static void SetVoiceTextMapping(string text, string callBy)
    {
        if (string.IsNullOrEmpty(_currentVoiceId) || string.IsNullOrEmpty(text))
            return;

        // 直接更新映射
        VoiceIdToTextMap[_currentVoiceId] = text;

        LogManager.Debug(
            $"SetVoiceText called by {callBy}, create a mapping: voiceId={_currentVoiceId}, text={text}");
    }

    // <summary>
    //     由声音 Patch 调用，用于设置当前 voiceId
    // </summary>
    public static void SetCurrentVoiceId(string voiceId)
    {
        _currentVoiceId = voiceId;
    }

    // <summary>
    //      由声音 Patch 调用，用于设置当前说话者
    // </summary>
    public static void SetCurrentSpeaker(Maid speakerMaid)
    {
        _currentSpeaker = speakerMaid;
    }


    // <summary>
    //     由声音 Patch 调用，用于设置当前字幕类型
    // </summary>
    public static void SetSubtitleType(JustAnotherTranslator.SubtitleTypeEnum subtitleType)
    {
        _currentSubtitleType = subtitleType;
    }

    # endregion
}