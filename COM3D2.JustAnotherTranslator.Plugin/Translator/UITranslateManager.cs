using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BepInEx.Logging;
using COM3D2.JustAnotherTranslator.Plugin.Hooks.UI;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace COM3D2.JustAnotherTranslator.Plugin.Translator;

/// <summary>
///     UI 翻译管理器
/// </summary>
public static class UITranslateManager
{
    private static Harmony _uiTranslatePatch;
    private static Harmony _uiDebugPatch;
    private static bool _initialized;

    /// 存储翻译数据的字典
    private static readonly Dictionary<string, TranslationData> _translations = new(); // term -> TranslationData

    /// 缓存文件名到完整路径的映射
    private static readonly Dictionary<string, string> SpritePathCache = new(); // filename -> path

    /// LRU缓存已加载的图片纹理
    private static LRUCache<string, Texture2D> _spriteCache; // filename -> texture

    /// 异步 UI 文本加载器
    private static AsyncUiTextLoader _uiTextLoader;

    public static void Init()
    {
        if (_initialized) return;

        _spriteCache = new LRUCache<string, Texture2D>(JustAnotherTranslator.UICacheSize.Value);

        LoadTextTranslationsAsync();
        LoadSpriteTextures();

        _uiTranslatePatch = Harmony.CreateAndPatchAll(typeof(UITranslatePatch),
            "github.meidopromotionassociation.com3d2.justanothertranslator.plugin.hooks.ui.uitranslatepatch");

        if (JustAnotherTranslator.LogLevelConfig.Value >= LogLevel.Debug)
        {
            _uiDebugPatch =
                new Harmony(
                    "github.meidopromotionassociation.com3d2.justanothertranslator.plugin.hooks.ui.uidebugpatch");
            UIDebugPatch.LocalizeTargetPatcher.ApplyPatch(_uiDebugPatch);
        }

        _initialized = true;
    }

    public static void Unload()
    {
        if (!_initialized) return;

        _uiTextLoader?.Cancel();
        _uiTextLoader = null;

        _uiTranslatePatch?.UnpatchSelf();
        _uiTranslatePatch = null;

        // 清理纹理缓存
        foreach (var texture in _spriteCache.GetAllValues())
            if (texture != null)
                Object.DestroyImmediate(texture);

        _translations.Clear();
        _spriteCache.Clear();
        SpritePathCache.Clear();

        _initialized = false;
    }

    #region Text

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

        return "";
    }

    /// <summary>
    ///     异步加载翻译文件
    /// </summary>
    private static void LoadTextTranslationsAsync()
    {
        _uiTextLoader =
            new AsyncUiTextLoader(JustAnotherTranslator.UITextPath, OnUiTextLoadProgress, OnUiTextLoadComplete);
        _uiTextLoader.StartLoading();
    }

    /// <summary>
    ///     异步加载进度回调
    /// </summary>
    /// <param name="progress"></param>
    /// <param name="filesProcessed"></param>
    /// <param name="totalFiles"></param>
    private static void OnUiTextLoadProgress(float progress, int filesProcessed, int totalFiles)
    {
        // 进度变化超过 10% 时输出日志
        if ((int)(progress * 100) % 10 == 0)
            LogManager.Info(
                $"Translation loading progress: {progress:P0} ({filesProcessed}/{totalFiles})/翻译加载进度: {progress:P0} ({filesProcessed}/{totalFiles})");
    }


    /// <summary>
    ///     异步加载加载完成回调
    /// </summary>
    /// <param name="result"></param>
    /// <param name="totalEntries"></param>
    /// <param name="totalFiles"></param>
    /// <param name="elapsedMilliseconds"></param>
    private static void OnUiTextLoadComplete(
        Dictionary<string, TranslationData> result,
        int totalEntries,
        int totalFiles,
        long elapsedMilliseconds
    )
    {
        foreach (var pair in result) _translations[pair.Key] = pair.Value;

        if (totalEntries > 0)
            LogManager.Info(
                $"UI translation loading completed! Total entries: {totalEntries}, from {totalFiles} files, took {elapsedMilliseconds} ms/UI翻译文件加载完成！总计加载 {totalEntries} 条翻译条目，来自 {totalFiles} 个文件, 耗时 {elapsedMilliseconds} ms");
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

    #endregion

    #region Sprite

    /// <summary>
    ///     替换整个 Atlas，创建包含单个精灵图的新 Atlas
    ///     请确保已检查 atlas 名称是否以 JAT_ 开头
    /// </summary>
    public static void ProcessSpriteReplacementWithNewAtlas(UIButton uiButton, string spriteName)
    {
        try
        {
            if (IsSpriteReplaceAvailable(spriteName))
            {
                var replacementTexture = GetSpriteTexture(spriteName);

                // 获取目标 UISprite 组件
                var sprite = uiButton.tweenTarget?.GetComponent<UISprite>();
                if (sprite == null)
                {
                    sprite = uiButton.GetComponent<UISprite>(); // 备用方案
                    LogManager.Debug($"use backup method to find UISprite component on UIButton '{uiButton.name}'.");
                }

                if (sprite == null)
                {
                    LogManager.Error(
                        $"Unable to find UISprite component on UIButton/无法找到 UISprite 组件： '{uiButton.name}'");
                    return;
                }

                ReplaceSprite(sprite, replacementTexture, spriteName);

                LogManager.Debug($"Successfully replaced UIButton '{uiButton.name}' sprite with '{spriteName}'.");
            }
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"ProcessSpriteReplacementWithNewAtlas unknown error, please report this issue/未知错误，请报告此错误 {e.Message}\n{e.StackTrace}");
        }
    }


    /// <summary>
    ///     通过创建新的 UIAtlas 来替换 UISprite
    /// </summary>
    /// <param name="sprite">The UISprite to modify.</param>
    /// <param name="newTexture">The new Texture2D to apply.</param>
    /// <param name="spriteName">A unique name for the new sprite. If null, the texture's name will be used.</param>
    public static void ReplaceSprite(UISprite sprite, Texture2D newTexture, string spriteName)
    {
        if (sprite == null || newTexture == null)
        {
            LogManager.Error(
                $"Sprite or new Texture is null, please report this issue/精灵图或新纹理为空，请报告此问题  spriteName: {spriteName}");
            return;
        }

        var originalAtlas = sprite.atlas;
        if (originalAtlas == null)
        {
            LogManager.Error(
                $"The original sprite does not have an atlas, please report this issue/原始精灵图没有 Atlas，请报告此问题 sprite.name: {sprite.name}");
            return;
        }

        // 创建一个新的 GameObject 来承载 UIAtlas
        var atlasGO = new GameObject($"JAT_ReplacementAtlas_{spriteName}");
        atlasGO.transform.SetParent(sprite.transform, false); // 设置为子对象，以便跟随销毁

        var newAtlas = atlasGO.AddComponent<UIAtlas>();

        // 复制旧材质可以确保 Shader 和其他属性（如 premultiplied alpha）保持一致
        var newMaterial = new Material(originalAtlas.spriteMaterial);
        newMaterial.mainTexture = newTexture;
        newAtlas.spriteMaterial = newMaterial;
        newAtlas.pixelSize = originalAtlas.pixelSize;

        // 创建新的 UISpriteData
        if (string.IsNullOrEmpty(spriteName)) spriteName = newTexture.name;

        var spriteData = new UISpriteData
        {
            name = spriteName,
            x = 0,
            y = 0,
            width = newTexture.width,
            height = newTexture.height,
            borderLeft = 0,
            borderRight = 0,
            borderTop = 0,
            borderBottom = 0,
            paddingLeft = 0,
            paddingRight = 0,
            paddingTop = 0,
            paddingBottom = 0
        };

        // 将 SpriteData 添加到新图集
        // UIAtlas.spriteList 返回的是一个副本，所以我们需要获取、修改再赋值回去
        var spriteList = newAtlas.spriteList;
        spriteList.Add(spriteData);
        newAtlas.spriteList = spriteList;

        // 将新图集和精灵名赋给目标 UISprite
        sprite.atlas = newAtlas;
        sprite.spriteName = spriteName;
        // 确保 atlas 具有 JAT_ 前缀
        // Component.name 实际上是 GameObject.name 的快捷方式，但我们只是明确意图+确保
        sprite.atlas.name = atlasGO.name;


        sprite.MarkAsChanged();
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
                LogManager.Info(
                    $"Translation UISpritePath directory not found, try to create/未找到UI精灵图目录，尝试创建: {JustAnotherTranslator.UISpritePath}");
                try
                {
                    Directory.CreateDirectory(JustAnotherTranslator.UISpritePath);
                }
                catch (Exception e)
                {
                    LogManager.Error(
                        $"Create translation UISprite folder failed, plugin may not work/创建翻译UI精灵图目录失败，插件可能无法运行: {e.Message}");
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
            LogManager.Error($"Error while scanning for UI Sprite/扫描UI精灵图时发生错误: {e.Message}");
        }
        finally
        {
            sw.Stop();
            if (SpritePathCache.Count > 0)
                LogManager.Info(
                    $"UI Sprite scanning completed! Found {SpritePathCache.Count} items, took {sw.ElapsedMilliseconds} ms/UI精灵图扫描完成！共找到 {SpritePathCache.Count} 个项目, 耗时 {sw.ElapsedMilliseconds} ms");
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
            var texture = new Texture2D(1, 1);
            texture.LoadImage(fileData); // LoadImage会自动调整纹理大小
            LogManager.Debug($"Loaded texture {spriteName} from {path}");
            _spriteCache.Set(spriteName, texture);
            return texture;
        }
        catch (Exception e)
        {
            LogManager.Error($"Failed to load texture/加载材质失败: {spriteName}  {e.Message}");
        }

        return null;
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