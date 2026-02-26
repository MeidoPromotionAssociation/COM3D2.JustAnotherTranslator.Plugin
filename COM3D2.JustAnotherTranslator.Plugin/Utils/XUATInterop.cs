using System;
using System.Linq;
using System.Reflection;
using COM3D2.JustAnotherTranslator.Plugin.Manger;
using HarmonyLib;

namespace COM3D2.JustAnotherTranslator.Plugin.Utils;

/// <summary>
///     XUAT（XUnity.AutoTranslator）互操作类，用于与 XUAT 翻译器集成
/// </summary>
public static class XUATInterop
{
    private static bool _initialized;
    private static Harmony _xuatInteropPatch;

    /// <summary>
    ///     XUAT 用来标记已翻译的特殊字符，可以提前检查
    ///     初始化时会从插件实际获取，安全起见设置默认值
    /// </summary>
    public static string XuatSpicalMaker = "\u180e";

    /// <summary>
    ///     初始化 XUAT 互操作功能
    /// </summary>
    /// <returns>如果成功初始化返回true，否则返回false</returns>
    public static void Init()
    {
        if (_initialized)
            return;

        try
        {
            // 查找 XUAT 程序集
            var xuatAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "XUnity.AutoTranslator.Plugin.Core");
            if (xuatAssembly == null)
            {
                LogManager.Info(
                    "No XUnity.AutoTranslator(XUAT) Plugin detected, skipping interop/未检测到 XUnity.AutoTranslator (XUAT)插件，跳过互操作");
                return;
            }

            // 获取 LanguageHelper 类型
            var langHelper =
                xuatAssembly.GetType("XUnity.AutoTranslator.Plugin.Core.Utilities.LanguageHelper");
            if (langHelper == null)
            {
                LogManager.Warning(
                    "Could not find XUnity.AutoTranslator LanguageHelper; skipping XUAT interop/无法找到 XUnity.AutoTranslator LanguageHelper，跳过 XUAT 互操作");
                return;
            }


            _xuatInteropPatch =
                new Harmony(
                    "github.meidopromotionassociation.com3d2.justanothertranslator.plugin.utils.xuatinterop");

            // 对 LanguageHelper.IsTranslatable 方法应用补丁
            var isTranslatableMethod = langHelper.GetMethod("IsTranslatable",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (isTranslatableMethod != null)
            {
                _xuatInteropPatch.Patch(isTranslatableMethod,
                    new HarmonyMethod(typeof(XUATInterop).GetMethod(
                        nameof(XUAT_IsTranslatable_Prefix),
                        BindingFlags.Static | BindingFlags.NonPublic)));
                LogManager.Debug("XUAT LanguageHelper.IsTranslatable patched successfully");
            }

            var translationCacheMethodNames = new[]
            {
                "XUnity.AutoTranslator.Plugin.Core.TextTranslationCache",
                "XUnity.AutoTranslator.Plugin.Core.CompositeTextTranslationCache"
            };

            // 对 TranslationCache 类型中的 “IsTranslatable”方法应用补丁
            foreach (var typeName in translationCacheMethodNames)
            {
                var type = xuatAssembly.GetType(typeName);
                if (type == null) continue;

                var method = type.GetMethod("IsTranslatable",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method == null) continue;

                _xuatInteropPatch.Patch(method,
                    new HarmonyMethod(typeof(XUATInterop).GetMethod(
                        nameof(XUAT_IsTranslatable_Prefix),
                        BindingFlags.Static | BindingFlags.NonPublic)));
                LogManager.Debug($"XUAT {type.Name}.IsTranslatable patched successfully");
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
        }
        catch (Exception e)
        {
            LogManager.Error(
                $"XUAT interop init failed, set default value/XUAT 互操作初始化失败，设置为默认值: {e.Message}");
            XuatSpicalMaker = "\u180e";
            _initialized = false;
            return;
        }

        _initialized = true;
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
    ///     3 种 XUAT IsTranslatable 的补丁前缀，用于让 JAT 优先处理翻译
    ///     若 __result = false，则 XUAT 不会尝试翻译此文本
    /// </summary>
    /// <param name="text"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    private static bool XUAT_IsTranslatable_Prefix(string text, ref bool __result)
    {
        try
        {
            // 检查是否包含 XUAT 标记
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
            if (TextTranslateManger.IsJatTranslatedText(text))
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
    ///     卸载插件时清理资源
    /// </summary>
    public static void Unload()
    {
        try
        {
            XuatSpicalMaker = "\u180e";
            _initialized = false;
            _xuatInteropPatch?.UnpatchSelf();
            _xuatInteropPatch = null;
        }
        catch (Exception e)
        {
            LogManager.Error($"Error during cleanup resources/清理资源失败: {e.Message}");
            _initialized = false;
        }
    }
}