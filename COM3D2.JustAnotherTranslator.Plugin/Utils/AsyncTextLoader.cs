using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using ICSharpCode.SharpZipLib.Zip;

namespace COM3D2.JustAnotherTranslator.Plugin.Utils;

/// <summary>
///     异步文件加载器，用于在后台线程中加载翻译文件
/// </summary>
public class AsyncTextLoader
{
    /// 加载完成委托
    public delegate void CompletionCallback(Dictionary<string, string> result, Dictionary<Regex, string> resultRegex,
        int totalEntries, int totalFiles,
        long elapsedMilliseconds);

    /// 加载进度委托
    public delegate void ProgressCallback(float progress, int filesProcessed, int totalFiles);

    /// 大文件缓冲区大小
    private static readonly int LargeFileBufferSize = 16 * 1024 * 1024; // 16MB

    /// 翻译目录路径
    private static string _translationPath;

    /// 翻译字典
    private static readonly Dictionary<string, string> TranslationDict = new();

    /// 正则翻译字典
    private static readonly Dictionary<Regex, string> RegexTranslationDict = new();

    /// 完成回调
    private readonly CompletionCallback _completionCallback;

    /// 进度回调
    private readonly ProgressCallback _progressCallback;

    /// 取消标志
    private volatile bool _cancelRequested;

    /// 加载线程
    private Thread _loaderThread;

    /// <summary>
    ///     创建一个新的异步文件加载器
    /// </summary>
    /// <param name="translationPath">翻译文件目录路径</param>
    /// <param name="progressCallback">进度回调</param>
    /// <param name="completionCallback">完成回调</param>
    public AsyncTextLoader(string translationPath, ProgressCallback progressCallback,
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
            LogManager.Warning("Async file loader is already running, please report this issue/异步文件加载器已在运行中，请报告此问题");
            return;
        }

        TranslationDict.Clear();
        RegexTranslationDict.Clear();

        _cancelRequested = false;
        _loaderThread = new Thread(LoadFilesThreadFunc);
        _loaderThread.IsBackground = true;
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
    ///     线程函数，用于加载文件
    /// </summary>
    private void LoadFilesThreadFunc()
    {
        // 记录加载时间
        var sw = new Stopwatch();
        sw.Start();

        var totalFiles = 0;
        var totalEntries = 0;
        var filesProcessed = 0;

        try
        {
            if (!Directory.Exists(_translationPath))
            {
                LogManager.Warning(
                    "Translation directory not found/未找到翻译目录: " + _translationPath);

                // 调用完成回调，传递空字典
                _completionCallback?.Invoke(TranslationDict, RegexTranslationDict, 0, 0, 0);
                return;
            }

            LogManager.Info(
                "Loading translation files asynchronously, other plugins can load at the same time/正在异步加载翻译文件，其他插件可以同时进行加载");
            LogManager.Info(
                "Translation files are read in Unicode order, if there are duplicate translations, later read translations will overwrite earlier read translations/翻译文件按照 Unicode 顺序读取，如有相同翻译则后读取的翻译会覆盖先读取的翻译");
            LogManager.Info(
                "If you have many small files, it is recommended to compress to .zip or merge into a single .txt file to speed up loading/如果您有很多个小文件，建议压缩为 .zip 或合并到单个 .txt 中以加速加载");
            LogManager.Info("Please note that files in zip file do not support sorting/请注意zip中的文件不支持排序");

            // Get all files to calculate the total
            var allFiles = FileTool.GetAllTranslationFiles(_translationPath, new[] { ".txt", ".zip" });
            totalFiles = allFiles.Count;

            if (totalFiles == 0)
            {
                LogManager.Info("No translation files found/未找到翻译文件");
                _completionCallback?.Invoke(TranslationDict, RegexTranslationDict, 0, 0, sw.ElapsedMilliseconds);
                return;
            }

            LogManager.Info($"Found {totalFiles} translation files/发现 {totalFiles} 个翻译文件");

            // 处理所有文件
            foreach (var file in allFiles)
            {
                // 检查是否请求取消
                if (_cancelRequested)
                {
                    LogManager.Info("Translation loading cancelled/翻译加载已被取消");
                    break;
                }

                var entriesInFile = ProcessTranslationFile(file);
                totalEntries += entriesInFile;
                filesProcessed++;

                var progress = (float)filesProcessed / totalFiles;
                _progressCallback?.Invoke(progress, filesProcessed, totalFiles);
            }
        }
        catch (Exception e)
        {
            LogManager.Error("Error loading translation files/加载翻译文件时出错: " + e.Message);
        }
        finally
        {
            sw.Stop();
            _completionCallback?.Invoke(TranslationDict, RegexTranslationDict, totalEntries, filesProcessed,
                sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    ///     处理单个翻译文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>处理的条目数</returns>
    private int ProcessTranslationFile(string filePath)
    {
        var entriesCount = 0;

        try
        {
            if (filePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                if (JustAnotherTranslator.AllowFilesInZipLoadInOrder.Value)
                    entriesCount = ProcessZipFileInOrder(filePath);
                else
                    entriesCount = ProcessZipFile(filePath);
            else if (filePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                entriesCount = ProcessTextFile(filePath);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"Error processing file/处理文件时出错 {Path.GetFileName(filePath)}: {e.Message}");
        }

        return entriesCount;
    }

    /// <summary>
    ///     处理 ZIP 压缩文件
    /// </summary>
    /// <param name="zipFilePath">ZIP 文件路径</param>
    /// <returns>处理的条目数</returns>
    private int ProcessZipFileInOrder(string zipFilePath)
    {
        var entriesCount = 0;
        var fileName = Path.GetFileName(zipFilePath);

        try
        {
            using (var zf = new ZipFile(zipFilePath))
            {
                var textFiles = new List<ZipEntry>();
                foreach (ZipEntry zipEntry in zf)
                {
                    if (!zipEntry.IsFile || !zipEntry.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (FileTool.IsZipPathUnsafe(zipEntry.Name))
                    {
                        LogManager.Warning($"Skipping unsafe entry in ZIP archive/跳过ZIP压缩包中的不安全条目： {zipEntry.Name}");
                        continue;
                    }

                    textFiles.Add(zipEntry);
                }

                if (textFiles.Count == 0)
                    // LogManager.Warning($"No .txt files found in ZIP archive/ZIP压缩包中未找到.txt文件: {fileName}");
                    return 0;

                // 按照Unicode顺序排序
                textFiles.Sort((a, b) => StringComparer.Ordinal.Compare(a.Name, b.Name));

                LogManager.Info(
                    $"Processing ZIP archive {fileName} - {textFiles.Count} .txt files/正在处理ZIP压缩包: {fileName} - {textFiles.Count} 个.txt文件)");

                foreach (var entry in textFiles)
                    try
                    {
                        using (var stream = zf.GetInputStream(entry))
                        {
                            // 读取条目内容
                            var buffer = new byte[entry.Size];
                            var totalBytesRead = 0;

                            while (totalBytesRead < entry.Size)
                            {
                                var bytesRead = stream.Read(buffer, totalBytesRead,
                                    (int)(entry.Size - totalBytesRead));
                                if (bytesRead == 0) break;
                                totalBytesRead += bytesRead;
                            }

                            // 将字节转换为文本并逐行处理
                            var content = Encoding.UTF8.GetString(buffer, 0, totalBytesRead);
                            var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                            foreach (var line in lines)
                                if (ProcessTranslationLine(line))
                                    entriesCount++;
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
    /// <param name="zipFilePath">ZIP 文件路径</param>
    /// <returns>处理的条目数</returns>
    private int ProcessZipFile(string zipFilePath)
    {
        var entriesCount = 0;
        var fileName = Path.GetFileName(zipFilePath);

        try
        {
            LogManager.Info($"Processing ZIP archive {fileName} text files/正在处理ZIP压缩包: {fileName})");

            // 重新打开文件流来读取内容
            using (var fileStream = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read))
            using (var zipStream = new ZipInputStream(fileStream))
            {
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
                        !entry.Name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                        continue;


                    try
                    {
                        // 读取条目内容
                        var buffer = new byte[entry.Size];
                        var totalBytesRead = 0;

                        while (totalBytesRead < entry.Size)
                        {
                            var bytesRead = zipStream.Read(buffer, totalBytesRead,
                                (int)(entry.Size - totalBytesRead));
                            if (bytesRead == 0) break;
                            totalBytesRead += bytesRead;
                        }

                        // 将字节转换为文本并逐行处理
                        var content = Encoding.UTF8.GetString(buffer, 0, totalBytesRead);
                        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var line in lines)
                            if (ProcessTranslationLine(line))
                                entriesCount++;
                    }
                    catch (Exception e)
                    {
                        LogManager.Warning(
                            $"Error processing entry in ZIP/处理ZIP中的条目时出错 {entry.Name}: {e.Message}");
                    }
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
    ///     处理单个翻译文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>处理的条目数</returns>
    private static int ProcessTextFile(string filePath)
    {
        var entriesCount = 0;

        try
        {
            var fileInfo = new FileInfo(filePath);
            var fileSize = fileInfo.Length;
            var bigFile = fileSize > 10 * 1024 * 1024; // > 10MB

            // 只对大文件显示处理信息
            if (bigFile)
                LogManager.Info(
                    $"Processing large file/正在处理大文件: {Path.GetFileName(filePath)} ({fileSize / (1024 * 1024):F1} MB)");

            // 对于小文件，直接一次性读取全部内容
            if (fileSize < 1024 * 1024) // < 1MB
            {
                var contents = File.ReadAllLines(filePath, Encoding.UTF8);
                foreach (var line in contents)
                    if (ProcessTranslationLine(line))
                        entriesCount++;
            }
            else
            {
                // 对于大文件，使用 StreamReader 逐行读取
                using (var reader = new StreamReader(filePath, Encoding.UTF8, false, LargeFileBufferSize))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                        if (ProcessTranslationLine(line))
                            entriesCount++;
                }
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
    ///     处理单行翻译文本
    /// </summary>
    /// <param name="line">文本行</param>
    /// <returns>是否成功处理</returns>
    private static bool ProcessTranslationLine(string line)
    {
        if (string.IsNullOrEmpty(line) || line.StartsWith(";"))
            return false;

        var parts = line.Split(new[] { '\t' }, 2);
        if (parts.Length != 2)
            return false;

        // Unescape 将字符串中的转义序列转换为对应的实际字符
        var original = parts[0].Unescape();
        var translation = parts[1].Unescape();

        if (string.IsNullOrEmpty(original) || string.IsNullOrEmpty(translation))
            return false;


        if (line.StartsWith("$"))
            RegexTranslationDict[new Regex(original.Substring(1), RegexOptions.Compiled)] = translation;
        else
            TranslationDict[original] = translation;

        return true;
    }
}