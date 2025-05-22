using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using COM3D2.JustAnotherTranslator.Plugin.Hooks;
using COM3D2.JustAnotherTranslator.Plugin.Subtitle;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using UnityEngine;

namespace COM3D2.JustAnotherTranslator.Plugin.Translator;

public static class YotogiSubtitleManager
{
    private const string FallbackSubtitleId = "Subtitle_JustAnotherTranslator_YotogiSubtitle";
    private const float SubtitleDuration = 0f;
    private static Harmony _yotogiSubtitlePatch;
    private static bool _initialized;
    private static SubtitleConfig _subtitleConfig;

    // 存储每个Maid的监听协程ID
    private static readonly Dictionary<Maid, string> MaidMonitorCoroutineIds = new();

    // 存储voiceId与文本的映射关系
    public static readonly Dictionary<string, string> VoiceIdToTextMap = new();

    // 当前正在说话的角色
    public static Maid CurrentSpeaker;

    // 当前正在播放的语音ID
    public static string CurrentVoiceId;


    public static void Init()
    {
        if (_initialized) return;

        # region YotogiSubtitleConfig

        Font font;
        if (JustAnotherTranslator.YotogiSubtitleFont.Value == "Arial")
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        else
            try
            {
                font = Font.CreateDynamicFontFromOSFont(JustAnotherTranslator.YotogiSubtitleFont.Value,
                    JustAnotherTranslator.YotogiSubtitleFontSize.Value);
            }
            catch (Exception e)
            {
                LogManager.Error($"Failed to create font/创建字体失败: {e.Message}");
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

        Color textColor;
        if (ColorUtility.TryParseHtmlString(JustAnotherTranslator.YotogiSubtitleColor.Value, out var color))
            textColor = color;
        else
            textColor = Color.white;
        textColor.a = JustAnotherTranslator.YotogiSubtitleOpacity.Value;

        Color backgroundColor;
        if (ColorUtility.TryParseHtmlString(JustAnotherTranslator.YotogiSubtitleBackgroundColor.Value, out color))
            backgroundColor = color;
        else
            backgroundColor = Color.black;
        backgroundColor.a = JustAnotherTranslator.YotogiSubtitleBackgroundOpacity.Value;

        Color outlineColor;
        if (ColorUtility.TryParseHtmlString(JustAnotherTranslator.YotogiSubtitleOutlineColor.Value, out color))
            outlineColor = color;
        else
            outlineColor = Color.black;
        outlineColor.a = JustAnotherTranslator.YotogiSubtitleOutlineOpacity.Value;


        _subtitleConfig = new SubtitleConfig
        {
            EnableSpeakerName = JustAnotherTranslator.EnableYotogiSubtitleSpeakerName.Value,
            Font = font,
            FontSize = JustAnotherTranslator.YotogiSubtitleFontSize.Value,
            TextColor = textColor,
            BackgroundColor = backgroundColor,
            VerticalPosition = JustAnotherTranslator.YotogiSubtitlePosition.Value,
            Height = JustAnotherTranslator.YotogiSubtitleBackgroundHeight.Value,
            EnableAnimation = JustAnotherTranslator.YotogiSubtitleAnimation.Value,
            FadeInDuration = JustAnotherTranslator.YotogiSubtitleFadeInDuration.Value,
            FadeOutDuration = JustAnotherTranslator.YotogiSubtitleFadeOutDuration.Value,
            EnableOutline = JustAnotherTranslator.EnableYotogiSubtitleOutline.Value,
            OutlineColor = outlineColor,
            OutlineWidth = JustAnotherTranslator.YotogiSubtitleOutlineWidth.Value
        };

        # endregion

        _yotogiSubtitlePatch = Harmony.CreateAndPatchAll(typeof(YotogiSubtitlePatch));

        _initialized = true;
    }

    public static void Unload()
    {
        if (!_initialized) return;

        if (_yotogiSubtitlePatch != null)
        {
            _yotogiSubtitlePatch.UnpatchSelf();
            _yotogiSubtitlePatch = null;
        }

        _subtitleConfig = null;

        SubtitleManager.DestroySubtitle(FallbackSubtitleId);

        CleanupAllCoroutines();

        _initialized = false;
    }


    /// <summary>
    ///     更新字幕配置
    /// </summary>
    public static void UpdateSubtitleConfig()
    {
        if (!_initialized) return;

        # region YotogiSubtitleConfig

        Font font;
        if (JustAnotherTranslator.YotogiSubtitleFont.Value == "Arial")
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        else
            try
            {
                font = Font.CreateDynamicFontFromOSFont(JustAnotherTranslator.YotogiSubtitleFont.Value,
                    JustAnotherTranslator.YotogiSubtitleFontSize.Value);
            }
            catch (Exception e)
            {
                LogManager.Error($"Failed to create font/创建字体失败: {e.Message}");
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

        Color textColor;
        if (ColorUtility.TryParseHtmlString(JustAnotherTranslator.YotogiSubtitleColor.Value, out var color))
            textColor = color;
        else
            textColor = Color.white;
        textColor.a = JustAnotherTranslator.YotogiSubtitleOpacity.Value;

        Color backgroundColor;
        if (ColorUtility.TryParseHtmlString(JustAnotherTranslator.YotogiSubtitleBackgroundColor.Value, out color))
            backgroundColor = color;
        else
            backgroundColor = Color.black;
        backgroundColor.a = JustAnotherTranslator.YotogiSubtitleBackgroundOpacity.Value;

        Color outlineColor;
        if (ColorUtility.TryParseHtmlString(JustAnotherTranslator.YotogiSubtitleOutlineColor.Value, out color))
            outlineColor = color;
        else
            outlineColor = Color.black;
        outlineColor.a = JustAnotherTranslator.YotogiSubtitleOutlineOpacity.Value;


        _subtitleConfig = new SubtitleConfig
        {
            EnableSpeakerName = JustAnotherTranslator.EnableYotogiSubtitleSpeakerName.Value,
            Font = font,
            FontSize = JustAnotherTranslator.YotogiSubtitleFontSize.Value,
            TextColor = textColor,
            BackgroundColor = backgroundColor,
            VerticalPosition = JustAnotherTranslator.YotogiSubtitlePosition.Value,
            Height = JustAnotherTranslator.YotogiSubtitleBackgroundHeight.Value,
            EnableAnimation = JustAnotherTranslator.YotogiSubtitleAnimation.Value,
            FadeInDuration = JustAnotherTranslator.YotogiSubtitleFadeInDuration.Value,
            FadeOutDuration = JustAnotherTranslator.YotogiSubtitleFadeOutDuration.Value,
            EnableOutline = JustAnotherTranslator.EnableYotogiSubtitleOutline.Value,
            OutlineColor = outlineColor,
            OutlineWidth = JustAnotherTranslator.YotogiSubtitleOutlineWidth.Value
        };

        # endregion

        // 首先隐藏所有字幕
        SubtitleManager.HideAllSubtitles();

        // 存储当前显示的字幕信息，用于后续重新显示
        var activeSubtitles = new Dictionary<Maid, string>();
        foreach (var pair in MaidMonitorCoroutineIds)
            if (pair.Key != null && CurrentSpeaker == pair.Key && !string.IsNullOrEmpty(CurrentVoiceId))
                if (VoiceIdToTextMap.TryGetValue(CurrentVoiceId, out var text) && !string.IsNullOrEmpty(text))
                    activeSubtitles[pair.Key] = text;

        // 销毁所有字幕组件，强制重新创建
        SubtitleManager.DestroyAllSubtitles();

        LogManager.Debug($"重新创建默认字幕: {FallbackSubtitleId}");

        // 更新并重新显示活动字幕
        foreach (var pair in activeSubtitles)
        {
            var maid = pair.Key;
            var text = pair.Value;

            // 创建字幕并显示
            ShowSubtitle(text, maid);
            LogManager.Debug($"重新显示字幕: {MaidInfo.GetMaidFullName(maid)} - {text}");
        }

        LogManager.Info("所有字幕配置已更新/All subtitle configs updated");
    }


    // 显示字幕
    private static void ShowSubtitle(string text, Maid maid)
    {
        if (string.IsNullOrEmpty(text) || maid == null)
            return;

        var speakerName = MaidInfo.GetMaidFullName(maid);

        // 使用浮动字幕显示，自动避免重叠
        SubtitleManager.ShowFloatingSubtitle(
            text,
            speakerName,
            SubtitleDuration,
            _subtitleConfig
        );

        LogManager.Debug($"Showing subtitle for {speakerName}: {text}");
    }

    // 隐藏字幕
    private static void HideSubtitle(Maid maid)
    {
        if (maid is null)
            return;

        var speakerName = MaidInfo.GetMaidFullName(maid);
        SubtitleManager.HideSubtitle(SubtitleManager.GetSpeakerSubtitleId(speakerName));

        LogManager.Debug($"Hiding subtitle for {speakerName}");
    }


    // 启动Maid监听协程
    public static void StartMaidMonitoringCoroutine(Maid maid)
    {
        if (maid == null)
            return;

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
        if (maid == null || !MaidMonitorCoroutineIds.ContainsKey(maid))
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

        while (maid.Visible)
        {
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
                        LogManager.Debug($"Found text for voice {currentVoiceId}: {text}, showing subtitle");

                        // 显示字幕
                        ShowSubtitle(text, maid);
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

        // Maid不可见，停止协程
        if (!maid.Visible)
        {
            HideSubtitle(maid);
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
            if (pair.Key != null) HideSubtitle(pair.Key);

            if (!string.IsNullOrEmpty(pair.Value)) CoroutineManager.StopCoroutine(pair.Value);
        }

        MaidMonitorCoroutineIds.Clear();
        VoiceIdToTextMap.Clear();
        CurrentSpeaker = null;
        CurrentVoiceId = null;

        LogManager.Debug("All coroutines and resources cleaned up");
    }
}