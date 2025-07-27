using System;
using System.Globalization;

namespace COM3D2.JustAnotherTranslator.Plugin.Utils;

public static class StringTool
{
    /// <summary>
    ///     检查字符串是否为数字（包含小数点）
    ///     应当比直接 decimal.TryParse 和正则表达式更快
    ///     因为 .NET Framework 3.5 比较慢
    /// </summary>
    /// <param name="text">要检查的字符串</param>
    /// <returns>如果字符串是数字，则返回 true；否则返回 false</returns>
    public static bool IsNumeric(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        // 特殊情况：单字符非数字
        if (text.Length == 1)
            return text[0] >= '0' && text[0] <= '9';

        // 短字符串优化
        if (text.Length <= 8)
        {
            var hasDecimalPoint = false;
            var hasDigit = false;

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                // 处理符号（只能在开头）
                if (i == 0 && (c == '-' || c == '+'))
                    continue;

                // 处理小数点
                if (c == '.')
                {
                    if (hasDecimalPoint) return false;
                    hasDecimalPoint = true;
                }
                // 处理数字
                else if (c >= '0' && c <= '9')
                {
                    hasDigit = true;
                }
                else
                {
                    return false;
                }
            }

            return hasDigit;
        }

        // 长字符串回退到 decimal.TryParse
        return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out _);
    }

    /// <summary>
    ///     检查字符串是否为空或者只包含空白
    ///     因为 .NET Framework 3.5 没有
    /// </summary>
    /// <param name="text">要检查的字符串</param>
    /// <returns>如果字符串为空或者只包含空白字符，则返回 true；否则返回 false</returns>
    public static bool IsNullOrWhiteSpace(string text)
    {
        return String.IsNullOrEmpty(text) || text.Trim().Length == 0;
    }
}