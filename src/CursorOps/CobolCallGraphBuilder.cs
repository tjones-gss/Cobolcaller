using System.Text.Json;
using System.Text.RegularExpressions;

namespace CursorOps;

/// <summary>
/// Represents a node in the COBOL call graph.
/// Each node corresponds to a COBOL program with metadata about location and calls.
/// </summary>
public class CallGraphNode
{
    /// <summary>
    /// Program name (PROGRAM-ID) as identified in COBOL source.
    /// </summary>
    public string ProgramName { get; set; } = string.Empty;

    /// <summary>
    /// Full file path to the COBOL source file.
    /// Null if program file was not found during tracing.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// List of program names called by this program.
    /// Extracted via regex matching CALL statements.
    /// </summary>
    public List<string> Calls { get; set; } = new();

    /// <summary>
    /// Depth level in the call graph starting from entry point (0 = entry).
    /// Used to limit traversal depth.
    /// </summary>
    public int Depth { get; set; }
}

/// <summary>
/// Builds a static call graph for COBOL programs using regex-based pattern matching.
/// Performs breadth-first traversal starting from an entry program up to a specified depth.
/// </summary>
public class CobolCallGraphBuilder
{
    private readonly CursorOpsConfig _config;
    private readonly Dictionary<string, CallGraphNode> _graph = new();
    private readonly Dictionary<string, string> _programToFile = new(); // Maps PROGRAM-ID to file path

    /// <summary>
    /// Initializes the builder with configuration settings.
    /// </summary>
    /// <param name="config">Configuration containing COBOL patterns and call regex patterns</param>
    public CobolCallGraphBuilder(CursorOpsConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Builds the complete call graph starting from an entry program.
    /// Uses breadth-first search to traverse call relationships up to maxDepth.
    /// </summary>
    /// <param name="entryProgram">Name of the starting COBOL program (PROGRAM-ID)</param>
    /// <param name="maxDepth">Maximum depth to traverse (0 = entry only, 1 = entry + direct calls, etc.)</param>
    /// <returns>Dictionary of program names to CallGraphNode objects</returns>
    public Dictionary<string, CallGraphNode> BuildGraph(string entryProgram, int maxDepth)
    {
        // Index all COBOL files to map PROGRAM-ID to file paths
        IndexCobolFiles();

        // Breadth-first search queue: (program name, current depth)
        var queue = new Queue<(string program, int depth)>();
        queue.Enqueue((entryProgram, 0));

        var visited = new HashSet<string>();

        // Process queue until empty or max depth exceeded
        while (queue.Count > 0)
        {
            var (currentProgram, currentDepth) = queue.Dequeue();

            // Skip if already processed or depth exceeded
            if (visited.Contains(currentProgram) || currentDepth > maxDepth)
                continue;

            visited.Add(currentProgram);

            // Create or retrieve node for current program
            if (!_graph.ContainsKey(currentProgram))
            {
                _graph[currentProgram] = new CallGraphNode
                {
                    ProgramName = currentProgram,
                    Depth = currentDepth,
                    FilePath = _programToFile.GetValueOrDefault(currentProgram)
                };
            }

            // Extract calls from program source if file exists
            var node = _graph[currentProgram];
            if (node.FilePath != null && File.Exists(node.FilePath))
            {
                var calls = ExtractCalls(node.FilePath);
                node.Calls = calls;

                // Enqueue called programs for next level if depth allows
                if (currentDepth < maxDepth)
                {
                    foreach (var call in calls)
                    {
                        if (!visited.Contains(call))
                        {
                            queue.Enqueue((call, currentDepth + 1));
                        }
                    }
                }
            }
        }

        return _graph;
    }

    /// <summary>
    /// Indexes all COBOL files in the root directory to build PROGRAM-ID to file path mapping.
    /// Searches for files matching CobolFilePatterns and extracts PROGRAM-ID from each file.
    /// </summary>
    private void IndexCobolFiles()
    {
        var rootDir = _config.ResolvePath(_config.RootDirectory);

        if (!Directory.Exists(rootDir))
        {
            ConsoleHelper.WriteWarning($"Root directory not found: {rootDir}");
            return;
        }

        // Search for all COBOL files matching configured patterns
        foreach (var pattern in _config.CobolFilePatterns)
        {
            var files = Directory.GetFiles(rootDir, pattern, SearchOption.AllDirectories);

            foreach (var file in files)
            {
                // Extract PROGRAM-ID from each file
                var programId = ExtractProgramId(file);
                if (!string.IsNullOrEmpty(programId))
                {
                    // Store mapping (later entries overwrite earlier ones if duplicate PROGRAM-IDs exist)
                    _programToFile[programId] = file;
                }
            }
        }

        ConsoleHelper.WriteInfo($"Indexed {_programToFile.Count} COBOL programs in {rootDir}");
    }

    /// <summary>
    /// Extracts the PROGRAM-ID from a COBOL source file.
    /// Searches for lines matching "PROGRAM-ID. <name>" pattern.
    /// </summary>
    /// <param name="filePath">Path to COBOL source file</param>
    /// <returns>Program ID if found, otherwise null</returns>
    private string? ExtractProgramId(string filePath)
    {
        try
        {
            // Regex pattern to match PROGRAM-ID declarations
            // Handles variations: "PROGRAM-ID. PROGNAME.", "PROGRAM-ID IS PROGNAME", etc.
            var programIdPattern = new Regex(@"PROGRAM-ID\.\s+(?:IS\s+)?(?<prog>\w+)", RegexOptions.IgnoreCase);

            foreach (var line in File.ReadLines(filePath))
            {
                var match = programIdPattern.Match(line);
                if (match.Success)
                {
                    return match.Groups["prog"].Value;
                }
            }
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteWarning($"Failed to read {filePath}: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Extracts all CALL statements from a COBOL source file using configured regex patterns.
    /// Returns a list of unique called program names.
    /// </summary>
    /// <param name="filePath">Path to COBOL source file</param>
    /// <returns>List of program names called by this file</returns>
    private List<string> ExtractCalls(string filePath)
    {
        var calls = new HashSet<string>();

        try
        {
            var content = File.ReadAllText(filePath);

            // Apply each configured call pattern
            foreach (var patternStr in _config.CallPatterns)
            {
                var pattern = new Regex(patternStr, RegexOptions.IgnoreCase);
                var matches = pattern.Matches(content);

                foreach (Match match in matches)
                {
                    // Extract program name from named capture group "prog"
                    if (match.Groups["prog"].Success)
                    {
                        calls.Add(match.Groups["prog"].Value);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteWarning($"Failed to extract calls from {filePath}: {ex.Message}");
        }

        return calls.ToList();
    }

    /// <summary>
    /// Serializes the call graph to JSON format.
    /// Creates a compact adjacency list representation with program metadata.
    /// </summary>
    /// <returns>JSON string representation of the call graph</returns>
    public string ToJson()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true, // Pretty print for readability
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(_graph.Values, options);
    }

    /// <summary>
    /// Generates a Markdown representation of the call graph.
    /// Creates an indented hierarchical view suitable for documentation.
    /// </summary>
    /// <returns>Markdown-formatted call graph</returns>
    public string ToMarkdown()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# COBOL Call Graph");
        sb.AppendLine();

        // Sort nodes by depth then by name for consistent output
        var sortedNodes = _graph.Values
            .OrderBy(n => n.Depth)
            .ThenBy(n => n.ProgramName);

        foreach (var node in sortedNodes)
        {
            // Indent based on depth
            var indent = new string(' ', node.Depth * 2);

            // Node header with program name
            sb.AppendLine($"{indent}- **{node.ProgramName}** (Depth: {node.Depth})");

            // File path if known
            if (node.FilePath != null)
            {
                sb.AppendLine($"{indent}  - File: `{node.FilePath}`");
            }
            else
            {
                sb.AppendLine($"{indent}  - File: *Not found*");
            }

            // List of calls
            if (node.Calls.Count > 0)
            {
                sb.AppendLine($"{indent}  - Calls: {string.Join(", ", node.Calls)}");
            }
            else
            {
                sb.AppendLine($"{indent}  - Calls: *None*");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates a simple text tree representation of the call graph.
    /// Uses ASCII tree characters for hierarchical visualization.
    /// </summary>
    /// <param name="entryProgram">Root program to start tree from</param>
    /// <returns>ASCII tree representation</returns>
    public string ToTree(string entryProgram)
    {
        var sb = new System.Text.StringBuilder();
        var visited = new HashSet<string>();

        void RenderNode(string programName, string prefix, bool isLast)
        {
            // Prevent infinite recursion on circular calls
            if (visited.Contains(programName))
            {
                sb.AppendLine($"{prefix}└── {programName} (circular)");
                return;
            }

            visited.Add(programName);

            // Get node from graph
            if (!_graph.TryGetValue(programName, out var node))
            {
                sb.AppendLine($"{prefix}└── {programName} (not found)");
                return;
            }

            // Render current node
            var connector = isLast ? "└── " : "├── ";
            var fileInfo = node.FilePath != null ? $" [{Path.GetFileName(node.FilePath)}]" : " [not found]";
            sb.AppendLine($"{prefix}{connector}{node.ProgramName}{fileInfo}");

            // Render children
            var childPrefix = prefix + (isLast ? "    " : "│   ");
            var calls = node.Calls;

            for (int i = 0; i < calls.Count; i++)
            {
                bool isLastChild = (i == calls.Count - 1);
                RenderNode(calls[i], childPrefix, isLastChild);
            }
        }

        RenderNode(entryProgram, "", true);
        return sb.ToString();
    }

    /// <summary>
    /// Returns statistics about the call graph.
    /// </summary>
    public string GetStatistics()
    {
        var totalPrograms = _graph.Count;
        var foundPrograms = _graph.Values.Count(n => n.FilePath != null);
        var missingPrograms = totalPrograms - foundPrograms;
        var totalCalls = _graph.Values.Sum(n => n.Calls.Count);
        var maxDepth = _graph.Values.Any() ? _graph.Values.Max(n => n.Depth) : 0;

        return $@"
Call Graph Statistics:
  Total Programs: {totalPrograms}
  Found Programs: {foundPrograms}
  Missing Programs: {missingPrograms}
  Total CALL Statements: {totalCalls}
  Maximum Depth: {maxDepth}
";
    }
}
