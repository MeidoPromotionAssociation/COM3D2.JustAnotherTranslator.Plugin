using System.IO;

namespace COM3D2.JustAnotherTranslator.Plugin.Loader;

/// <summary>
///     翻译文件处理器接口，每种文件格式实现一个处理器
/// </summary>
public interface ITranslationFileProcessor
{
    /// <summary>
    ///     支持的文件扩展名 (e.g., ".txt", ".csv")
    /// </summary>
    string SupportedExtension { get; }

    /// <summary>
    ///     从流中处理翻译数据（用于 ZIP 内条目等无法直接访问文件的场景）
    /// </summary>
    /// <param name="stream">文件流</param>
    /// <param name="result">翻译结果，处理器向其中添加条目</param>
    /// <returns>处理的条目数</returns>
    int ProcessStream(Stream stream, TranslationLoadResult result);

    /// <summary>
    ///     从文件路径处理翻译数据（可针对文件 I/O 进行优化）
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="result">翻译结果，处理器向其中添加条目</param>
    /// <returns>处理的条目数</returns>
    int ProcessFile(string filePath, TranslationLoadResult result);
}