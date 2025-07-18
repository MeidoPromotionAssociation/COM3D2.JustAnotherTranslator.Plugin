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
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace COM3D2.JustAnotherTranslator.Plugin.Translator;

/// <summary>
///     歌词翻译管理器
/// </summary>
public static class LyricManger
{
    private static bool _initialized;

    private static Harmony _lyricPatch;

    /// 当前正在播放的歌词列表
    private static readonly List<LyricCsvEntry> CurrentLyrics = new();

    /// 当前正在播放的歌词的索引
    private static int _currentLyricIndex;

    /// 舞蹈管理器实例
    private static RhythmAction_Mgr _rhythmActionMgr;

    /// 处理歌词显示的协程
    private static string _playbackMonitorCoroutineID;

    /// 主舞蹈Maid名
    private static string _mainDanceMaidName = "";


    /// <summary>
    ///     CsvHelper 配置
    /// </summary>
    private static readonly CsvConfiguration CsvConfig = new()
    {
        CultureInfo = CultureInfo.InvariantCulture,
        AllowComments = true,
        HasHeaderRecord = true,
        Encoding = Encoding.UTF8, // The Encoding config is only used for byte counting. https://github.com/JoshClose/CsvHelper/issues/2278#issuecomment-2274128445
        IgnoreBlankLines = true,
        IgnoreHeaderWhiteSpace = true,
        IsHeaderCaseSensitive = false,
        SkipEmptyRecords = true,
        WillThrowOnMissingField = false
    };

    public static void Init()
    {
        if (_initialized) return;

        // 初始化字幕组件管理器
        SubtitleComponentManager.Init();

        // 无需提前加载字幕到内存，播放时加载即可
        SceneManager.sceneUnloaded += OnSceneUnloaded;

        _lyricPatch = Harmony.CreateAndPatchAll(typeof(LyricPatch),
            "com3d2.justanothertranslator.plugin.hooks.lyric.lyricpatch");

        _initialized = true;
    }

    public static void Unload()
    {
        if (!_initialized) return;

        SceneManager.sceneUnloaded -= OnSceneUnloaded;

        _lyricPatch?.UnpatchSelf();
        _lyricPatch = null;

        ClearSceneResources();

        _initialized = false;
    }

    /// <summary>
    ///     场景卸载时清理资源
    /// </summary>
    /// <param name="scene"></param>
    private static void OnSceneUnloaded(Scene scene)
    {
        ClearSceneResources();
        SubtitleComponentManager.DestroyAllSubtitleComponents();
    }


    /// <summary>
    ///     清理场景资源
    /// </summary>
    private static void ClearSceneResources()
    {
        ClearLyric();
        _rhythmActionMgr = null;

        if (_playbackMonitorCoroutineID != null)
        {
            CoroutineManager.StopCoroutine(_playbackMonitorCoroutineID);
            _playbackMonitorCoroutineID = null;
        }
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
        _currentLyricIndex = 0;
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
            using (var reader = new StreamReader(path, Encoding.UTF8)) // This can process utf-8-sig as well, which is csv should be
            using (var csv = new CsvReader(reader, CsvConfig))
            {
                var records = csv.GetRecords<LyricCsvEntry>();
                CurrentLyrics.AddRange(records);
            }

            // Sort by StartTime
            CurrentLyrics.Sort((a, b) => a.StartTime.CompareTo(b.StartTime));

            LogManager.Info(
                $"Successfully loaded {CurrentLyrics.Count} lyric entries from {path}/成功加载 {CurrentLyrics.Count} 条歌词");
        }
        catch (Exception ex)
        {
            LogManager.Error($"Error loading lyric file {path}: {ex.Message}/加载歌词文件出错 {path}: {ex.Message}");
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

        // 音频文件是通过 PlayDanceBGM 的形式加载的（m_strMasterAudioFileName）
        // 不存在歌唱的 Maid，因此 SpeakerName 显示始终为主 Maid
        if (JustAnotherTranslator.EnableLyricSubtitleSpeakerName.Value)
            _mainDanceMaidName = MaidInfo.GetMaidFullName(_rhythmActionMgr.DanceMaid[0]);
        else
            _mainDanceMaidName = "";

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

        ClearLyric();
        SubtitleComponentManager.DestroyAllSubtitleComponents();
    }

    /// <summary>
    ///     歌曲播放监控协程
    /// </summary>
    /// <returns></returns>
    private static IEnumerator PlaybackMonitor()
    {
        LyricCsvEntry activeLyric = null;

        LogManager.Debug("Lyric Playback monitor started");

        while (true)
        {
            if (_rhythmActionMgr == null || CurrentLyrics.Count == 0)
            {
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            var currentTime = _rhythmActionMgr.DanceTimer; //当前舞蹈从开始播放到现在的累计时间

            // 查找当前应该显示的歌词
            LyricCsvEntry lyricToShow = null;

            // 将索引推进到已经完成的所有歌词之后
            while (_currentLyricIndex < CurrentLyrics.Count && currentTime > CurrentLyrics[_currentLyricIndex].EndTime)
                _currentLyricIndex++;

            // 检查是否应显示当前（或下一个）歌词
            if (_currentLyricIndex < CurrentLyrics.Count)
            {
                var currentLyric = CurrentLyrics[_currentLyricIndex];
                if (currentTime >= currentLyric.StartTime && currentTime <= currentLyric.EndTime)
                    lyricToShow = currentLyric;
            }

            // 仅当活动歌词发生变化时才更新字幕显示
            if (activeLyric != lyricToShow)
            {
                activeLyric = lyricToShow;
                if (activeLyric != null)
                {
                    var lyric = ProcessLyric(activeLyric);

                    SubtitleComponentManager.ShowSubtitle(lyric, _mainDanceMaidName,
                        activeLyric.EndTime - activeLyric.StartTime,
                        JustAnotherTranslator.SubtitleTypeEnum.Lyric);

                    LogManager.Debug($"Showing lyric: {lyric}");
                }
                else
                {
                    SubtitleComponentManager.HideSubtitleById(SubtitleComponentManager.GetSpeakerSubtitleId(null));
                    LogManager.Debug("Hiding lyric");
                }
            }

            yield return null;
        }
    }


    /// <summary>
    ///     处理歌词显示类型
    /// </summary>
    /// <param name="lyricEntry"></param>
    /// <returns>lyric</returns>
    private static string ProcessLyric(LyricCsvEntry lyricEntry)
    {
        var lyric = "";
        var placeHolder = "";

        if (JustAnotherTranslator.EnableLyricSubtitleSpeakerName.Value)
            // Add 2 for ": "
            placeHolder = new string(' ', (_mainDanceMaidName?.Length ?? 0) + 2);

        switch (JustAnotherTranslator.LyricSubtitleType.Value)
        {
            case JustAnotherTranslator.LyricSubtitleTypeEnum.OriginalOnly:
                lyric = lyricEntry.OriginalLyric;
                break;
            case JustAnotherTranslator.LyricSubtitleTypeEnum.TranslationOnly:
                lyric = lyricEntry.TranslatedLyric;
                break;
            case JustAnotherTranslator.LyricSubtitleTypeEnum.TranslationAndOriginal:
                lyric = string.Concat(lyricEntry.TranslatedLyric, "\n", placeHolder, lyricEntry.OriginalLyric);
                break;
            case JustAnotherTranslator.LyricSubtitleTypeEnum.OriginalAndTranslation:
                lyric = string.Concat(lyricEntry.OriginalLyric, "\n", placeHolder, lyricEntry.TranslatedLyric);
                break;
            default:
                lyric = string.Concat(lyricEntry.TranslatedLyric, "\n", placeHolder, lyricEntry.OriginalLyric);
                break;
        }

        lyric = XUATInterop.MarkTranslated(lyric);
        return lyric;
    }

    /// <summary>
    ///     CSV structure for lyrics
    /// </summary>
    private class LyricCsvEntry
    {
        public float StartTime { get; set; }
        public float EndTime { get; set; }
        [CanBeNull] public string OriginalLyric { get; set; }
        [CanBeNull] public string TranslatedLyric { get; set; }
    }
}