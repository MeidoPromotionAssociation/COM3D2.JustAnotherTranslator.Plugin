using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using COM3D2.JustAnotherTranslator.Plugin.Translator;
using HarmonyLib;
using UnityEngine.SceneManagement;

namespace COM3D2.JustAnotherTranslator.Plugin.Utils;

/// <summary>
///     XUAT（XUnity.AutoTranslator）互操作类，用于与 XUAT 翻译器集成
/// </summary>
public static class XUATInterop
{
    /// <summary>
    ///     是否已初始化
    /// </summary>
    private static bool _initialized;

    /// <summary>
    ///     XUAT 用来标记已翻译的特殊字符，可以提前检查
    ///     初始化时会从插件实际获取，安全起见设置默认值
    /// </summary>
    public static string XuatSpicalMaker = "\u180e";

    /// <summary>
    ///     LanguageHelper.HasRedirectedTexts 字段引用
    /// </summary>
    private static FieldInfo _hasRedirectedTextsField;

    /// <summary>
    ///     存储已翻译文本的集合，用于防止重复翻译
    /// </summary>
    private static readonly HashSet<string> TranslatedTexts = new();

    /// <summary>
    ///     初始化 XUAT 互操作功能
    /// </summary>
    /// <returns>如果成功初始化返回true，否则返回false</returns>
    public static bool Init()
    {
        if (_initialized)
            return true;

        SceneManager.sceneUnloaded += OnSceneUnloaded;

        try
        {
            // 查找 XUAT 程序集
            var xuatAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "XUnity.AutoTranslator.Plugin.Core");
            if (xuatAssembly == null)
            {
                LogManager.Info(
                    "No XUnity.AutoTranslator(XUAT) Plugin detected, skipping interop/未检测到 XUnity.AutoTranslator (XUAT)插件，跳过互操作");
                return false;
            }

            // 获取 LanguageHelper 类型
            var langHelper =
                xuatAssembly.GetType("XUnity.AutoTranslator.Plugin.Core.Utilities.LanguageHelper");
            if (langHelper == null)
            {
                LogManager.Warning(
                    "Could not find XUnity.AutoTranslator LanguageHelper; skipping XUAT interop/无法找到 XUnity.AutoTranslator LanguageHelper，跳过 XUAT 互操作");
                return false;
            }

            // 获取 HasRedirectedTexts 字段
            _hasRedirectedTextsField = langHelper.GetField("HasRedirectedTexts",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

            // 获取 IsTranslatable 方法进行补丁
            var isTranslatableMethod = langHelper.GetMethod("IsTranslatable",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (isTranslatableMethod != null)
            {
                var harmony = new Harmony("COM3D2.JustAnotherTranslator.Plugin.XUATInterop");
                harmony.Patch(isTranslatableMethod,
                    new HarmonyMethod(typeof(XUATInterop).GetMethod(
                        nameof(XUAT_IsTranslatable_Prefix),
                        BindingFlags.Static | BindingFlags.NonPublic)));
                LogManager.Debug("XUAT LanguageHelper.IsTranslatable patched successfully");
            }

            // 通过反射获取 LanguageHelper.MogolianVowelSeparatorString 字段值
            var mogolianVowelSeparatorStringField = langHelper.GetField(
                "MogolianVowelSeparatorString",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (mogolianVowelSeparatorStringField != null)
            {
                var val = (string)mogolianVowelSeparatorStringField.GetValue(null);
                if (!string.IsNullOrEmpty(val)) XuatSpicalMaker = val;
                LogManager.Debug($"XuatSpicalMaker is {val}");
            }

            LogManager.Info(
                "Found XUnity.AutoTranslator Plugin; enabled interop(translated text will not be translated by XUAT again)/检测到 XUnity.AutoTranslator 插件，已启用互操作（已翻译的文本将不会被 XUAT 再次翻译）");

            setXUATHasRedirectedTextsField();
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"XUAT interop init failed, set default value/XUAT 互操作初始化失败，设置为默认值: {e.Message}");
            XuatSpicalMaker = "\u180e";
            _initialized = false;
            return false;
        }

        _initialized = true;
        return true;
    }

    /// <summary>
    ///     标记文本为已翻译，防止 XUAT 重复翻译
    /// </summary>
    /// <param name="text">要标记的文本</param>
    /// <param name="skipMark">是否跳过标记</param>
    /// <returns>标记后的文本</returns>
    public static string MarkTranslated(string text, bool skipMark = false)
    {
        if (StringTool.IsNullOrWhiteSpace(text)) return text;

        // 去除各种空白后添加到已翻译记录
        var cleanText = StringTool.NormalizeText(text).Replace(XuatSpicalMaker, "");
        TranslatedTexts.Add(cleanText);

        if (skipMark) return text;

        // 强制设置 XUAT 的检测标志，否则 XUAT 可能会跳过 IsRedirected 检查
        setXUATHasRedirectedTextsField();

        // 添加特殊标记
        if (!text.Contains(XuatSpicalMaker)) return string.Concat(text, XuatSpicalMaker);

        return text;
    }

    /// <summary>
    ///     判断指定文本是否已被 JAT 翻译
    /// </summary>
    /// <param name="text">需要检查的文本内容</param>
    /// <returns>如果文本存在于已翻译记录中返回true，否则返回false</returns>
    public static bool IsJATTranslatedText(string text)
    {
        if (StringTool.IsNullOrWhiteSpace(text))
            return false;

        return TranslatedTexts.Contains(
            StringTool.NormalizeText(text).Replace(XuatSpicalMaker, ""));
    }

    /// <summary>
    ///     强制设置 XUAT 的检测标志
    /// </summary>
    private static void setXUATHasRedirectedTextsField()
    {
        try
        {
            if (_initialized && _hasRedirectedTextsField != null)
                _hasRedirectedTextsField.SetValue(null, true);
        }
        catch
        {
            /* ignore */
        }
    }

    /// <summary>
    ///     检查文本是否包含 XUAT 特殊标记
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static bool IsContainsXuatSpicalMaker(string text)
    {
        return text.Contains(XuatSpicalMaker);
    }

    /// <summary>
    ///     XUnity.AutoTranslator.Plugin.Core.Utilities.LanguageHelper  IsTranslatable 补丁前缀，用于让 JAT 优先处理翻译
    ///     若 __result = false，则 XUAT 不会尝试翻译此文本
    /// </summary>
    /// <param name="text"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    private static bool XUAT_IsTranslatable_Prefix(string text, ref bool __result)
    {
        try
        {
            // 检查是否包含 XUAT/JAT 标记
            if (IsContainsXuatSpicalMaker(text))
            {
                LogManager.Debug($"Block XUAT translate because contain spical mark: {text}");
                __result = false;
                return false;
            }

            // 检查是否在 JAT 翻译字典中（作为原文），阻止以免 XUAT 先于 JAT 翻译
            if (TextTranslateManger.IsInTranslateDict(text))
            {
                LogManager.Debug(
                    $"Block XUAT translate because it could be translate by JAT: {text}");
                __result = false;
                return false;
            }

            // 检查是否是 JAT 的翻译结果（防止 XUAT 翻译 JAT 的译文）
            if (IsJATTranslatedText(text))
            {
                LogManager.Debug(
                    $"Block XUAT translate because it already translate by JAT: {text}");
                __result = false;
                return false;
            }

            return true;
        }
        catch (Exception e)
        {
            LogManager.Error($"Error during XUAT block translation/阻止 XUAT 翻译失败: {e.Message}");
            return false;
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
            TranslatedTexts.Clear(); // 场景卸载时清理已标记的文本，以免过大
        }
        catch (Exception e)
        {
            LogManager.Error($"Error during cleanup scene resources/清理场景资源失败: {e.Message}");
        }
    }


    /// <summary>
    ///     卸载插件时清理资源
    /// </summary>
    public static void Unload()
    {
        try
        {
            TranslatedTexts.Clear();
            XuatSpicalMaker = "\u180e";
            _hasRedirectedTextsField = null;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            _initialized = false;
        }
        catch (Exception e)
        {
            LogManager.Error($"Error during cleanup resources/清理资源失败: {e.Message}");
            _initialized = false;
        }
    }
}