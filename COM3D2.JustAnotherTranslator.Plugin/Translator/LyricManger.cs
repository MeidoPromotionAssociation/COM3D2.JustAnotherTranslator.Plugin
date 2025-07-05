using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using COM3D2.JustAnotherTranslator.Plugin.Hooks.Lyric;
using COM3D2.JustAnotherTranslator.Plugin.Subtitle;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using CsvHelper;
using CsvHelper.Configuration;
using HarmonyLib;
using UnityEngine;

namespace COM3D2.JustAnotherTranslator.Plugin.Translator;

public static class LyricManger
{
    private static bool _initialized;

    private static Harmony _lyricPatch;

    private static readonly List<LyricCsvEntry> CurrentLyrics = new();
    private static int _currentLyricIndex;

    private static readonly CsvConfiguration CsvConfig = new()
    {
        CultureInfo = CultureInfo.InvariantCulture,
        AllowComments = true,
        HasHeaderRecord = true,
        Encoding = Encoding.UTF8,
        IgnoreBlankLines = true,
        IgnoreHeaderWhiteSpace = true,
        IsHeaderCaseSensitive = false,
        SkipEmptyRecords = true,
        WillThrowOnMissingField = false
    };

    private static RhythmAction_Mgr _rhythmActionMgr;

    private static string _playbackMonitorCoroutineID;

    public static void Init()
    {
        if (_initialized) return;

        // 初始化字幕组件管理器
        SubtitleComponentManager.Init();

        // 无需提取加载字幕到内存，播放时加载即可

        _lyricPatch = Harmony.CreateAndPatchAll(typeof(LyricPatch));

        _initialized = true;
    }

    public static void Unload()
    {
        if (!_initialized) return;

        _lyricPatch?.UnpatchSelf();
        _lyricPatch = null;

        _initialized = false;
    }

    /// <summary>
    ///     创建音乐对应的字幕文件夹
    /// </summary>
    /// <param name="musicName"></param>
    public static void CreateMusicPath(string musicName)
    {
        try
        {
            var path = Path.Combine(JustAnotherTranslator.LyricPath, musicName);

            Directory.CreateDirectory(path);

            var lyricPath = Path.Combine(path, "lyric.csv");
            if (!File.Exists(lyricPath))
                File.WriteAllText(lyricPath, string.Empty);
        }
        catch (Exception e)
        {
            LogManager.Error($"Create music folder failed/创建音乐文件夹失败: {e.Message}");
        }
    }

    /// <summary>
    ///     尝试加载字幕
    ///     如果字幕文件存在就加载
    /// </summary>
    public static void TryToLoadLyric(string musicName)
    {
        // I don't have 3 args in .net framework 3.5
        var path = Path.Combine(JustAnotherTranslator.LyricPath, musicName);
        path = Path.Combine(path, "lyric.csv");

        if (File.Exists(path))
            LoadSubtitle(path);
    }

    /// <summary>
    ///     清空字幕
    /// </summary>
    public static void ClearLyric()
    {
        CurrentLyrics.Clear();
    }

    /// <summary>
    ///     加载字幕
    ///     CSV 格式为 startTime,endTime,originalLyric,translatedLyric
    ///     允许 originalLyric 或 translatedLyric 为空
    /// </summary>
    /// <param name="path"></param>
    private static void LoadSubtitle(string path)
    {
        CurrentLyrics.Clear();
        _currentLyricIndex = 0;

        if (!File.Exists(path))
        {
            LogManager.Info($"Lyric file not found: {path}/未找到字幕文件: {path}");
            return;
        }

        try
        {
            using (var reader = new StreamReader(path, Encoding.UTF8))
            using (var csv = new CsvReader(reader, CsvConfig))
            {
                var records = csv.GetRecords<LyricCsvEntry>();
                CurrentLyrics.AddRange(records);
            }

            // Sort by StartTime
            CurrentLyrics.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));

            LogManager.Debug($"Successfully loaded {CurrentLyrics.Count} lyric entries from {path}");
        }
        catch (Exception ex)
        {
            LogManager.Error($"Error loading lyric file {path}: {ex.Message}/加载字幕文件出错 {path}: {ex.Message}");
        }
    }

    /// <summary>
    ///     处理舞蹈加载
    /// </summary>
    /// <param name="musicName"></param>
    public static void HandleDanceLoaded(string musicName)
    {
        if (string.IsNullOrEmpty(musicName))
            return;

        CreateMusicPath(musicName);
        TryToLoadLyric(musicName);
    }

    /// <summary>
    ///     处理舞蹈开始
    /// </summary>
    /// <param name="instance"></param>
    public static void HandleDanceStart(RhythmAction_Mgr instance)
    {
        _rhythmActionMgr = instance;
        _currentLyricIndex = 0;

        // Start the playback monitor coroutine
        if (_playbackMonitorCoroutineID == null)
            _playbackMonitorCoroutineID = CoroutineManager.LaunchCoroutine(PlaybackMonitor());
    }

    /// <summary>
    ///     处理舞蹈结束
    /// </summary>
    public static void HandleDanceEnd()
    {
        // Stop the playback monitor coroutine
        if (_playbackMonitorCoroutineID != null)
        {
            CoroutineManager.StopCoroutine(_playbackMonitorCoroutineID);
            _playbackMonitorCoroutineID = null;
        }

        _currentLyricIndex = 0;
        ClearLyric();
        SubtitleComponentManager.DestroyAllSubtitleComponents();
    }

    /// <summary>
    ///     监控协程
    /// </summary>
    /// <returns></returns>
    private static IEnumerator PlaybackMonitor()
    {
        var lastLyricIndex = -1;

        while (true)
        {
            var currentTime = _rhythmActionMgr.DanceTimer; //当前舞蹈从开始播放到现在的累计时间


            // 查找当前时间点应该显示的歌词索引
            var currentLyricIndex = -1;
            for (var i = 0; i < CurrentLyrics.Count; i++)
                if (CurrentLyrics[i].StartTime <= currentTime && CurrentLyrics[i].EndTime > currentTime)

                {
                    currentLyricIndex = i;

                    break;
                }

            // 只有当需要显示的歌词索引与上一帧不同时，才执行操作
            if (currentLyricIndex != lastLyricIndex)
            {
                // 如果有新的歌词要显示 (currentLyricIndex != -1)
                if (currentLyricIndex != -1)
                {
                    var entry = CurrentLyrics[currentLyricIndex];


                    // TODO 支持双语显示
                    SubtitleComponentManager.ShowSubtitle(entry.TranslatedLyric, null, entry.EndTime - entry.StartTime,
                        JustAnotherTranslator.SubtitleTypeEnum.Lyric);
                    LogManager.Debug($"Showing lyric: {entry.TranslatedLyric}");
                }
                else
                {
                    SubtitleComponentManager.HideSubtitleById(SubtitleComponentManager.GetSpeakerSubtitleId(null));
                    LogManager.Debug("Hiding lyric");
                }

                // 更新最后显示的歌词索引，以便下一帧进行比较
                lastLyricIndex = currentLyricIndex;
            }

            yield return null;
        }
    }

    // CSV structure for lyrics
    private class LyricCsvEntry
    {
        public float StartTime { get; set; }
        public float EndTime { get; set; }
        public string OriginalLyric { get; set; }
        public string TranslatedLyric { get; set; }
    }
}