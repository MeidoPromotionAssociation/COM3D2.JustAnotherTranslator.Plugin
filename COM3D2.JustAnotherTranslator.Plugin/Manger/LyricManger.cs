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
#if COM3D25_UNITY_2022
using System.Linq;
#endif

namespace COM3D2.JustAnotherTranslator.Plugin.Manger;

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

    /// 当前舞蹈或卡拉OK的歌词目录名
    private static string _currentLyricFolderName = "";

    /// 当前舞蹈ID
    private static string _currentDanceId = "";

    /// 当前官方 musicName
    private static string _currentMusicName = "";

    /// 当前官方 BgmFileName
    private static string _currentBgmFileName = "";

    /// 当前是否为卡拉OK模式
    private static bool _currentIsKaraoke;

    /// 当前卡拉OK乐曲ID
    private static string _currentKaraokeMusicId = "";

    /// 当前卡拉OK的主 .ogg 文件名
    private static string _currentKaraokeOggFileName = "";

    /// 当前卡拉OK场景名
    private static string _currentKaraokeSceneName = "";

    /// 当前卡拉OK缩略图名
    private static string _currentKaraokeThumbnailName = "";

    /// 当前卡拉OK插件类型
    private static string _currentKaraokePluginType = "";

    /// 当前卡拉OK二进制资源目录
    private static string _currentKaraokeBinaryFolderName = "";

    /// 当前卡拉OK口型文件列表
    private static string _currentKaraokeKuchiPakuFileList = "";

    /// 当前实际播放过的 .ogg 文件
    private static readonly List<string> CurrentPlayedOggFiles = new();

    /// 用于对实际播放过的 .ogg 文件去重
    private static readonly HashSet<string> CurrentPlayedOggFileSet =
        new(StringComparer.OrdinalIgnoreCase);


    /// <summary>
    ///     CsvHelper 配置
    /// </summary>
    /// CSV配置
#if COM3D25_UNITY_2022
    private static readonly CsvConfiguration CsvConfig = new(CultureInfo.InvariantCulture)
    {
        AllowComments = true,
        HasHeaderRecord = true,
        Encoding = Encoding
            .UTF8, // The Encoding config is only used for byte counting. https://github.com/JoshClose/CsvHelper/issues/2278#issuecomment-2274128445
        IgnoreBlankLines = true,
        PrepareHeaderForMatch = args => args.Header?.Trim().ToLowerInvariant(),
        ShouldSkipRecord = args =>
        {
            var record = args.Row?.Context?.Parser?.Record;
            return record == null || record.All(string.IsNullOrWhiteSpace);
        },
        MissingFieldFound = null
    };
#else
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
#endif

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
    ///     重置当前舞蹈上下文
    /// </summary>
    private static void ResetDanceContext()
    {
        _currentLyricFolderName = "";
        _currentDanceId = "";
        _currentMusicName = "";
        _currentBgmFileName = "";
        _currentIsKaraoke = false;
        _currentKaraokeMusicId = "";
        _currentKaraokeOggFileName = "";
        _currentKaraokeSceneName = "";
        _currentKaraokeThumbnailName = "";
        _currentKaraokePluginType = "";
        _currentKaraokeBinaryFolderName = "";
        _currentKaraokeKuchiPakuFileList = "";
        _mainDanceMaidName = "";

        CurrentPlayedOggFiles.Clear();
        CurrentPlayedOggFileSet.Clear();
    }

    /// <summary>
    ///     捕获当前卡拉OK上下文
    /// </summary>
    /// <param name="karaokeData"></param>
    private static void CaptureKaraokeContext(KaraokeDataManager.MusicData karaokeData)
    {
        if (karaokeData == null)
            return;

        _currentKaraokeMusicId = karaokeData.ID.ToString(CultureInfo.InvariantCulture);
        _currentKaraokeOggFileName = karaokeData.strFileNameOgg ?? string.Empty;
        _currentKaraokeSceneName = karaokeData.strSceneName ?? string.Empty;
        _currentKaraokeThumbnailName = karaokeData.strThumbnailName ?? string.Empty;
        _currentKaraokePluginType = karaokeData.pluginType ?? string.Empty;
        _currentKaraokeBinaryFolderName = karaokeData.binaryFolderName ?? string.Empty;
        _currentKaraokeKuchiPakuFileList =
            StringTool.JoinStringList(karaokeData.kuchiPakuFileList) ?? string.Empty;
    }

    /// <summary>
    ///     输出当前卡拉OK上下文
    /// </summary>
    /// <param name="selectDanceData"></param>
    private static void LogKaraokeContext(DanceData selectDanceData)
    {
        var danceDataId = selectDanceData != null
            ? selectDanceData.ID.ToString(CultureInfo.InvariantCulture)
            : string.Empty;
        var danceBinaryFolderName = selectDanceData?.binaryFolderName ?? string.Empty;
        var danceKuchiPakuFileList =
            StringTool.JoinStringList(selectDanceData?.kuchiPakuFileList) ?? string.Empty;

        var lyricFolderSource = string.Equals(_currentLyricFolderName, _currentKaraokeMusicId,
            StringComparison.Ordinal)
            ? "KaraokeMusicId"
            : "fallback BgmFileName";

        LogManager.Info(
            $"Karaoke lyric folder resolved to {lyricFolderSource}: {_currentLyricFolderName} (KaraokeMusicId: {_currentKaraokeMusicId}, KaraokeOggFileName: {_currentKaraokeOggFileName}, ScriptInjectedBgmFileName: {_currentBgmFileName}, MusicName: {_currentMusicName})/卡拉OK歌词目录解析结果: {_currentLyricFolderName}（优先主键为 KaraokeMusicId: {_currentKaraokeMusicId}，KaraokeOggFileName: {_currentKaraokeOggFileName}，脚本注入的 BgmFileName: {_currentBgmFileName}，MusicName: {_currentMusicName}）");
        LogManager.Info(
            $"Karaoke data summary - KaraokeMusicId: {_currentKaraokeMusicId}, KaraokeOggFileName: {_currentKaraokeOggFileName}, KaraokeSceneName: {_currentKaraokeSceneName}, KaraokeThumbnailName: {_currentKaraokeThumbnailName}, KaraokePluginType: {_currentKaraokePluginType}, KaraokeBinaryFolderName: {_currentKaraokeBinaryFolderName}, KaraokeKuchiPakuFileList: {_currentKaraokeKuchiPakuFileList}/卡拉OK数据摘要 - KaraokeMusicId: {_currentKaraokeMusicId}，KaraokeOggFileName: {_currentKaraokeOggFileName}，KaraokeSceneName: {_currentKaraokeSceneName}，KaraokeThumbnailName: {_currentKaraokeThumbnailName}，KaraokePluginType: {_currentKaraokePluginType}，KaraokeBinaryFolderName: {_currentKaraokeBinaryFolderName}，KaraokeKuchiPakuFileList: {_currentKaraokeKuchiPakuFileList}");
        LogManager.Info(
            $"Karaoke injected DanceData summary - DanceDataId: {danceDataId}, DanceDataBgmFileName: {_currentBgmFileName}, DanceDataBinaryFolderName: {danceBinaryFolderName}, DanceDataKuchiPakuFileList: {danceKuchiPakuFileList}/卡拉OK注入 DanceData 摘要 - DanceDataId: {danceDataId}，DanceDataBgmFileName: {_currentBgmFileName}，DanceDataBinaryFolderName: {danceBinaryFolderName}，DanceDataKuchiPakuFileList: {danceKuchiPakuFileList}");
    }

    /// <summary>
    ///     将传入的音频文件名规范化为用于日志和 CSV 的 .ogg 文件名
    /// </summary>
    /// <param name="audioFileName"></param>
    /// <returns></returns>
    private static string NormalizePlayedOggFileName(string audioFileName)
    {
        if (StringTool.IsNullOrWhiteSpace(audioFileName))
            return null;

        var normalizedPath = audioFileName.Replace('\\', '/');
        var fileName = Path.GetFileName(normalizedPath);
        if (StringTool.IsNullOrWhiteSpace(fileName))
            fileName = normalizedPath;

        if (string.IsNullOrEmpty(Path.GetExtension(fileName)))
            fileName += ".ogg";

        return fileName;
    }

    /// <summary>
    ///     记录当前舞蹈实际播放的 .ogg 文件
    /// </summary>
    /// <param name="audioFileName"></param>
    /// <param name="isParallel"></param>
    public static void TrackPlayedDanceAudio(string audioFileName, bool isParallel)
    {
        if (string.IsNullOrEmpty(_currentLyricFolderName))
            return;

        var normalizedFileName = NormalizePlayedOggFileName(audioFileName);
        if (string.IsNullOrEmpty(normalizedFileName))
            return;

        if (!CurrentPlayedOggFileSet.Add(normalizedFileName))
            return;

        CurrentPlayedOggFiles.Add(normalizedFileName);

        var modeNameEn = _currentIsKaraoke ? "karaoke" : "dance";
        var modeNameZh = _currentIsKaraoke ? "卡拉OK" : "舞蹈";
        var audioTypeEn = isParallel ? "parallel track" : "main track";
        var audioTypeZh = isParallel ? "并行轨道" : "主轨道";
        if (_currentIsKaraoke)
            LogManager.Info(
                $"Tracked played {modeNameEn} audio ({audioTypeEn}): {normalizedFileName} (KaraokeMusicId: {_currentKaraokeMusicId}, LyricFolderName: {_currentLyricFolderName}, KaraokeOggFileName: {_currentKaraokeOggFileName}, DanceDataBgmFileName: {_currentBgmFileName})/记录到卡拉OK实际播放音频（{audioTypeZh}）: {normalizedFileName}（KaraokeMusicId: {_currentKaraokeMusicId}，歌词目录名: {_currentLyricFolderName}，KaraokeOggFileName: {_currentKaraokeOggFileName}，DanceDataBgmFileName: {_currentBgmFileName}）");
        else
            LogManager.Info(
                $"Tracked played {modeNameEn} audio ({audioTypeEn}): {normalizedFileName}/记录到{modeNameZh}实际播放音频（{audioTypeZh}）: {normalizedFileName}");

        TryDumpDanceInfo();
    }

    /// <summary>
    ///     输出当前舞蹈实际播放音频摘要
    /// </summary>
    private static void LogTrackedDanceAudioSummary()
    {
        if (string.IsNullOrEmpty(_currentLyricFolderName))
            return;

        var playedOggFiles = StringTool.JoinStringList(CurrentPlayedOggFiles) ?? string.Empty;
        if (_currentIsKaraoke)
            LogManager.Info(
                $"Karaoke audio summary - KaraokeMusicId: {_currentKaraokeMusicId}, LyricFolderName: {_currentLyricFolderName}, KaraokeOggFileName: {_currentKaraokeOggFileName}, PlayedOggFiles: {playedOggFiles}/卡拉OK音频摘要 - KaraokeMusicId: {_currentKaraokeMusicId}，歌词目录名: {_currentLyricFolderName}，KaraokeOggFileName: {_currentKaraokeOggFileName}，实际播放Ogg: {playedOggFiles}");
        else
            LogManager.Info(
                $"Dance audio summary - Id: {_currentDanceId}, LyricFolderName: {_currentLyricFolderName}, PlayedOggFiles: {playedOggFiles}/舞蹈音频摘要 - ID: {_currentDanceId}，歌词目录名: {_currentLyricFolderName}，实际播放Ogg: {playedOggFiles}");
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
            LyricFolderName = null,
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
            PlayedOggFiles = null,

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
    ///     根据当前舞蹈上下文写入汇总文件
    /// </summary>
    private static void DumpDanceInfo()
    {
        try
        {
            if (string.IsNullOrEmpty(_currentLyricFolderName))
                return;

            var selectDanceData = DanceMain.SelectDanceData;
            if (_currentIsKaraoke)
            {
                var karaokeInfoEntry = BuildKaraokeInfoCsvEntry(selectDanceData);
                if (karaokeInfoEntry == null)
                    return;

                UpsertKaraokeInfoSummary(karaokeInfoEntry);
                return;
            }

            if (selectDanceData == null)
                return;

            var infoEntry = MapDanceDataToCsvEntry(selectDanceData);
            if (infoEntry == null)
                return;

            TextTranslateManger.GetTranslateText(infoEntry.Title, out var translatedTitle);
            TextTranslateManger.GetTranslateText(infoEntry.CommentaryText,
                out var translatedCommentaryText);

            infoEntry.MusicName = _currentMusicName;
            infoEntry.LyricFolderName = _currentLyricFolderName;
            infoEntry.TranslatedTitle = translatedTitle;
            infoEntry.TranslatedCommentaryText = translatedCommentaryText;
            infoEntry.Mode = "Dance";
            infoEntry.PlayedOggFiles = StringTool.JoinStringList(CurrentPlayedOggFiles);

            UpsertDanceInfoSummary(infoEntry);
        }
        catch (Exception e)
        {
            LogManager.Error($"Failed to export dance info/导出舞蹈信息失败: {e.Message}");
        }
    }

    /// <summary>
    ///     按配置决定是否转储当前舞蹈信息
    /// </summary>
    private static KaraokeInfoCsvEntry BuildKaraokeInfoCsvEntry(DanceData selectDanceData)
    {
        var title = selectDanceData?.title;
        var commentaryText = selectDanceData?.commentary_text;
        var translatedTitle = string.Empty;
        var translatedCommentaryText = string.Empty;

        if (!string.IsNullOrEmpty(title))
            TextTranslateManger.GetTranslateText(title, out translatedTitle);

        if (!string.IsNullOrEmpty(commentaryText))
            TextTranslateManger.GetTranslateText(commentaryText, out translatedCommentaryText);

        return new KaraokeInfoCsvEntry
        {
            KaraokeMusicId = _currentKaraokeMusicId,
            LyricFolderName = _currentLyricFolderName,
            Title = title,
            TranslatedTitle = translatedTitle,
            CommentaryText = commentaryText,
            TranslatedCommentaryText = translatedCommentaryText,
            KaraokeOggFileName = _currentKaraokeOggFileName,
            PlayedOggFiles = StringTool.JoinStringList(CurrentPlayedOggFiles),
            KaraokeSceneName = _currentKaraokeSceneName,
            KaraokeThumbnailName = _currentKaraokeThumbnailName,
            KaraokePluginType = _currentKaraokePluginType,
            KaraokeBinaryFolderName = _currentKaraokeBinaryFolderName,
            KaraokeKuchiPakuFileList = _currentKaraokeKuchiPakuFileList,
            DanceDataId = selectDanceData != null
                ? selectDanceData.ID.ToString(CultureInfo.InvariantCulture)
                : string.Empty,
            DanceDataBgmFileName = selectDanceData?.bgm_file_name ?? _currentBgmFileName,
            DanceDataBinaryFolderName = selectDanceData?.binaryFolderName ?? string.Empty,
            DanceDataKuchiPakuFileList =
                StringTool.JoinStringList(selectDanceData?.kuchiPakuFileList) ?? string.Empty,
            DanceDataCsvFolderName = selectDanceData?.csvFolderName ?? string.Empty,
            ObservedMusicName = _currentMusicName,
            SubtitleSheetName = selectDanceData?.SubtitleSheetName ?? string.Empty,
            DanceSceneName = selectDanceData?.scene_name ?? string.Empty,
            Mode = "Karaoke"
        };
    }

    private static void TryDumpDanceInfo()
    {
        if (!JustAnotherTranslator.EnableDumpDanceInfo.Value)
            return;

        DumpDanceInfo();
    }

    /// <summary>
    ///     Upsert 普通舞曲信息到汇总文件 danceInfos.csv
    /// </summary>
    /// <param name="entry">要写入的条目</param>
    private static void UpsertDanceInfoSummary(DanceInfoCsvEntry entry)
    {
        if (entry == null) return;

        var key = entry.Id;
        if (string.IsNullOrEmpty(key))
        {
            LogManager.Error(
                "Upsert dance info skipped: Key is null or empty/跳过写入：主键为空");
            return;
        }

        try
        {
            var summaryPath = Path.Combine(JustAnotherTranslator.LyricPath, "danceInfos.csv");

            Directory.CreateDirectory(Path.GetDirectoryName(summaryPath));

            var list = new List<DanceInfoCsvEntry>();
            if (File.Exists(summaryPath))
                using (var reader = new StreamReader(summaryPath, Encoding.UTF8, true, 4096))
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

                var existingKey = existing.Id;
                if (string.Equals(existingKey, key, StringComparison.Ordinal))
                {
                    list[i] = entry;
                    replaced = true;
                    break;
                }
            }

            if (!replaced) list.Add(entry);

            // 根据模式选择排序方式
            list.Sort(CompareDanceInfoById);

            // new UTF8Encoding(true) make sure it's UTF-8-BOM
            using (var writer = new StreamWriter(summaryPath, false, new UTF8Encoding(true)))
            using (var csv = new CsvWriter(writer, CsvConfig))
            {
                csv.WriteHeader(typeof(DanceInfoCsvEntry));
#if COM3D25_UNITY_2022
                csv.NextRecord();
#endif
                foreach (var r in list)
                {
                    csv.WriteRecord(r);
#if COM3D25_UNITY_2022
                    csv.NextRecord();
#endif
                }
#if !COM3D25_UNITY_2022
                csv.NextRecord();
#endif
            }

            LogManager.Info($"Upsert dance info succeeded/写入 {Path.GetFileName(summaryPath)} 成功");
        }
        catch (Exception e)
        {
            LogManager.Error($"Upsert dance info failed/写入失败: {e.Message}");
        }
    }

    /// <summary>
    ///     Upsert 卡拉OK模式舞蹈信息到汇总文件 danceInfosKaraoke.csv
    /// </summary>
    /// <param name="entry">要写入的条目</param>
    private static void UpsertKaraokeInfoSummary(KaraokeInfoCsvEntry entry)
    {
        if (entry == null) return;

        var key = entry.KaraokeMusicId;
        if (string.IsNullOrEmpty(key))
        {
            LogManager.Warning(
                "Upsert karaoke info skipped: KaraokeMusicId is null or empty/跳过写入卡拉OK汇总：KaraokeMusicId 为空");
            return;
        }

        try
        {
            var summaryPath = Path.Combine(JustAnotherTranslator.KaraokeLyricPath,
                "danceInfosKaraoke.csv");
            Directory.CreateDirectory(Path.GetDirectoryName(summaryPath));

            var list = new List<KaraokeInfoCsvEntry>();
            if (File.Exists(summaryPath))
                try
                {
                    using (var reader = new StreamReader(summaryPath, Encoding.UTF8, true, 4096))
                    using (var csv = new CsvReader(reader, CsvConfig))
                    {
                        foreach (var r in csv.GetRecords<KaraokeInfoCsvEntry>())
                            if (r != null && !string.IsNullOrEmpty(r.KaraokeMusicId))
                                list.Add(r);
                    }
                }
                catch (Exception e)
                {
                    LogManager.Warning(
                        $"Existing danceInfosKaraoke.csv could not be parsed with the new karaoke schema and will be rebuilt/旧版 danceInfosKaraoke.csv 无法按新版卡拉OK结构解析，将重建: {e.Message}");
                }

            var replaced = false;
            for (var i = 0; i < list.Count; i++)
            {
                var existing = list[i];
                if (existing == null) continue;

                if (string.Equals(existing.KaraokeMusicId, key, StringComparison.Ordinal))
                {
                    list[i] = entry;
                    replaced = true;
                    break;
                }
            }

            if (!replaced) list.Add(entry);

            list.Sort(CompareKaraokeInfoByMusicId);

            using (var writer = new StreamWriter(summaryPath, false, new UTF8Encoding(true)))
            using (var csv = new CsvWriter(writer, CsvConfig))
            {
                csv.WriteHeader(typeof(KaraokeInfoCsvEntry));
#if COM3D25_UNITY_2022
                csv.NextRecord();
#endif
                foreach (var r in list)
                {
                    csv.WriteRecord(r);
#if COM3D25_UNITY_2022
                    csv.NextRecord();
#endif
                }
#if !COM3D25_UNITY_2022
                csv.NextRecord();
#endif
            }

            LogManager.Info($"Upsert karaoke info succeeded/写入 {Path.GetFileName(summaryPath)} 成功");
        }
        catch (Exception e)
        {
            LogManager.Error($"Upsert karaoke info failed/写入卡拉OK汇总失败: {e.Message}");
        }
    }


    /// <summary>
    ///     通过 ID 比较两个 <see cref="DanceInfoCsvEntry" /> 对象。
    /// </summary>
    /// <param name="a">要比较的第一个 <see cref="DanceInfoCsvEntry" /> 对象。</param>
    /// <param name="b">要比较的第二个 <see cref="DanceInfoCsvEntry" /> 对象。</param>
    /// <returns>
    ///     返回一个整数，指示被比较对象的相对顺序。
    ///     如果 <paramref name="a" /> 小于 <paramref name="b" />，则返回小于零的值。
    ///     如果 <paramref name="a" /> 等于 <paramref name="b" />，则返回零。
    ///     如果 <paramref name="a" /> 大于 <paramref name="b" />，则返回大于零的值。
    /// </returns>
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
    ///     根据 KaraokeMusicId 属性比较两个 KaraokeInfoCsvEntry 实例。
    ///     如果 KaraokeMusicId 值为整数，则按数值进行比较；
    ///     否则，按字符串进行比较。
    /// </summary>
    /// <param name="a">要比较的第一个 KaraokeInfoCsvEntry 实例。</param>
    /// <param name="b">要比较的第二个 KaraokeInfoCsvEntry 实例。</param>
    /// <returns>
    ///     一个整数值，指示条目的相对顺序：
    ///     如果两者相等或为空，则返回 0；如果第一个小于第二个，则返回 -1；
    ///     如果第一个大于第二个，则返回 1。
    /// </returns>
    private static int CompareKaraokeInfoByMusicId(KaraokeInfoCsvEntry a, KaraokeInfoCsvEntry b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

        var sa = a.KaraokeMusicId ?? string.Empty;
        var sb = b.KaraokeMusicId ?? string.Empty;

        int ia, ib;
        if (int.TryParse(sa, out ia) && int.TryParse(sb, out ib))
            return ia.CompareTo(ib);

        return string.Compare(sa, sb, StringComparison.Ordinal);
    }

    /// <summary>
    ///     清理场景资源
    /// </summary>
    private static void ClearSceneResources()
    {
        ClearLyric();
        _rhythmActionMgr = null;
        ResetDanceContext();

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
                   new StreamReader(path, Encoding.UTF8, true,
                       4096)) // This can process utf-8-sig as well, which is csv should be
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
        ClearLyric();
        ResetDanceContext();

        _currentMusicName = musicName ?? string.Empty;
        _currentIsKaraoke = DanceMain.KaraokeMode;

        var selectDanceData = DanceMain.SelectDanceData;
        if (selectDanceData == null)
        {
            LogManager.Warning(
                "DanceMain.SelectDanceData is null, subtitle will not be displayed/选择的舞蹈数据为空，字幕将不会显示");
            return;
        }

        _currentDanceId = selectDanceData.ID.ToString(CultureInfo.InvariantCulture);
        _currentBgmFileName = selectDanceData.bgm_file_name ?? string.Empty;

        var path = JustAnotherTranslator.LyricPath;

        LogManager.Info($"Current dance name (musicName)/当前舞蹈（musicName）: {_currentMusicName}");

        if (_currentIsKaraoke)
        {
            var selectKaraokeData = DanceMain.SelectKaraokeData;
            CaptureKaraokeContext(selectKaraokeData);
            _currentLyricFolderName = _currentKaraokeMusicId;

            if (string.IsNullOrEmpty(_currentLyricFolderName))
            {
                _currentLyricFolderName = _currentBgmFileName;

                if (!string.IsNullOrEmpty(_currentLyricFolderName))
                    LogManager.Warning(
                        $"KaraokeMusicId is null, fallback to BgmFileName for lyric folder: {_currentLyricFolderName}/KaraokeMusicId 为空，歌词目录回退到 BgmFileName: {_currentLyricFolderName}");
            }

            if (selectKaraokeData == null)
                LogManager.Warning(
                    "DanceMain.SelectKaraokeData is null, karaoke diagnostics will rely on the injected DanceData only/当前 DanceMain.SelectKaraokeData 为空，卡拉OK诊断将只能依赖注入后的 DanceData");

            if (string.IsNullOrEmpty(_currentLyricFolderName))
            {
                LogManager.Warning(
                    "BgmFileName is null, subtitle will not be displayed/BgmFileName 为空，字幕将不会显示");
                return;
            }

            Directory.CreateDirectory(JustAnotherTranslator.KaraokeLyricPath);
            path = Path.Combine(JustAnotherTranslator.KaraokeLyricPath, _currentLyricFolderName);
            LogManager.Info(
                $"Mode: Karaoke, Current dance name (musicName)/模式: 卡拉OK，当前观察到的 musicName: {_currentMusicName}");
            LogKaraokeContext(selectDanceData);

            LogManager.Info(
                $"Mode: Karaoke, Current dance internal name (DanceData.bgm_file_name)/模式：卡拉OK，当前注入后的 DanceData.bgm_file_name: {_currentBgmFileName}");
            LogManager.Info(
                $"Karaoke lyric folder resolved summary/卡拉OK歌词目录解析摘要: {_currentLyricFolderName}");

            if (DanceMain.SelectKaraokeData != null)
                LogManager.Info(
                    $"Observed KaraokeDataManager.MusicData.ID: {DanceMain.SelectKaraokeData.ID}/观察到的卡拉OK MusicData.ID: {DanceMain.SelectKaraokeData.ID}");
        }
        else
        {
            _currentLyricFolderName = _currentDanceId;

            if (string.IsNullOrEmpty(_currentLyricFolderName))
            {
                LogManager.Warning(
                    "dance ID is null, subtitle will not be displayed/舞蹈 ID 为空，字幕将不会显示");
                return;
            }

            path = Path.Combine(path, _currentLyricFolderName);

            LogManager.Info(
                $"Mode: Dance, Current dance internal name (musicName)/模式：舞蹈，当前舞蹈内部名称（musicName）: {_currentMusicName}");
            LogManager.Info(
                $"Dance lyric folder resolved to ID: {_currentLyricFolderName} (MusicName: {_currentMusicName}, CsvFolderName: {selectDanceData.csvFolderName})/舞蹈歌词目录按 ID 解析: {_currentLyricFolderName}（MusicName: {_currentMusicName}，CsvFolderName: {selectDanceData.csvFolderName}）");
        }

        CreateMusicPath(path);
        TryToLoadLyric(path);
        TryDumpDanceInfo();
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

        TryDumpDanceInfo();

        // Start the playback monitor coroutine
        if (_playbackMonitorCoroutineID == null)
            _playbackMonitorCoroutineID = CoroutineManager.LaunchCoroutine(PlaybackMonitor());
    }

    /// <summary>
    ///     处理舞蹈结束
    /// </summary>
    public static void HandleDanceEnd()
    {
        LogTrackedDanceAudioSummary();
        TryDumpDanceInfo();

        // Stop the playback monitor coroutine
        if (_playbackMonitorCoroutineID != null)
        {
            CoroutineManager.StopCoroutine(_playbackMonitorCoroutineID);
            _playbackMonitorCoroutineID = null;
        }

        ClearLyric();
        SubtitleComponentManager.DestroyAllSubtitleComponents();
        _rhythmActionMgr = null;
        ResetDanceContext();
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

        TextTranslateManger.MarkTranslated(lyric);
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
        [CanBeNull] public string LyricFolderName { get; set; }
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
        [CanBeNull] public string PlayedOggFiles { get; set; } // 实际播放过的 .ogg，使用竖线(|)分隔

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

    private class KaraokeInfoCsvEntry
    {
        [CanBeNull] public string KaraokeMusicId { get; set; }
        [CanBeNull] public string LyricFolderName { get; set; }
        [CanBeNull] public string Title { get; set; }
        [CanBeNull] public string TranslatedTitle { get; set; }
        [CanBeNull] public string CommentaryText { get; set; }
        [CanBeNull] public string TranslatedCommentaryText { get; set; }
        [CanBeNull] public string KaraokeOggFileName { get; set; }
        [CanBeNull] public string PlayedOggFiles { get; set; }
        [CanBeNull] public string KaraokeSceneName { get; set; }
        [CanBeNull] public string KaraokeThumbnailName { get; set; }
        [CanBeNull] public string KaraokePluginType { get; set; }
        [CanBeNull] public string KaraokeBinaryFolderName { get; set; }
        [CanBeNull] public string KaraokeKuchiPakuFileList { get; set; }
        [CanBeNull] public string DanceDataId { get; set; }
        [CanBeNull] public string DanceDataBgmFileName { get; set; }
        [CanBeNull] public string DanceDataBinaryFolderName { get; set; }
        [CanBeNull] public string DanceDataKuchiPakuFileList { get; set; }
        [CanBeNull] public string DanceDataCsvFolderName { get; set; }
        [CanBeNull] public string ObservedMusicName { get; set; }
        [CanBeNull] public string SubtitleSheetName { get; set; }
        [CanBeNull] public string DanceSceneName { get; set; }
        [CanBeNull] public string Mode { get; set; }
    }
}