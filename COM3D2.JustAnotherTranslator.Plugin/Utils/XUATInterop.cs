using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MonoMod.Utils;

namespace COM3D2.JustAnotherTranslator.Plugin.Utils;

/// <summary>
///     XUAT（XUnity.AutoTranslator）互操作类，用于与 XUAT 翻译器集成
/// </summary>
public static class XUATInterop
{
    /// <summary>
    ///     标记文本为已翻译的委托
    /// </summary>
    private static MarkTranslatedDelegate _markTranslated;

    /// <summary>
    ///     是否已初始化
    /// </summary>
    private static bool _initialized;

    /// <summary>
    /// XUAT 用来标记已翻译的特殊字符，可以提前检查
    /// 初始化时会从插件实际获取，安全起见设置默认值
    /// </summary>
    public static string XuatSpicalMaker = "\u180e";

    /// <summary>
    ///     初始化XUAT互操作功能
    /// </summary>
    /// <returns>如果成功初始化返回true，否则返回false</returns>
    public static bool Initialize()
    {
        // 如果已经初始化，则检查委托是否有效
        if (_initialized)
            return _markTranslated != null;

        // 标记为已初始化
        _initialized = true;

        // 查找XUAT程序集
        var xuatAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "XUnity.AutoTranslator.Plugin.Core");
        if (xuatAssembly == null)
        {
            LogManager.Info(
                "No XUnity.AutoTranslator Plugin detected, skipping interop/未检测到 XUnity.AutoTranslator 插件，跳过互操作");
            return false;
        }

        // 获取LanguageHelper类型
        var langHelper = xuatAssembly.GetType("XUnity.AutoTranslator.Plugin.Core.Utilities.LanguageHelper");
        if (langHelper == null)
        {
            LogManager.Warning("Could not find LanguageHelper; skipping XUAT interop/无法找到 LanguageHelper，跳过 XUAT 互操作");
            return false;
        }

        // 通过反射获取 LanguageHelper.MogolianVowelSeparatorString 字段值
        var mogolianVowelSeparatorStringField = langHelper.GetField("MogolianVowelSeparatorString", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        if (mogolianVowelSeparatorStringField != null)
        {
            XuatSpicalMaker = (string)mogolianVowelSeparatorStringField.GetValue(null);
        }

        // 获取MakeRedirected方法
        var makeRedirectedMethod = AccessTools.Method(langHelper, "MakeRedirected");
        if (makeRedirectedMethod == null)
        {
            LogManager.Warning(
                "Could not find LanguageHelper.MakeRedirected; skipping XUAT interop/无法找到 LanguageHelper.MakeRedirected，跳过 XUAT 互操作");
            return false;
        }

        // 创建委托
        _markTranslated = makeRedirectedMethod.CreateDelegate<MarkTranslatedDelegate>();
        LogManager.Info(
            "Found XUnity.AutoTranslator Plugin; enabled interop(translated text will not be translated by XUAT again)/检测到 XUnity.AutoTranslator 插件，已启用互操作（已翻译的文本将不会被 XUAT 再次翻译）");

        return true;
    }

    /// <summary>
    ///     标记文本为已翻译，防止XUAT重复翻译
    ///     实际上只是检查是否有 \u180e，但是我们选择更安全的方式
    /// </summary>
    /// <param name="text">要标记的文本</param>
    /// <returns>标记后的文本</returns>
    public static string MarkTranslated(string text)
    {
        // 如果初始化失败则返回原文本，否则调用XUAT的标记方法
        // 需要使用标记后的文本，否则XUAT会重复翻译
        return !Initialize() ? text : _markTranslated(text);
    }

    /// <summary>
    ///     标记文本为已翻译的委托类型
    /// </summary>
    /// <param name="text">要标记的文本</param>
    /// <returns>标记后的文本</returns>
    private delegate string MarkTranslatedDelegate(string text);
}