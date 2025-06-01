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

/// <summary>
///     字幕管理器，负责管理语音监控和补丁应用
/// </summary>
public static class SubtitleManager
{
    private static bool _initialized;

    // 存储每个Maid的监听协程ID
    private static readonly Dictionary<Maid, string> MaidMonitorCoroutineIds = new();

    // 存储voiceId与文本的映射关系
    public static readonly Dictionary<string, string> VoiceIdToTextMap = new();

    // 当前正在说话的角色
    public static Maid CurrentSpeaker;

    // 当前正在播放的语音ID
    public static string CurrentVoiceId;

    // 当前字幕类型
    public static JustAnotherTranslator.SubtitleTypeEnum SubtitleType;

    // 各种补丁实例
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
        SubtitleComponentManager.Initialize();

        // 应用补丁
        ApplySubtitlePatches();

        // 注册场景变更事件
        SceneManager.sceneUnloaded += OnSceneChange;

        _initialized = true;
    }

    /// <summary>
    ///     卸载字幕管理器
    /// </summary>
    public static void Unload()
    {
        if (!_initialized) return;

        // 卸载字幕补丁
        UnloadSubtitlePatches();

        // 取消事件订阅
        SceneManager.sceneUnloaded -= OnSceneChange;

        // 清理资源
        CleanResources();

        _initialized = false;
    }

    /// <summary>
    ///     场景切换时的处理
    /// </summary>
    private static void OnSceneChange(Scene scene)
    {
        LogManager.Debug($"Scene {scene.name} - {scene.buildIndex}, unloaded  cleaning up");

        CleanResources();
    }


    /// <summary>
    ///     应用字幕补丁
    /// </summary>
    private static void ApplySubtitlePatches()
    {
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

        // 调试补丁
        if (JustAnotherTranslator.LogLevelConfig.Value >= LogLevel.Debug)
            _debugPatch = Harmony.CreateAndPatchAll(typeof(DebugPatch));
    }


    /// <summary>
    ///     卸载字幕补丁
    /// </summary>
    private static void UnloadSubtitlePatches()
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

        // 重置状态
        CurrentSpeaker = null;
        CurrentVoiceId = null;
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
        if (MaidMonitorCoroutineIds.ContainsKey(maid) && !string.IsNullOrEmpty(MaidMonitorCoroutineIds[maid]))
            return;

        // 启动新的协程监控 Maid 的语音播放状态
        var coroutineId = CoroutineManager.LaunchCoroutine(MonitorMaidVoicePlayback(maid));
        MaidMonitorCoroutineIds[maid] = coroutineId;

        LogManager.Debug($"Started monitoring coroutine for Maid: {MaidInfo.GetMaidFullName(maid)}, ID: {coroutineId}");
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

        LogManager.Debug($"Stopped monitoring coroutine for Maid: {MaidInfo.GetMaidFullName(maid)}");
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

        LogManager.Debug($"SetVoiceText called by {callBy}, create a mapping: voiceId={voiceId}, text={text}");
    }


    /// <summary>
    ///     由声音 Patch 调用，用于设置文本和 voiceId 的映射
    ///     使用 CurrentVoiceId
    /// </summary>
    public static void SetVoiceTextMapping(string text, string callBy)
    {
        if (string.IsNullOrEmpty(CurrentVoiceId) || string.IsNullOrEmpty(text))
            return;

        // 直接更新映射
        VoiceIdToTextMap[CurrentVoiceId] = text;

        LogManager.Debug($"SetVoiceText called by {callBy}, create a mapping: voiceId={CurrentVoiceId}, text={text}");
    }

    // <summary>
    //     由声音 Patch 调用，用于设置当前 voiceId
    // </summary>
    public static void SetCurrentVoiceId(string voiceId)
    {
        CurrentVoiceId = voiceId;
    }

    // <summary>
    //      由声音 Patch 调用，用于设置当前说话者
    // </summary>
    public static void SetCurrentSpeaker(Maid speakerMaid)
    {
        CurrentSpeaker = speakerMaid;
    }


    // <summary>
    //     由声音 Patch 调用，用于设置当前字幕类型
    // </summary>
    public static void SetSubtitleType(JustAnotherTranslator.SubtitleTypeEnum subtitleType)
    {
        SubtitleType = subtitleType;
    }

    # endregion
}