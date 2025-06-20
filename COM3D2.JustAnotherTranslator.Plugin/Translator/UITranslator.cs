using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx.Logging;
using COM3D2.JustAnotherTranslator.Plugin.Hooks.UI;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;

namespace COM3D2.JustAnotherTranslator.Plugin.Translator;

public static class UITranslator
{
    private static Harmony _uiTranslatePatch;
    private static bool _initialized;

    // 存储翻译数据的字典
    private static Dictionary<string, string> _translations = new Dictionary<string, string>();

    public static void Init()
    {
        if (_initialized) return;

        LoadTranslation();

        _uiTranslatePatch = Harmony.CreateAndPatchAll(typeof(UITranslatePatch));

        _initialized = true;
    }

    public static void Unload()
    {
        if (!_initialized) return;

        _uiTranslatePatch?.UnpatchSelf();
        _uiTranslatePatch = null;
        _translations.Clear();

        _initialized = false;
    }

    public static string HandleTerm(string term)
    {
        if (string.IsNullOrEmpty(term)) return term;

        // 查找翻译
        if (_translations.TryGetValue(term, out string translation))
        {
            return translation;
        }

        return term; // 如果没有找到翻译，返回原文
    }

    private static void LoadTranslation()
    {
        try
        {
            // 获取所有子目录，按Unicode排序
            var directories = Directory.GetDirectories(JustAnotherTranslator.UIPath)
                .OrderBy(d => d, StringComparer.Ordinal)
                .ToList();

            // 添加根目录到列表开头，确保先处理根目录中的文件
            directories.Insert(0, JustAnotherTranslator.UIPath);

            // 获取所有文件以计算总数
            var allFiles = new List<string>();
            foreach (var directory in directories)
            {
                // 获取当前目录下的所有文件，按Unicode排序
                var files = Directory.GetFiles(directory, "*.csv")
                    .OrderBy(f => f, StringComparer.Ordinal)
                    .ToList();

                allFiles.AddRange(files);
            }

            // 计算总数
            var totalFiles = allFiles.Count;
            var totalEntries = 0;
            var processedFiles = 0;

            LogManager.Info($"开始加载翻译文件，共找到 {totalFiles} 个 CSV 文件");

            // 处理每个文件
            foreach (var filePath in allFiles)
            {
                try
                {
                    int entriesLoaded = LoadCsvFile(filePath);
                    totalEntries += entriesLoaded;
                    processedFiles++;

                    LogManager.Info(
                        $"已加载文件 ({processedFiles}/{totalFiles}): {Path.GetFileName(filePath)} - {entriesLoaded} 条条目");
                }
                catch (Exception ex)
                {
                    LogManager.Error($"加载文件失败: {filePath}, 错误: {ex.Message}");
                }
            }

            LogManager.Info($"翻译文件加载完成！总计加载 {totalEntries} 条翻译条目，来自 {processedFiles} 个文件");
        }
        catch (Exception ex)
        {
            LogManager.Error($"加载翻译文件时发生错误: {ex.Message}");
        }
    }

    private static int LoadCsvFile(string filePath)
    {
        return 1;
    }
}