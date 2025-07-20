using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

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

    ///  大文件缓冲区大小
    private static readonly int LargeFileBufferSize = 16 * 1024 * 1024; // 16MB

    /// 完成回调
    private readonly CompletionCallback _completionCallback;

    /// 进度回调
    private readonly ProgressCallback _progressCallback;

    /// 翻译目录路径
    private static readonly string TranslationPath;

    /// 取消标志
    private volatile bool _cancelRequested;

    /// 加载线程
    private Thread _loaderThread;

    /// 翻译字典
    private static readonly Dictionary<string, string> TranslationDict = new();

    /// 正则翻译字典
    private static readonly Dictionary<Regex, string> RegexTranslationDict = new();

    /// <summary>
    ///     创建一个新的异步文件加载器
    /// </summary>
    /// <param name="translationPath">翻译文件目录路径</param>
    /// <param name="progressCallback">进度回调</param>
    /// <param name="completionCallback">完成回调</param>
    public AsyncTextLoader(string translationPath, ProgressCallback progressCallback,
        CompletionCallback completionCallback)
    {
        TranslationPath = translationPath;
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
        _loaderThread.IsBackground = true; // 设置为后台线程，这样不会阻止应用程序退出
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
            if (!Directory.Exists(TranslationPath))
            {
                LogManager.Warning(
                    "Translation directory not found/未找到翻译目录: " + TranslationPath);

                // 调用完成回调，传递空字典
                _completionCallback?.Invoke(TranslationDict, RegexTranslationDict, 0, 0, 0);
                return;
            }

            LogManager.Info(
                "Loading translation files asynchronously, other plugins can load at the same time/正在异步加载翻译文件，其他插件可以同时进行加载");
            LogManager.Info(
                "Translation files are read in Unicode order, if there are duplicate translations, later read translations will overwrite earlier read translations/翻译文件按照 Unicode 顺序读取，如有相同翻译则后读取的翻译会覆盖先读取的翻译");

            // Get all files to calculate the total
            var allFiles = GetAllTranslationFiles();
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
    ///     获取所有翻译文件列表，按 Unicode 顺序排序
    /// </summary>
    /// <returns>文件路径列表</returns>
    private List<string> GetAllTranslationFiles()
    {
        var allFiles = new List<string>();

        // 首先添加根目录的文件
        try
        {
            var rootFiles = Directory.GetFiles(TranslationPath, "*.txt", SearchOption.TopDirectoryOnly)
                .OrderBy(f => f, StringComparer.Ordinal);
            allFiles.AddRange(rootFiles);
        }
        catch (Exception e)
        {
            LogManager.Warning($"Error reading root directory files/读取根目录文件时出错: {e.Message}");
        }

        // 然后添加子目录的文件
        try
        {
            var directories = Directory.GetDirectories(TranslationPath, "*", SearchOption.AllDirectories)
                .OrderBy(d => d, StringComparer.Ordinal);

            foreach (var directory in directories)
            {
                if (_cancelRequested) break;

                try
                {
                    var files = Directory.GetFiles(directory, "*.txt", SearchOption.TopDirectoryOnly)
                        .OrderBy(f => f, StringComparer.Ordinal);
                    allFiles.AddRange(files);
                }
                catch (Exception e)
                {
                    LogManager.Warning($"Error reading directory files/读取目录文件时出错 {directory}: {e.Message}");
                }
            }
        }
        catch (Exception e)
        {
            LogManager.Warning($"Error enumerating directories/枚举目录时出错: {e.Message}");
        }

        return allFiles;
    }


    /// <summary>
    ///     处理单个翻译文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>处理的条目数</returns>
    private static int ProcessTranslationFile(string filePath)
    {
        var entriesCount = 0;

        try
        {
            var fileInfo = new FileInfo(filePath);
            var fileSize = fileInfo.Length;
            var bigFile = fileSize > 10 * 1024 * 1024; // > 10MB

            // 只对大文件显示处理信息
            if (bigFile)
            {
                LogManager.Info(
                    $"Processing large file/正在处理大文件: {Path.GetFileName(filePath)} ({fileSize / (1024 * 1024):F1} MB)");
            }

            // 对于小文件，直接一次性读取全部内容
            if (fileSize < 1024 * 1024) // < 1MB
            {
                var contents = File.ReadAllLines(filePath, Encoding.UTF8);
                foreach (var line in contents)
                {
                    if (ProcessTranslationLine(line))
                        entriesCount++;
                }
            }
            else
            {
                // 对于大文件，使用 StreamReader 逐行读取
                using (var reader = new StreamReader(filePath, Encoding.UTF8, false, LargeFileBufferSize))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (ProcessTranslationLine(line))
                            entriesCount++;
                    }
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
    private bool ProcessTranslationLine(string line)
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