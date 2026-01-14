using System;
using System.Linq;
using System.Reflection;
using COM3D2.JustAnotherTranslator.Plugin.Translator;
using HarmonyLib;
using MonoMod.Utils;

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
    ///     初始化 XUAT 互操作功能
    /// </summary>
    /// <returns>如果成功初始化返回true，否则返回false</returns>
    public static bool Initialize()
    {
        // 如果已经初始化，则检查字段是否有效
        if (_initialized)
            return _hasRedirectedTextsField != null;

        _initialized = true;

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
                    new HarmonyMethod(typeof(XUATInterop).GetMethod(nameof(XUAT_IsTranslatable_Prefix),
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
            LogManager.Error($"XUAT interop failed: {e.Message}");
            return false;
        }

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
        if (string.IsNullOrEmpty(text) || skipMark) return text;

        // 尝试初始化
        bool initialized = Initialize();

        // 强制设置 XUAT 的检测标志，否则 XUAT 会跳过 IsRedirected 检查
        if (initialized && _hasRedirectedTextsField != null)
        {
            setXUATHasRedirectedTextsField();
        }

        // 添加特殊标记吗，直接使用获取到的 XuatSpicalMaker
        //
        if (!text.Contains(XuatSpicalMaker))
        {
            return string.Concat(text, XuatSpicalMaker);
        }

        return text;
    }

    /// <summary>
    ///     强制设置 XUAT 的检测标志
    /// </summary>
    private static void setXUATHasRedirectedTextsField()
    {
        try
        {
            _hasRedirectedTextsField.SetValue(null, true);
        }
        catch
        {
            /* ignore */
        }
    }

    /// <summary>
    ///      XUnity.AutoTranslator.Plugin.Core.Utilities.LanguageHelper  IsTranslatable 补丁前缀，用于让 JAT 优先处理翻译
    ///      若 __result = false，则 XUAT 不会尝试翻译此文本
    /// </summary>
    /// <param name="text"></param>
    /// <param name="__result"></param>
    /// <returns></returns>
    private static bool XUAT_IsTranslatable_Prefix(string text, ref bool __result)
    {
        if (TextTranslateManger.IsTranslatable(text,true))
        {
            __result = false;
            return false;
        }
        //如果 JAT 不需要处理，返回 true，让 XUAT 继续执行它自带的判定逻辑
        return true;
    }
}