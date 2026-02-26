using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using ICSharpCode.SharpZipLib.Zip;

namespace COM3D2.JustAnotherTranslator.Plugin.Loader;

/// <summary>
///     通用异步翻译加载器，通过注入 ITranslationFileProcessor 支持不同文件格式
/// </summary>
public class AsyncTranslationLoader : IAsyncTranslationLoader
{
    /// 压缩文件缓冲区大小
    private static readonly int ZipBuffSize = 131072;

    /// 压缩文件流缓冲区大小
    private static readonly int ZipStreamCacheSize = 4096;

    /// 完成回调
    private readonly TranslationLoadCompletionCallback _completionCallback;

    /// 加载器名称（用于日志）
    private readonly string _loaderName;

    /// 扩展名到处理器的映射
    private readonly Dictionary<string, ITranslationFileProcessor> _processorMap;

    /// 翻译文件处理器列表
    private readonly ITranslationFileProcessor[] _processors;

    /// 进度回调
    private readonly TranslationLoadProgressCallback _progressCallback;

    /// 支持的文件扩展名（包含 .zip）
    private readonly string[] _supportedExtensions;

    /// 翻译目录路径
    private readonly string _translationPath;

    /// 取消标志
    private volatile bool _cancelRequested;

    /// 加载线程
    private Thread _loaderThread;

    /// <summary>
    ///     创建一个新的异步翻译加载器
    /// </summary>
    /// <param name="loaderName">加载器名称（用于日志标识）</param>
    /// <param name="translationPath">翻译文件目录路径</param>
    /// <param name="progressCallback">进度回调</param>
    /// <param name="completionCallback">完成回调</param>
    /// <param name="processors">翻译文件处理器</param>
    public AsyncTranslationLoader(
        string loaderName,
        string translationPath,
        TranslationLoadProgressCallback progressCallback,
        TranslationLoadCompletionCallback completionCallback,
        params ITranslationFileProcessor[] processors)
    {
        if (processors == null || processors.Length == 0)
            throw new ArgumentException(
                "At least one ITranslationFileProcessor must be provided/至少需要提供一个翻译文件处理器");

        _loaderName = loaderName;
        _translationPath = translationPath;
        _progressCallback = progressCallback;
        _completionCallback = completionCallback;
        _processors = processors;

        // 构建扩展名 -> 处理器映射
        _processorMap =
            new Dictionary<string, ITranslationFileProcessor>(StringComparer.OrdinalIgnoreCase);
        foreach (var processor in processors)
            _processorMap[processor.SupportedExtension] = processor;

        // 构建支持的扩展名列表（处理器扩展名 + .zip）
        var extensions = new List<string>(_processorMap.Keys) { ".zip" };
        _supportedExtensions = extensions.ToArray();
    }

    /// <summary>
    ///     开始异步加载
    /// </summary>
    public void StartLoading()
    {
        if (_loaderThread != null && _loaderThread.IsAlive)
        {
            LogManager.Warning(
                $"[{_loaderName}] Async file loader is already running, please report this issue/异步文件加载器已在运行中，请报告此问题");
            return;
        }

        _cancelRequested = false;
        _loaderThread = new Thread(LoadFilesThreadFunc)
        {
            IsBackground = true
        };
        _loaderThread.Start();
    }

    /// <summary>
    ///     取消加载
    /// </summary>
    public void Cancel()
    {
        _cancelRequested = true;
    }

    /// <summary>
    ///     加载线程
    /// </summary>
    private void LoadFilesThreadFunc()
    {
        var sw = new Stopwatch();
        sw.Start();

        var result = new TranslationLoadResult();
        var filesProcessed = 0;

        try
        {
            if (!Directory.Exists(_translationPath))
            {
                LogManager.Warning(
                    $"[{_loaderName}] Translation directory not found, try to create/未找到翻译目录, 尝试创建: {_translationPath}");

                try
                {
                    Directory.CreateDirectory(_translationPath);
                }
                catch (Exception e)
                {
                    LogManager.Error(
                        $"[{_loaderName}] Create translation folder failed, plugin may not work/创建翻译目录失败，插件可能无法运行: {e.Message}");
                    _completionCallback?.Invoke(result);
                    return;
                }
            }

            LogManager.Info(
                $"[{_loaderName}] Loading translation files asynchronously/正在异步加载翻译文件");

            // 获取所有匹配的文件
            var allFiles = FileTool.GetAllTranslationFiles(_translationPath, _supportedExtensions);
            var totalFiles = allFiles.Count;

            if (totalFiles == 0)
            {
                LogManager.Info($"[{_loaderName}] No translation files found/未找到翻译文件");
                _completionCallback?.Invoke(result);
                return;
            }

            LogManager.Info(
                $"[{_loaderName}] Found {totalFiles} translation files/发现 {totalFiles} 个翻译文件");

            result.TotalFiles = totalFiles;

            // 处理所有文件
            foreach (var file in allFiles)
            {
                if (_cancelRequested)
                {
                    LogManager.Info(
                        $"[{_loaderName}] Translation loading cancelled/翻译加载已被取消");
                    break;
                }

                var entriesInFile = ProcessTranslationFile(file, result);
                result.TotalEntries += entriesInFile;
                filesProcessed++;

                var progress = (float)filesProcessed / totalFiles;
                _progressCallback?.Invoke(progress, filesProcessed, totalFiles);
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"[{_loaderName}] Error loading translation files/加载翻译文件时出错: {e.Message}");
        }
        finally
        {
            sw.Stop();
            result.ElapsedMilliseconds = sw.ElapsedMilliseconds;
            result.TotalFiles = filesProcessed;
            _completionCallback?.Invoke(result);
        }
    }

    /// <summary>
    ///     根据文件扩展名查找对应的处理器
    /// </summary>
    /// <param name="filePath">文件路径或文件名</param>
    /// <returns>匹配的处理器，未找到返回 null</returns>
    private ITranslationFileProcessor FindProcessor(string filePath)
    {
        foreach (var kvp in _processorMap)
            if (filePath.EndsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;

        return null;
    }

    /// <summary>
    ///     处理单个翻译文件（根据扩展名分发到对应处理器或 ZIP 处理）
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="result">翻译结果</param>
    /// <returns>处理的条目数</returns>
    private int ProcessTranslationFile(string filePath, TranslationLoadResult result)
    {
        var entriesCount = 0;

        try
        {
            if (filePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                if (JustAnotherTranslator.AllowFilesInZipLoadInOrder.Value)
                    entriesCount = ProcessZipFileInOrder(filePath, result);
                else
                    entriesCount = ProcessZipFile(filePath, result);
            }
            else
            {
                var processor = FindProcessor(filePath);
                if (processor != null)
                    entriesCount = ProcessFile(filePath, result, processor);
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"Error processing file/处理文件时出错 {Path.GetFileName(filePath)}: {e.Message}");
        }

        return entriesCount;
    }

    /// <summary>
    ///     使用指定处理器处理单个文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="result">翻译结果</param>
    /// <param name="processor">文件处理器</param>
    /// <returns>处理的条目数</returns>
    private static int ProcessFile(string filePath, TranslationLoadResult result,
        ITranslationFileProcessor processor)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var fileSize = fileInfo.Length;

            if (fileSize > 10 * 1024 * 1024) // > 10MB
                LogManager.Info(
                    $"Processing large file/正在处理大文件: {Path.GetFileName(filePath)} ({fileSize / (1024 * 1024):F1} MB)");

            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read,
                       FileShare.Read))
            {
                return processor.ProcessStream(fileStream, result);
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"Error processing file/处理文件时出错 {Path.GetFileName(filePath)}: {e.Message}");
            return 0;
        }
    }

    /// <summary>
    ///     处理 ZIP 压缩文件 - 按文件名排序
    /// </summary>
    /// <param name="zipFilePath">ZIP 文件路径</param>
    /// <param name="result">翻译结果</param>
    /// <returns>处理的条目数</returns>
    private int ProcessZipFileInOrder(string zipFilePath, TranslationLoadResult result)
    {
        var entriesCount = 0;
        var fileName = Path.GetFileName(zipFilePath);

        try
        {
            using (var zf = new ZipFile(zipFilePath))
            {
                var matchingFiles = new List<ZipEntry>();
                foreach (ZipEntry zipEntry in zf)
                {
                    if (!zipEntry.IsFile)
                        continue;

                    if (FileTool.IsZipPathUnsafe(zipEntry.Name))
                    {
                        LogManager.Warning(
                            $"Skipping unsafe entry in ZIP archive/跳过ZIP压缩包中的不安全条目： {zipEntry.Name}");
                        continue;
                    }

                    // 检查是否有任何处理器支持此扩展名
                    if (FindProcessor(zipEntry.Name) != null)
                        matchingFiles.Add(zipEntry);
                }

                if (matchingFiles.Count == 0)
                {
                    LogManager.Info(
                        $"No supported files found in ZIP archive/ZIP压缩包中未找到支持的文件: {fileName}");
                    return 0;
                }

                // 按照 Unicode 顺序排序
                matchingFiles.Sort((a, b) => StringComparer.Ordinal.Compare(a.Name, b.Name));

                LogManager.Info(
                    $"Processing ZIP archive {fileName} in order, which has {matchingFiles.Count} files/正在按顺序处理ZIP压缩包: {fileName} 内含 {matchingFiles.Count} 个文件)");

                foreach (var entry in matchingFiles)
                    try
                    {
                        var processor = FindProcessor(entry.Name);
                        if (processor == null) continue;

                        using (var stream = zf.GetInputStream(entry))
                        {
                            entriesCount += processor.ProcessStream(stream, result);
                        }
                    }
                    catch (Exception e)
                    {
                        LogManager.Warning(
                            $"Error processing entry in ZIP/处理ZIP中的条目时出错 {entry.Name}: {e.Message}");
                    }
            }
        }
        catch (Exception e)
        {
            LogManager.Error($"Error processing ZIP file/处理ZIP文件时出错 {fileName}: {e.Message}");
        }

        return entriesCount;
    }

    /// <summary>
    ///     处理 ZIP 压缩文件（流式读取，不排序）
    /// </summary>
    /// <param name="zipFilePath">ZIP 文件路径</param>
    /// <param name="result">翻译结果</param>
    /// <returns>处理的条目数</returns>
    private int ProcessZipFile(string zipFilePath, TranslationLoadResult result)
    {
        var entriesCount = 0;
        var fileName = Path.GetFileName(zipFilePath);

        try
        {
            LogManager.Info(
                $"Processing ZIP archive {fileName}/正在处理ZIP压缩包: {fileName})");

            using var fileStream = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read,
                FileShare.Read,
                ZipBuffSize);
            using var zipStream = new ZipInputStream(fileStream);

            ZipEntry entry;
            while ((entry = zipStream.GetNextEntry()) != null)
            {
                if (FileTool.IsZipPathUnsafe(entry.Name))
                {
                    LogManager.Warning(
                        $"Skipping unsafe entry in ZIP archive/跳过ZIP压缩包中的不安全条目： {entry.Name}");
                    continue;
                }

                if (!entry.IsFile)
                    continue;

                // 查找对应的处理器
                var processor = FindProcessor(entry.Name);
                if (processor == null)
                    continue;

                try
                {
                    // 预估容量以减少内存重分配
                    var estimatedSize = entry.Size > 0
                        ? (int)Math.Min(entry.Size, int.MaxValue)
                        : ZipStreamCacheSize;
                    using (var memStream =
                           new MemoryStream(estimatedSize > 0 ? estimatedSize : ZipStreamCacheSize))
                    {
                        var buffer = new byte[ZipStreamCacheSize];
                        int bytesRead;
                        while ((bytesRead = zipStream.Read(buffer, 0, buffer.Length)) > 0)
                            memStream.Write(buffer, 0, bytesRead);
                        // 重置流位置
                        memStream.Position = 0;

                        entriesCount += processor.ProcessStream(memStream, result);
                    }
                }
                catch (Exception e)
                {
                    LogManager.Warning(
                        $"Error processing entry in ZIP/处理ZIP中的条目时出错 {entry.Name}: {e.Message}");
                }
            }
        }
        catch (Exception e)
        {
            LogManager.Error($"Error processing ZIP file/处理ZIP文件时出错 {fileName}: {e.Message}");
        }

        return entriesCount;
    }
}