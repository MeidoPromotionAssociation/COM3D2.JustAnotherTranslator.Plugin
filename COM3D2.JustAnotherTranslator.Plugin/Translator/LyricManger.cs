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
        Encoding = Encoding
            .UTF8, // The Encoding config is only used for byte counting. https://github.com/JoshClose/CsvHelper/issues/2278#issuecomment-2274128445
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
            "github.meidopromotionassociation.com3d2.justanothertranslator.plugin.hooks.lyric.lyricpatch");

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
        try
        {
            ClearSceneResources();
            SubtitleComponentManager.DestroyAllSubtitleComponents();
        }
        catch (Exception e)
        {
            LogManager.Error($"Clear scene resources failed/清理场景资源失败: {e.Message}");
        }
    }

    /// <summary>
    ///     将 DanceData 映射为 DanceInfoCsvEntry（列表序列化为逗号分隔字符串）
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private static DanceInfoCsvEntry MapDanceDataToCsvEntry(DanceData data)
    {
        if (data == null) return null;

        var entry = new DanceInfoCsvEntry
        {
            Id = data.ID.ToString(),
            Title = data.title,

            MusicName = null,
            TranslatedTitle = null,
            TranslatedCommentaryText = null,
            Mode = null,

            TitleFontSize = data.title_font_size,
            TitleOffsetY = data.title_offset_y,

            SceneName = data.scene_name,
            SelectCharaNum = data.select_chara_num,
            SampleImageName = data.sample_image_name,
            CommentaryText = data.commentary_text,
            BgmFileName = data.bgm_file_name,

            PresetName = StringTool.JoinStringList(data.preset_name),
            ScenarioProgress = data.scenario_progress,
            Term = data.Term.ToString(),

            AppealCutinName = data.AppealCutinName,
            ReversalCutinName = data.ReversalCutinName,
            DanceshowScene = data.danceshow_scene,
            DanceshowImage = data.danceshow_image,
            MaidOrder = StringTool.JoinIntList(data.maid_order),

            BgType = data.bgType.ToString(),
            InitialPlayable = data.InitialPlayable,
            IsPlayable = data.IsPlayable,
            RhythmGameCorrespond = data.RhythmGameCorrespond,

            SubtitleSheetName = data.SubtitleSheetName,
            IsShowSelectScene = data.isShowSelectScene,
            CsvFolderName = data.csvFolderName,

            KuchiPakuFileList = StringTool.JoinStringList(data.kuchiPakuFileList),
            MotionFileList = StringTool.JoinStringList(data.motionFileList),
            MovieFileName = data.movieFileName,
            BinaryFolderName = data.binaryFolderName,

            SingPartList = StringTool.SerializeSingPartList(data.singPartList),

            PersonalityFilter = StringTool.JoinStringList(data.personalityFilter),
            BodyFilterMode = data.bodyFilterMode.ToString()
        };

        return entry;
    }

    /// <summary>
    ///     将舞曲信息写入汇总文件
    /// </summary>
    /// <param name="musicName"></param>
    /// <param name="isKaraoke"></param>
    /// <param name="bgmFileName"></param>
    public static void DumpDanceInfo(string musicName, bool isKaraoke, string bgmFileName)
    {
        try
        {
            if (DanceMain.SelectDanceData == null)
                return;

            var infoEntry = MapDanceDataToCsvEntry(DanceMain.SelectDanceData);
            if (infoEntry != null)
            {
                TextTranslateManger.GetTranslateText(infoEntry.Title, out var translatedTitle,
                    true);
                TextTranslateManger.GetTranslateText(infoEntry.CommentaryText,
                    out var translatedCommentaryText, true);

                var mode = isKaraoke ? "Karaoke" : "Dance";

                infoEntry.MusicName = isKaraoke ? bgmFileName : musicName;
                infoEntry.TranslatedTitle = translatedTitle;
                infoEntry.TranslatedCommentaryText = translatedCommentaryText;
                infoEntry.Mode = mode;

                UpsertDanceInfoSummary(infoEntry, isKaraoke);
            }
        }
        catch (Exception e)
        {
            LogManager.Error($"Failed to export dance info/导出舞蹈信息失败: {e.Message}");
        }
    }

    /// <summary>
    ///     Upsert 舞曲信息到汇总文件 danceInfos.csv 或 danceInfosKaraoke.csv
    /// </summary>
    /// <param name="entry">要写入的条目</param>
    /// <param name="isKaraoke">是否为卡拉OK模式</param>
    private static void UpsertDanceInfoSummary(DanceInfoCsvEntry entry, bool isKaraoke)
    {
        if (entry == null) return;

        var key = isKaraoke ? entry.BgmFileName : entry.Id;
        if (string.IsNullOrEmpty(key))
        {
            LogManager.Error(
                "Upsert dance info skipped: Key is null or empty/跳过写入：主键为空");
            return;
        }

        try
        {
            var summaryPath = "";

            if (isKaraoke)
            {
                summaryPath = Path.Combine(JustAnotherTranslator.LyricPath, "_Karaoke");
                summaryPath = Path.Combine(summaryPath, "danceInfosKaraoke.csv");
            }
            else
            {
                summaryPath = Path.Combine(JustAnotherTranslator.LyricPath, "danceInfos.csv");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(summaryPath));

            var list = new List<DanceInfoCsvEntry>();
            if (File.Exists(summaryPath))
                using (var reader = new StreamReader(summaryPath, Encoding.UTF8))
                using (var csv = new CsvReader(reader, CsvConfig))
                {
                    foreach (var r in csv.GetRecords<DanceInfoCsvEntry>()) list.Add(r);
                }

            // 根据模式选择主键进行 Upsert
            var replaced = false;
            for (var i = 0; i < list.Count; i++)
            {
                var existing = list[i];
                if (existing == null) continue;

                var existingKey = isKaraoke ? existing.BgmFileName : existing.Id;
                if (string.Equals(existingKey, key, StringComparison.Ordinal))
                {
                    list[i] = entry;
                    replaced = true;
                    break;
                }
            }

            if (!replaced) list.Add(entry);

            // 根据模式选择排序方式
            if (isKaraoke)
                list.Sort(CompareDanceInfoByBgmFileName);
            else
                list.Sort(CompareDanceInfoById);

            // new UTF8Encoding(true) make sure it's UTF-8-BOM
            using (var writer = new StreamWriter(summaryPath, false, new UTF8Encoding(true)))
            using (var csv = new CsvWriter(writer, CsvConfig))
            {
                csv.WriteHeader(typeof(DanceInfoCsvEntry));
                foreach (var r in list) csv.WriteRecord(r);
                csv.NextRecord();
            }

            LogManager.Info($"Upsert dance info succeeded/写入 {Path.GetFileName(summaryPath)} 成功");
        }
        catch (Exception e)
        {
            LogManager.Error($"Upsert dance info failed/写入失败: {e.Message}");
        }
    }

    /// <summary>
    ///     比较 DanceInfoCsvEntry 的 Id
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    private static int CompareDanceInfoById(DanceInfoCsvEntry a, DanceInfoCsvEntry b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

        var sa = a.Id ?? string.Empty;
        var sb = b.Id ?? string.Empty;

        int ia, ib;
        if (int.TryParse(sa, out ia) && int.TryParse(sb, out ib))
            return ia.CompareTo(ib);

        return string.Compare(sa, sb, StringComparison.Ordinal);
    }

    /// <summary>
    ///     比较 DanceInfoCsvEntry 的 BgmFileName
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    private static int CompareDanceInfoByBgmFileName(DanceInfoCsvEntry a, DanceInfoCsvEntry b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

        var sa = a.BgmFileName ?? string.Empty;
        var sb = b.BgmFileName ?? string.Empty;

        return string.Compare(sa, sb, StringComparison.Ordinal);
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
    ///     创建音乐对应的字幕文件夹，并写入信息
    /// </summary>
    /// <param name="path"></param>
    private static void CreateMusicPath(string path)
    {
        try
        {
            Directory.CreateDirectory(path);

            // 创建歌词文件
            var lyricPath = Path.Combine(path, "lyric.csv");
            if (!File.Exists(lyricPath))
            {
                // UTF8Encoding(true) 明确为 UTF-8-BOM
                using (var writer = new StreamWriter(lyricPath, false, new UTF8Encoding(true)))
                using (var csv = new CsvWriter(writer, CsvConfig))
                {
                    csv.WriteHeader(typeof(LyricCsvEntry));
                    csv.NextRecord();
                }

                var infoPath = Path.Combine(path, "ThisLyricIsEmpty_这个歌词是空的.txt");
                if (!File.Exists(infoPath))
                    File.WriteAllText(infoPath,
                        "This lyric is empty, subtitle will not be displayed, please refer to the document to fill in\n这个歌词是空的，字幕将不会显示，请参考文档进行补充");
            }
        }
        catch (Exception e)
        {
            LogManager.Error($"Create music folder failed/创建音乐文件夹失败: {e.Message}\n{e}");
        }
    }

    /// <summary>
    ///     尝试加载字幕
    ///     如果字幕文件存在就加载
    /// </summary>
    /// <param name="path"></param>
    private static void TryToLoadLyric(string path)
    {
        path = Path.Combine(path, "lyric.csv");

        if (File.Exists(path))
        {
            LoadSubtitle(path);
            return;
        }

        LogManager.Info($"Lyric file not found: {path}/未找到字幕文件: {path}");
    }

    /// <summary>
    ///     清空字幕
    /// </summary>
    private static void ClearLyric()
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

        try
        {
            using (var reader =
                   new StreamReader(path,
                       Encoding.UTF8)) // This can process utf-8-sig as well, which is csv should be
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
            LogManager.Error(
                $"Error loading lyric file {path}: {ex.Message}/加载歌词文件出错 {path}: {ex.Message}");
        }
    }

    /// <summary>
    ///     处理舞蹈加载
    /// </summary>
    /// <param name="musicName"></param>
    public static void HandleDanceLoaded(string musicName)
    {
        if (string.IsNullOrEmpty(musicName))
        {
            LogManager.Warning("musicName is null, subtitle will not be displayed/音乐名称为空，字幕将不会显示");
            return;
        }


        var path = Path.Combine(JustAnotherTranslator.LyricPath, musicName);
        var isKaraoke = DanceMain.KaraokeMode;
        var bgmFileName = "";

        LogManager.Info($"Current dance name (musicName)/当前舞蹈（musicName）: {musicName}");


        // 如果是卡拉OK模式，那么 MusicName 是无效的，ID 也是无效的（0），只有 BgmFileName 是有效的
        if (isKaraoke)
        {
            if (DanceMain.SelectDanceData == null)
            {
                LogManager.Warning(
                    "DanceMain.SelectDanceData is null, subtitle will not be displayed/选择的舞蹈数据为空，字幕将不会显示");
                return;
            }

            bgmFileName = DanceMain.SelectDanceData.bgm_file_name;

            LogManager.Info($"Mode: Karaoke, Current dance internal name (BgmFileName)/模式：卡拉OK，当前舞蹈内部名称（BgmFileName）: {bgmFileName}");

            path = Path.Combine(JustAnotherTranslator.LyricPath, "_Karaoke");
            Directory.CreateDirectory(path);
            path = Path.Combine(path, bgmFileName);
        }
        else
        {
            LogManager.Info($"Mode: Dance, Current dance internal name (musicName)/模式：舞蹈，当前舞蹈内部名称（musicName）: {musicName}");
        }


        CreateMusicPath(path);
        TryToLoadLyric(path);

        if (JustAnotherTranslator.EnableDumpDanceInfo.Value)
            DumpDanceInfo(musicName, isKaraoke, bgmFileName);
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
        // 不存在歌唱的 Maid，因此 SpeakerName 显示始终为舞蹈主 Maid
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
            while (_currentLyricIndex < CurrentLyrics.Count &&
                   currentTime > CurrentLyrics[_currentLyricIndex].EndTime)
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
                    SubtitleComponentManager.HideSubtitleById(
                        SubtitleComponentManager.GetSpeakerSubtitleId(null));
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
                lyric = string.Concat(lyricEntry.TranslatedLyric, "\n", placeHolder,
                    lyricEntry.OriginalLyric);
                break;
            case JustAnotherTranslator.LyricSubtitleTypeEnum.OriginalAndTranslation:
                lyric = string.Concat(lyricEntry.OriginalLyric, "\n", placeHolder,
                    lyricEntry.TranslatedLyric);
                break;
            default:
                lyric = string.Concat(lyricEntry.TranslatedLyric, "\n", placeHolder,
                    lyricEntry.OriginalLyric);
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


    /// <summary>
    ///     CSV structure for dances info
    /// </summary>
    private class DanceInfoCsvEntry
    {
        // 以下字段基本对应于 DanceData 结构，用于在 CSV 中描述舞蹈信息，部分字段略有调整以便查看
        // 注：集合类型使用竖线(|)分隔（例如："a|b|c"），数值/布尔可留空表示不设置

        // 自定义字段或移动过顺序
        [CanBeNull] public string Id { get; set; }
        [CanBeNull] public string MusicName { get; set; }
        [CanBeNull] public string Title { get; set; }
        [CanBeNull] public string TranslatedTitle { get; set; }
        [CanBeNull] public string CommentaryText { get; set; }
        [CanBeNull] public string TranslatedCommentaryText { get; set; }
        [CanBeNull] public string Mode { get; set; }

        // 显示相关
        public int? TitleFontSize { get; set; }
        public int? TitleOffsetY { get; set; }

        // 场景与资源
        [CanBeNull] public string SceneName { get; set; }
        public int? SelectCharaNum { get; set; }
        [CanBeNull] public string SampleImageName { get; set; }
        [CanBeNull] public string BgmFileName { get; set; }

        // 预设、进度与标签
        [CanBeNull] public string PresetName { get; set; } // 多值用竖线(|)分隔
        public int? ScenarioProgress { get; set; }
        [CanBeNull] public string Term { get; set; }

        // 演出相关
        [CanBeNull] public string AppealCutinName { get; set; }
        [CanBeNull] public string ReversalCutinName { get; set; }
        [CanBeNull] public string DanceshowScene { get; set; }
        [CanBeNull] public string DanceshowImage { get; set; }
        [CanBeNull] public string MaidOrder { get; set; } // 以竖线(|)分隔的整数序列

        // 背景与开关
        [CanBeNull] public string BgType { get; set; }
        public bool? InitialPlayable { get; set; }
        public bool? IsPlayable { get; set; }
        public bool? RhythmGameCorrespond { get; set; }

        // 字幕、可见性与目录
        [CanBeNull] public string SubtitleSheetName { get; set; }
        public bool? IsShowSelectScene { get; set; }
        [CanBeNull] public string CsvFolderName { get; set; }

        // 文件清单
        [CanBeNull] public string KuchiPakuFileList { get; set; } // 竖线(|)分隔
        [CanBeNull] public string MotionFileList { get; set; } // 竖线(|)分隔
        [CanBeNull] public string MovieFileName { get; set; }
        [CanBeNull] public string BinaryFolderName { get; set; }
        [CanBeNull] public string SingPartList { get; set; } // 竖线(|)分隔

        // 过滤器
        [CanBeNull] public string PersonalityFilter { get; set; } // 竖线(|)分隔
        [CanBeNull] public string BodyFilterMode { get; set; }
    }
}