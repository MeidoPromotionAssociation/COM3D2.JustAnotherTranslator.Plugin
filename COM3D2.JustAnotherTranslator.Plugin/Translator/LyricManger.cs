using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using COM3D2.JustAnotherTranslator.Plugin.Hooks.Lyric;
using COM3D2.JustAnotherTranslator.Plugin.Subtitle;
using HarmonyLib;

namespace COM3D2.JustAnotherTranslator.Plugin.Translator;

public struct LyricEntry
{
    public float StartTime;
    public float EndTime;
    public string OriginalText;
    public string TranslatedText;
}

public static class LyricManger
{
    private static bool _initialized;

    private static Harmony _lyricPatch;

    public static List<LyricEntry> CurrentLyrics = new();

    public static void Init()
    {
        if (_initialized) return;

        // 初始化字幕组件管理器
        SubtitleComponentManager.Init();

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
            File.WriteAllText(lyricPath, string.Empty);
        }
        catch
        {
            LogManager.Error("Create music folder failed/创建音乐文件夹失败");
        }
    }


    /// <summary>
    ///     尝试加载字幕
    ///     如果字幕文件存在就加载
    /// </summary>
    public static void TryToLoadLyric(string musicName)
    {
        var path = Path.Combine(JustAnotherTranslator.LyricPath, musicName) + "/lyric.csv";

        if (File.Exists(path))
            LoadSubtitle(path);
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

        if (!File.Exists(path))
        {
            LogManager.Info($"Lyric file not found: {path}/未找到字幕文件: {path}");
            return;
        }

        try
        {
            using (var reader = new StreamReader(path))
            {
                string line;
                var lineNumber = 0;

                // Try to read and parse the first line
                if ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    var trimmedLine = line.Trim();
                    if (!string.IsNullOrEmpty(trimmedLine))
                    {
                        var fields = trimmedLine.Split(',');
                        // Ensure fields array is long enough before accessing elements
                        var firstStartTimeStr = fields.Length > 0 ? fields[0].Trim() : string.Empty;
                        var firstEndTimeStr = fields.Length > 1 ? fields[1].Trim() : string.Empty;

                        if (fields.Length == 4 &&
                            !string.IsNullOrEmpty(firstStartTimeStr) &&
                            !string.IsNullOrEmpty(firstEndTimeStr) &&
                            float.TryParse(firstStartTimeStr, NumberStyles.Any, CultureInfo.InvariantCulture,
                                out var firstLineStartTime) &&
                            float.TryParse(firstEndTimeStr, NumberStyles.Any, CultureInfo.InvariantCulture,
                                out var firstLineEndTime))
                            // First line is valid data
                            CurrentLyrics.Add(new LyricEntry
                            {
                                StartTime = firstLineStartTime,
                                EndTime = firstLineEndTime,
                                OriginalText = fields[2].Trim(), // Can be empty
                                TranslatedText = fields[3].Trim() // Can be empty
                            });
                        else
                            // First line is not valid data (header, malformed, or empty/invalid time fields)
                            LogManager.Info(
                                $"Skipping line {lineNumber} in {path} as potential header or malformed data (e.g., empty/invalid time fields): {line}/跳过行 {lineNumber} 在 {path} 中作为可能的标题或格式错误的数据(例如，空/无效的时间字段): {line}");
                    }
                }

                // Process remaining lines
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    var trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine)) continue;

                    var fields = trimmedLine.Split(',');

                    if (fields.Length != 4)
                    {
                        LogManager.Warning(
                            $"Incorrect number of fields in lyric file: {path}, line: {lineNumber} - Expected 4, got {fields.Length}. Content: {line}/字幕文件中的字段数量不正确: {path}, 行: {lineNumber} - 预期 4, 实际 {fields.Length}. 内容: {line}");
                        continue;
                    }

                    var startTimeStr = fields[0].Trim();
                    var endTimeStr = fields[1].Trim();
                    var originalText = fields[2].Trim(); // Can be empty
                    var translatedText = fields[3].Trim(); // Can be empty

                    if (string.IsNullOrEmpty(startTimeStr) || string.IsNullOrEmpty(endTimeStr))
                    {
                        LogManager.Info(
                            $"Time field(s) cannot be empty in lyric file: {path}, line: {lineNumber} - Content: {line}/字幕文件中的时间字段不能为空: {path}, 行: {lineNumber} - 内容: {line}");
                        continue;
                    }

                    if (float.TryParse(startTimeStr, NumberStyles.Any, CultureInfo.InvariantCulture,
                            out var startTime) &&
                        float.TryParse(endTimeStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var endTime))
                        CurrentLyrics.Add(new LyricEntry
                        {
                            StartTime = startTime,
                            EndTime = endTime,
                            OriginalText = originalText,
                            TranslatedText = translatedText
                        });
                    else
                        LogManager.Info(
                            $"Could not parse non-empty time values in lyric file: {path}, line: {lineNumber} - Content: {line}/无法解析字幕文件中的非空时间值: {path}, 行: {lineNumber} - 内容: {line}");
                }
            }

            LogManager.Debug($"Successfully loaded {CurrentLyrics.Count} lyric entries from {path}");
        }
        catch (Exception ex)
        {
            LogManager.Error($"Error loading lyric file {path}: {ex.Message}/加载字幕文件出错 {path}: {ex.Message}");
        }
    }
}