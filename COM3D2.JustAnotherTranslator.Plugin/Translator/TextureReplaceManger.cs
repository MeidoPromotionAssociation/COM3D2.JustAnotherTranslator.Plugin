using System;
using System.Collections.Generic;
using System.IO;
using COM3D2.JustAnotherTranslator.Plugin.Hooks.Texture;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    /// 已经导出的纹理
    private static readonly HashSet<string> DumpedTextures = new();

    public static void Init()
    {
        if (_initialized)
            return;

        ScanReplaceTextures();

        // Apply patch
        _textureReplacePatch = Harmony.CreateAndPatchAll(typeof(TextureReplacePatch),
            "github.meidopromotionassociation.com3d2.justanothertranslator.plugin.hooks.texture.texturereplacepatch");

        if (JustAnotherTranslator.EnableTexturesDump.Value)
            SceneManager.sceneUnloaded += OnSceneUnloaded;

        _initialized = true;
    }

    public static void Unload()
    {
        if (!_initialized)
            return;

        _textureReplacePatch?.UnpatchSelf();
        _textureReplacePatch = null;

        SceneManager.sceneUnloaded -= OnSceneUnloaded;

        _initialized = false;
    }

    /// <summary>
    ///     同步扫描替换纹理
    /// </summary>
    private static void ScanReplaceTextures()
    {
        if (!Directory.Exists(JustAnotherTranslator.TextureReplacePath))
        {
            LogManager.Warning(
                "Texture replace directory not found, try to create/未找到纹理替换目录，尝试创建");
            try
            {
                Directory.CreateDirectory(JustAnotherTranslator.TextureReplacePath);
            }
            catch (Exception e)
            {
                LogManager.Error(
                    $"Create Texture replace folder failed, plugin may not work/创建纹理替换文件夹失败，插件可能无法运行: {e.Message}");
                return;
            }
        }

        var files = Directory.GetFiles(JustAnotherTranslator.TextureReplacePath, "*.png",
            SearchOption.AllDirectories);
        LogManager.Info(
            $"Found {files.Length} texture files in translation texture directory/在翻译贴图目录中找到 {files.Length} 个贴图文件");

        // Cache all texture file path
        foreach (var path in files)
        {
            var fileName = Path.GetFileName(path);
            FilePathCache[fileName] = path;
        }
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
    public static bool GetReplaceTexture(string filename, out byte[] replacedTexture,
        Texture originalTexture = null)
    {
        replacedTexture = null;

        if (StringTool.IsNullOrWhiteSpace(filename))
            return false;

        if (filename.EndsWith(".tex")) filename = filename.Replace(".tex", ".png");

        if (Path.GetExtension(filename) == string.Empty)
            filename = string.Concat(filename, ".png");

        if (!IsReplaceTextureExist(filename))
        {
            // 如果替换纹理不存在，则 Dump
            if (JustAnotherTranslator.EnableTexturesDump.Value)
            {
                if (originalTexture != null)
                {
                    // 添加成功则为 true
                    if (DumpedTextures.Add(filename))
                    {
                        LogManager.Debug(
                            $"Texture replace for {filename} not found, try to dump original texture");
                        var bytes = GetTextureBytes(originalTexture);
                        if (bytes != null)
                            DumpTexture(filename, bytes);
                    }
                }
            }

            return false;
        }

        if (FilePathCache.TryGetValue(filename, out var cachePath))
            try
            {
                replacedTexture = File.ReadAllBytes(cachePath);

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
        try
        {
            if (Path.GetExtension(textureName) != ".png")
                textureName = string.Concat(Path.GetFileName(textureName), ".png");

            LogManager.Debug($"Writing texture: {textureName}");
            var filePath = Path.Combine(JustAnotherTranslator.TextureDumpPath, textureName);

            if (!File.Exists(filePath))
                File.WriteAllBytes(filePath, textureData);
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"Failed to write texture file: {textureName}/写入贴图文件失败: {textureName}, 错误: {e.Message}");
        }
    }


    /// <summary>
    ///     获取 .png 格式的纹理数据
    /// </summary>
    /// <param name="originalTex"></param>
    /// <returns></returns>
    private static byte[] GetTextureBytes(Texture originalTex)
    {
        if (originalTex is Texture2D tex2d)
            try
            {
                // 尝试直接获取
                return tex2d.EncodeToPNG();
            }
            catch
            {
                // 失败则走通用逻辑
            }

        // 如果失败（例如，纹理不可读），则创建副本
        try
        {
            // 创建临时渲染纹理
            var renderTex = RenderTexture.GetTemporary(
                originalTex.width, originalTex.height, 0,
                RenderTextureFormat.Default, RenderTextureReadWrite.Linear);

            // 复制原始纹理到渲染纹理
            Graphics.Blit(originalTex, renderTex);
            var previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            var readableText = new Texture2D(originalTex.width, originalTex.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);

            // 返回纹理数据
            var bytes = readableText.EncodeToPNG();
            Object.Destroy(readableText); // 销毁临时纹理
            return bytes;
        }
        catch (Exception e)
        {
            LogManager.Error($"Failed to get texture bytes for {originalTex.name}: {e.Message}");
            return null;
        }
    }


    /// <summary>
    ///     场景卸载时清理资源
    /// </summary>
    /// <param name="scene"></param>
    private static void OnSceneUnloaded(Scene scene)
    {
        try
        {
            DumpedTextures.Clear();
        }
        catch (Exception e)
        {
            LogManager.Error($"Error during cleanup scene resources/清理场景资源失败: {e.Message}");
        }
    }
}