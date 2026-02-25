using System;
using System.Collections.Generic;
using UnityEngine;

namespace COM3D2.JustAnotherTranslator.Plugin.Utils;

public static class FontTool
{
    /// 缓存自定义字体
    private static readonly Dictionary<string, Font> customFonts = new();

    /// <summary>
    ///     将给定字体替换为插件配置中指定的自定义 UI 字体。
    ///     如果未配置自定义字体或自定义字体与原始字体相同，则返回原始字体。
    /// </summary>
    /// <param name="originalFont">要替换的原始字体。</param>
    /// <returns>替换后的自定义字体；如果未配置有效的自定义字体，则返回原始字体。</returns>
    public static Font SwapUIFont(Font originalFont)
    {
        var customFont = JustAnotherTranslator.UIFont.Value.Trim();
        if (string.IsNullOrEmpty(customFont) || originalFont.name == customFont)
            return originalFont;
        return SwapFont(originalFont, customFont);
    }

    /// <summary>
    ///     替换字体为自定义字体。
    ///     如果未配置或未找到自定义字体，则返回原始字体。
    /// </summary>
    /// <param name="originalFont">要替换的原始字体</param>
    /// <returns>替换后的字体或原始字体（如果未找到自定义字体）</returns>
    public static Font SwapFont(Font originalFont, string fontName)
    {
        if (originalFont == null)
            return null;

        var font = GetFontByName(fontName, originalFont.fontSize);

        return font ?? originalFont;
    }

    /// <summary>
    ///     获取字体
    ///     如果字体不存在，使用默认字体
    /// </summary>
    /// <param name="name">字体名称</param>
    /// <param name="size">字体大小</param>
    /// <returns>字体</returns>
    public static Font GetFontByName(string name, int size)
    {
        try
        {
            var fontId = $"{name}#{size}";
            if (!customFonts.TryGetValue(fontId, out var font))
            {
                if (name == "Arial" || name == "Arial.ttf")
                    return Resources.GetBuiltinResource<Font>("Arial.ttf");

                font = Font.CreateDynamicFontFromOSFont(name, size);
                if (font is null)
                {
                    LogManager.Warning(
                        $"Failed to load font: {name}, using default font/无法加载字体：{name}。使用默认字体");
                    return Resources.GetBuiltinResource<Font>("Arial.ttf");
                }

                customFonts[fontId] = font;
            }

            return font;
        }
        catch (Exception e)
        {
            LogManager.Warning(
                $"Failed to load font: {name}, using default font{e.Message}/无法加载字体：{name}。使用默认字体");
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }


    /// <summary>
    ///     打印操作系统上所有已安装字体的名称。
    ///     使用 Unity 的 Font.GetOSInstalledFontNames 方法检索字体列表
    ///     并使用 LogManager 记录每个字体的名称。
    /// </summary>
    public static void PrintOSInstalledFontNames()
    {
        var fonts = Font.GetOSInstalledFontNames();
        LogManager.Info("OS Installed Font Name:/系统内已安装的字体名称:");
        foreach (var font in fonts)
            LogManager.Info(font);
    }
}