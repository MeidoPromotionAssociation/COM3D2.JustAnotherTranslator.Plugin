using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using COM3D2.JustAnotherTranslator.Plugin.Hooks.Text;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using MaidCafe;

namespace COM3D2.JustAnotherTranslator.Plugin.Translator;

/// <summary>
///     文本翻译管理器
/// </summary>
public static class TextTranslateManger
{
    private static Harmony _textTranslatePatch;

    private static Harmony _maidCafeDlcLineBreakCommentFixPatch;

    private static bool _initialized;

    /// 翻译字典
    private static Dictionary<string, string> _translationDict = new(); // original -> translation

    /// 正则表达式翻译字典
    private static Dictionary<Regex, string> _regexTranslationDict = new(); // regex -> translation

    /// 异步加载器
    private static AsyncTextLoader _asyncLoader;

    /// 翻译是否已加载完成
    public static bool IsTranslationLoaded;

    /// 加载状态
    public static bool IsLoading { get; private set; }

    /// 加载进度 (0.0-1.0)
    public static float LoadingProgress { get; private set; }

    /// 已处理文件数
    public static int FilesProcessed { get; private set; }

    /// 总文件数
    public static int TotalFiles { get; private set; }

    public static void Init()
    {
        if (_initialized) return;

        // 加载翻译
        IsTranslationLoaded = false;
        LoadTextAsync();

        // 创建 Harmony 实例
        _textTranslatePatch = Harmony.CreateAndPatchAll(typeof(TextTranslatePatch),
            "com3d2.justanothertranslator.plugin.hooks.text.texttranslatepatch");

        // 手动注册 NGUIText.WrapText 方法的补丁
        TextTranslatePatch.RegisterNGUITextPatches(_textTranslatePatch);

        if (MaidCafeManagerHelper.IsMaidCafeAvailable())
            try
            {
                var original = typeof(MaidCafeComment).GetMethod("LineBreakComment",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                var isPatchedByOthers = false;
                if (original != null)
                {
                    var patches = Harmony.GetPatchInfo(original);
                    isPatchedByOthers = patches?.Owners?.Count > 0;
                }

                var isPatchedByLegacy =
                    Harmony.HasAnyPatches("com.github.90135.com3d2_scripts_901.maidcafelinebreakcommentfix");

                if (isPatchedByLegacy || isPatchedByOthers)
                    LogManager.Warning(
                        "MaidCafeDlcLineBreakCommentFix patch already applied by someone else, skipping/MaidCafeDlcLineBreakCommentFix 已被其他人应用，跳过\n" +
                        "if you got maid_cafe_line_break_fix.cs in your scripts folder, please remove it/如果你在 scripts 脚本文件夹中有 maid_cafe_line_break_fix.cs，请删除它");
                else
                    _maidCafeDlcLineBreakCommentFixPatch = Harmony.CreateAndPatchAll(
                        typeof(MaidCafeDlcLineBreakCommentFix),
                        "com3d2.justanothertranslator.plugin.hooks.text.maidcafedlclinebreakcommentfix");
            }
            catch (Exception e)
            {
                LogManager.Warning(
                    $"Failed to patch MaidCafeDlcLineBreakCommentFix/补丁 MaidCafeDlcLineBreakCommentFix 失败: {e.Message}");
            }

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
            LoadingProgress = 0f;
            FilesProcessed = 0;
            TotalFiles = 0;
        }

        _textTranslatePatch?.UnpatchSelf();
        _textTranslatePatch = null;

        _maidCafeDlcLineBreakCommentFixPatch?.UnpatchSelf();
        _maidCafeDlcLineBreakCommentFixPatch = null;

        _translationDict.Clear();
        _regexTranslationDict.Clear();
        IsTranslationLoaded = false;

        _initialized = false;
    }

    /// <summary>
    ///     异步加载翻译文本
    /// </summary>
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


    /// <summary>
    ///     异步加载进度回调
    /// </summary>
    /// <param name="progress"></param>
    /// <param name="filesProcessed"></param>
    /// <param name="totalFiles"></param>
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

    /// <summary>
    ///     异步加载加载完成回调
    /// </summary>
    /// <param name="result"></param>
    /// <param name="regexResult"></param>
    /// <param name="totalEntries"></param>
    /// <param name="totalFiles"></param>
    /// <param name="elapsedMilliseconds"></param>
    private static void OnLoadingComplete(Dictionary<string, string> result, Dictionary<Regex, string> regexResult,
        int totalEntries, int totalFiles,
        long elapsedMilliseconds)
    {
        // 更新状态
        IsLoading = false;
        LoadingProgress = 1.0f;
        FilesProcessed = totalFiles;
        TotalFiles = totalFiles;
        IsTranslationLoaded = true;

        // 更新翻译字典
        _translationDict = result;
        _regexTranslationDict = regexResult;

        LogManager.Info(
            $"Asynchronous translation loading complete: {totalEntries} entries from {totalFiles} files, cost {elapsedMilliseconds} ms/异步翻译加载完成: 从 {totalFiles} 个文件中加载了 {totalEntries} 条翻译，耗时 {elapsedMilliseconds} 毫秒");
    }


    /// <summary>
    ///     尝试获取翻译文本
    /// </summary>
    /// <param name="original"></param>
    /// <param name="translated"></param>
    /// <returns></returns>
    public static bool GetTranslateText(string original, out string translated)
    {
        translated = original;

        if (!IsTranslationLoaded)
        {
            LogManager.Warning("Translation not loaded yet, return original text/翻译未加载或是加载中，返回原文");
            return false;
        }

        // XUAT 标记过的文本不进行翻译
        // 除非 XUAT 进行了更改，否则特殊标记永远位于结尾，但目前已有的的文本特殊标记位于开头
        if (original.StartsWith(XUATInterop.XuatSpicalMaker) || original.EndsWith(XUATInterop.XuatSpicalMaker))
            return false;

        LogManager.Debug($"Translating text: {original}");

        if (string.IsNullOrEmpty(original))
            return false;

        if (_translationDict.TryGetValue(original, out var value))
        {
            LogManager.Debug($"Translated text: {value}");
            translated = XUATInterop.MarkTranslated(value);
            return true;
        }

        // KISS did something in cm3d2.dll
        // it seems [HF] will become [hf]
        // 尝试去除换行符和空格后进行翻译，现有翻译的原文均无换行符
        if (_translationDict.TryGetValue(original.ToUpper().Replace("\r", "").Replace("\n", "").Replace("\t", "").Trim(),
                out var lowerValue))
        {
            LogManager.Debug($"Translated text (to upper, replace and trimmed): {lowerValue}");
            translated = XUATInterop.MarkTranslated(lowerValue);
            return true;
        }

        // 尝试使用正则表达式匹配
        // 为了保持兼容 Template 方法是从 CM3D2.YATranslator 移植的
        foreach (var keyValuePair in _regexTranslationDict)
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

                if (_translationDict.TryGetValue(capturedString, out var groupTranslation))
                {
                    LogManager.Debug($"Found translation for '{capturedString}' => '{groupTranslation}'");
                    return groupTranslation;
                }

                return capturedString;
            });

            LogManager.Debug($"translated text (Regex): {translated}");
            translated = XUATInterop.MarkTranslated(translated);
            return true;
        }

        return false;
    }


    /// <summary>
    ///     检查字符串是否为数字（包含小数点）
    ///     应当比直接 decimal.TryParse 和正则表达式更快
    /// </summary>
    /// <param name="text">要检查的字符串</param>
    /// <returns>如果字符串是数字，则返回 true；否则返回 false</returns>
    public static bool IsNumeric(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        // 特殊情况优化：短数字直接检查
        if (text.Length <= 8)
        {
            var hasDecimalPoint = false;
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (i == 0 && c == '-') continue;
                if (c == '.' || c == ',')
                {
                    if (hasDecimalPoint) return false;
                    hasDecimalPoint = true;
                }
                else if (c < '0' || c > '9')
                {
                    return false;
                }
            }

            return true;
        }

        // 长字符串回退到 decimal.TryParse
        return decimal.TryParse(text,
            NumberStyles.Any,
            CultureInfo.InvariantCulture,
            out _);
    }
}