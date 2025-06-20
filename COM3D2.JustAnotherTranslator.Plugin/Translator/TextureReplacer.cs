using System;
using System.Collections.Generic;
using System.IO;
using COM3D2.JustAnotherTranslator.Plugin.Hooks;
using COM3D2.JustAnotherTranslator.Plugin.Hooks.Texture;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;

namespace COM3D2.JustAnotherTranslator.Plugin.Translator;

public static class TextureReplacer
{
    private static bool _initialized;
    private static Harmony _textureReplacePatch;
    private static readonly Dictionary<string, string> _filePathCache = new();
    private static LRUCache<string, byte[]> _textureCache;

    public static void Init()
    {
        if (_initialized)
            return;

        if (!Directory.Exists(JustAnotherTranslator.TranslationTexturePath))
        {
            LogManager.Warning(
                "Translation texture directory not found, try to create/未找到翻译贴图目录，尝试创建: " +
                JustAnotherTranslator.TranslationTexturePath);
            try
            {
                Directory.CreateDirectory(JustAnotherTranslator.TranslationTexturePath);
            }
            catch (Exception e)
            {
                LogManager.Error(
                    "Create translation texture folder failed, plugin may not work/创建翻译贴图文件夹失败，插件可能无法运行: " +
                    e.Message);
            }

            return;
        }

        // 初始化LRU缓存
        _textureCache = new LRUCache<string, byte[]>(JustAnotherTranslator.TextureCacheSize.Value);
        LogManager.Debug(
            $"Texture LRU cache initialized with capacity {JustAnotherTranslator.TextureCacheSize.Value}");

        var files = Directory.GetFiles(JustAnotherTranslator.TranslationTexturePath, "*.png",
            SearchOption.AllDirectories);
        LogManager.Info(
            $"Found {files.Length} texture files in translation texture directory/在翻译贴图目录中找到 {files.Length} 个贴图文件");

        // Cache all texture file path
        foreach (var path in files)
        {
            var fileName = Path.GetFileName(path);
            _filePathCache[fileName] = path;
        }

        // Apply patch
        _textureReplacePatch = Harmony.CreateAndPatchAll(typeof(TextureReplacePatch));

        _initialized = true;
    }

    public static void Unload()
    {
        if (!_initialized)
            return;

        _textureReplacePatch?.UnpatchSelf();

        // 清空缓存
        if (_textureCache != null)
        {
            _textureCache.Clear();
            _textureCache = null;
        }

        _initialized = false;
    }

    public static bool GetReplaceTexture(string filename, out byte[] replaced)
    {
        replaced = null;

        if (string.IsNullOrEmpty(filename))
            return false;

        if (filename.EndsWith(".tex")) filename = filename.Replace(".tex", ".png");

        if (Path.GetExtension(filename) == string.Empty) filename += ".png";

        // 首先尝试从LRU缓存中获取纹理数据
        if (_textureCache != null && _textureCache.TryGet(filename, out replaced))
        {
            LogManager.Debug($"Texture cache hit: {filename}");
            return true;
        }

        // 如果缓存中没有，则从文件中读取
        if (_filePathCache.TryGetValue(filename, out var cachePath))
            try
            {
                replaced = File.ReadAllBytes(cachePath);

                // 将读取的纹理数据添加到LRU缓存中
                if (_textureCache != null)
                {
                    _textureCache.Set(filename, replaced);
                    LogManager.Debug($"Texture added to cache: {filename}");
                }

                return true;
            }
            catch (Exception e)
            {
                LogManager.Error(
                    $"Failed to read texture file: {cachePath}, error: {e.Message}/读取贴图文件失败: {cachePath}, 错误: {e.Message}");
                return false;
            }

        return false;
    }

    public static bool IsTextureExist(string filename)
    {
        if (string.IsNullOrEmpty(filename))
            return false;

        if (filename.EndsWith(".tex")) filename = filename.Replace(".tex", ".png");

        if (Path.GetExtension(filename) == string.Empty) filename += ".png";

        return _filePathCache.ContainsKey(filename);
    }
}