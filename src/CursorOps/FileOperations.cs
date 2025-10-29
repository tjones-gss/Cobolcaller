using System.Security;
using System.Text;

namespace CursorOps;

/// <summary>
/// Provides centralized file I/O operations with security validation and error handling.
/// Eliminates code duplication across command handlers.
/// </summary>
public static class FileOperations
{
    /// <summary>
    /// Safely writes content to a file with security validation and error handling.
    /// Consolidates the duplicated write pattern introduced in Phase 1.
    /// </summary>
    /// <param name="content">Content to write to the file</param>
    /// <param name="outputPath">User-provided output path</param>
    /// <param name="successMessage">Message to display on successful write</param>
    /// <param name="logger">Optional logger for troubleshooting</param>
    /// <returns>True if write succeeded, false if it failed</returns>
    public static bool SafeWriteToFile(
        string content,
        string? outputPath,
        string successMessage,
        ILogger? logger = null)
    {
        if (string.IsNullOrEmpty(outputPath))
        {
            // Output to console instead
            Console.WriteLine(content);
            return true;
        }

        try
        {
            var safePath = SecurityHelper.ValidateOutputPath(outputPath, Directory.GetCurrentDirectory());
            SecurityHelper.EnsureDirectoryExists(safePath, Directory.GetCurrentDirectory());
            File.WriteAllText(safePath, content);
            ConsoleHelper.WriteSuccess($"{successMessage}: {safePath}");
            logger?.Information("File written successfully: {Path}", safePath);
            return true;
        }
        catch (SecurityException ex)
        {
            ConsoleHelper.WriteError($"Security error: {ex.Message}");
            logger?.Error(ex, "Security validation failed for path: {Path}", outputPath);
            return false;
        }
        catch (IOException ex)
        {
            ConsoleHelper.WriteError($"Failed to write file: {ex.Message}");
            logger?.Error(ex, "File write failed: {Path}", outputPath);
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            ConsoleHelper.WriteError($"Permission denied: {ex.Message}");
            logger?.Error(ex, "Permission denied: {Path}", outputPath);
            return false;
        }
    }

    /// <summary>
    /// Safely reads file content with comprehensive exception handling.
    /// </summary>
    /// <param name="filePath">Path to the file to read</param>
    /// <param name="logger">Optional logger for troubleshooting</param>
    /// <returns>Tuple of (success, content) where success indicates if read succeeded</returns>
    public static (bool success, string content) SafeReadFile(
        string filePath,
        ILogger? logger = null)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            logger?.Debug("File read successfully: {Path}, {Size} bytes", filePath, content.Length);
            return (true, content);
        }
        catch (UnauthorizedAccessException ex)
        {
            ConsoleHelper.WriteError($"Permission denied reading file: {ex.Message}");
            logger?.Error(ex, "Permission denied: {Path}", filePath);
            return (false, string.Empty);
        }
        catch (IOException ex)
        {
            ConsoleHelper.WriteError($"Failed to read file: {ex.Message}");
            logger?.Error(ex, "File read failed: {Path}", filePath);
            return (false, string.Empty);
        }
    }

    /// <summary>
    /// Safely truncates file content for display with proper bounds checking.
    /// Consolidates the duplicated truncation logic from HandleContextPack and HandleCobolFocus.
    /// </summary>
    /// <param name="sb">StringBuilder to append content to</param>
    /// <param name="lines">File lines to truncate</param>
    /// <param name="maxLines">Maximum lines before truncation triggers</param>
    /// <param name="headLines">Number of lines to show from start</param>
    /// <param name="tailLines">Number of lines to show from end</param>
    /// <param name="syntaxHighlight">Optional syntax highlighting language (e.g., "cobol", "csharp")</param>
    public static void AppendTruncatedFile(
        StringBuilder sb,
        string[] lines,
        int maxLines,
        int headLines,
        int tailLines,
        string syntaxHighlight = "")
    {
        if (lines.Length <= maxLines)
        {
            // Full file - no truncation needed
            sb.AppendLine($"```{syntaxHighlight}");
            foreach (var line in lines)
            {
                sb.AppendLine(line);
            }
            sb.AppendLine("```");
            return;
        }

        // Truncated file - use safe bounds checking (Phase 1 fix)
        int actualHeadLines = Math.Min(headLines, lines.Length);
        int actualTailLines = Math.Min(tailLines, lines.Length - actualHeadLines);

        sb.AppendLine($"*File truncated: {lines.Length} lines (showing first {actualHeadLines} and last {actualTailLines})*");
        sb.AppendLine();
        sb.AppendLine($"```{syntaxHighlight}");

        // Head lines
        for (int i = 0; i < actualHeadLines; i++)
        {
            sb.AppendLine(lines[i]);
        }

        // Omitted section indicator
        int omittedLines = lines.Length - actualHeadLines - actualTailLines;
        if (omittedLines > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"... ({omittedLines} lines omitted) ...");
            sb.AppendLine();
        }

        // Tail lines
        int tailStart = Math.Max(actualHeadLines, lines.Length - actualTailLines);
        for (int i = tailStart; i < lines.Length; i++)
        {
            sb.AppendLine(lines[i]);
        }

        sb.AppendLine("```");
    }

    /// <summary>
    /// Appends file content to StringBuilder with automatic truncation if needed.
    /// Convenience method that combines reading and truncation.
    /// </summary>
    /// <param name="sb">StringBuilder to append content to</param>
    /// <param name="filePath">Path to the file to include</param>
    /// <param name="maxLines">Maximum lines before truncation</param>
    /// <param name="headLines">Lines to show from start if truncated</param>
    /// <param name="tailLines">Lines to show from end if truncated</param>
    /// <param name="syntaxHighlight">Syntax highlighting language</param>
    /// <param name="logger">Optional logger</param>
    /// <returns>True if file was successfully appended</returns>
    public static bool AppendFileWithTruncation(
        StringBuilder sb,
        string filePath,
        int maxLines,
        int headLines,
        int tailLines,
        string syntaxHighlight = "",
        ILogger? logger = null)
    {
        if (!File.Exists(filePath))
        {
            ConsoleHelper.WriteWarning($"File not found: {filePath}");
            logger?.Warning("File not found: {Path}", filePath);
            return false;
        }

        try
        {
            sb.AppendLine($"### {Path.GetFileName(filePath)}");
            sb.AppendLine($"Path: `{filePath}`");
            sb.AppendLine();

            var lines = File.ReadAllLines(filePath);
            AppendTruncatedFile(sb, lines, maxLines, headLines, tailLines, syntaxHighlight);
            sb.AppendLine();

            logger?.Debug("File appended: {Path}, {Lines} lines", filePath, lines.Length);
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            ConsoleHelper.WriteWarning($"Failed to read file {filePath}: {ex.Message}");
            logger?.Warning(ex, "Failed to read file: {Path}", filePath);
            sb.AppendLine($"*Error reading file: {ex.Message}*");
            sb.AppendLine();
            return false;
        }
    }
}
