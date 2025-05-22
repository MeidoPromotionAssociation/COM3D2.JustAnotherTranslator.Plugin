using System;
using System.Text;

namespace COM3D2.JustAnotherTranslator.Plugin.Utils
{
    public static class StringExtensions
    {
        /// <summary>
        /// 将模板字符串中的占位符替换为指定函数返回的值
        /// 支持以下格式：
        /// $1, $2, $n - 数字索引
        /// ${name} - 命名变量
        /// </summary>
        /// <param name="template">模板字符串</param>
        /// <param name="templateFunc">用于处理占位符的函数</param>
        /// <returns>处理后的字符串</returns>
        public static string Template(this string template, Func<string, string> templateFunc)
        {
            var sb = new StringBuilder(template.Length);
            var sbTemplate = new StringBuilder();

            bool insideTemplate = false;
            bool bracedTemplate = false;
            for (int i = 0; i < template.Length; i++)
            {
                char c = template[i];
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
    }
}
