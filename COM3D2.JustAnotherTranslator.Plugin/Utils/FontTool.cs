using System;
using System.Collections.Generic;
using UnityEngine;

namespace COM3D2.JustAnotherTranslator.Plugin.Utils;

public static class FontTool
{
    /// 缓存自定义字体
    private static readonly Dictionary<string, Font> CustomFonts = new();

    /// <summary>
    ///     将给定字体替换为插件配置中指定的自定义 UI 字体。
    ///     如果未配置自定义字体或自定义字体与原始字体相同，则返回原始字体。
    /// </summary>
    /// <param name="originalFont">要替换的原始字体。</param>
    /// <returns>替换后的自定义字体；如果未配置有效的自定义字体，则返回原始字体。</returns>
    public static Font SwapUIFont(Font originalFont)
    {
        if (originalFont == null)
            return null;

        var customFont = JustAnotherTranslator.UIFont.Value?.Trim();
        if (string.IsNullOrEmpty(customFont) || string.Equals(originalFont.name, customFont,
                StringComparison.OrdinalIgnoreCase))
            return originalFont;

        return SwapFont(originalFont, customFont);
    }

    /// <summary>
    ///     替换字体为自定义字体。
    ///     如果未配置或未找到自定义字体，则返回原始字体。
    /// </summary>
    /// <param name="originalFont">要替换的原始字体</param>
    /// <param name="fontName">要替换的字体名称</param>
    /// <returns>替换后的字体或原始字体（如果未找到自定义字体）</returns>
    public static Font SwapFont(Font originalFont, string fontName)
    {
        if (originalFont == null)
            return null;

        var font = GetFontByNameNoDefault(fontName, originalFont.fontSize);

        if (font == null)
            return originalFont;

        return font;
    }

    /// <summary>
    ///     获取字体。如果字体不存在，使用默认的 Arial 字体。
    /// </summary>
    public static Font GetFontByName(string name, int size)
    {
        if (string.IsNullOrEmpty(name))
            return GetDefaultFont();

        if (IsArial(name))
            return GetDefaultFont();

        var font = GetOrCreateFont(name, size);

        if (font == null)
        {
            LogManager.Warning(
                $"Failed to load font: {name}. Falling back to default font (Arial)/加载字体 {name} 失败，回退使用默认 Arial 字体");
            return GetDefaultFont();
        }

        return font;
    }

    /// <summary>
    ///     获取字体。如果字体不存在，返回 null
    /// </summary>
    /// <param name="name"></param>
    /// <param name="size"></param>
    public static Font GetFontByNameNoDefault(string name, int size)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        return GetOrCreateFont(name, size);
    }

    /// <summary>
    ///     尝试从自定义字体缓存中检索具有指定名称和大小的字体，或者创建新的动态字体并将其添加到缓存中。
    ///     如果字体加载失败，将返回 null。
    /// </summary>
    /// <param name="name">字体名称。</param>
    /// <param name="size">字体大小。</param>
    /// <returns>
    ///     如果加载成功，则返回与指定名称和大小匹配的字体；
    ///     如果加载失败或字体无效，则返回 null。
    /// </returns>
    private static Font GetOrCreateFont(string name, int size)
    {
        var fontId = $"{name}#{size}";
        try
        {
            // Unity 的伪空检查
            if (CustomFonts.TryGetValue(fontId, out var font) && font != null) return font;

            font = Font.CreateDynamicFontFromOSFont(name, size);
            if (font != null)
            {
                CustomFonts[fontId] = font;
                return font;
            }

            CustomFonts.Remove(fontId);
            return null;
        }
        catch (Exception e)
        {
            LogManager.Warning(
                $"Exception occurred while loading font '{name}'/加载字体 {name} 时发生异常: {e.Message}");
            CustomFonts.Remove(fontId);
            return null;
        }
    }

    /// <summary>
    ///     检查给定的字体名称是否是 Arial
    /// </summary>
    /// <param name="name">要检查的字体名称。</param>
    /// <returns>如果该名称表示 Arial 字体，则返回 true；否则返回 false。</returns>
    private static bool IsArial(string name)
    {
        return string.Equals(name, "Arial", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(name, "Arial.ttf", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     获取默认字体（Arial）。
    /// </summary>
    /// <returns>默认字体（Arial）</returns>
    private static Font GetDefaultFont()
    {
        return Resources.GetBuiltinResource<Font>("Arial.ttf");
    }


    /// <summary>
    ///     打印操作系统上所有已安装字体的名称到控制台和日志
    /// </summary>
    public static void PrintOSInstalledFontNames()
    {
        try
        {
            var fonts = Font.GetOSInstalledFontNames();

            LogManager.Info("OS Installed Font Name:/系统内已安装的字体名称:");
            
            foreach (var font in fonts)
                LogManager.Info(font);
        }
        catch (Exception e)
        {
            LogManager.Warning(
                $"Failed to retrieve OS installed fonts/获取系统内已安装的字体名称时发生异常: {e.Message}");
        }
    }
}