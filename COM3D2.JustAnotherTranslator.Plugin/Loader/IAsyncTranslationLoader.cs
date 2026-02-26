namespace COM3D2.JustAnotherTranslator.Plugin.Loader;

/// <summary>
///     加载进度委托
/// </summary>
public delegate void TranslationLoadProgressCallback(float progress, int filesProcessed,
    int totalFiles);

/// <summary>
///     加载完成委托
/// </summary>
public delegate void TranslationLoadCompletionCallback(TranslationLoadResult result);

/// <summary>
///     异步翻译加载器接口
/// </summary>
public interface IAsyncTranslationLoader
{
    /// <summary>
    ///     开始异步加载
    /// </summary>
    void StartLoading();

    /// <summary>
    ///     取消加载
    /// </summary>
    void Cancel();
}