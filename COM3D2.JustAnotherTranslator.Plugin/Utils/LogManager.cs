using BepInEx.Logging;

namespace COM3D2.JustAnotherTranslator.Plugin;

/// <summary>
///     A wrapper for BepInEx's logging system that provides different log levels
///     and only shows debug logs in DEBUG builds.
/// </summary>
public static class LogManager
{
    private static ManualLogSource _logSource;
    private static string _lastDebugMessage = "";


    /// <summary>
    ///     Initializes the logger with the specified log source.
    /// </summary>
    /// <param name="logSource">The BepInEx log source to use.</param>
    public static void Initialize(ManualLogSource logSource)
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

        // It just got too many same messages
        if (message.ToString() == _lastDebugMessage) return;

        _logSource?.LogDebug(message);

        _lastDebugMessage = message.ToString();
    }

    /// <summary>
    ///     Logs an info message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public static void Info(object message)
    {
        if (JustAnotherTranslator.LogLevelConfig.Value < LogLevel.Info) return;

        _logSource?.LogInfo(message);
    }

    /// <summary>
    ///     Logs a warning message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public static void Warning(object message)
    {
        if (JustAnotherTranslator.LogLevelConfig.Value < LogLevel.Warning) return;

        _logSource?.LogWarning(message);
    }

    /// <summary>
    ///     Logs an error message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public static void Error(object message)
    {
        if (JustAnotherTranslator.LogLevelConfig.Value < LogLevel.Error) return;

        _logSource?.LogError(message);
    }
}