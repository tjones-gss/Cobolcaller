using System.CommandLine;
using System.Text.RegularExpressions;

namespace CursorOps;

/// <summary>
/// Provides configuration management commands.
/// Allows users to validate and inspect their configuration.
/// </summary>
public static class ConfigCommand
{
    /// <summary>
    /// Creates the "config" command group with validate and show subcommands.
    /// </summary>
    public static Command CreateConfigCommand(CursorOpsConfig config)
    {
        var configCommand = new Command("config", "Configuration management and validation");

        // config validate - Validate configuration settings
        var validateCommand = new Command("validate", "Validate configuration and check for common issues");
        validateCommand.SetHandler(() => HandleConfigValidate(config));

        // config show - Display current configuration
        var showCommand = new Command("show", "Display current configuration settings");
        showCommand.SetHandler(() => HandleConfigShow(config));

        configCommand.AddCommand(validateCommand);
        configCommand.AddCommand(showCommand);

        return configCommand;
    }

    /// <summary>
    /// Handler for "config validate" command.
    /// Validates all configuration settings and provides actionable feedback.
    /// </summary>
    private static void HandleConfigValidate(CursorOpsConfig config)
    {
        var logger = LoggingHelper.Logger;
        logger?.Information("Running configuration validation");

        Console.WriteLine();
        ConsoleHelper.WriteInfo("Validating configuration...");
        Console.WriteLine();

        int errors = 0;
        int warnings = 0;

        // Check config file exists
        var configPath = Path.Combine(AppContext.BaseDirectory, "config", "cursorops.json");
        if (File.Exists(configPath))
        {
            Console.WriteLine($"✓ Config file found: {configPath}");
            logger?.Debug("Config file exists: {Path}", configPath);
        }
        else
        {
            Console.WriteLine($"✗ Config file not found: {configPath}");
            Console.WriteLine($"  Next steps:");
            Console.WriteLine($"    1. Create the config directory: mkdir config");
            Console.WriteLine($"    2. Copy the default config from the repository");
            errors++;
            return; // Can't continue without config
        }

        // Check RootDirectory
        var rootDir = config.ResolvePath(config.RootDirectory);
        if (Directory.Exists(rootDir))
        {
            try
            {
                var fileCount = Directory.GetFiles(rootDir, "*", SearchOption.AllDirectories).Length;
                Console.WriteLine($"✓ RootDirectory found: {rootDir}");
                Console.WriteLine($"  Found {fileCount} files in directory tree");
                logger?.Information("RootDirectory valid: {Path}, {Count} files", rootDir, fileCount);
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"⚠ RootDirectory found but not accessible: {rootDir}");
                Console.WriteLine($"  Next steps:");
                Console.WriteLine($"    1. Check file permissions on the directory");
                Console.WriteLine($"    2. Run as administrator if needed");
                warnings++;
            }
        }
        else
        {
            Console.WriteLine($"✗ RootDirectory not found: {rootDir}");
            Console.WriteLine($"  Next steps:");
            Console.WriteLine($"    1. Create the directory: mkdir \"{rootDir}\"");
            Console.WriteLine($"    2. Or update config.json to point to your COBOL source directory");
            Console.WriteLine($"    3. Example: \"RootDirectory\": \"C:\\\\MyCompany\\\\Source\"");
            errors++;
        }

        // Check RulesFile
        var rulesPath = Path.Combine(AppContext.BaseDirectory, config.RulesFile);
        if (File.Exists(rulesPath))
        {
            var size = new FileInfo(rulesPath).Length;
            Console.WriteLine($"✓ RulesFile found: {config.RulesFile} ({size} bytes)");
            logger?.Debug("RulesFile exists: {Path}, {Size} bytes", rulesPath, size);
        }
        else
        {
            Console.WriteLine($"⚠ RulesFile not found: {config.RulesFile}");
            Console.WriteLine($"  Note: Rules file is optional but recommended");
            Console.WriteLine($"  Next steps:");
            Console.WriteLine($"    1. Create {config.RulesFile} with your team's coding standards");
            Console.WriteLine($"    2. Or use 'cursorops rules inject' to see example format");
            warnings++;
        }

        // Check PromptsPath
        var promptsPath = Path.Combine(AppContext.BaseDirectory, config.PromptsPath);
        if (Directory.Exists(promptsPath))
        {
            var promptCount = Directory.GetFiles(promptsPath, "*.md").Length;
            Console.WriteLine($"✓ PromptsPath found: {config.PromptsPath} ({promptCount} templates)");
            logger?.Debug("PromptsPath exists: {Path}, {Count} templates", promptsPath, promptCount);

            if (promptCount == 0)
            {
                Console.WriteLine($"  ⚠ No prompt templates found in directory");
                Console.WriteLine($"  Next steps:");
                Console.WriteLine($"    1. Add .md files to {config.PromptsPath}");
                Console.WriteLine($"    2. Use 'cursorops prompt list' to verify");
                warnings++;
            }
        }
        else
        {
            Console.WriteLine($"⚠ PromptsPath not found: {config.PromptsPath}");
            Console.WriteLine($"  Note: Prompt templates are optional");
            Console.WriteLine($"  Next steps:");
            Console.WriteLine($"    1. Create directory: mkdir {config.PromptsPath}");
            Console.WriteLine($"    2. Add prompt template .md files");
            warnings++;
        }

        // Check COBOL file patterns
        Console.WriteLine($"✓ COBOL file patterns: {config.CobolFilePatterns.Count} configured");
        foreach (var pattern in config.CobolFilePatterns)
        {
            Console.WriteLine($"  - {pattern}");
        }
        logger?.Debug("COBOL patterns: {Patterns}", string.Join(", ", config.CobolFilePatterns));

        // Check Call patterns (compile validation)
        Console.WriteLine($"✓ Call patterns: {config.CallPatterns.Count} configured");
        int validPatterns = 0;
        foreach (var patternStr in config.CallPatterns)
        {
            try
            {
                // Try to compile the regex
                var regex = new Regex(patternStr, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));

                // Check for required capture group
                if (!regex.GetGroupNames().Contains("prog"))
                {
                    Console.WriteLine($"  ✗ Pattern missing 'prog' capture group: {patternStr}");
                    Console.WriteLine($"    Next steps:");
                    Console.WriteLine($"      1. Add (?<prog>...) capture group to pattern");
                    Console.WriteLine($"      2. Example: CALL\\\\s+['\"](?<prog>\\\\w+)['\"]");
                    errors++;
                }
                else
                {
                    validPatterns++;
                    Console.WriteLine($"  ✓ {patternStr}");
                }
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"  ✗ Invalid regex pattern: {patternStr}");
                Console.WriteLine($"    Error: {ex.Message}");
                Console.WriteLine($"    Next steps:");
                Console.WriteLine($"      1. Fix the regex syntax error");
                Console.WriteLine($"      2. Test regex at https://regex101.com");
                Console.WriteLine($"      3. Ensure proper escaping for JSON: \\\\ for backslash");
                errors++;
            }
        }

        if (validPatterns == 0 && config.CallPatterns.Count > 0)
        {
            Console.WriteLine($"  ⚠ No valid call patterns! COBOL call extraction will not work.");
            warnings++;
        }

        // Check file size limits
        Console.WriteLine($"✓ File handling settings:");
        Console.WriteLine($"  - MaxFileLines: {config.MaxFileLines}");
        Console.WriteLine($"  - TrimHeadLines: {config.TrimHeadLines}");
        Console.WriteLine($"  - TrimTailLines: {config.TrimTailLines}");

        // Validate trim settings
        if (config.TrimHeadLines + config.TrimTailLines > config.MaxFileLines)
        {
            Console.WriteLine($"  ⚠ TrimHeadLines + TrimTailLines ({config.TrimHeadLines + config.TrimTailLines}) > MaxFileLines ({config.MaxFileLines})");
            Console.WriteLine($"    This may cause issues with file truncation");
            Console.WriteLine($"    Next steps:");
            Console.WriteLine($"      1. Increase MaxFileLines, or");
            Console.WriteLine($"      2. Decrease TrimHeadLines/TrimTailLines");
            warnings++;
        }

        // Summary
        Console.WriteLine();
        Console.WriteLine("─".PadRight(60, '─'));

        if (errors == 0 && warnings == 0)
        {
            ConsoleHelper.WriteSuccess("✓ Configuration valid! All checks passed.");
            Console.WriteLine();
            Console.WriteLine("Next steps:");
            Console.WriteLine("  1. Run 'cursorops cobol trace --entry YOURPROGRAM' to test COBOL analysis");
            Console.WriteLine("  2. Run 'cursorops context pack --files yourfile.cbl' to test context generation");
            Console.WriteLine("  3. See README.md for more examples");
            logger?.Information("Configuration validation passed: 0 errors, 0 warnings");
        }
        else if (errors == 0)
        {
            ConsoleHelper.WriteWarning($"Configuration has {warnings} warning(s).");
            Console.WriteLine("Tool will work but some features may be limited.");
            Console.WriteLine();
            Console.WriteLine("Review warnings above and consider fixing them.");
            logger?.Warning("Configuration validation: 0 errors, {Warnings} warnings", warnings);
        }
        else
        {
            ConsoleHelper.WriteError($"Configuration has {errors} error(s) and {warnings} warning(s).");
            Console.WriteLine("Fix errors above before running commands.");
            Console.WriteLine();
            Console.WriteLine("Need help? See TROUBLESHOOTING.md or run with --verbose for debug logs");
            logger?.Error(null, "Configuration validation failed: {Errors} errors, {Warnings} warnings", errors, warnings);
        }

        // Show log file location if available
        LoggingHelper.ShowLogLocation();
        Console.WriteLine();
    }

    /// <summary>
    /// Handler for "config show" command.
    /// Displays current configuration in readable format.
    /// </summary>
    private static void HandleConfigShow(CursorOpsConfig config)
    {
        var logger = LoggingHelper.Logger;
        logger?.Information("Displaying configuration");

        Console.WriteLine();
        ConsoleHelper.WriteInfo("Current Configuration:");
        Console.WriteLine();

        Console.WriteLine($"Root Directory:");
        Console.WriteLine($"  Configured: {config.RootDirectory}");
        Console.WriteLine($"  Resolved:   {config.ResolvePath(config.RootDirectory)}");
        Console.WriteLine();

        Console.WriteLine($"Files and Paths:");
        Console.WriteLine($"  RulesFile:   {config.RulesFile}");
        Console.WriteLine($"  PromptsPath: {config.PromptsPath}");
        Console.WriteLine();

        Console.WriteLine($"File Processing:");
        Console.WriteLine($"  MaxFileLines:  {config.MaxFileLines} lines");
        Console.WriteLine($"  TrimHeadLines: {config.TrimHeadLines} lines");
        Console.WriteLine($"  TrimTailLines: {config.TrimTailLines} lines");
        Console.WriteLine();

        Console.WriteLine($"COBOL File Patterns ({config.CobolFilePatterns.Count}):");
        foreach (var pattern in config.CobolFilePatterns)
        {
            Console.WriteLine($"  - {pattern}");
        }
        Console.WriteLine();

        Console.WriteLine($"COBOL Call Patterns ({config.CallPatterns.Count}):");
        foreach (var pattern in config.CallPatterns)
        {
            Console.WriteLine($"  - {pattern}");
        }
        Console.WriteLine();

        Console.WriteLine("To validate this configuration, run:");
        Console.WriteLine("  cursorops config validate");
        Console.WriteLine();

        logger?.Debug("Configuration displayed successfully");
    }
}
