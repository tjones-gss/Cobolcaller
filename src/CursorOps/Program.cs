using System.CommandLine;
using System.Text;

namespace CursorOps;

/// <summary>
/// Main entry point for CursorOps CLI tool.
/// Provides slash-style commands for context packing, COBOL tracing, and prompt/rule management.
/// </summary>
class Program
{
    // Non-nullable config - guaranteed to be initialized before use
    private static CursorOpsConfig _config = null!;

    static async Task<int> Main(string[] args)
    {
        // Load configuration from default location
        var configPath = Path.Combine(AppContext.BaseDirectory, "config", "cursorops.json");
        _config = CursorOpsConfig.Load(configPath);

        // Verify configuration loaded successfully
        if (_config == null)
        {
            ConsoleHelper.WriteError("Failed to initialize configuration");
            return 1;
        }

        // Create root command
        var rootCommand = new RootCommand("CursorOps - Local context engineering and COBOL call-graph analysis for Cursor editor");

        // Add all subcommands
        rootCommand.AddCommand(CreateRulesCommand());
        rootCommand.AddCommand(CreatePromptCommand());
        rootCommand.AddCommand(CreateContextCommand());
        rootCommand.AddCommand(CreateCobolCommand());
        rootCommand.AddCommand(CreateDemoCommand());

        // Execute command line
        return await rootCommand.InvokeAsync(args);
    }

    #region Rules Command

    /// <summary>
    /// Creates the "rules" command group for managing team development rules.
    /// </summary>
    private static Command CreateRulesCommand()
    {
        var rulesCommand = new Command("rules", "Manage team development rules");

        // rules inject - Output the team rules document
        var injectCommand = new Command("inject", "Output team rules to console or file")
        {
            new Option<string?>(
                aliases: new[] { "--out", "-o" },
                description: "Output file path (default: stdout)")
        };

        injectCommand.SetHandler((string? outputPath) =>
        {
            HandleRulesInject(outputPath);
        }, injectCommand.Options.OfType<Option<string?>>().First());

        rulesCommand.AddCommand(injectCommand);
        return rulesCommand;
    }

    /// <summary>
    /// Handler for "rules inject" command.
    /// Reads and outputs the team rules Markdown file.
    /// </summary>
    private static void HandleRulesInject(string? outputPath)
    {
        var rulesPath = Path.Combine(AppContext.BaseDirectory, _config.RulesFile);

        if (!File.Exists(rulesPath))
        {
            ConsoleHelper.WriteError($"Rules file not found: {rulesPath}");
            return;
        }

        string content;
        try
        {
            content = File.ReadAllText(rulesPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            ConsoleHelper.WriteError($"Permission denied reading rules file: {ex.Message}");
            return;
        }
        catch (IOException ex)
        {
            ConsoleHelper.WriteError($"Failed to read rules file: {ex.Message}");
            return;
        }

        if (string.IsNullOrEmpty(outputPath))
        {
            // Output to console
            Console.WriteLine(content);
        }
        else
        {
            // Write to file with security validation
            try
            {
                var safePath = SecurityHelper.ValidateOutputPath(outputPath, Directory.GetCurrentDirectory());
                File.WriteAllText(safePath, content);
                ConsoleHelper.WriteSuccess($"Rules written to: {safePath}");
            }
            catch (System.Security.SecurityException ex)
            {
                ConsoleHelper.WriteError($"Security error: {ex.Message}");
                return;
            }
            catch (IOException ex)
            {
                ConsoleHelper.WriteError($"Failed to write file: {ex.Message}");
                return;
            }
            catch (UnauthorizedAccessException ex)
            {
                ConsoleHelper.WriteError($"Permission denied: {ex.Message}");
                return;
            }
        }
    }

    #endregion

    #region Prompt Command

    /// <summary>
    /// Creates the "prompt" command group for managing reusable prompt templates.
    /// </summary>
    private static Command CreatePromptCommand()
    {
        var promptCommand = new Command("prompt", "Manage prompt templates");

        // prompt list - List all available prompts
        var listCommand = new Command("list", "List all available prompt templates");
        listCommand.SetHandler(HandlePromptList);

        // prompt pick - Output a specific prompt
        var pickCommand = new Command("pick", "Output content of a specific prompt template")
        {
            new Argument<string>("name", "Name of the prompt template (without .md extension)"),
            new Option<string?>(
                aliases: new[] { "--out", "-o" },
                description: "Output file path (default: stdout)")
        };

        pickCommand.SetHandler((string name, string? outputPath) =>
        {
            HandlePromptPick(name, outputPath);
        },
        pickCommand.Arguments.OfType<Argument<string>>().First(),
        pickCommand.Options.OfType<Option<string?>>().First());

        promptCommand.AddCommand(listCommand);
        promptCommand.AddCommand(pickCommand);
        return promptCommand;
    }

    /// <summary>
    /// Handler for "prompt list" command.
    /// Lists all .md files in the prompts directory.
    /// </summary>
    private static void HandlePromptList()
    {
        var promptsPath = Path.Combine(AppContext.BaseDirectory, _config.PromptsPath);

        if (!Directory.Exists(promptsPath))
        {
            ConsoleHelper.WriteError($"Prompts directory not found: {promptsPath}");
            return;
        }

        string[] promptFiles;
        try
        {
            promptFiles = Directory.GetFiles(promptsPath, "*.md");
        }
        catch (UnauthorizedAccessException ex)
        {
            ConsoleHelper.WriteError($"Permission denied accessing prompts directory: {ex.Message}");
            return;
        }
        catch (IOException ex)
        {
            ConsoleHelper.WriteError($"Failed to read prompts directory: {ex.Message}");
            return;
        }

        if (promptFiles.Length == 0)
        {
            ConsoleHelper.WriteWarning("No prompt templates found.");
            return;
        }

        ConsoleHelper.WriteInfo("Available prompt templates:");
        foreach (var file in promptFiles)
        {
            var name = Path.GetFileNameWithoutExtension(file);
            Console.WriteLine($"  - {name}");
        }
    }

    /// <summary>
    /// Handler for "prompt pick" command.
    /// Outputs the content of a specific prompt template.
    /// </summary>
    private static void HandlePromptPick(string name, string? outputPath)
    {
        // Validate name to prevent path traversal
        try
        {
            name = SecurityHelper.ValidateInputName(name);
        }
        catch (System.Security.SecurityException ex)
        {
            ConsoleHelper.WriteError($"Invalid prompt name: {ex.Message}");
            return;
        }

        var promptsPath = Path.Combine(AppContext.BaseDirectory, _config.PromptsPath);
        var promptFile = Path.Combine(promptsPath, $"{name}.md");

        // Verify the resolved path is within the prompts directory
        if (!SecurityHelper.IsPathSafe(promptFile, promptsPath))
        {
            ConsoleHelper.WriteError("Invalid prompt name: path traversal detected");
            return;
        }

        if (!File.Exists(promptFile))
        {
            ConsoleHelper.WriteError($"Prompt template not found: {name}");
            ConsoleHelper.WriteInfo("Use 'cursorops prompt list' to see available templates.");
            return;
        }

        string content;
        try
        {
            content = File.ReadAllText(promptFile);
        }
        catch (UnauthorizedAccessException ex)
        {
            ConsoleHelper.WriteError($"Permission denied reading prompt file: {ex.Message}");
            return;
        }
        catch (IOException ex)
        {
            ConsoleHelper.WriteError($"Failed to read prompt file: {ex.Message}");
            return;
        }

        if (string.IsNullOrEmpty(outputPath))
        {
            // Output to console
            Console.WriteLine(content);
        }
        else
        {
            // Write to file with security validation
            try
            {
                var safePath = SecurityHelper.ValidateOutputPath(outputPath, Directory.GetCurrentDirectory());
                File.WriteAllText(safePath, content);
                ConsoleHelper.WriteSuccess($"Prompt written to: {safePath}");
            }
            catch (System.Security.SecurityException ex)
            {
                ConsoleHelper.WriteError($"Security error: {ex.Message}");
                return;
            }
            catch (IOException ex)
            {
                ConsoleHelper.WriteError($"Failed to write file: {ex.Message}");
                return;
            }
            catch (UnauthorizedAccessException ex)
            {
                ConsoleHelper.WriteError($"Permission denied: {ex.Message}");
                return;
            }
        }
    }

    #endregion

    #region Context Command

    /// <summary>
    /// Creates the "context" command group for packaging context files.
    /// </summary>
    private static Command CreateContextCommand()
    {
        var contextCommand = new Command("context", "Package context for Cursor editor");

        // context pack - Combine files + rules + graph into Markdown
        var packCommand = new Command("pack", "Combine files, rules, and optional call graph into Markdown context")
        {
            new Option<string[]>(
                aliases: new[] { "--files", "-f" },
                description: "Source files to include in context")
            {
                IsRequired = true,
                AllowMultipleArgumentsPerToken = true
            },
            new Option<string?>(
                aliases: new[] { "--graph", "-g" },
                description: "Optional call graph JSON file to include"),
            new Option<string?>(
                aliases: new[] { "--out", "-o" },
                description: "Output file path (default: stdout)")
        };

        packCommand.SetHandler((string[] files, string? graphPath, string? outputPath) =>
        {
            HandleContextPack(files, graphPath, outputPath);
        },
        packCommand.Options.OfType<Option<string[]>>().First(),
        packCommand.Options.OfType<Option<string?>>().First(),
        packCommand.Options.OfType<Option<string?>>().Skip(1).First());

        contextCommand.AddCommand(packCommand);
        return contextCommand;
    }

    /// <summary>
    /// Handler for "context pack" command.
    /// Combines source files, rules, and optional call graph into a Markdown context document.
    /// </summary>
    private static void HandleContextPack(string[] files, string? graphPath, string? outputPath)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("# Context Package");
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        // Include team rules
        var rulesPath = Path.Combine(AppContext.BaseDirectory, _config.RulesFile);
        if (File.Exists(rulesPath))
        {
            try
            {
                sb.AppendLine("## Team Rules");
                sb.AppendLine();
                sb.AppendLine(File.ReadAllText(rulesPath));
                sb.AppendLine();
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                ConsoleHelper.WriteWarning($"Failed to read rules file: {ex.Message}");
            }
        }

        // Include call graph if provided
        if (!string.IsNullOrEmpty(graphPath) && File.Exists(graphPath))
        {
            try
            {
                sb.AppendLine("## Call Graph");
                sb.AppendLine();
                sb.AppendLine("```json");
                sb.AppendLine(File.ReadAllText(graphPath));
                sb.AppendLine("```");
                sb.AppendLine();
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                ConsoleHelper.WriteWarning($"Failed to read call graph file: {ex.Message}");
            }
        }

        // Include source files
        sb.AppendLine("## Source Files");
        sb.AppendLine();

        foreach (var file in files)
        {
            if (!File.Exists(file))
            {
                ConsoleHelper.WriteWarning($"File not found: {file}");
                continue;
            }

            try
            {
                sb.AppendLine($"### {Path.GetFileName(file)}");
                sb.AppendLine($"Path: `{file}`");
                sb.AppendLine();

                var lines = File.ReadAllLines(file);

            // Check if truncation is needed
            if (lines.Length > _config.MaxFileLines)
            {
                // Calculate safe bounds to prevent index out of range errors
                int actualHeadLines = Math.Min(_config.TrimHeadLines, lines.Length);
                int actualTailLines = Math.Min(_config.TrimTailLines, lines.Length - actualHeadLines);

                sb.AppendLine($"*File truncated: {lines.Length} lines (showing first {actualHeadLines} and last {actualTailLines})*");
                sb.AppendLine();
                sb.AppendLine("```");

                // Head lines (safe)
                for (int i = 0; i < actualHeadLines; i++)
                {
                    sb.AppendLine(lines[i]);
                }

                int omittedLines = lines.Length - actualHeadLines - actualTailLines;
                if (omittedLines > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"... ({omittedLines} lines omitted) ...");
                    sb.AppendLine();
                }

                // Tail lines (safe)
                int tailStart = Math.Max(actualHeadLines, lines.Length - actualTailLines);
                for (int i = tailStart; i < lines.Length; i++)
                {
                    sb.AppendLine(lines[i]);
                }

                sb.AppendLine("```");
            }
            else
            {
                // Include full file
                var extension = Path.GetExtension(file).TrimStart('.');
                sb.AppendLine($"```{extension}");
                sb.AppendLine(File.ReadAllText(file));
                sb.AppendLine("```");
            }

                sb.AppendLine();
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                ConsoleHelper.WriteWarning($"Failed to read source file {file}: {ex.Message}");
                sb.AppendLine($"*Error reading file: {ex.Message}*");
                sb.AppendLine();
            }
        }

        var output = sb.ToString();

        if (string.IsNullOrEmpty(outputPath))
        {
            // Output to console
            Console.WriteLine(output);
        }
        else
        {
            // Write to file with security validation
            try
            {
                var safePath = SecurityHelper.ValidateOutputPath(outputPath, Directory.GetCurrentDirectory());
                File.WriteAllText(safePath, output);
                ConsoleHelper.WriteSuccess($"Context package written to: {safePath}");
            }
            catch (System.Security.SecurityException ex)
            {
                ConsoleHelper.WriteError($"Security error: {ex.Message}");
                return;
            }
            catch (IOException ex)
            {
                ConsoleHelper.WriteError($"Failed to write file: {ex.Message}");
                return;
            }
            catch (UnauthorizedAccessException ex)
            {
                ConsoleHelper.WriteError($"Permission denied: {ex.Message}");
                return;
            }
        }
    }

    #endregion

    #region COBOL Command

    /// <summary>
    /// Creates the "cobol" command group for COBOL call graph analysis.
    /// </summary>
    private static Command CreateCobolCommand()
    {
        var cobolCommand = new Command("cobol", "COBOL call graph analysis");

        // cobol trace - Generate call graph
        var traceCommand = new Command("trace", "Trace COBOL call graph from entry program")
        {
            new Option<string>(
                aliases: new[] { "--entry", "-e" },
                description: "Entry program name (PROGRAM-ID)")
            {
                IsRequired = true
            },
            new Option<int>(
                aliases: new[] { "--depth", "-d" },
                description: "Maximum depth to trace",
                getDefaultValue: () => 2),
            new Option<string?>(
                aliases: new[] { "--graph", "-g" },
                description: "Output call graph JSON file"),
            new Option<bool>(
                aliases: new[] { "--tree", "-t" },
                description: "Show ASCII tree view",
                getDefaultValue: () => false)
        };

        traceCommand.SetHandler((string entry, int depth, string? graphPath, bool showTree) =>
        {
            HandleCobolTrace(entry, depth, graphPath, showTree);
        },
        traceCommand.Options.OfType<Option<string>>().First(),
        traceCommand.Options.OfType<Option<int>>().First(),
        traceCommand.Options.OfType<Option<string?>>().First(),
        traceCommand.Options.OfType<Option<bool>>().First());

        // cobol focus - Generate full context for a COBOL program
        var focusCommand = new Command("focus", "Generate Markdown context for a COBOL program and its call graph")
        {
            new Option<string>(
                aliases: new[] { "--entry", "-e" },
                description: "Entry program name (PROGRAM-ID)")
            {
                IsRequired = true
            },
            new Option<int>(
                aliases: new[] { "--depth", "-d" },
                description: "Maximum depth to trace",
                getDefaultValue: () => 2),
            new Option<string?>(
                aliases: new[] { "--out", "-o" },
                description: "Output Markdown file (default: <PROGRAM>_context.md)")
        };

        focusCommand.SetHandler((string entry, int depth, string? outputPath) =>
        {
            HandleCobolFocus(entry, depth, outputPath);
        },
        focusCommand.Options.OfType<Option<string>>().First(),
        focusCommand.Options.OfType<Option<int>>().First(),
        focusCommand.Options.OfType<Option<string?>>().First());

        cobolCommand.AddCommand(traceCommand);
        cobolCommand.AddCommand(focusCommand);
        return cobolCommand;
    }

    /// <summary>
    /// Handler for "cobol trace" command.
    /// Builds and outputs a COBOL call graph.
    /// </summary>
    private static void HandleCobolTrace(string entry, int depth, string? graphPath, bool showTree)
    {
        ConsoleHelper.WriteInfo($"Tracing COBOL call graph from '{entry}' (depth: {depth})...");

        var builder = new CobolCallGraphBuilder(_config);
        var graph = builder.BuildGraph(entry, depth);

        if (graph.Count == 0)
        {
            ConsoleHelper.WriteWarning($"No programs found in call graph for '{entry}'");
            return;
        }

        // Output statistics
        Console.WriteLine(builder.GetStatistics());

        // Output tree view if requested
        if (showTree)
        {
            ConsoleHelper.WriteInfo("Call Tree:");
            Console.WriteLine(builder.ToTree(entry));
        }

        // Save JSON graph if path provided
        if (!string.IsNullOrEmpty(graphPath))
        {
            try
            {
                var safePath = SecurityHelper.ValidateOutputPath(graphPath, Directory.GetCurrentDirectory());
                var json = builder.ToJson();
                File.WriteAllText(safePath, json);
                ConsoleHelper.WriteSuccess($"Call graph JSON written to: {safePath}");
            }
            catch (System.Security.SecurityException ex)
            {
                ConsoleHelper.WriteError($"Security error: {ex.Message}");
                return;
            }
            catch (IOException ex)
            {
                ConsoleHelper.WriteError($"Failed to write file: {ex.Message}");
                return;
            }
            catch (UnauthorizedAccessException ex)
            {
                ConsoleHelper.WriteError($"Permission denied: {ex.Message}");
                return;
            }
        }
        else
        {
            // Output JSON to console if no file specified
            Console.WriteLine(builder.ToJson());
        }
    }

    /// <summary>
    /// Handler for "cobol focus" command.
    /// Generates a complete Markdown context package for a COBOL program including call graph.
    /// </summary>
    private static void HandleCobolFocus(string entry, int depth, string? outputPath)
    {
        ConsoleHelper.WriteInfo($"Generating context for COBOL program '{entry}' (depth: {depth})...");

        var builder = new CobolCallGraphBuilder(_config);
        var graph = builder.BuildGraph(entry, depth);

        if (graph.Count == 0)
        {
            ConsoleHelper.WriteWarning($"No programs found in call graph for '{entry}'");
            return;
        }

        var sb = new StringBuilder();

        // Header
        sb.AppendLine($"# COBOL Focus: {entry}");
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        // Include team rules
        var rulesPath = Path.Combine(AppContext.BaseDirectory, _config.RulesFile);
        if (File.Exists(rulesPath))
        {
            try
            {
                sb.AppendLine("## Team Rules");
                sb.AppendLine();
                sb.AppendLine(File.ReadAllText(rulesPath));
                sb.AppendLine();
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                ConsoleHelper.WriteWarning($"Failed to read rules file: {ex.Message}");
            }
        }

        // Include call graph statistics
        sb.AppendLine("## Call Graph Statistics");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine(builder.GetStatistics());
        sb.AppendLine("```");
        sb.AppendLine();

        // Include call tree
        sb.AppendLine("## Call Tree");
        sb.AppendLine();
        sb.AppendLine("```");
        sb.AppendLine(builder.ToTree(entry));
        sb.AppendLine("```");
        sb.AppendLine();

        // Include source files for all programs in graph
        sb.AppendLine("## Source Files");
        sb.AppendLine();

        foreach (var node in graph.Values.OrderBy(n => n.Depth).ThenBy(n => n.ProgramName))
        {
            if (node.FilePath == null || !File.Exists(node.FilePath))
            {
                sb.AppendLine($"### {node.ProgramName} (Not Found)");
                sb.AppendLine();
                continue;
            }

            try
            {
                sb.AppendLine($"### {node.ProgramName}");
                sb.AppendLine($"- File: `{node.FilePath}`");
                sb.AppendLine($"- Depth: {node.Depth}");
                sb.AppendLine($"- Calls: {(node.Calls.Count > 0 ? string.Join(", ", node.Calls) : "None")}");
                sb.AppendLine();

                var lines = File.ReadAllLines(node.FilePath);

            if (lines.Length > _config.MaxFileLines)
            {
                // Calculate safe bounds to prevent index out of range errors
                int actualHeadLines = Math.Min(_config.TrimHeadLines, lines.Length);
                int actualTailLines = Math.Min(_config.TrimTailLines, lines.Length - actualHeadLines);

                sb.AppendLine($"*File truncated: {lines.Length} lines (showing first {actualHeadLines} and last {actualTailLines})*");
                sb.AppendLine();
                sb.AppendLine("```cobol");

                // Head lines (safe)
                for (int i = 0; i < actualHeadLines; i++)
                {
                    sb.AppendLine(lines[i]);
                }

                int omittedLines = lines.Length - actualHeadLines - actualTailLines;
                if (omittedLines > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine($"... ({omittedLines} lines omitted) ...");
                    sb.AppendLine();
                }

                // Tail lines (safe)
                int tailStart = Math.Max(actualHeadLines, lines.Length - actualTailLines);
                for (int i = tailStart; i < lines.Length; i++)
                {
                    sb.AppendLine(lines[i]);
                }

                sb.AppendLine("```");
            }
            else
            {
                sb.AppendLine("```cobol");
                sb.AppendLine(File.ReadAllText(node.FilePath));
                sb.AppendLine("```");
            }

                sb.AppendLine();
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                ConsoleHelper.WriteWarning($"Failed to read COBOL file {node.FilePath}: {ex.Message}");
                sb.AppendLine($"*Error reading file: {ex.Message}*");
                sb.AppendLine();
            }
        }

        var output = sb.ToString();

        // Determine output path
        var finalOutputPath = outputPath ?? $"{entry}_context.md";

        // Write to file with security validation
        try
        {
            var safePath = SecurityHelper.ValidateOutputPath(finalOutputPath, Directory.GetCurrentDirectory());
            File.WriteAllText(safePath, output);
            ConsoleHelper.WriteSuccess($"COBOL focus context written to: {safePath}");
            ConsoleHelper.WriteInfo($"Total programs included: {graph.Count}");
        }
        catch (System.Security.SecurityException ex)
        {
            ConsoleHelper.WriteError($"Security error: {ex.Message}");
            return;
        }
        catch (IOException ex)
        {
            ConsoleHelper.WriteError($"Failed to write file: {ex.Message}");
            return;
        }
        catch (UnauthorizedAccessException ex)
        {
            ConsoleHelper.WriteError($"Permission denied: {ex.Message}");
            return;
        }
    }

    #endregion

    #region Demo Command

    /// <summary>
    /// Creates the "demo" command for demonstrating tool capabilities.
    /// </summary>
    private static Command CreateDemoCommand()
    {
        var demoCommand = new Command("demo", "Show example output for all major features (no file modifications)");

        demoCommand.SetHandler(HandleDemo);

        return demoCommand;
    }

    /// <summary>
    /// Handler for "demo" command.
    /// Displays example output for all major features without modifying files.
    /// </summary>
    private static void HandleDemo()
    {
        Console.WriteLine();
        ConsoleHelper.WriteInfo("=== CursorOps Demo ===");
        Console.WriteLine();

        // Demo 1: Rules inject
        Console.WriteLine("1. Team Rules (cursorops rules inject)");
        Console.WriteLine("   ----------------------------------------");
        var rulesPath = Path.Combine(AppContext.BaseDirectory, _config.RulesFile);
        if (File.Exists(rulesPath))
        {
            try
            {
                var rulesContent = File.ReadAllText(rulesPath);
                Console.WriteLine(rulesContent.Length > 200 ? rulesContent.Substring(0, 200) + "..." : rulesContent);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                ConsoleHelper.WriteWarning($"   (Failed to read rules file: {ex.Message})");
            }
        }
        else
        {
            ConsoleHelper.WriteWarning("   (Rules file not found - create rules/rules.md)");
        }
        Console.WriteLine();

        // Demo 2: Prompt list
        Console.WriteLine("2. Prompt Templates (cursorops prompt list)");
        Console.WriteLine("   ----------------------------------------");
        var promptsPath = Path.Combine(AppContext.BaseDirectory, _config.PromptsPath);
        if (Directory.Exists(promptsPath))
        {
            try
            {
                var promptFiles = Directory.GetFiles(promptsPath, "*.md");
                if (promptFiles.Length > 0)
                {
                    foreach (var file in promptFiles)
                    {
                        Console.WriteLine($"   - {Path.GetFileNameWithoutExtension(file)}");
                    }
                }
                else
                {
                    ConsoleHelper.WriteWarning("   (No prompts found - create .md files in prompts/)");
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                ConsoleHelper.WriteWarning($"   (Failed to read prompts directory: {ex.Message})");
            }
        }
        else
        {
            ConsoleHelper.WriteWarning("   (Prompts directory not found)");
        }
        Console.WriteLine();

        // Demo 3: Context pack example
        Console.WriteLine("3. Context Pack Example");
        Console.WriteLine("   ----------------------------------------");
        Console.WriteLine("   Command: cursorops context pack --files prog1.cbl utils.cs --out context.md");
        Console.WriteLine("   Result: Combines source files + team rules into Markdown");
        Console.WriteLine();

        // Demo 4: COBOL trace example
        Console.WriteLine("4. COBOL Call Graph Example");
        Console.WriteLine("   ----------------------------------------");
        Console.WriteLine("   Command: cursorops cobol trace --entry SCR100 --depth 2 --tree");
        Console.WriteLine("   Result: ASCII tree showing CALL relationships:");
        Console.WriteLine("   └── SCR100 [scr100.cbl]");
        Console.WriteLine("       ├── UTIL001 [util001.cbl]");
        Console.WriteLine("       │   └── LOGGER [logger.cbl]");
        Console.WriteLine("       └── DBACCESS [dbaccess.cbl]");
        Console.WriteLine();

        // Demo 5: COBOL focus example
        Console.WriteLine("5. COBOL Focus Example");
        Console.WriteLine("   ----------------------------------------");
        Console.WriteLine("   Command: cursorops cobol focus --entry SCR100 --depth 2");
        Console.WriteLine("   Result: Full Markdown context with:");
        Console.WriteLine("   - Team rules");
        Console.WriteLine("   - Call graph statistics");
        Console.WriteLine("   - ASCII call tree");
        Console.WriteLine("   - Source code for all programs in graph");
        Console.WriteLine();

        ConsoleHelper.WriteSuccess("Demo complete! Use --help on any command for more details.");
        Console.WriteLine();
    }

    #endregion
}
