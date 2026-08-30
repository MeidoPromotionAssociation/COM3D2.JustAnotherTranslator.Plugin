using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using COM3D2.JustAnotherTranslator.Plugin.Utils;

namespace COM3D2.JustAnotherTranslator.Plugin.Loader;

/// <summary>
///     翻译结果磁盘缓存，用于在二次启动时跳过翻译文件的重复解析以提升加载速度。
///     缓存以翻译文件的路径、大小和最后修改时间作为签名，签名变化或缓存损坏时自动失效并重新解析。
///     缓存使用自定义二进制格式存储，避免大量翻译条目在 JSON 序列化时的额外开销
/// </summary>
public static class TranslationCache
{
    /// <summary>
    ///     缓存格式版本号，当解析或二进制格式发生变化而不兼容旧缓存时应递增该值以强制重建缓存
    /// </summary>
    private const int CacheVersion = 2;

    /// <summary>
    ///     二进制文件头的魔数，用于区分本插件缓存文件与其它文件，避免误读
    /// </summary>
    private const int MagicFileHeader = 0x4A415443; // "JATC"

    /// <summary>
    ///     SHA256 校验和长度
    /// </summary>
    private const int ChecksumLength = 32;

    /// <summary>
    ///     缓存文件扩展名
    /// </summary>
    private const string CacheFileExtension = ".jatc";

    /// <summary>
    ///     生成缓存文件路径。
    /// </summary>
    /// <returns>缓存文件路径</returns>
    public static string GetCacheFilePath(string loaderName)
    {
        // 缓存统一存放于 BepInEx\cache 目录下的子目录，避免与其它插件的缓存文件冲突
        var cacheDirectory = Path.Combine(Paths.CachePath, "JustAnotherTranslator");
        Directory.CreateDirectory(cacheDirectory);

        return Path.Combine(cacheDirectory, $"TranslationCache{CacheFileExtension}");
    }

    /// <summary>
    ///     计算所有翻译文件的签名，用于判断翻译文件是否发生变化。
    ///     签名基于文件路径、大小和最后修改时间，其开销远小于重新解析翻译文件本身
    /// </summary>
    /// <param name="translationFiles">翻译文件路径列表</param>
    /// <returns>文件的 SHA256 十六进制字符串签名，无法读取任何文件元信息时返回 null</returns>
    public static string ComputeSignature(List<string> translationFiles)
    {
        var builder = new StringBuilder();
        foreach (var file in translationFiles)
            try
            {
                var fileInfo = new FileInfo(file);
                builder.Append(file).Append('|')
                    .Append(fileInfo.Length).Append('|')
                    .Append(fileInfo.LastWriteTimeUtc.Ticks).Append('\n');
            }
            catch (Exception e)
            {
                LogManager.Warning(
                    $"Failed to read translation file metadata, signature may be incomplete/读取翻译文件元信息失败，签名可能不完整: {e.Message}");
            }

        if (builder.Length == 0)
            return null;

        var hash = ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
        return BitConverter.ToString(hash).Replace("-", "");
    }

    /// <summary>
    ///     尝试从缓存文件加载翻译结果。
    ///     仅当缓存文件存在、文件头合法、版本一致、翻译文件签名匹配且校验和通过时才视为有效。
    ///     数据会先完整解析并校验正则，全部通过后才写入内存字典，避免部分损坏的缓存污染翻译结果
    /// </summary>
    /// <param name="cacheFilePath">缓存文件路径</param>
    /// <param name="signature">当前翻译文件签名</param>
    /// <param name="result">加载成功时写入的翻译结果</param>
    /// <returns>缓存是否有效并成功加载</returns>
    public static bool TryLoad(string cacheFilePath, string signature,
        TranslationLoadResult result)
    {
        if (signature == null || !File.Exists(cacheFilePath))
            return false;

        try
        {
            using (var fileStream = new FileStream(cacheFilePath, FileMode.Open, FileAccess.Read,
                       FileShare.Read))
            using (var reader = new BinaryReader(fileStream))
            {
                if (reader.ReadInt32() != MagicFileHeader)
                {
                    LogManager.Warning(
                        $"Translation cache format is invalid, reloading translation files/翻译缓存格式无效，正在重新解析翻译文件: {cacheFilePath}");
                    return false;
                }

                if (reader.ReadInt32() != CacheVersion)
                {
                    LogManager.Info(
                        $"Translation cache format is outdated, reloading translation files/翻译缓存格式已过时，正在重新解析翻译文件: {cacheFilePath}");
                    return false;
                }

                var storedChecksum = reader.ReadBytes(ChecksumLength);
                if (storedChecksum.Length != ChecksumLength)
                {
                    LogManager.Warning(
                        $"Translation cache is truncated, reloading translation files/翻译缓存文件不完整，正在重新解析翻译文件: {cacheFilePath}");
                    return false;
                }

                var storedSignature = reader.ReadString();
                reader.ReadInt64(); // SavedAtUtc.Ticks，仅用于信息展示，读取后丢弃

                if (storedSignature != signature)
                {
                    LogManager.Info(
                        $"Translation files have changed, reloading translation files/翻译文件已发生变化，正在重新解析翻译文件: {cacheFilePath}");
                    return false;
                }

                var payloadLength = reader.ReadInt32();
                // 防御损坏的长度值
                if (payloadLength < 0 || payloadLength > fileStream.Length)
                {
                    LogManager.Warning(
                        $"Translation cache length is invalid, reloading translation files/翻译缓存长度无效，正在重新解析翻译文件: {cacheFilePath}");
                    return false;
                }

                var payload = reader.ReadBytes(payloadLength);
                if (payload.Length != payloadLength)
                {
                    LogManager.Warning(
                        $"Translation cache is truncated, reloading translation files/翻译缓存文件不完整，正在重新解析翻译文件: {cacheFilePath}");
                    return false;
                }

                // 校验数据完整性，防止缓存被篡改或出现位翻转
                if (!BytesEqual(ComputeHash(payload), storedChecksum))
                {
                    LogManager.Warning(
                        $"Translation cache checksum mismatch, reloading translation files/翻译缓存校验和不匹配，正在重新解析翻译文件: {cacheFilePath}");
                    return false;
                }

                return TryParsePayload(payload, result);
            }
        }
        catch (Exception e)
        {
            LogManager.Warning(
                $"Failed to read translation cache, reloading translation files/读取翻译缓存失败，正在重新解析翻译文件: {e.Message}");
            return false;
        }
    }

    /// <summary>
    ///     将翻译结果保存到缓存文件。
    ///     保存失败不影响翻译加载，仅记录日志
    /// </summary>
    /// <param name="cacheFilePath">缓存文件路径</param>
    /// <param name="signature">当前翻译文件签名</param>
    /// <param name="result">需要缓存的翻译结果</param>
    public static void Save(string cacheFilePath, string signature, TranslationLoadResult result)
    {
        if (signature == null)
            return;

        try
        {
            // 先将数据序列化到内存，再对其计算校验和，从而可校验缓存的完整性
            var payload = SerializePayload(result);
            var checksum = ComputeHash(payload);

            using (var fileStream = new FileStream(cacheFilePath, FileMode.Create,
                       FileAccess.Write))
            using (var writer = new BinaryWriter(fileStream))
            {
                writer.Write(MagicFileHeader);
                writer.Write(CacheVersion);
                writer.Write(checksum);
                writer.Write(signature);
                writer.Write(DateTime.UtcNow.Ticks);
                writer.Write(payload.Length);
                writer.Write(payload);
            }

            LogManager.Info($"Translation cache saved to/翻译缓存已保存至: {cacheFilePath}");
        }
        catch (Exception e)
        {
            LogManager.Warning($"Failed to save translation cache/保存翻译缓存失败: {e.Message}");
        }
    }

    /// <summary>
    ///     将翻译结果序列化为二进制数据。
    ///     格式依次为：普通条目数（原文、译文），正则条目数（模式、译文），字符串均以 UTF8 长度前缀形式存储
    /// </summary>
    /// <param name="result">翻译结果</param>
    /// <returns>序列化后的二进制数据</returns>
    private static byte[] SerializePayload(TranslationLoadResult result)
    {
        using (var memoryStream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(memoryStream))
            {
                writer.Write(result.Translations.Count);
                foreach (var pair in result.Translations)
                {
                    writer.Write(pair.Key);
                    writer.Write(pair.Value);
                }

                writer.Write(result.RegexTranslations.Count);
                foreach (var pair in result.RegexTranslations)
                {
                    writer.Write(pair.Key.ToString());
                    writer.Write(pair.Value);
                }

                writer.Flush();
            }

            return memoryStream.ToArray();
        }
    }

    /// <summary>
    ///     解析缓存的二进制数据并写入翻译结果。
    ///     先完整解析到临时列表并校验所有正则，全部通过后再写入内存字典，避免部分损坏的缓存污染翻译结果
    /// </summary>
    /// <param name="payload">缓存数据</param>
    /// <param name="result">写入的翻译结果</param>
    /// <returns>解析是否成功</returns>
    private static bool TryParsePayload(byte[] payload, TranslationLoadResult result)
    {
        try
        {
            var translations = new List<KeyValuePair<string, string>>();
            var compiledRegexes = new List<KeyValuePair<Regex, string>>();

            using (var memoryStream = new MemoryStream(payload))
            using (var reader = new BinaryReader(memoryStream))
            {
                var translationCount = reader.ReadInt32();
                if (translationCount < 0)
                    return false;

                for (var i = 0; i < translationCount; i++)
                    translations.Add(new KeyValuePair<string, string>(reader.ReadString(),
                        reader.ReadString()));

                var regexCount = reader.ReadInt32();
                if (regexCount < 0)
                    return false;

                for (var i = 0; i < regexCount; i++)
                {
                    var pattern = reader.ReadString();
                    var replacement = reader.ReadString();
                    try
                    {
                        compiledRegexes.Add(new KeyValuePair<Regex, string>(
                            new Regex(pattern, RegexOptions.Compiled), replacement));
                    }
                    catch (Exception e)
                    {
                        LogManager.Warning(
                            $"Translation cache contains an invalid regex, reloading translation files/翻译缓存包含无效的正则表达式，正在重新解析翻译文件: {e.Message}");
                        return false;
                    }
                }
            }

            // 全部校验通过后写入结果，确保缓存部分损坏时不会污染内存字典
            foreach (var pair in translations)
                result.Translations[pair.Key] = pair.Value;

            foreach (var pair in compiledRegexes)
                result.RegexTranslations[pair.Key] = pair.Value;

            result.TotalEntries = translations.Count + compiledRegexes.Count;
            return true;
        }
        catch (Exception e)
        {
            LogManager.Warning(
                $"Failed to parse translation cache, reloading translation files/解析翻译缓存失败，正在重新解析翻译文件: {e.Message}");
            return false;
        }
    }

    /// <summary>
    ///     计算数据块的 SHA256 哈希
    /// </summary>
    /// <param name="data">数据块</param>
    /// <returns>哈希值</returns>
    private static byte[] ComputeHash(byte[] data)
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(data);
    }

    /// <summary>
    ///     比较两个字节数组是否相等
    /// </summary>
    private static bool BytesEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
            return false;

        for (var i = 0; i < left.Length; i++)
            if (left[i] != right[i])
                return false;

        return true;
    }
}