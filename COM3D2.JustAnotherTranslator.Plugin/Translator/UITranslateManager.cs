using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BepInEx.Logging;
using COM3D2.JustAnotherTranslator.Plugin.Hooks.UI;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    /// 缓存原始精灵图信息
    private static readonly Dictionary<UISprite, OriginalSpriteInfoStruct>
        OriginalSpriteInfo = new(); // UISprite -> OriginalSpriteInfo

    /// 缓存已创建的替换 Atlas，避免重复创建
    private static readonly Dictionary<string, UIAtlas> ReplacementAtlasCache = new(); // spriteName -> UIAtlas

    /// 异步 UI 文本加载器
    private static AsyncUiTextLoader _uiTextLoader;

    public static void Init()
    {
        if (_initialized) return;

        _spriteCache = new LRUCache<string, Texture2D>(JustAnotherTranslator.UICacheSize.Value);

        SceneManager.sceneUnloaded += OnSceneUnloaded;

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

        SceneManager.sceneUnloaded -= OnSceneUnloaded;

        CleanupSceneResources();

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

    #region Text

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
            texture.LoadImage(new byte[0]); // 强制刷新内部纹理
            texture.LoadImage(fileData); // LoadImage会自动调整纹理大小
            LogManager.Debug($"Loaded texture {spriteName} from {path}");
            _spriteCache.Set(spriteName, texture);
            return texture;
        }
        catch (Exception e)
        {
            LogManager.Error($"Failed to load texture: {spriteName}/加载材质失败: {spriteName}  {e.Message}");
        }

        return null;
    }

    /// <summary>
    ///     替换整个 Atlas，创建包含单个精灵图的新 Atlas
    /// </summary>
    public static void ProcessSpriteReplacementWithNewAtlas(UISprite sprite, string spriteName)
    {
        if (sprite == null) return;

        // 检查是否为空字符串调用
        if (string.IsNullOrEmpty(spriteName))
        {
            LogManager.Debug("Sprite replacement restore for empty sprite name");
            RestoreOriginalSprite(sprite);
            return;
        }

        if (IsSpriteReplaceAvailable(spriteName))
        {
            var replacementTexture = GetSpriteTexture(spriteName);
            if (replacementTexture != null)
            {
                // 保存原始信息（仅在第一次替换时保存）
                if (!OriginalSpriteInfo.ContainsKey(sprite))
                    OriginalSpriteInfo[sprite] = new OriginalSpriteInfoStruct
                    {
                        OriginalAtlas = sprite.atlas,
                        OriginalSpriteName = sprite.spriteName,
                        OriginalSpriteData = sprite.GetAtlasSprite(),
                        OriginalMaterial = sprite.material
                    };

                // 尝试从缓存获取 Atlas，如果没有则创建新的
                var newAtlas = GetOrCreateReplacementAtlas(replacementTexture, sprite.material, spriteName);

                if (newAtlas != null)
                {
                    sprite.atlas = newAtlas;
                    sprite.spriteName = spriteName;

                    // 强制刷新精灵图
                    sprite.MarkAsChanged();

                    LogManager.Debug($"Applied new atlas for {spriteName} on {sprite.name}");
                }
                else
                {
                    LogManager.Warning($"Failed to create replacement atlas for {spriteName}");
                }
            }
        }
        else
        {
            // 恢复原始精灵图，因为按钮可能会被复用，如果不还原可能导致显示错误
            LogManager.Debug($"Sprite replacement restore for {spriteName}");
            RestoreOriginalSprite(sprite);
        }
    }

    /// <summary>
    ///     获取或创建替换 Atlas（带缓存）
    /// </summary>
    private static UIAtlas GetOrCreateReplacementAtlas(Texture2D texture, Material originalMaterial, string spriteName)
    {
        // 使用纹理实例ID和精灵名称作为缓存键
        var cacheKey = $"{texture.GetInstanceID()}_{spriteName}";

        if (ReplacementAtlasCache.TryGetValue(cacheKey, out var cachedAtlas))
        {
            // 检查缓存的 Atlas 是否仍然有效
            if (cachedAtlas != null && cachedAtlas.spriteMaterial != null) return cachedAtlas;

            // 清理无效缓存时也要清理对应的GameObject
            if (cachedAtlas != null && cachedAtlas.gameObject != null) Object.DestroyImmediate(cachedAtlas.gameObject);
            ReplacementAtlasCache.Remove(cacheKey);
        }

        // 创建新的 Atlas
        var newAtlas = CreateSingleSpriteAtlas(texture, originalMaterial, spriteName);
        if (newAtlas != null) ReplacementAtlasCache[cacheKey] = newAtlas;

        return newAtlas;
    }

    /// <summary>
    ///     创建包含单个精灵图的 Atlas
    /// </summary>
    private static UIAtlas CreateSingleSpriteAtlas(Texture2D texture, Material originalMaterial, string spriteName)
    {
        try
        {
            if (texture == null)
            {
                LogManager.Warning($"Texture is null for sprite {spriteName}");
                return null;
            }

            if (originalMaterial == null)
            {
                LogManager.Warning($"Original material is null for sprite {spriteName}");
                return null;
            }

            if (texture.width <= 0 || texture.height <= 0)
            {
                LogManager.Warning(
                    $"Invalid texture dimensions for sprite {spriteName}: {texture.width}x{texture.height}");
                return null;
            }

            // 创建 Atlas GameObject
            var atlasGo = new GameObject($"JAT_ReplacementAtlas_{spriteName}");
            var atlas = atlasGo.AddComponent<UIAtlas>();

            if (originalMaterial == null)
            {
                LogManager.Warning($"Original material is null for sprite {spriteName}");
                return null;
            }

            // 复制材质，并使用新的纹理
            var newMaterial = new Material(originalMaterial.shader);
            newMaterial.mainTexture = texture;

            // 复制原始材质的其他属性
            newMaterial.color = originalMaterial.color;
            newMaterial.renderQueue = originalMaterial.renderQueue;
            newMaterial.globalIlluminationFlags = originalMaterial.globalIlluminationFlags;
            newMaterial.enableInstancing = originalMaterial.enableInstancing;
            newMaterial.doubleSidedGI = originalMaterial.doubleSidedGI;
            newMaterial.hideFlags = originalMaterial.hideFlags;

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

            // 设置 Atlas 不被销毁
            // UnityEngine.Object.DontDestroyOnLoad(atlasGo);

            return atlas;
        }
        catch (Exception e)
        {
            LogManager.Error($"Failed to create single sprite atlas for {spriteName}: {e.Message}");
            return null;
        }
    }

    /// <summary>
    ///     恢复原始精灵图
    /// </summary>
    private static void RestoreOriginalSprite(UISprite sprite)
    {
        if (sprite == null) return;

        if (OriginalSpriteInfo.TryGetValue(sprite, out var info))
            try
            {
                // 恢复原始属性
                sprite.atlas = info.OriginalAtlas;
                sprite.spriteName = info.OriginalSpriteName;

                // 安全地恢复精灵数据
                if (info.OriginalSpriteData != null) sprite.SetAtlasSprite(info.OriginalSpriteData);

                // 恢复材质
                if (info.OriginalMaterial != null) sprite.material = info.OriginalMaterial;

                // 强制刷新精灵图
                sprite.MarkAsChanged();

                // 移除缓存的原始信息
                OriginalSpriteInfo.Remove(sprite);

                LogManager.Debug($"Restored original sprite for {sprite.name}");
            }
            catch (Exception e)
            {
                LogManager.Error($"Failed to restore original sprite for {sprite.name}: {e.Message}");
            }
    }

    /// <summary>
    ///     场景卸载时的清理
    /// </summary>
    /// <param name="scene"></param>
    private static void OnSceneUnloaded(Scene scene)
    {
        CleanupSceneResources();
    }

    /// <summary>
    ///     清理场景资源
    /// </summary>
    private static void CleanupSceneResources()
    {
        try
        {
            // 清理所有替换的Atlas和相关资源
            foreach (var atlas in ReplacementAtlasCache.Values)
                if (atlas != null)
                {
                    if (atlas.spriteMaterial != null)
                        Object.DestroyImmediate(atlas.spriteMaterial);
                    if (atlas.gameObject != null)
                        Object.DestroyImmediate(atlas.gameObject);
                }

            // 清理缓存
            ReplacementAtlasCache.Clear();
            OriginalSpriteInfo.Clear();

            LogManager.Debug("Scene resources cleaned up");
        }
        catch (Exception e)
        {
            LogManager.Error($"Error during scene cleanup: {e.Message}");
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