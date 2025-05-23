using System;
using System.Text;

namespace COM3D2.JustAnotherTranslator.Plugin.Utils;

/// <summary>
///     字符串扩展方法
///     为了保持兼容性，此部分代码从
///     https://github.com/ghorsington/CM3D2.YATranslator/blob/master/CM3D2.YATranslator.Plugin/Utils/Extensions.cs 复制
///     CM3D2.YATranslator 基于 The Unlicense 许可证，协议开源，作者为 ghorsington
///     一开始我没有这样做，但产生了一些和现有翻译的兼容问题，所以我决定保留这个部分代码
/// </summary>
public static class StringExtensions
{
    /// <summary>
    ///     将模板字符串中的占位符替换为指定函数返回的值
    ///     支持以下格式：
    ///     $1, $2, $n - 数字索引
    ///     ${name} - 命名变量
    /// </summary>
    /// <param name="template">模板字符串</param>
    /// <param name="templateFunc">用于处理占位符的函数</param>
    /// <returns>处理后的字符串</returns>
    public static string Template(this string template, Func<string, string> templateFunc)
    {
        var sb = new StringBuilder(template.Length);
        var sbTemplate = new StringBuilder();

        var insideTemplate = false;
        var bracedTemplate = false;
        // 遍历模板字符串中的每个字符
        for (var i = 0; i < template.Length; i++)
        {
            // 处理转义字符和占位符
            var c = template[i];
            switch (c)
            {
                case '\\':
                    if (i + 1 < template.Length && template[i + 1] == '$')
                    {
                        sb.Append('$');
                        i++;
                        continue;
                    }

                    break;
                case '$':
                    insideTemplate = true;
                    continue;
                case '{':
                    if (insideTemplate)
                    {
                        bracedTemplate = true;
                        continue;
                    }

                    break;
                case '}':
                    if (insideTemplate && sbTemplate.Length > 0)
                    {
                        sb.Append(templateFunc(sbTemplate.ToString()));
                        sbTemplate.Length = 0;
                        insideTemplate = false;
                        bracedTemplate = false;
                        continue;
                    }

                    break;
            }

            if (insideTemplate && !bracedTemplate && !char.IsDigit(c))
            {
                sb.Append(templateFunc(sbTemplate.ToString()));
                sbTemplate.Length = 0;
                insideTemplate = false;
            }

            if (insideTemplate)
                sbTemplate.Append(c);
            else
                sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     将字符串中的特殊字符转换为对应的转义序列
    ///     处理的特殊字符包括：
    ///     \0 - 空字符
    ///     \a - 警报
    ///     \b - 退格
    ///     \t - 水平制表符
    ///     \n - 换行
    ///     \v - 垂直制表符
    ///     \f - 换页
    ///     \r - 回车
    ///     \' - 单引号
    ///     \\ - 反斜杠
    ///     \" - 双引号
    /// </summary>
    /// <param name="txt">需要处理的原始字符串</param>
    /// <returns>处理后的字符串，所有特殊字符被替换为对应的转义序列</returns>
    public static string Escape(this string txt)
    {
        // 创建一个新的StringBuilder，预分配比原字符串略大的容量以提高效率
        var stringBuilder = new StringBuilder(txt.Length + 2);
        // 遍历字符串中的每个字符
        foreach (var c in txt)
            switch (c)
            {
                // 对特殊字符进行转义处理
                case '\0':
                    stringBuilder.Append(@"\0");
                    break;
                case '\a':
                    stringBuilder.Append(@"\a");
                    break;
                case '\b':
                    stringBuilder.Append(@"\b");
                    break;
                case '\t':
                    stringBuilder.Append(@"\t");
                    break;
                case '\n':
                    stringBuilder.Append(@"\n");
                    break;
                case '\v':
                    stringBuilder.Append(@"\v");
                    break;
                case '\f':
                    stringBuilder.Append(@"\f");
                    break;
                case '\r':
                    stringBuilder.Append(@"\r");
                    break;
                case '\'':
                    stringBuilder.Append(@"\'");
                    break;
                case '\\':
                    stringBuilder.Append(@"\");
                    break;
                case '\"':
                    stringBuilder.Append(@"\""");
                    break;
                default:
                    // 如果字符不是特殊字符，则直接添加到StringBuilder中
                    stringBuilder.Append(c);
                    break;
            }

        // 返回转义后的字符串
        return stringBuilder.ToString();
    }

    /// <summary>
    ///     将字符串中的转义序列转换为对应的实际字符
    ///     处理的转义字符包括：
    ///     \0 - 空字符
    ///     \a - 警报
    ///     \b - 退格
    ///     \t - 水平制表符
    ///     \n - 换行
    ///     \v - 垂直制表符
    ///     \f - 换页
    ///     \r - 回车
    ///     \' - 单引号
    ///     \" - 双引号
    ///     \\ - 反斜杠
    /// </summary>
    /// <param name="txt">需要处理转义字符的字符串</param>
    /// <returns>处理后的字符串，所有转义序列被替换为对应的实际字符</returns>
    public static string Unescape(this string txt)
    {
        if (string.IsNullOrEmpty(txt))
            return txt;
        var stringBuilder = new StringBuilder(txt.Length);
        for (var i = 0; i < txt.Length;)
        {
            // 查找下一个转义字符
            var num = txt.IndexOf('\\', i);
            // 如果没有找到转义字符，或者转义字符是字符串的最后一个字符，则直接添加到StringBuilder中
            if (num < 0 || num == txt.Length - 1)
                num = txt.Length;
            // 将字符串中i到num-1之间的字符添加到StringBuilder中
            stringBuilder.Append(txt, i, num - i);
            if (num >= txt.Length)
                break;
            // 处理转义字符
            var c = txt[num + 1];
            switch (c)
            {
                case '0':
                    stringBuilder.Append('\0');
                    break;
                case 'a':
                    stringBuilder.Append('\a');
                    break;
                case 'b':
                    stringBuilder.Append('\b');
                    break;
                case 't':
                    stringBuilder.Append('\t');
                    break;
                case 'n':
                    stringBuilder.Append('\n');
                    break;
                case 'v':
                    stringBuilder.Append('\v');
                    break;
                case 'f':
                    stringBuilder.Append('\f');
                    break;
                case 'r':
                    stringBuilder.Append('\r');
                    break;
                case '\'':
                    stringBuilder.Append('\'');
                    break;
                case '\"':
                    stringBuilder.Append('\"');
                    break;
                case '\\':
                    stringBuilder.Append('\\');
                    break;
                default:
                    stringBuilder.Append('\\').Append(c);
                    break;
            }

            i = num + 2;
        }

        return stringBuilder.ToString();
    }
}