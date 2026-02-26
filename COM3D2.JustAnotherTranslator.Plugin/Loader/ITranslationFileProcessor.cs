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
    ///     从流中处理翻译数据
    /// </summary>
    /// <param name="stream">文件流</param>
    /// <param name="result">翻译结果，处理器向其中添加条目</param>
    /// <returns>处理的条目数</returns>
    int ProcessStream(Stream stream, TranslationLoadResult result);
}