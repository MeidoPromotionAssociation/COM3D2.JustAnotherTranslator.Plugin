using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using CsvHelper;
using CsvHelper.Configuration;
using ICSharpCode.SharpZipLib.Zip;
using JetBrains.Annotations;
#if COM3D25_UNITY_2022
using System.Linq;
#endif

namespace COM3D2.JustAnotherTranslator.Plugin.Loader;

/// <summary>
///     异步UI翻译加载器
/// </summary>
public class AsyncUiTextLoader
{
    /// 加载完成委托
    public delegate void CompletionCallback(
        Dictionary<string, string> result,
        int totalEntries,
        int totalFiles,
        long elapsedMilliseconds
    );

    /// 加载进度委托
    public delegate void ProgressCallback(float progress, int filesProcessed, int totalFiles);


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

    /// 压缩文件缓冲区大小
    private static readonly int ZipBuffSize = 131072;

    /// 压缩文件流缓冲区大小
    private static readonly int ZipSteamCacheSize = 4096;

    /// 翻译结果字典
    private static readonly Dictionary<string, string> Translations = new(); // Term -> translation

    /// 取消标志
    private static volatile bool _cancelRequested;

    /// 加载线程
    private static Thread _loaderThread;

    /// 完成回调
    private readonly CompletionCallback _completionCallback;

    /// 进度回调
    private readonly ProgressCallback _progressCallback;

    /// 翻译文件目录路径
    private readonly string _translationPath;


    /// <summary>
    ///     创建一个新的异步文件加载器
    /// </summary>
    /// <param name="translationPath"></param>
    /// <param name="completionCallback"></param>
    /// <param name="progressCallback"></param>
    public AsyncUiTextLoader(string translationPath, ProgressCallback progressCallback,
        CompletionCallback completionCallback)
    {
        _translationPath = translationPath;
        _progressCallback = progressCallback;
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

        Translations.Clear();

        _cancelRequested = false;
        _loaderThread = new Thread(LoadTranslationsThread)
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
    ///     加载线程，用于加载文件
    /// </summary>
    private void LoadTranslationsThread()
    {
        var sw = new Stopwatch();
        sw.Start();

        var totalFiles = 0;
        var totalEntries = 0;
        var filesProcessed = 0;

        if (!Directory.Exists(_translationPath))
        {
            LogManager.Warning(
                $"UI Translation directory not found, try to create/未找到UI翻译目录, 尝试创建: {_translationPath}");

            try
            {
                Directory.CreateDirectory(_translationPath);
            }
            catch (Exception e)
            {
                LogManager.Error(
                    $"Create translation UI folder failed, plugin may not work/创建翻译UI翻译目录失败，插件可能无法运行: {e.Message}");
                _completionCallback?.Invoke(Translations, 0, 0, 0);
                return;
            }
        }

        try
        {
            LogManager.Info(
                "Loading UI translation files asynchronously, other plugins can load at the same time/正在异步加载UI翻译文件，其他插件可以同时进行加载");

            var allFiles =
                FileTool.GetAllTranslationFiles(_translationPath, new[] { ".csv", ".zip" });
            totalFiles = allFiles.Count;

            if (allFiles.Count == 0)
            {
                LogManager.Info("No UI translation files found/未找到UI翻译文件");
                _completionCallback?.Invoke(Translations, 0, 0, 0);
                return;
            }

            LogManager.Info($"Found {totalFiles} CSV files/找到 {totalFiles} 个 CSV 文件");

            foreach (var filePath in allFiles)
            {
                if (_cancelRequested)
                {
                    LogManager.Info("UI Translation loading cancelled/UI翻译加载已被取消");
                    break;
                }

                var entriesInFile = ProcessTranslationFile(filePath);
                totalEntries += entriesInFile;
                filesProcessed++;

                var progress = (float)filesProcessed / totalFiles;
                _progressCallback?.Invoke(progress, filesProcessed, totalFiles);
            }
        }
        catch (Exception ex)
        {
            LogManager.Error(
                $"Error while loading UI translation file/加载UI翻译文件时发生错误: {ex.Message}");
        }
        finally
        {
            sw.Stop();
            _completionCallback?.Invoke(Translations, totalEntries, filesProcessed,
                sw.ElapsedMilliseconds);
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
            LogManager.Error(
                $"Error processing file/处理文件时出错 {Path.GetFileName(filePath)}: {e.Message}");
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
                    if (!zipEntry.IsFile ||
                        !zipEntry.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (FileTool.IsZipPathUnsafe(zipEntry.Name))
                    {
                        LogManager.Warning(
                            $"Skipping unsafe entry in ZIP archive/跳过ZIP压缩包中的不安全条目： {zipEntry.Name}");
                        continue;
                    }

                    csvFiles.Add(zipEntry);
                }

                if (csvFiles.Count == 0)
                {
                    LogManager.Info("No .csv files found in ZIP archive/ZIP压缩包中未找到.csv文件");
                    return 0;
                }

                // 按照 Unicode 顺序排序
                csvFiles.Sort((a, b) => StringComparer.Ordinal.Compare(a.Name, b.Name));

                LogManager.Info(
                    $"Processing ZIP archive {fileName} in order, which has {csvFiles.Count} .csv files/正在按顺序处理ZIP压缩包: {fileName} 内含 {csvFiles.Count} 个.csv文件)");

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
            LogManager.Info(
                $"Processing ZIP archive {fileName} text files/正在处理ZIP压缩包: {fileName})");

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

                if (!entry.IsFile ||
                    !entry.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    // 预估容量以减少内存重分配
                    var estimatedSize = entry.Size > 0
                        ? (int)Math.Min(entry.Size, int.MaxValue)
                        : ZipSteamCacheSize;
                    using (var memStream =
                           new MemoryStream(estimatedSize > 0 ? estimatedSize : ZipSteamCacheSize))
                    {
                        var buffer = new byte[ZipSteamCacheSize];
                        int bytesRead;
                        while ((bytesRead = zipStream.Read(buffer, 0, buffer.Length)) > 0)
                            memStream.Write(buffer, 0, bytesRead);

                        // 重置流位置
                        memStream.Position = 0;

                        // 处理 CSV 数据
                        var entryCount = ProcessCsvStream(memStream);
                        entriesCount += entryCount;
                    }
                }
                catch (Exception e)
                {
                    LogManager.Warning(
                        $"Error processing entry in ZIP/处理ZIP中的条目时出错 {entry.Name}: {e.Message}");
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

    /// <summary>
    ///     处理 CSV 文件
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
    private int ProcessCsvFile(string filePath)
    {
        try
        {
            using (var fileStream =
                   new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return ProcessCsvStream(fileStream);
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"Error processing CSV file/处理CSV文件时出错 {Path.GetFileName(filePath)}: {e.Message}");
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
        using (var reader = new StreamReader(stream, Encoding.UTF8, true, 4096))
        using (var csv = new CsvReader(reader, CsvConfig))
        {
            var records = csv.GetRecords<CsvEntry>();
            foreach (var record in records)
            {
                if (string.IsNullOrEmpty(record.Term) ||
                    string.IsNullOrEmpty(record.Translation)) continue;
                Translations[record.Term] = record.Translation;
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