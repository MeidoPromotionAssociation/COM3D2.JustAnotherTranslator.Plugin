using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using COM3D2.JustAnotherTranslator.Plugin.Utils;

namespace COM3D2.JustAnotherTranslator.Plugin.Loader.Processor;

/// <summary>
///     TXT 翻译文件处理器
///     处理 Tab 分隔的翻译文件，支持 $ 前缀的正则表达式翻译
/// </summary>
public class TxtTranslationFileProcessor : ITranslationFileProcessor
{
    /// 大文件缓冲区大小
    private static readonly int LargeFileBufferSize = 16 * 1024 * 1024; // 16MB

    public string SupportedExtension => ".txt";

    /// <summary>
    ///     处理翻译文件流并解析其中的翻译条目
    /// </summary>
    /// <param name="stream">包含翻译内容的文件流</param>
    /// <param name="result">翻译结果对象，解析的条目将追加到该对象中</param>
    /// <returns>成功处理的翻译条目数</returns>
    public int ProcessStream(Stream stream, TranslationLoadResult result)
    {
        var entriesCount = 0;

        using (var reader = new StreamReader(stream, Encoding.UTF8))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
                if (ProcessTranslationLine(line, result))
                    entriesCount++;
        }

        return entriesCount;
    }

    /// <summary>
    ///     从文件路径处理翻译数据，针对文件大小优化 I/O 策略
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="result">翻译结果对象，解析的条目将追加到该对象中</param>
    /// <returns>成功处理的翻译条目数</returns>
    public int ProcessFile(string filePath, TranslationLoadResult result)
    {
        var entriesCount = 0;
        var fileInfo = new FileInfo(filePath);
        var fileSize = fileInfo.Length;

        // 对于小文件，直接一次性读取全部内容
        if (fileSize < 1024 * 1024) // < 1MB
        {
            var contents = File.ReadAllLines(filePath, Encoding.UTF8);
            foreach (var line in contents)
                if (ProcessTranslationLine(line, result))
                    entriesCount++;
        }
        else
        {
            // 对于大文件，使用 StreamReader 逐行读取
            using (var reader =
                   new StreamReader(filePath, Encoding.UTF8, false, LargeFileBufferSize))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    if (ProcessTranslationLine(line, result))
                        entriesCount++;
            }
        }

        return entriesCount;
    }

    /// <summary>
    ///     处理单行翻译文本
    /// </summary>
    /// <param name="line">文本行</param>
    /// <param name="result">翻译结果</param>
    /// <returns>是否成功处理</returns>
    private static bool ProcessTranslationLine(string line, TranslationLoadResult result)
    {
        if (string.IsNullOrEmpty(line) || line.StartsWith(";"))
            return false;

        var parts = line.Split(new[] { '\t' }, 2);
        if (parts.Length != 2)
            return false;

        // Unescape 将字符串中的转义序列转换为对应的实际字符
        var original = parts[0].Unescape();
        var translation = StringTool.FastRemoveChar(parts[1].Unescape(), '\u180e');

        if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(translation))
            return false;

        if (line.StartsWith("$"))
            result.RegexTranslations[new Regex(original.Substring(1), RegexOptions.Compiled)] =
                translation;
        else
            result.Translations[original] = translation;

        return true;
    }
}