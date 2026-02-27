using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace COM3D2.JustAnotherTranslator.Plugin.Loader;

/// <summary>
///     翻译加载结果，统一所有加载器的输出
/// </summary>
public class TranslationLoadResult
{
    /// <summary>
    ///     正则翻译字典 (正则 -> 译文模板)，仅文本加载器使用
    /// </summary>
    public readonly Dictionary<Regex, string> RegexTranslations = new();

    /// <summary>
    ///     翻译字典 (原文 -> 译文)
    /// </summary>
    public readonly Dictionary<string, string> Translations = new();

    /// <summary>
    ///     加载耗时（毫秒）
    /// </summary>
    public long ElapsedMilliseconds;

    /// <summary>
    ///     总条目数
    /// </summary>
    public int TotalEntries;

    /// <summary>
    ///     总文件数
    /// </summary>
    public int TotalFiles;
}