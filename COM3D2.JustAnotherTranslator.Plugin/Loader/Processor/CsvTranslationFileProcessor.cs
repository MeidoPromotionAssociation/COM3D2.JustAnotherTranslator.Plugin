using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using JetBrains.Annotations;
#if COM3D25_UNITY_2022
using System.Linq;
#endif

namespace COM3D2.JustAnotherTranslator.Plugin.Loader.Processor;

/// <summary>
///     CSV 翻译文件处理器
///     处理 CsvHelper 格式的翻译文件
/// </summary>
public class CsvTranslationFileProcessor : ITranslationFileProcessor
{
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

    /// <summary>
    ///     获取 CSV 配置（供外部 dump 使用）
    /// </summary>
    public static CsvConfiguration GetCsvConfig()
    {
        return CsvConfig;
    }

    public string SupportedExtension => ".csv";

    /// <summary>
    ///     处理包含 CSV 翻译数据的流，并将翻译加载到提供的结果对象中
    /// </summary>
    /// <param name="stream">包含 CSV 翻译数据的输入流</param>
    /// <param name="result">翻译结果对象，解析的条目将追加到该对象中</param>
    /// <returns>成功加载的翻译条目数</returns>
    public int ProcessStream(Stream stream, TranslationLoadResult result)
    {
        var entriesLoaded = 0;
        using (var reader = new StreamReader(stream, Encoding.UTF8, true, 4096))
        using (var csv = new CsvReader(reader, CsvConfig))
        {
            var records = csv.GetRecords<CsvEntry>();
            foreach (var record in records)
            {
                if (string.IsNullOrEmpty(record.Term) ||
                    string.IsNullOrEmpty(record.Translation)) continue;
                result.Translations[record.Term] = record.Translation;
                entriesLoaded++;
            }

            return entriesLoaded;
        }
    }

    /// <summary>
    ///     CSV 结构
    /// </summary>
    public class CsvEntry
    {
        public string Term { get; set; } // 键名
        [CanBeNull] public string Original { get; set; } // 原文
        [CanBeNull] public string Translation { get; set; } // 译文
    }
}