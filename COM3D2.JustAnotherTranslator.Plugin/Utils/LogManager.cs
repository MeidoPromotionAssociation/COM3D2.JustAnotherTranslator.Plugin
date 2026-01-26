using System;
using System.Collections.Generic;
using BepInEx.Logging;

namespace COM3D2.JustAnotherTranslator.Plugin.Utils;

/// <summary>
///     A wrapper for BepInEx's logging system that provides different log levels
///     and only shows debug logs in DEBUG builds.
/// </summary>
public static class LogManager
{
    private static ManualLogSource _logSource;
    private static string _lastDebugMessage = "";
    private const int MaxConsoleMessageLength = 512;


    /// <summary>
    ///     Initializes the logger with the specified log source.
    /// </summary>
    /// <param name="logSource">The BepInEx log source to use.</param>
    public static void Init(ManualLogSource logSource)
    {
        _logSource = logSource;
    }

    /// <summary>
    ///     Logs a debug message. Only appears in DEBUG builds.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public static void Debug(object message)
    {
        if (JustAnotherTranslator.LogLevelConfig.Value < LogLevel.Debug) return;

        var safeMessage = SafeToString(message);
        // It just got too many same messages
        if (safeMessage == _lastDebugMessage) return;

        SafeLog(LogLevel.Debug, safeMessage);

        _lastDebugMessage = safeMessage;
    }

    /// <summary>
    ///     Logs an info message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public static void Info(object message)
    {
        if (JustAnotherTranslator.LogLevelConfig.Value < LogLevel.Info) return;

        SafeLog(LogLevel.Info, SafeToString(message));
    }

    /// <summary>
    ///     Logs a warning message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public static void Warning(object message)
    {
        if (JustAnotherTranslator.LogLevelConfig.Value < LogLevel.Warning) return;

        SafeLog(LogLevel.Warning, SafeToString(message));
    }

    /// <summary>
    ///     Logs an error message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public static void Error(object message)
    {
        if (JustAnotherTranslator.LogLevelConfig.Value < LogLevel.Error) return;

        SafeLog(LogLevel.Error, SafeToString(message));
    }


    /// <summary>
    /// Safely converts the given object to its string representation.
    /// </summary>
    /// <param name="message">The object to convert to a string.</param>
    /// <returns>The string representation of the object, or an error message if the conversion fails.</returns>
    private static string SafeToString(object message)
    {
        try
        {
            return message?.ToString() ?? "<error>";
        }
        catch (Exception e)
        {
            return "<log message threw: " + e.Message + ">";
        }
    }

    /// <summary>
    /// Safely logs a message at the specified log level, ensuring it adheres to console constraints.
    /// </summary>
    /// <param name="level">The severity level of the log message.</param>
    /// <param name="message">The message to be logged.</param>
    private static void SafeLog(LogLevel level, string message)
    {
        try
        {
            if (_logSource == null) return;

            var parts = SplitForConsole(message);
            if (parts.Count == 1)
            {
                LogSingle(level, parts[0]);
                return;
            }

            for (var i = 0; i < parts.Count; i++)
            {
                var prefix = "[" + (i + 1) + "/" + parts.Count + "] ";
                var part = parts[i];
                var maxPartLength = MaxConsoleMessageLength - prefix.Length;
                if (maxPartLength < 1) maxPartLength = 1;
                if (part.Length > maxPartLength)
                    part = part.Substring(0, maxPartLength);

                LogSingle(level, prefix + part);
            }
        }
        catch
        {
            // Ignore logging errors
        }
    }

    /// <summary>
    /// Logs a single message to the console with the specified log level.
    /// </summary>
    /// <param name="level">The log level of the message (e.g., Debug, Info, Warning, Error).</param>
    /// <param name="message">The message to log.</param>
    private static void LogSingle(LogLevel level, string message)
    {
        switch (level)
        {
            case LogLevel.Debug:
                _logSource.LogDebug(message);
                break;
            case LogLevel.Info:
                _logSource.LogInfo(message);
                break;
            case LogLevel.Warning:
                _logSource.LogWarning(message);
                break;
            case LogLevel.Error:
                _logSource.LogError(message);
                break;
        }
    }

    /// <summary>
    /// Splits the given message into smaller chunks suitable for console output,
    /// ensuring that each chunk does not exceed the maximum allowed message length.
    /// </summary>
    /// <param name="message">The message to be split into chunks.</param>
    /// <returns>A list of string chunks, each within the maximum console message length.</returns>
    private static List<string> SplitForConsole(string message)
    {
        var parts = new List<string>();

        if (string.IsNullOrEmpty(message))
        {
            parts.Add(message ?? string.Empty);
            return parts;
        }

        var i = 0;
        while (i < message.Length)
        {
            var remaining = message.Length - i;
            var max = remaining > MaxConsoleMessageLength ? MaxConsoleMessageLength : remaining;
            var end = i + max;

            if (end < message.Length &&
                char.IsHighSurrogate(message[end - 1]) &&
                char.IsLowSurrogate(message[end]))
                end--;

            if (end <= i) end = i + max;
            if (end > message.Length) end = message.Length;

            parts.Add(message.Substring(i, end - i));
            i = end;
        }

        return parts;
    }
}