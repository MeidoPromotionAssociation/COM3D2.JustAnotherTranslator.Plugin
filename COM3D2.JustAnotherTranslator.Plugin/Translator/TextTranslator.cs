using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using COM3D2.JustAnotherTranslator.Plugin.Hooks;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;

namespace COM3D2.JustAnotherTranslator.Plugin.Translator;

public static class TextTranslator
{
    // 文件读取缓冲区大小 (默认16MB)
    private const int DefaultBufferSize = 16 * 1024 * 1024;

    // 文件读取块大小 (默认4MB)
    private const int FileReadChunkSize = 4 * 1024 * 1024;

    // 翻译字典，存储原文和翻译的映射关系
    public static Dictionary<string, string> TranslationDict = new();

    // 正则表达式翻译字典
    public static Dictionary<Regex, string> RegexTranslationDict = new();

    private static Harmony _textTranslatePatch;

    private static bool _initialized;

    // 异步加载器
    private static AsyncTextLoader _asyncLoader;

    // 加载状态
    public static bool IsLoading { get; private set; }

    // 加载进度 (0.0-1.0)
    public static float LoadingProgress { get; private set; }

    // 已处理文件数
    public static int FilesProcessed { get; private set; }

    // 总文件数
    public static int TotalFiles { get; private set; }

    public static void Init()
    {
        if (_initialized) return;

        // 检查是否启用异步加载
        if (JustAnotherTranslator.EnableAsyncLoading.Value)
            LoadTextAsync();
        else
            LoadText();

        // 创建 Harmony 实例
        _textTranslatePatch = Harmony.CreateAndPatchAll(typeof(TextTranslatePatch));

        // 手动注册 NGUIText.WrapText 方法的补丁
        TextTranslatePatch.RegisterNGUITextPatches(_textTranslatePatch);

        _initialized = true;
    }

    public static void Unload()
    {
        if (!_initialized) return;

        // 如果异步加载正在进行，取消它
        if (IsLoading && _asyncLoader != null)
        {
            _asyncLoader.Cancel();
            IsLoading = false;
        }

        if (_textTranslatePatch != null)
        {
            _textTranslatePatch.UnpatchSelf();
            _textTranslatePatch = null;
        }

        TranslationDict.Clear();

        _initialized = false;
    }

    private static void LoadTextAsync()
    {
        // 重置状态
        IsLoading = true;
        LoadingProgress = 0f;
        FilesProcessed = 0;
        TotalFiles = 0;

        // 创建异步加载器
        _asyncLoader = new AsyncTextLoader(
            JustAnotherTranslator.TranslationTextPath,
            OnLoadingProgress,
            OnLoadingComplete
        );

        LogManager.Info("Starting asynchronous translation loading/开始异步加载翻译");

        // 开始异步加载
        _asyncLoader.StartLoading();
    }

    // 加载进度回调
    private static void OnLoadingProgress(float progress, int filesProcessed, int totalFiles)
    {
        LoadingProgress = progress;
        FilesProcessed = filesProcessed;
        TotalFiles = totalFiles;

        // 每处理10个文件或进度变化超过10%时输出日志
        if (filesProcessed % 10 == 0 || (int)(progress * 100) % 10 == 0)
            LogManager.Info(
                $"Translation loading progress: {progress:P0} ({filesProcessed}/{totalFiles})/翻译加载进度: {progress:P0} ({filesProcessed}/{totalFiles})");
    }

    // 加载完成回调
    private static void OnLoadingComplete(Dictionary<string, string> result, Dictionary<Regex, string> regexResult,
        int totalEntries, int totalFiles,
        long elapsedMilliseconds)
    {
        // 更新状态
        IsLoading = false;
        LoadingProgress = 1.0f;
        FilesProcessed = totalFiles;
        TotalFiles = totalFiles;

        // 更新翻译字典
        TranslationDict = result;
        RegexTranslationDict = regexResult;

        LogManager.Info(
            $"Asynchronous translation loading complete: {totalEntries} entries from {totalFiles} files, cost {elapsedMilliseconds} ms/异步翻译加载完成: 从 {totalFiles} 个文件中加载了 {totalEntries} 条翻译，耗时 {elapsedMilliseconds} 毫秒");
    }

    private static void LoadText()
    {
        // 记录加载时间
        var sw = new Stopwatch();
        sw.Start();

        TranslationDict.Clear();
        var totalFiles = 0;
        var totalEntries = 0;

        try
        {
            if (!Directory.Exists(JustAnotherTranslator.TranslationTextPath))
            {
                LogManager.Warning(
                    "Translation directory not found, try to create/未找到翻译目录，尝试创建: " +
                    JustAnotherTranslator.TranslationTextPath);
                try
                {
                    Directory.CreateDirectory(JustAnotherTranslator.TranslationTextPath);
                }
                catch (Exception e)
                {
                    LogManager.Error(
                        "Create translation folder failed, plugin may not work/创建翻译文件夹失败，插件可能无法运行: " + e.Message);
                }

                return;
            }

            LogManager.Info("Loading translation files/正在加载翻译文件");
            LogManager.Info(
                "Translation files are read in Unicode order, if there are duplicate translations, later read translations will overwrite earlier read translations/翻译文件按照 Unicode 顺序读取，如有相同翻译则后读取的翻译会覆盖先读取的翻译");

            // 获取所有子目录，按Unicode排序
            var directories = Directory.GetDirectories(JustAnotherTranslator.TranslationTextPath)
                .OrderBy(d => d, StringComparer.Ordinal)
                .ToList();

            // 添加根目录到列表开头，确保先处理根目录中的文件
            directories.Insert(0, JustAnotherTranslator.TranslationTextPath);

            foreach (var directory in directories)
            {
                var files = Directory.GetFiles(directory, "*.txt")
                    .OrderBy(f => f, StringComparer.Ordinal)
                    .ToList();


                totalFiles += files.Count;

                foreach (var file in files)
                {
                    var entriesInFile = ProcessTranslationFile(file);
                    totalEntries += entriesInFile;
                }
            }

            sw.Stop();
            LogManager.Info(
                $"Total loaded {totalEntries} translations from {totalFiles} files, cost {sw.ElapsedMilliseconds} ms/总共从 {totalFiles} 个文件中加载了 {totalEntries} 条翻译，耗时 {sw.ElapsedMilliseconds} 毫秒");
        }
        catch (Exception e)
        {
            LogManager.Error("Error loading translation files/加载翻译文件时出错: " + e.Message);
        }
    }

    // 处理单个翻译文件
    private static int ProcessTranslationFile(string filePath)
    {
        var entriesCount = 0;

        try
        {
            var fileInfo = new FileInfo(filePath);
            var fileSize = fileInfo.Length;

            LogManager.Debug($"Processing file: {Path.GetFileName(filePath)} ({fileSize / (1024 * 1024)} MB)");

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                       FileReadChunkSize))
            using (var bs = new BufferedStream(fs, DefaultBufferSize))
            using (var reader = new StreamReader(bs, Encoding.UTF8))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    if (ProcessTranslationLine(line))
                        entriesCount++;
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"Error processing file/处理文件时出错 {Path.GetFileName(filePath)}: {e.Message}");
        }

        return entriesCount;
    }

    // 处理单行翻译文本
    private static bool ProcessTranslationLine(string line)
    {
        if (string.IsNullOrEmpty(line) || line.StartsWith(";"))
            return false;

        var parts = line.Split(new[] { '\t' }, 2);
        if (parts.Length != 2)
            return false;

        var original = parts[0].Unescape();
        var translation = parts[1].Unescape();

        if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(translation))
            return false;

        if (line.StartsWith("$"))
            RegexTranslationDict[new Regex(original.Substring(1), RegexOptions.Compiled)] = translation;
        else
            TranslationDict[original] = translation;


        return true;
    }


    // 获取翻译文本
    public static bool GetTranslateText(string original, out string translated)
    {
        translated = original;
        // XUAT 标记过的文本不进行翻译
        if (original.Contains(XUATInterop.XuatSpicalMaker))
            return false;

        LogManager.Debug("Translating text: " + original);

        if (string.IsNullOrEmpty(original))
            return false;

        if (TranslationDict.TryGetValue(original, out var value))
        {
            LogManager.Debug("Translated text: " + value);
            translated = XUATInterop.MarkTranslated(value);
            return true;
        }

        // KISS did something in cm3d2.dll
        // it seems [HF] will become [hf]
        // 尝试去除换行符和空格后进行翻译，现有翻译的原文均无换行符
        if (TranslationDict.TryGetValue(original.ToUpper().Replace("\r", "").Replace("\n", "").Replace("\t", "").Trim(),
                out var lowerValue))
        {
            LogManager.Debug("Translated text: " + lowerValue);
            translated = XUATInterop.MarkTranslated(lowerValue);
            return true;
        }

        // 尝试使用正则表达式匹配
        // 为了保持兼容 Template 方法是从 CM3D2.YATranslator 移植的
        foreach (var keyValuePair in RegexTranslationDict)
        {
            var regex = keyValuePair.Key;
            var template = keyValuePair.Value;

            var match = regex.Match(original);
            if (!match.Success)
                continue;

            // 输出匹配到的捕获组信息，帮助调试
            LogManager.Debug($"Regex matched with {match.Groups.Count} groups");
            foreach (var groupName in regex.GetGroupNames())
            {
                if (groupName == "0") continue; // 跳过整个匹配
                LogManager.Debug($"Group {groupName}: '{match.Groups[groupName].Value}'");
            }

            translated = template.Template(s =>
            {
                string capturedString;
                if (int.TryParse(s, out var index) && index < match.Groups.Count)
                    capturedString = match.Groups[index].Value;
                else
                    capturedString = match.Groups[s].Value;

                LogManager.Debug($"Template placeholder ${s} => '{capturedString}'");

                if (TranslationDict.TryGetValue(capturedString, out var groupTranslation))
                {
                    LogManager.Debug($"Found translation for '{capturedString}' => '{groupTranslation}'");
                    return groupTranslation;
                }

                return capturedString;
            });

            LogManager.Debug($"Regex translated text: {translated}");
            translated = XUATInterop.MarkTranslated(translated);
            return true;
        }

        return false;
    }
}