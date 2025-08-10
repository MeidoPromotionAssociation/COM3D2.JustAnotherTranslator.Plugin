using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace COM3D2.JustAnotherTranslator.Plugin.Utils;

public static class FileTool
{
    /// 父目录符号
    private static readonly string ParentPathSymbol = $"..{Path.DirectorySeparatorChar}";

    /// 路径分隔符
    private static readonly string PathSeparator = Path.DirectorySeparatorChar.ToString();


    /// <summary>
    ///     获取所有翻译文件列表，按 Unicode 顺序排序，支持子目录
    /// </summary>
    /// <returns>文件路径列表</returns>
    public static List<string> GetAllTranslationFiles(string translationPath,
        string[] fileExtensions)
    {
        var allFiles = new List<string>();

        // 首先添加根目录的文件
        try
        {
            var rootFiles = Directory
                .GetFiles(translationPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f =>
                    fileExtensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(f => f, StringComparer.Ordinal);
            allFiles.AddRange(rootFiles);
        }
        catch (Exception e)
        {
            LogManager.Warning($"Error reading root directory files/读取根目录文件时出错: {e.Message}");
        }

        // 然后添加子目录的文件
        try
        {
            var directories = Directory
                .GetDirectories(translationPath, "*", SearchOption.AllDirectories)
                .OrderBy(d => d, StringComparer.Ordinal);

            foreach (var directory in directories)
                try
                {
                    var files = Directory.GetFiles(directory, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(f => fileExtensions.Any(ext =>
                            f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                        .OrderBy(f => f, StringComparer.Ordinal);
                    allFiles.AddRange(files);
                }
                catch (Exception e)
                {
                    LogManager.Warning(
                        $"Error reading directory files/读取目录文件时出错 {directory}: {e.Message}");
                }
        }
        catch (Exception e)
        {
            LogManager.Warning($"Error enumerating directories/枚举目录时出错: {e.Message}");
        }

        return allFiles;
    }


    /// <summary>
    ///     安全检查：禁止使用目录穿越路径（../）、绝对路径、根路径
    ///     虽然我们不解压文件到文件系统，但是安全总是好的
    /// </summary>
    public static bool IsZipPathUnsafe(string entryName)
    {
        if (string.IsNullOrEmpty(entryName))
            return true;

        // 统一路径分隔符为平台格式
        var normalizedPath = entryName.Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);

        // 1拒绝包含 ".."
        if (normalizedPath.Contains(ParentPathSymbol))
            return true;

        // 拒绝绝对路径
        if (Path.IsPathRooted(normalizedPath))
            return true;

        // 禁止以斜杠开头（Unix 风格绝对路径）
        if (normalizedPath.StartsWith(PathSeparator))
            return true;

        return false;
    }
}