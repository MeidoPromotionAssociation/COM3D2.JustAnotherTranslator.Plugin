using System;
using System.Collections.Generic;
using System.IO;
using COM3D2.JustAnotherTranslator.Plugin.Hooks.Texture;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace COM3D2.JustAnotherTranslator.Plugin.Translator;

/// <summary>
///     纹理替换管理器
/// </summary>
public static class TextureReplaceManger
{
    private static bool _initialized;

    private static Harmony _textureReplacePatch;

    /// 文件路径缓存
    private static readonly Dictionary<string, string> FilePathCache = new(); // filename -> path

    /// 纹理数据缓存
    private static LRUCache<string, byte[]> _textureCache; // filename -> texture

    /// 已经导出的纹理
    private static readonly HashSet<string> DumpedTextures = new();

    public static void Init()
    {
        if (_initialized)
            return;

        if (!Directory.Exists(JustAnotherTranslator.TranslationTexturePath))
        {
            LogManager.Warning(
                "Translation texture directory not found, try to create/未找到翻译贴图目录，尝试创建");
            try
            {
                Directory.CreateDirectory(JustAnotherTranslator.TranslationTexturePath);
            }
            catch (Exception e)
            {
                LogManager.Error(
                    $"Create translation texture folder failed, plugin may not work/创建翻译贴图文件夹失败，插件可能无法运行: {e.Message}");
                return;
            }
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
            FilePathCache[fileName] = path;
        }

        // Apply patch
        _textureReplacePatch = Harmony.CreateAndPatchAll(typeof(TextureReplacePatch),
            "github.meidopromotionassociation.com3d2.justanothertranslator.plugin.hooks.texture.texturereplacepatch");

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

    /// <summary>
    ///     从缓存中检查替换纹理是否存在
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    public static bool IsReplaceTextureExist(string filename)
    {
        if (string.IsNullOrEmpty(filename))
            return false;

        if (filename.EndsWith(".tex")) filename = filename.Replace(".tex", ".png");


        if (Path.GetExtension(filename) == string.Empty)
            filename = string.Concat(filename, ".png");


        return FilePathCache.ContainsKey(filename);
    }

    /// <summary>
    ///     尝试从文件中获取纹理数据
    /// </summary>
    /// <param name="filename"></param>
    /// <param name="originalTexture"></param>
    /// <param name="replacedTexture"></param>
    /// <returns></returns>
    public static bool GetReplaceTexture(string filename, out byte[] replacedTexture, Texture originalTexture = null)
    {
        replacedTexture = null;

        if (StringTool.IsNullOrWhiteSpace(filename))
            return false;

        if (filename.EndsWith(".tex")) filename = filename.Replace(".tex", ".png");

        if (Path.GetExtension(filename) == string.Empty)
            filename = string.Concat(filename, ".png");

        if (!IsReplaceTextureExist(filename))
        {
            if (JustAnotherTranslator.EnableTexturesDump.Value)
            {
                LogManager.Debug($"Texture replace for {filename} not found, try to dump original texture");
                // 则转储原始纹理
                if (originalTexture is Texture2D tex2d)
                {
                    var bytes = GetTextureBytes(tex2d);
                    if (bytes != null)
                        DumpTexture(filename, bytes);
                }
                else
                {
                    LogManager.Warning($"original texture {filename} is not Texture2D");
                }
            }

            return false;
        }

        // 首先尝试从LRU缓存中获取纹理数据
        if (_textureCache != null && _textureCache.TryGet(filename, out replacedTexture))
        {
            LogManager.Debug($"Texture cache hit: {filename}");
            return true;
        }

        // 如果缓存中没有，则从文件中读取
        if (FilePathCache.TryGetValue(filename, out var cachePath))
            try
            {
                replacedTexture = File.ReadAllBytes(cachePath);

                // 将读取的纹理数据添加到LRU缓存中
                if (_textureCache != null)
                {
                    _textureCache.Set(filename, replacedTexture);
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

    /// <summary>
    ///     将未翻译的纹理写入文件
    /// </summary>
    /// <param name="textureName"></param>
    /// <param name="textureData"></param>
    private static void DumpTexture(string textureName, byte[] textureData)
    {
        if (!JustAnotherTranslator.EnableTexturesDump.Value)
            return;

        // 如果纹理是新的 (之前未 dump 过), addResult 会是 true
        var added = DumpedTextures.Add(textureName);

        // 只有当纹理是新的，才执行写入文件的操作
        if (added)
        {
            if (Path.GetExtension(textureName) != ".png")
                textureName = string.Concat(Path.GetFileName(textureName), ".png");

            LogManager.Debug($"Texture not translated, dumping: {textureName}");
            var filePath = Path.Combine(JustAnotherTranslator.TranslationTexturePath, textureName);
            File.WriteAllBytes(filePath, textureData);
        }
    }

    /// <summary>
    ///     获取 .png 格式的纹理数据
    /// </summary>
    /// <param name="originalTex"></param>
    /// <returns></returns>
    private static byte[] GetTextureBytes(Texture2D originalTex)
    {
        try
        {
            // 尝试直接获取
            return originalTex.EncodeToPNG();
        }
        catch
        {
            // 如果失败（例如，纹理不可读），则创建副本
            try
            {
                var renderTex = RenderTexture.GetTemporary(
                    originalTex.width, originalTex.height, 0,
                    RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

                Graphics.Blit(originalTex, renderTex);
                var previous = RenderTexture.active;
                RenderTexture.active = renderTex;
                var readableText = new Texture2D(originalTex.width, originalTex.height);
                readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
                readableText.Apply();
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(renderTex);

                var bytes = readableText.EncodeToPNG();
                Object.Destroy(readableText);
                return bytes;
            }
            catch (Exception e)
            {
                LogManager.Error($"Failed to get texture bytes for {originalTex.name}: {e.Message}");
                return null;
            }
        }
    }
}