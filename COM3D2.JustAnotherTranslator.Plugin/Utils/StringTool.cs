using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace COM3D2.JustAnotherTranslator.Plugin.Utils;

public static class StringTool
{
    /// 空白字符
    public static readonly char[] WhitespaceChars = new[]
    {
        '\t', '\n', '\v', '\f', '\r', ' ', '\u0085', '\u00a0', '\u1680', '\u2000',
        '\u2001', '\u2002', '\u2003', '\u2004', '\u2005', '\u2006', '\u2007', '\u2008', '\u2009',
        '\u200a',
        '\u200b', '\u2028', '\u2029', '\u3000', '\ufeff'
    };


    /// <summary>
    ///     规范化字符串
    ///     1.将 \r \n \t 替换为空
    ///     2.修剪 WhitespaceChars（不包括 \u180e）
    ///     3.转大写
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static string NormalizeText(string text)
    {
        return text.Replace("\r", "").Replace("\n", "")
            .Replace("\t", "").Trim(WhitespaceChars).ToUpper();
    }

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
        return string.IsNullOrEmpty(text) || text.Trim().Length == 0;
    }


    /// <summary>
    ///     将字符串列表序列化为字符串：每项使用 '|' 分隔
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public static string JoinStringList(List<string> list)
    {
        if (list == null || list.Count == 0) return null;
        // 使用竖线分隔，避免与 CSV 的逗号分隔符冲突
        return string.Join("|", list.ToArray());
    }

    /// <summary>
    ///     将整数列表序列化为字符串：每项使用 '|' 分隔
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public static string JoinIntList(List<int> list)
    {
        if (list == null || list.Count == 0) return null;
        var arr = new string[list.Count];
        for (var i = 0; i < list.Count; i++)
            arr[i] = list[i].ToString(CultureInfo.InvariantCulture);
        // 使用竖线分隔，避免与 CSV 的逗号分隔符冲突
        return string.Join("|", arr);
    }

    /// <summary>
    ///     将 singPartList 序列化为字符串：每项使用 ':' 分隔字段，项之间使用 '|'
    ///     需要转义的字符包括: '\\', '|', ':', '\r', '\n'
    /// </summary>
    public static string SerializeSingPartList(List<DanceSingPartData> list)
    {
        if (list == null || list.Count == 0) return null;
        var sb = new StringBuilder();
        for (var i = 0; i < list.Count; i++)
        {
            var item = list[i];
            if (i > 0) sb.Append('|');

            sb.Append(item.danceID.ToString(CultureInfo.InvariantCulture));
            sb.Append(':');
            sb.Append(Escape(item.vocalJPName));
            sb.Append(':');
            sb.Append(Escape(item.vocalNameTerm));
            sb.Append(':');
            sb.Append(Escape(item.vocalFile));
        }

        return sb.ToString();
    }

    /// <summary>
    ///     将字符串转义
    /// </summary>
    public static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        // 注意顺序：先替换反斜杠
        s = s.Replace("\\", "\\\\");
        s = s.Replace("|", "\\|");
        s = s.Replace(":", "\\:");
        s = s.Replace("\r", "\\r");
        s = s.Replace("\n", "\\n");
        return s;
    }
}