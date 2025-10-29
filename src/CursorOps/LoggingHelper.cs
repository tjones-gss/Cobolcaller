namespace CursorOps;

/// <summary>
/// Simple logging interface for troubleshooting and debugging.
/// Designed for easy mocking in tests (Phase 2B).
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Logs informational messages about normal operations.
    /// </summary>
    void Information(string message, params object[] args);

    /// <summary>
    /// Logs warning messages about non-critical issues.
    /// </summary>
    void Warning(string message, params object[] args);

    /// <summary>
    /// Logs error messages with exception details.
    /// </summary>
    void Error(Exception? ex, string message, params object[] args);

    /// <summary>
    /// Logs debug messages (only when verbose mode enabled).
    /// </summary>
    void Debug(string message, params object[] args);
}

/// <summary>
/// Simple file-based logger implementation.
/// Writes timestamped log entries to a file for troubleshooting.
/// Thread-safe for concurrent logging.
/// </summary>
public class SimpleFileLogger : ILogger
{
    private readonly string _logFilePath;
    private readonly bool _enableDebug;
    private readonly object _lock = new object();

    /// <summary>
    /// Creates a new file logger.
    /// </summary>
    /// <param name="logDirectory">Directory to store log files</param>
    /// <param name="enableDebug">Whether to log debug messages (verbose mode)</param>
    public SimpleFileLogger(string logDirectory, bool enableDebug = false)
    {
        _enableDebug = enableDebug;

        try
        {
            Directory.CreateDirectory(logDirectory);
        }
        catch
        {
            // If we can't create log directory, logging will silently fail
            // This is acceptable - logging failure should never crash the app
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _logFilePath = Path.Combine(logDirectory, $"cursorops_{timestamp}.log");

        // Write header
        try
        {
            var header = $"""
                ============================================
                CursorOps Log Started
                Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
                Debug Mode: {enableDebug}
                ============================================

                """;
            File.WriteAllText(_logFilePath, header);
        }
        catch
        {
            // Silently fail if we can't write log header
        }
    }

    public void Information(string message, params object[] args)
    {
        WriteLog("INFO", message, null, args);
    }

    public void Warning(string message, params object[] args)
    {
        WriteLog("WARN", message, null, args);
    }

    public void Error(Exception? ex, string message, params object[] args)
    {
        WriteLog("ERROR", message, ex, args);
    }

    public void Debug(string message, params object[] args)
    {
        if (_enableDebug)
        {
            WriteLog("DEBUG", message, null, args);
        }
    }

    /// <summary>
    /// Writes a log entry to the log file.
    /// Thread-safe through locking.
    /// </summary>
    private void WriteLog(string level, string message, Exception? ex, params object[] args)
    {
        lock (_lock)
        {
            try
            {
                // Format message with arguments
                var formatted = args.Length > 0 ? string.Format(message, args) : message;

                // Build log entry
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logEntry = $"[{timestamp}] [{level}] {formatted}";

                // Add exception details if present
                if (ex != null)
                {
                    logEntry += Environment.NewLine;
                    logEntry += $"  Exception: {ex.GetType().Name}: {ex.Message}";

                    if (ex.StackTrace != null)
                    {
                        logEntry += Environment.NewLine;
                        logEntry += $"  StackTrace: {ex.StackTrace}";
                    }

                    // Include inner exception if present
                    if (ex.InnerException != null)
                    {
                        logEntry += Environment.NewLine;
                        logEntry += $"  Inner Exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}";
                    }
                }

                // Append to log file
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
            catch
            {
                // Logging failure should never crash the application
                // Silently fail and continue
            }
        }
    }

    /// <summary>
    /// Gets the full path to the log file.
    /// </summary>
    public string GetLogPath() => _logFilePath;
}

/// <summary>
/// Global logging helper for easy access to logger throughout application.
/// Initialized once at application startup.
/// </summary>
public static class LoggingHelper
{
    private static ILogger? _logger;
    private static string? _logFilePath;

    /// <summary>
    /// Initializes the global logger.
    /// Should be called once at application startup.
    /// </summary>
    /// <param name="verbose">Enable verbose/debug logging</param>
    public static void Initialize(bool verbose = false)
    {
        try
        {
            var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
            var fileLogger = new SimpleFileLogger(logDir, enableDebug: verbose);
            _logger = fileLogger;
            _logFilePath = fileLogger.GetLogPath();

            // Log initialization
            _logger.Information("Logging initialized (verbose: {Verbose})", verbose);
        }
        catch
        {
            // If logging initialization fails, continue without logging
            _logger = null;
            _logFilePath = null;
        }
    }

    /// <summary>
    /// Gets the global logger instance.
    /// Returns null if logging was not initialized or initialization failed.
    /// </summary>
    public static ILogger? Logger => _logger;

    /// <summary>
    /// Gets the path to the current log file.
    /// Returns null if logging was not initialized.
    /// </summary>
    public static string? LogFilePath => _logFilePath;

    /// <summary>
    /// Displays log file location to user.
    /// Useful for troubleshooting messages.
    /// </summary>
    public static void ShowLogLocation()
    {
        if (_logFilePath != null && File.Exists(_logFilePath))
        {
            ConsoleHelper.WriteInfo($"Log file: {_logFilePath}");
        }
    }
}
