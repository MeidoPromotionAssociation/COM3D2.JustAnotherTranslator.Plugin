using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using BepInEx.Logging;
using COM3D2.JustAnotherTranslator.Plugin.Hooks.UI;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using CsvHelper;
using CsvHelper.Configuration;
using HarmonyLib;
using UnityEngine;

namespace COM3D2.JustAnotherTranslator.Plugin.Translator;

public static class UITranslator
{
    private static Harmony _uiTranslatePatch;
    private static Harmony _uiDebugPatch;
    private static bool _initialized;
    private static Thread _textLoaderThread;

    // 存储翻译数据的字典
    private static Dictionary<string, TranslationData> _translations = new();

    // 缓存文件名到完整路径的映射
    private static readonly Dictionary<string, string> SpritePathCache = new(); // filename -> path

    // LRU缓存已加载的图片纹理
    private static LRUCache<string, Texture2D> _spriteCache;

    // 缓存原始精灵图信息
    private static readonly Dictionary<UIWidget, OriginalSpriteInfoStruct> OriginalSpriteInfo = new();

    public static void Init()
    {
        if (_initialized) return;

        _spriteCache = new LRUCache<string, Texture2D>(JustAnotherTranslator.TextureCacheSize.Value);

        LoadTextTranslationsAsync();
        LoadSpriteTextures();

        _uiTranslatePatch = Harmony.CreateAndPatchAll(typeof(UITranslatePatch),
            "com3d2.justanothertranslator.plugin.hooks.ui.uitranslatepatch");

        if (JustAnotherTranslator.LogLevelConfig.Value >= LogLevel.Debug)
        {
            _uiDebugPatch = new Harmony("com3d2.justanothertranslator.plugin.hooks.ui.uidebugpatch");
            UIDebugPatch.LocalizeTargetPatcher.ApplyPatch(_uiDebugPatch);
        }

        _initialized = true;
    }

    public static void Unload()
    {
        if (!_initialized) return;

        _textLoaderThread?.Join();
        _textLoaderThread = null;

        _uiTranslatePatch?.UnpatchSelf();
        _uiTranslatePatch = null;
        _translations.Clear();
        _spriteCache.Clear();
        SpritePathCache.Clear();

        _initialized = false;
    }

    /// <summary>
    ///     处理UI文本翻译
    /// </summary>
    /// <param name="term"></param>
    /// <returns></returns>
    public static string HandleTextTermTranslation(string term)
    {
        if (string.IsNullOrEmpty(term)) return term;

        // SceneDaily/ボタン文字/男エディット
        if (_translations.TryGetValue(term, out var translation))
        {
            var markedTranslation = XUATInterop.MarkTranslated(translation.Translation);
            LogManager.Debug($"Found translation for term: {term}, translation: {markedTranslation}");
            return markedTranslation;
        }

        // 剔除第一个/前的字符
        // ボタン文字/男エディット
        var slashIndex = term.IndexOf('/');
        if (slashIndex > -1)
        {
            var newTerm = term.Substring(slashIndex + 1);
            if (_translations.TryGetValue(newTerm, out translation))
            {
                var markedTranslation = XUATInterop.MarkTranslated(translation.Translation);
                LogManager.Debug(
                    $"Found translation for term: {term} (as {newTerm}), translation: {markedTranslation}");
                return markedTranslation;
            }
        }

        return term;
    }


    # region Text

    /// <summary>
    ///     异步加载翻译文件
    /// </summary>
    private static void LoadTextTranslationsAsync()
    {
        if (_textLoaderThread is { IsAlive: true })
        {
            LogManager.Warning(
                "UI Translation loader is already running, please report this issue/UI 翻译加载器已在运行中，请报告此问题");
            return;
        }

        _textLoaderThread = new Thread(LoadTranslationsThread)
        {
            IsBackground = true
        };
        _textLoaderThread.Start();
    }

    /// <summary>
    ///     加载翻译文件
    /// </summary>
    private static void LoadTranslationsThread()
    {
        var sw = new Stopwatch();
        sw.Start();

        var tempTranslations = new Dictionary<string, TranslationData>();
        var totalEntries = 0;
        var processedFiles = 0;


        if (!Directory.Exists(JustAnotherTranslator.UITextPath))
        {
            LogManager.Warning(
                "Translation UITextPath directory not found, try to create/未找到UI翻译目录，尝试创建: " +
                JustAnotherTranslator.UITextPath);
            try
            {
                Directory.CreateDirectory(JustAnotherTranslator.UITextPath);
            }
            catch (Exception e)
            {
                LogManager.Error(
                    "Create translation UIText folder failed, plugin may not work/创建翻译UI翻译目录失败，插件可能无法运行: " +
                    e.Message);
                return;
            }
        }


        try
        {
            // 获取所有子目录，按Unicode排序
            var directories = Directory.GetDirectories(JustAnotherTranslator.UITextPath)
                .OrderBy(d => d, StringComparer.Ordinal)
                .ToList();

            // 添加根目录到列表开头，确保先处理根目录中的文件
            directories.Insert(0, JustAnotherTranslator.UIPath);

            // 获取所有文件以计算总数
            var allFiles = directories.SelectMany(dir => Directory.GetFiles(dir, "*.csv")
                    .OrderBy(f => f, StringComparer.Ordinal))
                .ToList();

            var totalFiles = allFiles.Count;

            if (totalFiles > 0)
                LogManager.Info(
                    $"Starting asynchronous UI translation loading, found {totalFiles} CSV files/开始异步加载UI翻译文件，共找到 {totalFiles} 个 CSV 文件");

            // 处理每个文件
            foreach (var filePath in allFiles)
                try
                {
                    var entriesLoaded = LoadCsvFile(filePath, tempTranslations);
                    totalEntries += entriesLoaded;
                    processedFiles++;

                    LogManager.Info(
                        $"Loading UI file/正在加载UI文件 ({processedFiles}/{totalFiles}): {Path.GetFileName(filePath)} - {entriesLoaded} 条");
                }
                catch (Exception ex)
                {
                    processedFiles++;
                    LogManager.Warning(
                        $"Failed to load UI file/加载UI文件失败 ({processedFiles}/{totalFiles}): {filePath}, error: {ex.Message}");
                }

            _translations = tempTranslations;
        }
        catch (Exception ex)
        {
            LogManager.Error($"Error while loading UI translation file/加载UI翻译文件时发生错误: {ex.Message}");
        }
        finally
        {
            sw.Stop();
            if (totalEntries > 0)
                LogManager.Info(
                    $"UI translation loading completed! Total entries: {totalEntries}, from {processedFiles} files, took {sw.ElapsedMilliseconds} ms/UI翻译文件加载完成！总计加载 {totalEntries} 条翻译条目，来自 {processedFiles} 个文件, 耗时 {sw.ElapsedMilliseconds} ms");
        }
    }


    /// <summary>
    ///     加载CSV文件
    /// </summary>
    private static int LoadCsvFile(string filePath, IDictionary<string, TranslationData> translations)
    {
        var entriesLoaded = 0;

        var csvConfig = new CsvConfiguration();
        {
            csvConfig.CultureInfo = CultureInfo.InvariantCulture;
            csvConfig.AllowComments = true;
            csvConfig.HasHeaderRecord = true;
            csvConfig.Encoding = Encoding.UTF8;
            csvConfig.IgnoreBlankLines = true;
            csvConfig.IgnoreHeaderWhiteSpace = true;
            csvConfig.IsHeaderCaseSensitive = false;
            csvConfig.SkipEmptyRecords = true;
            csvConfig.WillThrowOnMissingField = true;
        }

        using var reader = new StreamReader(filePath, Encoding.UTF8);
        using var csv = new CsvReader(reader, csvConfig);

        var records = csv.GetRecords<CsvEntry>();

        foreach (var record in records)
        {
            if (string.IsNullOrEmpty(record.Term) || string.IsNullOrEmpty(record.Translation)) continue;

            // 使用精简结构以节省内存
            translations[record.Term] = new TranslationData(record.Translation);
            entriesLoaded++;
        }

        return entriesLoaded;
    }

    // CSV 结构
    private class CsvEntry
    {
        public string Term { get; set; } // 键名
        public string Original { get; set; } // 原文
        public string Translation { get; set; } // 译文
    }

    // 翻译数据
    public readonly struct TranslationData
    {
        public string Translation { get; } // 译文

        public TranslationData(string translation)
        {
            Translation = translation;
        }
    }

    # endregion


    #region Sprite

    /// <summary>
    ///     存储原始精灵图信息的结构
    /// </summary>
    private struct OriginalSpriteInfoStruct
    {
        public UIAtlas OriginalAtlas;
        public string OriginalSpriteName;
        public UISpriteData OriginalSpriteData;
        public Material OriginalMaterial;
    }


    /// <summary>
    ///     同步扫描可替换的图片文件路径
    /// </summary>
    private static void LoadSpriteTextures()
    {
        var sw = new Stopwatch();
        sw.Start();

        try
        {
            if (!Directory.Exists(JustAnotherTranslator.UISpritePath))
            {
                LogManager.Warning(
                    "Translation UISpritePath directory not found, try to create/未找到UI精灵图目录，尝试创建: " +
                    JustAnotherTranslator.UISpritePath);
                try
                {
                    Directory.CreateDirectory(JustAnotherTranslator.UISpritePath);
                }
                catch (Exception e)
                {
                    LogManager.Error(
                        "Create translation UISprite folder failed, plugin may not work/创建翻译UI精灵图目录失败，插件可能无法运行: " +
                        e.Message);
                    return;
                }
            }

            var files = Directory.GetFiles(JustAnotherTranslator.UISpritePath, "*.png", SearchOption.AllDirectories);

            LogManager.Info(
                $"Found {files.Length} Sprite files in translation UISprite directory/在UI精灵图目录目录中找到 {files.Length} 个精灵图文件 ");

            SpritePathCache.Clear();
            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                SpritePathCache[fileName] = file;
            }
        }
        catch (Exception e)
        {
            LogManager.Error($"Error while scanning for UI textures/扫描UI图片时发生错误: {e.Message}");
        }
        finally
        {
            sw.Stop();
            if (SpritePathCache.Count > 0)
                LogManager.Info(
                    $"UI texture scanning completed! Found {SpritePathCache.Count} items, took {sw.ElapsedMilliseconds} ms/UI图片扫描完成！共找到 {SpritePathCache.Count} 个项目, 耗时 {sw.ElapsedMilliseconds} ms");
        }
    }

    /// <summary>
    ///     检查是否有可用的替换精灵图
    /// </summary>
    /// <param name="spriteName">精灵图名称</param>
    /// <returns></returns>
    public static bool IsSpriteReplaceAvailable(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return false;
        return SpritePathCache.ContainsKey(spriteName);
    }

    /// <summary>
    ///     获取指定名称的替换纹理
    /// </summary>
    /// <param name="spriteName">图片名称</param>
    /// <returns>加载的Texture2D对象，如果失败则返回null</returns>
    public static Texture2D GetSpriteTexture(string spriteName)
    {
        if (!IsSpriteReplaceAvailable(spriteName)) return null;

        if (_spriteCache.TryGet(spriteName, out var cachedTexture)) return cachedTexture;

        try
        {
            if (!SpritePathCache.TryGetValue(spriteName, out var path)) return null;

            var fileData = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2);
            if (texture.LoadImage(fileData)) // LoadImage会自动调整纹理大小
            {
                LogManager.Debug($"Loaded texture {spriteName} from {path}");
                _spriteCache.Set(spriteName, texture);
                return texture;
            }

            LogManager.Warning($"Failed to load image data for texture: {spriteName}");
        }
        catch (Exception e)
        {
            LogManager.Error($"Failed to load texture {spriteName}: {e.Message}");
        }

        return null;
    }

    /// <summary>
    ///     替换整个 Atlas ，创建包含单个精灵图的新 Atlas
    /// </summary>
    public static void ProcessSpriteReplacementWithNewAtlas(UISprite sprite, string spriteName)
    {
        if (sprite == null) return;

        if (IsSpriteReplaceAvailable(spriteName))
        {
            var replacementTexture = GetSpriteTexture(spriteName);
            if (replacementTexture != null)
            {
                // 保存原始信息
                if (!OriginalSpriteInfo.ContainsKey(sprite))
                    OriginalSpriteInfo[sprite] = new OriginalSpriteInfoStruct
                    {
                        OriginalAtlas = sprite.atlas,
                        OriginalSpriteName = sprite.spriteName,
                        OriginalSpriteData = sprite.GetAtlasSprite(),
                        OriginalMaterial = sprite.material
                    };

                var newAtlas = CreateSingleSpriteAtlas(replacementTexture, sprite.material, spriteName);

                sprite.atlas = newAtlas;
                sprite.spriteName = spriteName;

                LogManager.Debug($"Applied new atlas for {spriteName} on {sprite.name}");
            }
        }
        else
        {
            // 恢复原始精灵图，因为按钮可能会被复用，如果不还原可能导致显示错误
            RestoreOriginalSprite(sprite);
        }
    }

    /// <summary>
    ///     创建包含单个精灵图的 Atlas
    /// </summary>
    private static UIAtlas CreateSingleSpriteAtlas(Texture2D texture, Material oldMaterial, string spriteName)
    {
        // 创建 Atlas GameObject
        var atlasGo = new GameObject($"JAT_ReplacementAtlas_{spriteName}");
        var atlas = atlasGo.AddComponent<UIAtlas>();

        // 复制材质，并使用新的纹理
        var newMaterial = new Material(oldMaterial);
        newMaterial.mainTexture = texture;
        atlas.spriteMaterial = newMaterial;

        // 创建精灵图数据 - 覆盖整个纹理
        var spriteData = new UISpriteData();
        spriteData.name = spriteName;
        spriteData.x = 0;
        spriteData.y = 0;
        spriteData.width = texture.width;
        spriteData.height = texture.height;

        // 设置边框为0（禁用九宫格拉伸）
        spriteData.borderLeft = 0;
        spriteData.borderRight = 0;
        spriteData.borderTop = 0;
        spriteData.borderBottom = 0;

        // 设置填充为0
        spriteData.paddingLeft = 0;
        spriteData.paddingRight = 0;
        spriteData.paddingTop = 0;
        spriteData.paddingBottom = 0;

        // 添加到 Atlas
        atlas.spriteList = new List<UISpriteData> { spriteData };

        return atlas;
    }

    /// <summary>
    ///     恢复原始精灵图
    /// </summary>
    private static void RestoreOriginalSprite(UISprite sprite)
    {
        if (OriginalSpriteInfo.TryGetValue(sprite, out var info))
        {
            sprite.atlas = info.OriginalAtlas;
            sprite.spriteName = info.OriginalSpriteName;
            sprite.SetAtlasSprite(info.OriginalSpriteData);
            sprite.material = info.OriginalMaterial;

            OriginalSpriteInfo.Remove(sprite);

            LogManager.Debug($"Restored original sprite for {sprite.name}");
        }
    }


    /// <summary>
    ///     调试方法：打印精灵图详细信息
    /// </summary>
    public static void DebugSpriteInfo(UISprite sprite)
    {
        if (sprite == null) return;

        try
        {
            LogManager.Debug($"=== Sprite Debug Info for {sprite.name} ===");
            LogManager.Debug($"Atlas: {(sprite.atlas != null ? sprite.atlas.name : "null")}");
            LogManager.Debug($"Sprite Name: {sprite.spriteName}");
            LogManager.Debug($"Material: {(sprite.material != null ? sprite.material.name : "null")}");
            LogManager.Debug(
                $"Main Texture: {(sprite.mainTexture != null ? $"{sprite.mainTexture.name} ({sprite.mainTexture.width}x{sprite.mainTexture.height})" : "null")}");
            LogManager.Debug($"Widget Size: {sprite.width}x{sprite.height}");
            LogManager.Debug($"Type: {sprite.type}");

            var spriteData = sprite.GetAtlasSprite();
            if (spriteData != null)
            {
                LogManager.Debug("Sprite Data:");
                LogManager.Debug($"  Position: ({spriteData.x}, {spriteData.y})");
                LogManager.Debug($"  Size: {spriteData.width}x{spriteData.height}");
                LogManager.Debug(
                    $"  Borders: L{spriteData.borderLeft} R{spriteData.borderRight} T{spriteData.borderTop} B{spriteData.borderBottom}");
                LogManager.Debug(
                    $"  Padding: L{spriteData.paddingLeft} R{spriteData.paddingRight} T{spriteData.paddingTop} B{spriteData.paddingBottom}");
            }
            else
            {
                LogManager.Debug("Sprite Data: null");
            }

            LogManager.Debug("=====================================");
        }
        catch
        {
        }
    }

    #endregion
}