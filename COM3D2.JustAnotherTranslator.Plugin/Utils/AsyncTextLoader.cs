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
    // 加载完成委托
    public delegate void CompletionCallback(Dictionary<string, string> result, Dictionary<Regex, string> resultRegex,
        int totalEntries, int totalFiles,
        long elapsedMilliseconds);

    // 加载进度委托
    public delegate void ProgressCallback(float progress, int filesProcessed, int totalFiles);

    // 文件读取缓冲区大小 (默认16MB)
    private const int DefaultBufferSize = 16 * 1024 * 1024;

    // 文件读取块大小 (默认4MB)
    private const int FileReadChunkSize = 4 * 1024 * 1024;

    // 完成回调
    private readonly CompletionCallback _completionCallback;

    // 进度回调
    private readonly ProgressCallback _progressCallback;

    // 翻译目录路径
    private readonly string _translationPath;

    // 取消标志
    private bool _cancelRequested;

    // 线程对象
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

        var translationDict = new Dictionary<string, string>();
        var regexTranslationDict = new Dictionary<Regex, string>();
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
                _completionCallback?.Invoke(translationDict, regexTranslationDict, 0, 0, 0);
                return;
            }

            LogManager.Info(
                "Loading translation files asynchronously, other plugins can load at the same time/正在异步加载翻译文件，其他插件可以同时进行加载");
            LogManager.Info(
                "Translation files are read in Unicode order, if there are duplicate translations, later read translations will overwrite earlier read translations/翻译文件按照 Unicode 顺序读取，如有相同翻译则后读取的翻译会覆盖先读取的翻译");

            // 获取所有子目录，按Unicode排序
            var directories = Directory.GetDirectories(_translationPath)
                .OrderBy(d => d, StringComparer.Ordinal)
                .ToList();

            // 添加根目录到列表开头，确保先处理根目录中的文件
            directories.Insert(0, _translationPath);

            // 获取所有文件以计算总数
            var allFiles = new List<string>();
            foreach (var directory in directories)
            {
                // 获取当前目录下的所有文件，按Unicode排序
                var files = Directory.GetFiles(directory, "*.txt")
                    .OrderBy(f => f, StringComparer.Ordinal)
                    .ToList();

                allFiles.AddRange(files);
            }

            totalFiles = allFiles.Count;

            // 处理所有文件
            foreach (var file in allFiles)
            {
                // 检查是否请求取消
                if (_cancelRequested)
                {
                    LogManager.Info("Translation loading cancelled/翻译加载已取消");
                    break;
                }

                var entriesInFile = ProcessTranslationFile(file, translationDict, regexTranslationDict);
                totalEntries += entriesInFile;
                filesProcessed++;

                // 报告进度
                var progress = (float)filesProcessed / totalFiles;
                _progressCallback?.Invoke(progress, filesProcessed, totalFiles);
            }

            sw.Stop();
            LogManager.Info(
                $"Total loaded {totalEntries} translations from {filesProcessed} files, cost {sw.ElapsedMilliseconds} ms/总共从 {filesProcessed} 个文件中加载了 {totalEntries} 条翻译，耗时 {sw.ElapsedMilliseconds} 毫秒");

            // 调用完成回调
            _completionCallback?.Invoke(translationDict, regexTranslationDict, totalEntries, filesProcessed,
                sw.ElapsedMilliseconds);
        }
        catch (Exception e)
        {
            LogManager.Error("Error loading translation files/加载翻译文件时出错: " + e.Message);

            // 调用完成回调，传递当前已加载的字典
            _completionCallback?.Invoke(translationDict, regexTranslationDict, totalEntries, filesProcessed,
                sw.ElapsedMilliseconds);
        }
    }

    /// <summary>
    ///     处理单个翻译文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="translationDict">翻译字典</param>
    /// <param name="regexTranslationDict">正则表达式翻译字典</param>
    /// <returns>处理的条目数</returns>
    private int ProcessTranslationFile(string filePath, Dictionary<string, string> translationDict,
        Dictionary<Regex, string> regexTranslationDict)
    {
        var entriesCount = 0;

        try
        {
            var fileInfo = new FileInfo(filePath);
            var fileSize = fileInfo.Length;

            LogManager.Info(
                $"Processing file/正在处理文件: {Path.GetFileName(filePath)} ({fileSize / (1024 * 1024)} MB)");

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, FileReadChunkSize))
            using (var bs = new BufferedStream(fs, DefaultBufferSize))
            using (var reader = new StreamReader(bs, Encoding.UTF8))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // 检查是否请求取消
                    if (_cancelRequested)
                        break;

                    if (ProcessTranslationLine(line, translationDict, regexTranslationDict))
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
    /// <param name="translationDict">翻译字典</param>
    /// <param name="regexTranslationDict">正则表达式翻译字典</param>
    /// <returns>是否成功处理</returns>
    private bool ProcessTranslationLine(string line, Dictionary<string, string> translationDict,
        Dictionary<Regex, string> regexTranslationDict)
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
            regexTranslationDict[new Regex(original.Substring(1), RegexOptions.Compiled)] = translation;
        else
            translationDict[original] = translation;

        return true;
    }
}