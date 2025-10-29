using System.Text.Json;
using System.Text.Json.Serialization;

namespace CursorOps;

/// <summary>
/// Configuration model for CursorOps tool.
/// Loaded from config/cursorops.json and provides settings for file patterns,
/// COBOL call tracing, and context generation.
/// </summary>
public class CursorOpsConfig
{
    /// <summary>
    /// Root directory containing source code to analyze.
    /// Used as base path for file searches and COBOL tracing.
    /// </summary>
    [JsonPropertyName("RootDirectory")]
    public string RootDirectory { get; set; } = ".";

    /// <summary>
    /// File patterns to match COBOL source files (e.g., "*.cbl", "*.cob").
    /// Used by COBOL call graph builder to locate programs.
    /// </summary>
    [JsonPropertyName("CobolFilePatterns")]
    public List<string> CobolFilePatterns { get; set; } = new() { "*.cbl", "*.cob" };

    /// <summary>
    /// File patterns to match VB.NET source files (e.g., "*.vb").
    /// Reserved for future VB.NET analysis features.
    /// </summary>
    [JsonPropertyName("VbFilePatterns")]
    public List<string> VbFilePatterns { get; set; } = new() { "*.vb" };

    /// <summary>
    /// File patterns to match C# source files (e.g., "*.cs").
    /// Used for context packing and future C# analysis.
    /// </summary>
    [JsonPropertyName("CsFilePatterns")]
    public List<string> CsFilePatterns { get; set; } = new() { "*.cs" };

    /// <summary>
    /// Maximum number of lines to include from a single file in context pack.
    /// Files exceeding this limit will be truncated with head/tail sections.
    /// </summary>
    [JsonPropertyName("MaxFileLines")]
    public int MaxFileLines { get; set; } = 500;

    /// <summary>
    /// Number of lines to include from the beginning of truncated files.
    /// Used when a file exceeds MaxFileLines.
    /// </summary>
    [JsonPropertyName("TrimHeadLines")]
    public int TrimHeadLines { get; set; } = 50;

    /// <summary>
    /// Number of lines to include from the end of truncated files.
    /// Used when a file exceeds MaxFileLines.
    /// </summary>
    [JsonPropertyName("TrimTailLines")]
    public int TrimTailLines { get; set; } = 50;

    /// <summary>
    /// Regex patterns for detecting COBOL CALL statements.
    /// Each pattern must contain a named capture group "prog" for the called program name.
    /// Example: "CALL\\s+['\"](?&lt;prog&gt;\\w+)['\"]" matches CALL "PROG123"
    /// </summary>
    [JsonPropertyName("CallPatterns")]
    public List<string> CallPatterns { get; set; } = new()
    {
        "CALL\\s+['\"](?<prog>\\w+)['\"]",  // CALL "PROGNAME"
        "CALL\\s+(?<prog>\\w+)"             // CALL PROGNAME (without quotes)
    };

    /// <summary>
    /// Path to the team rules Markdown file.
    /// Used by "rules inject" command to output coding standards and guidelines.
    /// </summary>
    [JsonPropertyName("RulesFile")]
    public string RulesFile { get; set; } = "rules/rules.md";

    /// <summary>
    /// Directory containing prompt template Markdown files.
    /// Used by "prompt list" and "prompt pick" commands.
    /// </summary>
    [JsonPropertyName("PromptsPath")]
    public string PromptsPath { get; set; } = "prompts";

    /// <summary>
    /// Loads configuration from the specified JSON file.
    /// Returns a default configuration if file doesn't exist or parsing fails.
    /// </summary>
    /// <param name="configPath">Path to cursorops.json configuration file</param>
    /// <returns>Loaded configuration object with all settings</returns>
    public static CursorOpsConfig Load(string configPath)
    {
        // Return default config if file doesn't exist
        if (!File.Exists(configPath))
        {
            ConsoleHelper.WriteWarning($"Config file not found: {configPath}. Using defaults.");
            return new CursorOpsConfig();
        }

        try
        {
            // Read and deserialize JSON config
            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<CursorOpsConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            return config ?? new CursorOpsConfig();
        }
        catch (Exception ex)
        {
            // Fall back to defaults on parse error
            ConsoleHelper.WriteError($"Failed to load config: {ex.Message}");
            ConsoleHelper.WriteWarning("Using default configuration.");
            return new CursorOpsConfig();
        }
    }

    /// <summary>
    /// Resolves a relative path against the configuration's RootDirectory.
    /// Handles both absolute and relative paths correctly.
    /// </summary>
    /// <param name="relativePath">Path to resolve (can be absolute or relative)</param>
    /// <returns>Fully resolved absolute path</returns>
    public string ResolvePath(string relativePath)
    {
        // If already absolute, return as-is
        if (Path.IsPathRooted(relativePath))
            return relativePath;

        // Combine with root directory
        return Path.GetFullPath(Path.Combine(RootDirectory, relativePath));
    }
}

/// <summary>
/// Helper class for color-coded console output.
/// Provides green (success), yellow (warning), and red (error) output methods.
/// </summary>
public static class ConsoleHelper
{
    /// <summary>
    /// Writes a success message in green.
    /// </summary>
    public static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    /// <summary>
    /// Writes a warning message in yellow.
    /// </summary>
    public static void WriteWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    /// <summary>
    /// Writes an error message in red.
    /// </summary>
    public static void WriteError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    /// <summary>
    /// Writes an informational message in cyan.
    /// </summary>
    public static void WriteInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
