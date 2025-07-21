using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using COM3D2.JustAnotherTranslator.Plugin.Translator;
using CsvHelper;
using CsvHelper.Configuration;
using ICSharpCode.SharpZipLib.Zip;

namespace COM3D2.JustAnotherTranslator.Plugin.Utils;

/// <summary>
///     异步UI翻译加载器
/// </summary>
public class AsyncUiTextLoader
{
    /// <summary>
    ///     加载完成委托
    /// </summary>
    public delegate void CompletionCallback(
        Dictionary<string, UITranslateManager.TranslationData> result,
        int totalEntries,
        int totalFiles,
        long elapsedMilliseconds
    );

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

    private readonly CompletionCallback _completionCallback;

    private readonly string _translationPath;
    private readonly Dictionary<string, UITranslateManager.TranslationData> _translations = new();
    private volatile bool _cancelRequested;
    private Thread _loaderThread;

    public AsyncUiTextLoader(string translationPath, CompletionCallback completionCallback)
    {
        _translationPath = translationPath;
        _completionCallback = completionCallback;
    }

    /// <summary>
    ///     开始异步加载
    /// </summary>
    public void StartLoading()
    {
        if (_loaderThread != null && _loaderThread.IsAlive)
        {
            LogManager.Warning(
                "UI Translation loader is already running, please report this issue/UI 翻译加载器已在运行中，请报告此问题");
            return;
        }

        _translations.Clear();

        _loaderThread = new Thread(LoadTranslationsThread)
        {
            IsBackground = true
        };
        _loaderThread.Start();
    }

    public void StopLoading()
    {
        _cancelRequested = true;
    }

    /// <summary>
    ///     加载线程，用于加载文件
    /// </summary>
    private void LoadTranslationsThread()
    {
        var sw = new Stopwatch();
        sw.Start();

        var totalEntries = 0;
        var processedFiles = 0;

        if (!Directory.Exists(_translationPath))
        {
            LogManager.Warning("Translation UITextPath directory not found, try to create/未找到UI翻译目录，尝试创建: " +
                               _translationPath);
            try
            {
                Directory.CreateDirectory(_translationPath);
            }
            catch (Exception e)
            {
                LogManager.Error(
                    "Create translation UIText folder failed, plugin may not work/创建翻译UI翻译目录失败，插件可能无法运行: " + e.Message);
                _completionCallback?.Invoke(_translations, 0, 0, 0);
                return;
            }
        }

        try
        {
            var allFiles = FileTool.GetAllTranslationFiles(_translationPath, new[] { ".csv", ".zip" });
            var totalFiles = allFiles.Count;

            LogManager.Info(
                $"Starting asynchronous UI translation loading, found {totalFiles} CSV files/开始异步加载UI翻译文件，共找到 {totalFiles} 个 CSV 文件");

            foreach (var filePath in allFiles)
            {
                if (_cancelRequested)
                {
                    LogManager.Info("UI Translation loading cancelled/UI翻译加载已被取消");
                    break;
                }

                var entriesInFile = ProcessTranslationFile(filePath);
                totalEntries += entriesInFile;
                if (entriesInFile > 0) processedFiles++;
            }
        }
        catch (Exception ex)
        {
            LogManager.Error($"Error while loading UI translation file/加载UI翻译文件时发生错误: {ex.Message}");
        }
        finally
        {
            sw.Stop();
            _completionCallback?.Invoke(_translations, totalEntries, processedFiles, sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    ///     处理单个翻译文件
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    private int ProcessTranslationFile(string filePath)
    {
        var entriesCount = 0;
        try
        {
            if (filePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                if (JustAnotherTranslator.AllowFilesInZipLoadInOrder.Value)
                    entriesCount = ProcessZipFileInOrder(filePath);
                else
                    entriesCount = ProcessZipFile(filePath);
            }
            else if (filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                entriesCount = ProcessCsvFile(filePath);
            }
        }
        catch (Exception e)
        {
            LogManager.Error($"Error processing file/处理文件时出错 {Path.GetFileName(filePath)}: {e.Message}");
        }

        return entriesCount;
    }

    /// <summary>
    ///     处理 ZIP 压缩文件 - 按文件名排序
    /// </summary>
    /// <param name="zipFilePath"></param>
    /// <returns></returns>
    private int ProcessZipFileInOrder(string zipFilePath)
    {
        var entriesCount = 0;
        var fileName = Path.GetFileName(zipFilePath);
        try
        {
            using (var zf = new ZipFile(zipFilePath))
            {
                var csvFiles = new List<ZipEntry>();
                foreach (ZipEntry zipEntry in zf)
                {
                    if (!zipEntry.IsFile || !zipEntry.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (FileTool.IsZipPathUnsafe(zipEntry.Name))
                    {
                        LogManager.Warning($"Skipping unsafe entry in ZIP archive/跳过ZIP压缩包中的不安全条目： {zipEntry.Name}");
                        continue;
                    }

                    csvFiles.Add(zipEntry);
                }

                if (csvFiles.Count == 0) return 0;

                csvFiles.Sort((a, b) => StringComparer.Ordinal.Compare(a.Name, b.Name));

                LogManager.Info(
                    $"Processing ZIP archive {fileName} - {csvFiles.Count} .csv files/正在处理ZIP压缩包: {fileName} - {csvFiles.Count} 个.csv文件)");

                foreach (var entry in csvFiles)
                    try
                    {
                        using (var stream = zf.GetInputStream(entry))
                        {
                            entriesCount += ProcessCsvStream(stream);
                        }
                    }
                    catch (Exception e)
                    {
                        LogManager.Warning($"Error processing entry in ZIP/处理ZIP中的条目时出错 {entry.Name}: {e.Message}");
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
    ///     处理 ZIP 压缩文件
    /// </summary>
    /// <param name="zipFilePath"></param>
    /// <returns></returns>
    private int ProcessZipFile(string zipFilePath)
    {
        var entriesCount = 0;
        var fileName = Path.GetFileName(zipFilePath);

        try
        {
            LogManager.Info($"Processing ZIP archive {fileName} text files/正在处理ZIP压缩包: {fileName})");

            using var fileStream = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var zipStream = new ZipInputStream(fileStream);

            // 使用更大的缓冲区提高读取性能
            const int bufferSize = 65536; // 64KB缓冲区
            var buffer = new byte[bufferSize];

            ZipEntry entry;
            while ((entry = zipStream.GetNextEntry()) != null)
            {
                // 提前检查取消请求，避免不必要的处理
                if (_cancelRequested)
                {
                    LogManager.Info("ZIP processing cancelled/ZIP处理已被取消");
                    break;
                }

                // 安全检查
                if (FileTool.IsZipPathUnsafe(entry.Name))
                {
                    LogManager.Warning($"Skipping unsafe entry in ZIP archive/跳过ZIP压缩包中的不安全条目： {entry.Name}");
                    continue;
                }

                // 只处理CSV文件
                if (!entry.IsFile || !entry.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    // 预估容量以减少内存重分配
                    var estimatedSize = entry.Size > 0 ? (int)Math.Min(entry.Size, int.MaxValue) : 4096;

                    using var memStream = new MemoryStream(estimatedSize > 0 ? estimatedSize : 4096);

                    // 高效的流复制
                    var totalBytesRead = 0;
                    int bytesRead;

                    while ((bytesRead = zipStream.Read(buffer, 0, bufferSize)) > 0)
                    {
                        memStream.Write(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;

                        // 定期检查取消请求
                        if (totalBytesRead % (bufferSize * 4) == 0 && _cancelRequested)
                        {
                            LogManager.Info("ZIP entry processing cancelled/ZIP条目处理已被取消");
                            return entriesCount;
                        }
                    }

                    // 重置流位置
                    memStream.Position = 0;

                    // 处理CSV数据
                    var entryCount = ProcessCsvStream(memStream);
                    entriesCount += entryCount;

                    if (entryCount > 0) LogManager.Debug($"Processed {entryCount} entries from {entry.Name}");
                }
                catch (Exception e)
                {
                    LogManager.Warning($"Error processing entry in ZIP/处理ZIP中的条目时出错 {entry.Name}: {e.Message}");
                }
            }

            LogManager.Debug($"ZIP processing completed. Total entries: {entriesCount}");
        }
        catch (Exception e)
        {
            LogManager.Error($"Error processing ZIP file/处理ZIP文件时出错 {fileName}: {e.Message}");
        }

        return entriesCount;
    }

    private int ProcessCsvFile(string filePath)
    {
        try
        {
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return ProcessCsvStream(fileStream);
            }
        }
        catch (Exception e)
        {
            LogManager.Error($"Error processing CSV file/处理CSV文件时出错 {Path.GetFileName(filePath)}: {e.Message}");
            return 0;
        }
    }

    /// <summary>
    ///     处理CSV流
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    private int ProcessCsvStream(Stream stream)
    {
        var entriesLoaded = 0;
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        using (var csv = new CsvReader(reader, CsvConfig))
        {
            var records = csv.GetRecords<CsvEntry>();
            foreach (var record in records)
            {
                if (string.IsNullOrEmpty(record.Term) || string.IsNullOrEmpty(record.Translation)) continue;
                _translations[record.Term] = new UITranslateManager.TranslationData(record.Translation);
                entriesLoaded++;
            }
        }

        return entriesLoaded;
    }

    private class CsvEntry
    {
        public string Term { get; set; } // 键名
        public string Original { get; set; } // 原文
        public string Translation { get; set; } // 译文
    }
}