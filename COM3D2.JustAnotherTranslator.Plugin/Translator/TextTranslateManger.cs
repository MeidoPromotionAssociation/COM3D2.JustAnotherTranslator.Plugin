using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using COM3D2.JustAnotherTranslator.Plugin.Hooks.Text;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using UnityEngine.SceneManagement;

namespace COM3D2.JustAnotherTranslator.Plugin.Translator;

/// <summary>
///     文本翻译管理器
/// </summary>
public static class TextTranslateManger
{
    private static Harmony _textTranslatePatch;

    private static bool _initialized;

    /// 翻译字典
    private static Dictionary<string, string> _translationDict = new(); // original -> translation

    /// 正则表达式翻译字典
    private static Dictionary<Regex, string> _regexTranslationDict = new(); // regex -> translation

    /// 异步加载器
    private static AsyncTextLoader _asyncLoader;

    /// 翻译是否已加载完成
    private static bool _isTranslationLoaded;

    /// 已导出文本
    private static readonly HashSet<string> DumpedTexts = new();

    /// 导出文本缓冲区
    private static readonly List<string> DumpBuffer = new();

    /// 导出规范化文本缓冲区
    private static readonly List<string> DumpBufferNormalized = new();

    /// 导出目标路径
    private static readonly string DumpFilePath =
        Path.Combine(JustAnotherTranslator.TextDumpPath,
            DateTime.Now.ToString("yyyy-MM-dd-HH-mm") + "_untranslate.txt");

    private static readonly string DumpFilePathNormalized =
        Path.Combine(JustAnotherTranslator.TextDumpPath,
            DateTime.Now.ToString("yyyy-MM-dd-HH-mm") + "_untranslate_normalized.txt");

    /// 加载状态
    private static bool IsLoading { get; set; }

    public static void Init()
    {
        if (_initialized) return;

        _isTranslationLoaded = false;
        LoadTextAsync();

        RegisterPatch();

        if (JustAnotherTranslator.EnableTextDump.Value)
            SceneManager.sceneUnloaded += OnSceneUnloaded;

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
            _asyncLoader = null;
        }

        _textTranslatePatch?.UnpatchSelf();
        _textTranslatePatch = null;

        _translationDict.Clear();
        _regexTranslationDict.Clear();
        _isTranslationLoaded = false;

        FlushDumpBuffer();

        SceneManager.sceneUnloaded -= OnSceneUnloaded;

        _initialized = false;
    }

    /// <summary>
    ///     异步加载翻译文本
    /// </summary>
    private static void LoadTextAsync()
    {
        IsLoading = true;

        // 创建异步加载器
        _asyncLoader = new AsyncTextLoader(
            JustAnotherTranslator.TranslationTextPath,
            OnLoadingProgress,
            OnLoadingComplete
        );

        LogManager.Info("Starting asynchronous translation loading/开始异步加载翻译");

        _asyncLoader.StartLoading();
    }


    /// <summary>
    ///     注册补丁
    /// </summary>
    private static void RegisterPatch()
    {
        _textTranslatePatch = Harmony.CreateAndPatchAll(typeof(TextTranslatePatch),
            "github.meidopromotionassociation.com3d2.justanothertranslator.plugin.hooks.text.texttranslatepatch");

        // 手动注册 NGUIText.WrapText 方法的补丁
        TextTranslatePatch.RegisterNGUITextPatches(_textTranslatePatch);
    }

    /// <summary>
    ///     异步加载进度回调
    /// </summary>
    /// <param name="progress"></param>
    /// <param name="filesProcessed"></param>
    /// <param name="totalFiles"></param>
    private static void OnLoadingProgress(float progress, int filesProcessed, int totalFiles)
    {
        // 进度变化超过 30% 时输出日志
        if ((int)(progress * 100) % 30 == 0)
            LogManager.Info(
                $"Translation loading progress: {progress:P0} ({filesProcessed}/{totalFiles})/翻译加载进度: {progress:P0} ({filesProcessed}/{totalFiles})");
    }

    /// <summary>
    ///     异步加载加载完成回调
    /// </summary>
    /// <param name="result"></param>
    /// <param name="regexResult"></param>
    /// <param name="totalEntries"></param>
    /// <param name="totalFiles"></param>
    /// <param name="elapsedMilliseconds"></param>
    private static void OnLoadingComplete(Dictionary<string, string> result,
        Dictionary<Regex, string> regexResult,
        int totalEntries, int totalFiles,
        long elapsedMilliseconds)
    {
        // 更新状态
        IsLoading = false;
        _isTranslationLoaded = true;

        // 更新翻译字典
        _translationDict = result;
        _regexTranslationDict = regexResult;

        LogManager.Info(
            $"Asynchronous translation loading complete: {totalEntries} entries from {totalFiles} files, cost {elapsedMilliseconds} ms/异步翻译加载完成: 从 {totalFiles} 个文件中加载了 {totalEntries} 条翻译，耗时 {elapsedMilliseconds} 毫秒");
    }


    /// <summary>
    ///     尝试获取翻译文本
    /// </summary>
    /// <param name="original">原文</param>
    /// <param name="translated">译文输出</param>
    /// <param name="skipMark">跳过已翻译标记</param>
    /// <returns>是否翻译成功</returns>
    public static bool GetTranslateText(string original, out string translated,
        bool skipMark = false)
    {
        translated = original;

        if (!_isTranslationLoaded)
        {
            if (!_initialized)
            {
                LogManager.Warning(
                    "Text translation is not enabled, return original text/未启用文本翻译，返回原文");
                return false;
            }

            LogManager.Warning("Translation not loaded yet, return original text/翻译未加载或是加载中，返回原文");
            return false;
        }

        // 考虑到音频文件可能是数字，这里不进行数字检查
        if (StringTool.IsNullOrWhiteSpace(original))
            return false;

        LogManager.Debug($"Try to translate text: {original}");

        // XUAT 标记过的文本不进行翻译
        // 除非 XUAT 进行了更改，否则特殊标记永远位于结尾，但目前已有的的文本特殊标记位于开头
        // 然而因为某种原因，StartsWith 和 EndsWith 会把所有文本都标记为已翻译，因此必须使用 Contains
        if (original.Contains(XUATInterop.XuatSpicalMaker))
        {
            LogManager.Debug($"Text already marked, skipping: {original}");
            return false;
        }

        // 注意：翻译到纯 [HF] 时如果被添加了特殊标记，会导致游戏崩溃，游戏还有其他的特殊标记，因此这里直接检查 []
        if (original.StartsWith("[") & original.EndsWith("]"))
        {
            LogManager.Debug($"Text StartsWith special mark [], skipping: {original}");
            return false;
        }

        if (_translationDict.TryGetValue(original, out var value))
        {
            translated = XUATInterop.MarkTranslated(value, skipMark);
            LogManager.Debug($"Translated {original}    =>    {translated}");
            return true;
        }

        // KISS did something in cm3d2.dll
        // it seems [HF] will become [hf]
        // 尝试去除换行符和空格后进行翻译，现有翻译的原文均无换行符
        var normalizedOriginal = StringTool.NormalizeText(original);

        if (_translationDict.TryGetValue(normalizedOriginal, out var lowerValue2))
        {
            translated = XUATInterop.MarkTranslated(lowerValue2, skipMark);
            LogManager.Debug(
                $"Translated (upper, replace and trimmed) {normalizedOriginal}    =>    {translated}");
            return true;
        }

        // 尝试使用正则表达式匹配
        // 为了保持兼容 Template 方法是从 CM3D2.YATranslator 移植的
        foreach (var keyValuePair in _regexTranslationDict)
        {
            var regex = keyValuePair.Key;
            var template = keyValuePair.Value;

            var match = regex.Match(original);
            // 避免把空匹配当作成功（有些正则可匹配空串，如 ".*"、".*?" 等）
            var hasValidMatch = match.Success && match.Length > 0;
            if (!hasValidMatch && normalizedOriginal != original && normalizedOriginal.Length > 0)
            {
                // 尝试用 normalized 再匹配一次（仅当 normalized 非空）
                match = regex.Match(normalizedOriginal);
                hasValidMatch = match.Success && match.Length > 0;
            }

            if (!hasValidMatch)
                continue;

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

                LogManager.Debug($"Template placeholder ${s}    =>    '{capturedString}'");

                if (_translationDict.TryGetValue(capturedString, out var groupTranslation))
                {
                    LogManager.Debug(
                        $"Found translation for '{capturedString}'    =>    '{groupTranslation}'");
                    return groupTranslation;
                }

                return capturedString;
            });

            translated = XUATInterop.MarkTranslated(translated, skipMark);
            LogManager.Debug(
                $"Translated (Regex) {keyValuePair.Key}:{keyValuePair.Value}    =>    {translated}");
            return true;
        }

        DumpText(original);
        return false;
    }


    /// <summary>
    ///     导出未翻译的文本
    /// </summary>
    /// <param name="text"></param>
    private static void DumpText(string text)
    {
        if (!JustAnotherTranslator.EnableTextDump.Value)
            return;

        // 如果文本是新的 (之前未 dump 过), addResult 会是 true
        var added = DumpedTexts.Add(text);

        // 只有当文本是新的，才执行写入文件的操作
        if (added)
        {
            LogManager.Debug($"Text not translated, dumping: {text}");
            DumpBuffer.Add(text);
            DumpBufferNormalized.Add(StringTool.NormalizeText(text));

            if (DumpBuffer.Count >= JustAnotherTranslator.TextDumpThreshold.Value)
                FlushDumpBuffer();
        }
    }

    /// <summary>
    ///     将缓存中的未翻译文本写入文件
    /// </summary>
    public static void FlushDumpBuffer()
    {
        if (DumpBuffer.Count == 0)
            return;

        try
        {
            // .NET 3.5 framework don't have AppendAllLines
            using (var streamWriter = new StreamWriter(DumpFilePath, true))
            {
                foreach (var line in DumpBuffer) streamWriter.WriteLine(line);
            }

            DumpBuffer.Clear();
        }
        catch (Exception e)
        {
            LogManager.Error($"Failed to dump text to file/写入文件失败: {e.Message}");
        }

        try
        {
            using (var streamWriter = new StreamWriter(DumpFilePathNormalized, true))
            {
                foreach (var line in DumpBufferNormalized) streamWriter.WriteLine(line);
            }

            DumpBufferNormalized.Clear();
        }
        catch (Exception e)
        {
            LogManager.Error($"Failed to dump text to file/写入文件失败: {e.Message}");
        }
    }

    /// <summary>
    ///     场景卸载时清理资源
    /// </summary>
    /// <param name="scene"></param>
    private static void OnSceneUnloaded(Scene scene)
    {
        try
        {
            FlushDumpBuffer();
            DumpedTexts.Clear();
        }
        catch (Exception e)
        {
            LogManager.Error($"Error during cleanup scene resources/清理场景资源失败: {e.Message}");
        }
    }
}