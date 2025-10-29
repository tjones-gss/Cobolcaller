# CursorOps Bug Review and Edge Case Analysis

**Generated:** 2025-10-29
**Reviewer:** Code Analysis
**Files Analyzed:** 3 C# source files

---

## Executive Summary

This review identified **17 distinct issues** ranging from critical runtime bugs to performance concerns and security vulnerabilities. The most severe issues involve:

- **Index out of bounds errors** when truncating files (4 instances)
- **Path traversal security vulnerability** in prompt picking
- **Regex DoS vulnerability** with user-supplied patterns
- **Missing exception handling** for file I/O operations (11+ instances)
- **Memory issues** with large file handling

**Severity Distribution:**
- **Critical:** 5 bugs
- **High:** 6 bugs
- **Medium:** 4 issues
- **Low:** 2 issues

---

## Critical Severity Issues

### BUG-001: Index Out of Bounds in File Truncation (TrimHead)
**Severity:** CRITICAL
**Location:** `/home/user/Cobolcaller/src/CursorOps/Program.cs`
- Lines 287-289 (context pack)
- Lines 522-524 (cobol focus)

**Issue:**
```csharp
// Lines 287-289
for (int i = 0; i < _config.TrimHeadLines; i++)
{
    sb.AppendLine(lines[i]);
}
```

The code assumes the file has at least `TrimHeadLines` lines. If a file has fewer lines than configured (default 50), this will throw `IndexOutOfRangeException`.

**Scenario:**
```
1. File has 30 lines
2. MaxFileLines = 500, TrimHeadLines = 50
3. File passes the > MaxFileLines check if that's lowered
4. Loop tries to access lines[0] through lines[49]
5. Crash at lines[30]
```

**Recommended Fix:**
```csharp
// Check if truncation is needed
if (lines.Length > _config.MaxFileLines)
{
    int actualHeadLines = Math.Min(_config.TrimHeadLines, lines.Length);
    int actualTailLines = Math.Min(_config.TrimTailLines, lines.Length - actualHeadLines);

    sb.AppendLine($"*File truncated: {lines.Length} lines (showing first {actualHeadLines} and last {actualTailLines})*");
    sb.AppendLine();
    sb.AppendLine("```");

    // Head
    for (int i = 0; i < actualHeadLines; i++)
    {
        sb.AppendLine(lines[i]);
    }

    sb.AppendLine();
    sb.AppendLine($"... ({lines.Length - actualHeadLines - actualTailLines} lines omitted) ...");
    sb.AppendLine();

    // Tail
    for (int i = lines.Length - actualTailLines; i < lines.Length; i++)
    {
        sb.AppendLine(lines[i]);
    }

    sb.AppendLine("```");
}
```

**Test Cases Needed:**
- File with 0 lines
- File with 1 line
- File with exactly TrimHeadLines (50) lines
- File with TrimHeadLines + TrimTailLines (100) lines
- File with TrimHeadLines + TrimTailLines + 1 (101) lines
- File with fewer lines than TrimHeadLines (e.g., 30 lines)

---

### BUG-002: Index Out of Bounds in File Truncation (TrimTail)
**Severity:** CRITICAL
**Location:** `/home/user/Cobolcaller/src/CursorOps/Program.cs`
- Lines 297-299 (context pack)
- Lines 531-533 (cobol focus)

**Issue:**
```csharp
// Lines 297-299
for (int i = lines.Length - _config.TrimTailLines; i < lines.Length; i++)
{
    sb.AppendLine(lines[i]);
}
```

If `lines.Length < TrimTailLines`, then `lines.Length - _config.TrimTailLines` becomes negative, causing `IndexOutOfRangeException`.

**Scenario:**
```
1. File has 30 lines
2. TrimTailLines = 50
3. Starting index = 30 - 50 = -20
4. Loop tries to access lines[-20], lines[-19], etc.
5. Immediate crash
```

**Recommended Fix:** See BUG-001 fix above (handles both head and tail).

**Test Cases Needed:** Same as BUG-001.

---

### BUG-003: Path Traversal Security Vulnerability
**Severity:** CRITICAL (Security)
**Location:** `/home/user/Cobolcaller/src/CursorOps/Program.cs`, Line 162

**Issue:**
```csharp
private static void HandlePromptPick(string name, string? outputPath)
{
    var promptsPath = Path.Combine(AppContext.BaseDirectory, _config!.PromptsPath);
    var promptFile = Path.Combine(promptsPath, $"{name}.md");  // VULNERABLE
```

User-supplied `name` parameter is not validated. An attacker could use path traversal to read arbitrary files:

**Attack Scenario:**
```bash
cursorops prompt pick "../../../etc/passwd"
# Attempts to read: /path/to/app/prompts/../../../etc/passwd.md

cursorops prompt pick "../../config/secrets"
# Attempts to read: /path/to/app/prompts/../../config/secrets.md
```

**Recommended Fix:**
```csharp
private static void HandlePromptPick(string name, string? outputPath)
{
    // Validate name contains no path separators
    if (name.Contains('/') || name.Contains('\\') || name.Contains(".."))
    {
        ConsoleHelper.WriteError("Invalid prompt name. Name must not contain path separators or '..'");
        return;
    }

    // Additional validation for invalid filename characters
    if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
    {
        ConsoleHelper.WriteError("Invalid prompt name. Name contains invalid characters.");
        return;
    }

    var promptsPath = Path.Combine(AppContext.BaseDirectory, _config!.PromptsPath);
    var promptFile = Path.Combine(promptsPath, $"{name}.md");

    // Verify the final path is still within prompts directory
    var fullPromptFile = Path.GetFullPath(promptFile);
    var fullPromptsPath = Path.GetFullPath(promptsPath);
    if (!fullPromptFile.StartsWith(fullPromptsPath, StringComparison.OrdinalIgnoreCase))
    {
        ConsoleHelper.WriteError("Invalid prompt name. Path traversal detected.");
        return;
    }

    if (!File.Exists(promptFile))
    {
        ConsoleHelper.WriteError($"Prompt template not found: {name}");
        ConsoleHelper.WriteInfo("Use 'cursorops prompt list' to see available templates.");
        return;
    }

    // ... rest of method
}
```

**Test Cases Needed:**
- `name = "../../../etc/passwd"`
- `name = "..\\..\\..\\windows\\system32\\config\\sam"`
- `name = "valid-prompt"`
- `name = "prompt/with/slash"`
- `name = "prompt\\with\\backslash"`
- `name = "prompt:with:colon"` (invalid on Windows)

---

### BUG-004: Regex Denial of Service (ReDoS) Vulnerability
**Severity:** CRITICAL (Security)
**Location:** `/home/user/Cobolcaller/src/CursorOps/CobolCallGraphBuilder.cs`, Line 201

**Issue:**
```csharp
foreach (var patternStr in _config.CallPatterns)
{
    var pattern = new Regex(patternStr, RegexOptions.IgnoreCase);
    var matches = pattern.Matches(content);
```

User-supplied regex patterns from config file have:
1. **No validation** - invalid patterns cause unhandled `ArgumentException`
2. **No timeout** - catastrophic backtracking could hang the application
3. **Created repeatedly** - performance issue

**Attack Scenario:**
A malicious or poorly written config could contain:
```json
{
  "CallPatterns": [
    "(a+)+b",  // Classic ReDoS pattern
    "CALL\\s+(.*)*\\s+USING"  // Catastrophic backtracking
  ]
}
```

When applied to COBOL file with malicious content, the regex engine could take minutes/hours or hang entirely.

**Recommended Fix:**
```csharp
public class CobolCallGraphBuilder
{
    private readonly CursorOpsConfig _config;
    private readonly Dictionary<string, CallGraphNode> _graph = new();
    private readonly Dictionary<string, string> _programToFile = new();
    private readonly List<Regex> _compiledCallPatterns = new(); // ADDED
    private readonly Regex _programIdPattern; // ADDED

    public CobolCallGraphBuilder(CursorOpsConfig config)
    {
        _config = config;

        // Pre-compile and validate regex patterns with timeout
        var timeout = TimeSpan.FromSeconds(2); // 2 second timeout per match

        // Compile PROGRAM-ID pattern
        try
        {
            _programIdPattern = new Regex(
                @"PROGRAM-ID\.\s+(?:IS\s+)?(?<prog>\w+)",
                RegexOptions.IgnoreCase | RegexOptions.Compiled,
                timeout);
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException("Invalid PROGRAM-ID regex pattern", ex);
        }

        // Compile and validate call patterns from config
        foreach (var patternStr in _config.CallPatterns)
        {
            try
            {
                var regex = new Regex(
                    patternStr,
                    RegexOptions.IgnoreCase | RegexOptions.Compiled,
                    timeout);

                // Verify it has the required "prog" capture group
                if (!regex.GetGroupNames().Contains("prog"))
                {
                    ConsoleHelper.WriteWarning(
                        $"Call pattern missing 'prog' capture group, skipping: {patternStr}");
                    continue;
                }

                _compiledCallPatterns.Add(regex);
            }
            catch (ArgumentException ex)
            {
                ConsoleHelper.WriteWarning(
                    $"Invalid regex pattern in config, skipping: {patternStr} - {ex.Message}");
            }
        }

        if (_compiledCallPatterns.Count == 0)
        {
            ConsoleHelper.WriteWarning("No valid call patterns configured!");
        }
    }

    private string? ExtractProgramId(string filePath)
    {
        try
        {
            foreach (var line in File.ReadLines(filePath))
            {
                try
                {
                    var match = _programIdPattern.Match(line); // Use pre-compiled
                    if (match.Success)
                    {
                        return match.Groups["prog"].Value;
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    ConsoleHelper.WriteWarning(
                        $"Regex timeout on line in {filePath}, skipping line");
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteWarning($"Failed to read {filePath}: {ex.Message}");
        }

        return null;
    }

    private List<string> ExtractCalls(string filePath)
    {
        var calls = new HashSet<string>();

        try
        {
            var content = File.ReadAllText(filePath);

            foreach (var pattern in _compiledCallPatterns) // Use pre-compiled
            {
                try
                {
                    var matches = pattern.Matches(content);

                    foreach (Match match in matches)
                    {
                        if (match.Groups["prog"].Success)
                        {
                            calls.Add(match.Groups["prog"].Value);
                        }
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    ConsoleHelper.WriteWarning(
                        $"Regex timeout processing {filePath}, pattern may be too complex");
                }
            }
        }
        catch (Exception ex)
        {
            ConsoleHelper.WriteWarning($"Failed to extract calls from {filePath}: {ex.Message}");
        }

        return calls.ToList();
    }
}
```

**Test Cases Needed:**
- Config with invalid regex pattern: `"((("`
- Config with ReDoS pattern: `"(a+)+b"`
- Config with pattern missing "prog" group
- Config with empty CallPatterns array
- COBOL file with 100MB of "aaaaaa..." to test timeout
- Valid patterns with various COBOL CALL syntaxes

---

### BUG-005: Unhandled Exception in Path Resolution
**Severity:** CRITICAL
**Location:** `/home/user/Cobolcaller/src/CursorOps/CursorOpsConfig.cs`, Line 138

**Issue:**
```csharp
public string ResolvePath(string relativePath)
{
    if (Path.IsPathRooted(relativePath))
        return relativePath;

    return Path.GetFullPath(Path.Combine(RootDirectory, relativePath));
}
```

`Path.GetFullPath()` can throw `ArgumentException`, `SecurityException`, `NotSupportedException`, or `PathTooLongException` on invalid paths. No exception handling.

**Scenario:**
```csharp
// Invalid characters in path
ResolvePath("file\0name.txt");  // Throws ArgumentException

// Path too long (> 260 chars on Windows)
ResolvePath(new string('a', 300));  // Throws PathTooLongException

// Malformed UNC path
ResolvePath("\\\\?\\");  // Throws ArgumentException
```

**Recommended Fix:**
```csharp
public string ResolvePath(string relativePath)
{
    try
    {
        // If already absolute, return as-is
        if (Path.IsPathRooted(relativePath))
            return Path.GetFullPath(relativePath); // Normalize even absolute paths

        // Combine with root directory
        return Path.GetFullPath(Path.Combine(RootDirectory, relativePath));
    }
    catch (ArgumentException ex)
    {
        ConsoleHelper.WriteError($"Invalid path: {ex.Message}");
        return relativePath; // Return original path as fallback
    }
    catch (PathTooLongException ex)
    {
        ConsoleHelper.WriteError($"Path too long: {ex.Message}");
        return relativePath;
    }
    catch (Exception ex)
    {
        ConsoleHelper.WriteError($"Error resolving path: {ex.Message}");
        return relativePath;
    }
}
```

**Test Cases Needed:**
- Path with null character: `"file\0name.txt"`
- Path exceeding MAX_PATH (260 chars on Windows)
- Invalid characters: `"file|name.txt"`, `"file?name.txt"`
- Valid relative path: `"subdir/file.txt"`
- Valid absolute path: `"/absolute/path/file.txt"`

---

## High Severity Issues

### BUG-006: Missing Exception Handling for File Operations
**Severity:** HIGH
**Location:** Multiple locations in `/home/user/Cobolcaller/src/CursorOps/Program.cs`

**Affected Lines:**
- Line 74: `File.ReadAllText(rulesPath)`
- Line 84: `File.WriteAllText(outputPath, content)`
- Line 171: `File.ReadAllText(promptFile)`
- Line 181: `File.WriteAllText(outputPath, content)`
- Line 246: `File.ReadAllText(rulesPath)`
- Line 256: `File.ReadAllText(graphPath)`
- Line 277: `File.ReadAllLines(file)`
- Line 309: `File.ReadAllText(file)`
- Line 326: `File.WriteAllText(outputPath, output)`
- Line 435: `File.WriteAllText(graphPath, json)`
- Line 514: `File.ReadAllLines(node.FilePath)`
- Line 541: `File.ReadAllText(node.FilePath)`
- Line 554: `File.WriteAllText(finalOutputPath, output)`

**Issue:**
None of these file operations have exception handling. They can fail due to:
- **File locked** by another process
- **Insufficient permissions** (read/write)
- **Disk full** (writes)
- **File deleted** between existence check and read
- **Network path unavailable**
- **Encoding issues** (binary files, invalid UTF-8)
- **Out of memory** (huge files)

**Scenario:**
```bash
# User runs cursorops while a file is open in exclusive mode
cursorops context pack --files locked.cbl --out output.md

# Crash: IOException: The process cannot access the file
# 'locked.cbl' because it is being used by another process.
```

**Recommended Fix (example for one method):**
```csharp
private static void HandleRulesInject(string? outputPath)
{
    var rulesPath = Path.Combine(AppContext.BaseDirectory, _config!.RulesFile);

    if (!File.Exists(rulesPath))
    {
        ConsoleHelper.WriteError($"Rules file not found: {rulesPath}");
        return;
    }

    try
    {
        var content = File.ReadAllText(rulesPath);

        if (string.IsNullOrEmpty(outputPath))
        {
            Console.WriteLine(content);
        }
        else
        {
            try
            {
                File.WriteAllText(outputPath, content);
                ConsoleHelper.WriteSuccess($"Rules written to: {outputPath}");
            }
            catch (UnauthorizedAccessException)
            {
                ConsoleHelper.WriteError($"Permission denied writing to: {outputPath}");
            }
            catch (IOException ex)
            {
                ConsoleHelper.WriteError($"Failed to write file: {ex.Message}");
            }
        }
    }
    catch (IOException ex)
    {
        ConsoleHelper.WriteError($"Failed to read rules file: {ex.Message}");
    }
    catch (UnauthorizedAccessException)
    {
        ConsoleHelper.WriteError($"Permission denied reading: {rulesPath}");
    }
}
```

Apply similar exception handling to ALL file I/O operations.

**Test Cases Needed:**
- File locked by another process
- Read-only file for write operations
- Directory instead of file
- Disk full scenario
- Permission denied (chmod 000 on Linux)
- File deleted between check and read
- Network path that becomes unavailable

---

### BUG-007: Memory Exhaustion with Large Files
**Severity:** HIGH
**Location:** Multiple locations

**Affected Lines:**
- `/home/user/Cobolcaller/src/CursorOps/CobolCallGraphBuilder.cs`, Line 196: `File.ReadAllText(filePath)`
- `/home/user/Cobolcaller/src/CursorOps/Program.cs`, Line 309: `File.ReadAllText(file)`
- `/home/user/Cobolcaller/src/CursorOps/Program.cs`, Line 541: `File.ReadAllText(node.FilePath)`

**Issue:**
`File.ReadAllText()` loads the entire file into memory. For very large COBOL files (100MB+ mainframe sources), this can cause:
- `OutOfMemoryException`
- Excessive GC pressure
- Application hang/slowdown

**Scenario:**
```bash
# User has a 500MB COBOL file
cursorops context pack --files huge-legacy-program.cbl --out context.md

# Application allocates 500MB+ for file content
# Potential OutOfMemoryException or extreme slowdown
```

**Recommended Fix:**

For reading entire file content when needed:
```csharp
// Use File.ReadAllLines instead of ReadAllText where possible
var lines = File.ReadAllLines(file);
var content = string.Join(Environment.NewLine, lines);
```

For regex matching (ExtractCalls):
```csharp
private List<string> ExtractCalls(string filePath)
{
    var calls = new HashSet<string>();

    try
    {
        // Process line-by-line instead of loading entire file
        foreach (var line in File.ReadLines(filePath))
        {
            foreach (var pattern in _compiledCallPatterns)
            {
                try
                {
                    var matches = pattern.Matches(line);
                    foreach (Match match in matches)
                    {
                        if (match.Groups["prog"].Success)
                        {
                            calls.Add(match.Groups["prog"].Value);
                        }
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    // Log and continue
                    continue;
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
```

**Alternative:** Add file size check before reading:
```csharp
var fileInfo = new FileInfo(file);
if (fileInfo.Length > 100 * 1024 * 1024) // 100MB limit
{
    ConsoleHelper.WriteWarning($"File too large, skipping: {file} ({fileInfo.Length / 1024 / 1024}MB)");
    continue;
}
```

**Test Cases Needed:**
- 1MB file
- 10MB file
- 100MB file
- 500MB file
- 1GB file

---

### BUG-008: Directory.GetFiles Exception Not Handled
**Severity:** HIGH
**Location:** `/home/user/Cobolcaller/src/CursorOps/CobolCallGraphBuilder.cs`, Line 136

**Issue:**
```csharp
foreach (var pattern in _config.CobolFilePatterns)
{
    var files = Directory.GetFiles(rootDir, pattern, SearchOption.AllDirectories);
```

`Directory.GetFiles()` can throw:
- `UnauthorizedAccessException` if a subdirectory denies access
- `DirectoryNotFoundException` if path becomes invalid
- `PathTooLongException` in deep directory structures
- `IOException` for various I/O errors

**Scenario:**
```
/cobol-project/
  ├── src/           (readable)
  │   └── prog1.cbl
  └── restricted/    (chmod 000, no access)
      └── prog2.cbl

Running: cursorops cobol trace --entry PROG1 --depth 2

Crash: UnauthorizedAccessException: Access to path '/cobol-project/restricted' is denied.
```

**Recommended Fix:**
```csharp
private void IndexCobolFiles()
{
    var rootDir = _config.ResolvePath(_config.RootDirectory);

    if (!Directory.Exists(rootDir))
    {
        ConsoleHelper.WriteWarning($"Root directory not found: {rootDir}");
        return;
    }

    foreach (var pattern in _config.CobolFilePatterns)
    {
        try
        {
            var files = Directory.GetFiles(rootDir, pattern, SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var programId = ExtractProgramId(file);
                if (!string.IsNullOrEmpty(programId))
                {
                    _programToFile[programId] = file;
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            ConsoleHelper.WriteWarning($"Permission denied accessing directory: {ex.Message}");
        }
        catch (PathTooLongException ex)
        {
            ConsoleHelper.WriteWarning($"Path too long: {ex.Message}");
        }
        catch (DirectoryNotFoundException ex)
        {
            ConsoleHelper.WriteWarning($"Directory not found: {ex.Message}");
        }
        catch (IOException ex)
        {
            ConsoleHelper.WriteWarning($"I/O error searching for files: {ex.Message}");
        }
    }

    ConsoleHelper.WriteInfo($"Indexed {_programToFile.Count} COBOL programs in {rootDir}");
}
```

**Test Cases Needed:**
- Directory with permission denied subdirectory
- Network path that disconnects mid-search
- Deeply nested directory structure (>260 chars path)
- Directory deleted during search

---

### BUG-009: File Deleted/Disappears During Processing
**Severity:** HIGH
**Location:** Multiple locations

**Issue:**
There's a race condition between checking file existence and reading it:

```csharp
// Program.cs, line 267-271
if (!File.Exists(file))
{
    ConsoleHelper.WriteWarning($"File not found: {file}");
    continue;
}
// ... later ...
var lines = File.ReadAllLines(file); // Could fail if file deleted here
```

**Scenario:**
```bash
# Terminal 1
cursorops context pack --files temp.cbl --out context.md

# Terminal 2 (while command running)
rm temp.cbl

# Result: FileNotFoundException thrown after existence check passes
```

**Recommended Fix:**

Don't check existence separately; handle the exception instead:
```csharp
foreach (var file in files)
{
    try
    {
        var lines = File.ReadAllLines(file);

        // ... rest of processing ...
    }
    catch (FileNotFoundException)
    {
        ConsoleHelper.WriteWarning($"File not found: {file}");
        continue;
    }
    catch (IOException ex)
    {
        ConsoleHelper.WriteWarning($"Error reading {file}: {ex.Message}");
        continue;
    }
}
```

**Test Cases Needed:**
- Delete file between existence check and read
- Move file during processing
- Replace file with directory during processing

---

### BUG-010: Invalid File Pattern Handling
**Severity:** HIGH
**Location:** `/home/user/Cobolcaller/src/CursorOps/CobolCallGraphBuilder.cs`, Line 136

**Issue:**
Config file could contain invalid glob patterns that cause `ArgumentException`:

```json
{
  "CobolFilePatterns": ["*.cbl", "**invalid**", "|badpattern|"]
}
```

**Recommended Fix:**
```csharp
foreach (var pattern in _config.CobolFilePatterns)
{
    try
    {
        var files = Directory.GetFiles(rootDir, pattern, SearchOption.AllDirectories);
        // ... process files ...
    }
    catch (ArgumentException ex)
    {
        ConsoleHelper.WriteWarning($"Invalid file pattern '{pattern}': {ex.Message}");
        continue;
    }
    // ... other exceptions ...
}
```

**Test Cases Needed:**
- Valid pattern: `"*.cbl"`
- Invalid pattern: `"|*.cbl"`
- Empty pattern: `""`
- Pattern with invalid chars: `"*.c<>bl"`

---

### BUG-011: Missing PROGRAM-ID Validation
**Severity:** HIGH
**Location:** `/home/user/Cobolcaller/src/CursorOps/CobolCallGraphBuilder.cs`, Line 145

**Issue:**
If multiple COBOL files have the same PROGRAM-ID, only the last one found is kept:

```csharp
_programToFile[programId] = file; // Silently overwrites duplicates
```

This could lead to analyzing the wrong version of a program.

**Scenario:**
```
/project/
  ├── old/CUSTOMER.cbl     (PROGRAM-ID: CUSTMGR, found first)
  └── current/CUSTOMER.cbl (PROGRAM-ID: CUSTMGR, found second)
```

The old version gets silently overwritten with no warning to the user.

**Recommended Fix:**
```csharp
foreach (var file in files)
{
    var programId = ExtractProgramId(file);
    if (!string.IsNullOrEmpty(programId))
    {
        if (_programToFile.ContainsKey(programId))
        {
            ConsoleHelper.WriteWarning(
                $"Duplicate PROGRAM-ID '{programId}' found:\n" +
                $"  Previous: {_programToFile[programId]}\n" +
                $"  Current:  {file}\n" +
                $"  Using the later one.");
        }
        _programToFile[programId] = file;
    }
}
```

**Test Cases Needed:**
- Two files with same PROGRAM-ID
- Two files with different PROGRAM-IDs
- File with no PROGRAM-ID

---

## Medium Severity Issues

### BUG-012: Tree Rendering Shows False Circular References
**Severity:** MEDIUM
**Location:** `/home/user/Cobolcaller/src/CursorOps/CobolCallGraphBuilder.cs`, Lines 297-335

**Issue:**
The `ToTree()` method uses a single `visited` HashSet across all recursive calls. This causes shared dependencies to be incorrectly marked as circular:

```
A calls B and C
B calls D
C calls D

Tree renders as:
A
├── B
│   └── D
└── C
    └── D (circular)  <-- FALSE! This is not circular, just shared.
```

**Impact:** Confusing output; shared dependencies appear as circular references.

**Recommended Fix:**

Track the current path separately from globally visited nodes:

```csharp
public string ToTree(string entryProgram)
{
    var sb = new System.Text.StringBuilder();
    var currentPath = new HashSet<string>(); // Track current recursion path

    void RenderNode(string programName, string prefix, bool isLast)
    {
        // Check for actual circular reference in current path
        if (currentPath.Contains(programName))
        {
            sb.AppendLine($"{prefix}└── {programName} (circular)");
            return;
        }

        currentPath.Add(programName);

        // Get node from graph
        if (!_graph.TryGetValue(programName, out var node))
        {
            sb.AppendLine($"{prefix}└── {programName} (not found)");
            currentPath.Remove(programName);
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

        currentPath.Remove(programName);
    }

    RenderNode(entryProgram, "", true);
    return sb.ToString();
}
```

**Test Cases Needed:**
- Diamond dependency (A->B, A->C, B->D, C->D)
- True circular (A->B->C->A)
- Shared leaf node
- Complex graph with both circular and shared

---

### BUG-013: No Encoding Specification for COBOL Files
**Severity:** MEDIUM
**Location:** Multiple file read locations

**Issue:**
All `File.ReadAllText()` and `File.ReadAllLines()` calls use default encoding (UTF-8). Legacy COBOL files may use:
- **EBCDIC** (mainframe)
- **ASCII**
- **Windows-1252** / **ISO-8859-1**
- **Other code pages**

This could result in garbled text or incorrect PROGRAM-ID/CALL extraction.

**Recommended Fix:**
Add encoding configuration:

```csharp
// In CursorOpsConfig.cs
[JsonPropertyName("CobolFileEncoding")]
public string CobolFileEncoding { get; set; } = "UTF-8";

private Encoding? _cachedEncoding;

public Encoding GetCobolEncoding()
{
    if (_cachedEncoding == null)
    {
        try
        {
            _cachedEncoding = Encoding.GetEncoding(CobolFileEncoding);
        }
        catch (ArgumentException)
        {
            ConsoleHelper.WriteWarning(
                $"Invalid encoding '{CobolFileEncoding}', using UTF-8");
            _cachedEncoding = Encoding.UTF8;
        }
    }
    return _cachedEncoding;
}
```

Then use it:
```csharp
var content = File.ReadAllText(filePath, _config.GetCobolEncoding());
```

**Test Cases Needed:**
- COBOL file in UTF-8
- COBOL file in ASCII
- COBOL file in Windows-1252
- Config with invalid encoding name

---

### BUG-014: Negative or Zero Depth Not Validated
**Severity:** MEDIUM
**Location:** Command line option handling

**Issue:**
The `--depth` parameter has a default of 2, but negative or zero values aren't validated:

```bash
cursorops cobol trace --entry PROG1 --depth -5
# Probably won't crash but won't work correctly
```

**Recommended Fix:**
```csharp
// In CreateCobolCommand, add validation
new Option<int>(
    aliases: new[] { "--depth", "-d" },
    description: "Maximum depth to trace (must be >= 0)",
    getDefaultValue: () => 2)
{
    ArgumentHelpName = "depth"
}.AddValidator(result =>
{
    if (result.GetValueOrDefault<int>() < 0)
    {
        result.ErrorMessage = "Depth must be non-negative";
    }
});
```

**Test Cases Needed:**
- depth = -1
- depth = 0
- depth = 1
- depth = 100

---

### BUG-015: Config Truncation Values Inconsistent
**Severity:** MEDIUM
**Location:** `/home/user/Cobolcaller/src/CursorOps/CursorOpsConfig.cs`

**Issue:**
No validation that `TrimHeadLines + TrimTailLines <= MaxFileLines`. Configuration could have:

```json
{
  "MaxFileLines": 100,
  "TrimHeadLines": 80,
  "TrimTailLines": 80
}
```

This means trying to show 160 lines total from a 100-line-max file, which is illogical.

**Recommended Fix:**
```csharp
public static CursorOpsConfig Load(string configPath)
{
    // ... existing code ...

    var config = JsonSerializer.Deserialize<CursorOpsConfig>(json, options)
                 ?? new CursorOpsConfig();

    // Validate truncation settings
    if (config.TrimHeadLines + config.TrimTailLines > config.MaxFileLines)
    {
        ConsoleHelper.WriteWarning(
            $"TrimHeadLines ({config.TrimHeadLines}) + TrimTailLines ({config.TrimTailLines}) " +
            $"exceeds MaxFileLines ({config.MaxFileLines}). Adjusting to balanced split.");

        int halfMax = config.MaxFileLines / 2;
        config.TrimHeadLines = halfMax;
        config.TrimTailLines = config.MaxFileLines - halfMax;
    }

    if (config.MaxFileLines < 1)
    {
        ConsoleHelper.WriteWarning("MaxFileLines must be at least 1, using default 500.");
        config.MaxFileLines = 500;
    }

    return config;
}
```

**Test Cases Needed:**
- MaxFileLines = 100, TrimHead = 80, TrimTail = 80
- MaxFileLines = 0
- MaxFileLines = -1
- TrimHeadLines = -1

---

## Low Severity Issues

### BUG-016: Regex Performance - Pattern Compiled Per File
**Severity:** LOW (Performance)
**Location:** `/home/user/Cobolcaller/src/CursorOps/CobolCallGraphBuilder.cs`, Line 165

**Issue:**
```csharp
private string? ExtractProgramId(string filePath)
{
    var programIdPattern = new Regex(@"PROGRAM-ID\.\s+(?:IS\s+)?(?<prog>\w+)",
                                     RegexOptions.IgnoreCase);
```

A new Regex object is created for EVERY file processed. This is inefficient.

**Impact:** Slower processing when analyzing thousands of files.

**Recommended Fix:**
Move regex to static field or pre-compile in constructor (already shown in BUG-004 fix).

---

### BUG-017: Statistics Division by Zero Check Missing
**Severity:** LOW
**Location:** `/home/user/Cobolcaller/src/CursorOps/CobolCallGraphBuilder.cs`, Line 346

**Issue:**
```csharp
var maxDepth = _graph.Values.Any() ? _graph.Values.Max(n => n.Depth) : 0;
```

This is actually handled correctly! The check for `Any()` prevents exception on empty collection. However, similar pattern appears elsewhere:

```csharp
var totalCalls = _graph.Values.Sum(n => n.Calls.Count);
```

If `_graph.Values` is empty, `Sum()` returns 0 (safe). No bug here, but worth noting for code review awareness.

**Recommended:** Add a check at the beginning:
```csharp
public string GetStatistics()
{
    if (_graph.Count == 0)
    {
        return "Call Graph Statistics: No programs analyzed.";
    }

    // ... rest of method
}
```

---

## Summary Table

| ID | Severity | Category | Location | Description |
|----|----------|----------|----------|-------------|
| BUG-001 | Critical | Index OOB | Program.cs:287-289, 522-524 | TrimHeadLines exceeds file length |
| BUG-002 | Critical | Index OOB | Program.cs:297-299, 531-533 | TrimTailLines causes negative index |
| BUG-003 | Critical | Security | Program.cs:162 | Path traversal in prompt pick |
| BUG-004 | Critical | Security | CobolCallGraphBuilder.cs:201 | ReDoS vulnerability, no regex timeout |
| BUG-005 | Critical | Exception | CursorOpsConfig.cs:138 | Unhandled exception in ResolvePath |
| BUG-006 | High | Exception | Program.cs (multiple) | No file I/O exception handling |
| BUG-007 | High | Memory | Multiple locations | Large files cause OOM |
| BUG-008 | High | Exception | CobolCallGraphBuilder.cs:136 | Directory.GetFiles exceptions |
| BUG-009 | High | Race | Program.cs:267-277 | File deleted during processing |
| BUG-010 | High | Exception | CobolCallGraphBuilder.cs:136 | Invalid glob patterns not handled |
| BUG-011 | High | Logic | CobolCallGraphBuilder.cs:145 | Duplicate PROGRAM-IDs silently overwrite |
| BUG-012 | Medium | Logic | CobolCallGraphBuilder.cs:302 | False circular refs in tree |
| BUG-013 | Medium | Encoding | Multiple locations | No encoding config for COBOL files |
| BUG-014 | Medium | Validation | Program.cs:351-354 | Negative depth not validated |
| BUG-015 | Medium | Validation | CursorOpsConfig.cs | Truncation config not validated |
| BUG-016 | Low | Performance | CobolCallGraphBuilder.cs:165 | Regex compiled per file |
| BUG-017 | Low | Edge Case | CobolCallGraphBuilder.cs:346 | Empty graph stats (handled) |

---

## Recommended Test Coverage

### Unit Tests

**ConfigTests.cs:**
- Load valid config
- Load missing config (uses defaults)
- Load malformed config (uses defaults)
- ResolvePath with absolute path
- ResolvePath with relative path
- ResolvePath with invalid characters
- ResolvePath with path too long
- Validate truncation settings
- Validate encoding settings

**CobolCallGraphBuilderTests.cs:**
- Build graph with valid entry program
- Build graph with missing entry program
- Build graph with circular dependencies
- Build graph with shared dependencies
- ExtractProgramId from valid COBOL file
- ExtractProgramId from file without PROGRAM-ID
- ExtractCalls with various CALL syntaxes
- Handle duplicate PROGRAM-IDs
- Handle regex timeout
- Handle invalid regex patterns
- ToTree with circular refs
- ToTree with shared deps
- Statistics with empty graph

**ProgramTests.cs:**
- File truncation with various file sizes
- File truncation edge cases (< head, < tail, < both)
- Path traversal attack prevention
- File I/O exceptions
- Locked file handling
- Missing file handling
- Permission denied handling

### Integration Tests

- Process real COBOL project structure
- Handle 1000+ COBOL files
- Process 100MB+ COBOL file
- Handle permission denied directories
- Process files with different encodings
- Concurrent file access scenarios

### Security Tests

- Path traversal attempts in prompt pick
- ReDoS with malicious patterns
- Invalid config injection attempts
- Large file DoS attempts

---

## Priority Recommendations

### Immediate (Before Release)
1. **Fix BUG-001 & BUG-002** - Index out of bounds crashes
2. **Fix BUG-003** - Path traversal security vulnerability
3. **Fix BUG-004** - ReDoS vulnerability
4. **Fix BUG-006** - Add file I/O exception handling everywhere

### Short Term (Next Sprint)
5. **Fix BUG-005** - ResolvePath exception handling
6. **Fix BUG-007** - Memory issues with large files
7. **Fix BUG-008** - Directory.GetFiles exception handling
8. **Fix BUG-011** - Duplicate PROGRAM-ID warning

### Medium Term (Next Release)
9. **Fix BUG-012** - Tree rendering false circulars
10. **Fix BUG-013** - Add encoding configuration
11. **Fix BUG-014 & BUG-015** - Input validation
12. **Add comprehensive test suite**

### Long Term (Backlog)
13. **Fix BUG-016** - Performance optimizations
14. **Add file size limits and warnings**
15. **Add progress indicators for large operations**
16. **Consider async/parallel processing for large projects**

---

## Additional Recommendations

### Error Handling Strategy
Implement a consistent error handling pattern across all commands:

```csharp
private static class CommandHelper
{
    public static bool TryReadFile(string path, out string content, out string error)
    {
        content = string.Empty;
        error = string.Empty;

        try
        {
            content = File.ReadAllText(path);
            return true;
        }
        catch (FileNotFoundException)
        {
            error = $"File not found: {path}";
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            error = $"Permission denied: {path}";
            return false;
        }
        catch (IOException ex)
        {
            error = $"I/O error reading {path}: {ex.Message}";
            return false;
        }
    }

    // Similar for TryWriteFile, TryReadLines, etc.
}
```

### Logging
Add optional verbose logging:

```csharp
[JsonPropertyName("VerboseLogging")]
public bool VerboseLogging { get; set; } = false;

public static class Logger
{
    public static void Debug(string message)
    {
        if (_config?.VerboseLogging == true)
        {
            Console.WriteLine($"[DEBUG] {message}");
        }
    }
}
```

### Configuration Validation
Add a `Validate()` method to CursorOpsConfig:

```csharp
public List<string> Validate()
{
    var errors = new List<string>();

    if (MaxFileLines < 1)
        errors.Add("MaxFileLines must be at least 1");

    if (TrimHeadLines < 0)
        errors.Add("TrimHeadLines must be non-negative");

    if (TrimTailLines < 0)
        errors.Add("TrimTailLines must be non-negative");

    if (TrimHeadLines + TrimTailLines > MaxFileLines)
        errors.Add("TrimHeadLines + TrimTailLines exceeds MaxFileLines");

    if (CallPatterns.Count == 0)
        errors.Add("At least one CallPattern must be specified");

    // Validate each regex pattern
    foreach (var pattern in CallPatterns)
    {
        try
        {
            var regex = new Regex(pattern);
            if (!regex.GetGroupNames().Contains("prog"))
            {
                errors.Add($"Pattern missing 'prog' group: {pattern}");
            }
        }
        catch (ArgumentException ex)
        {
            errors.Add($"Invalid regex pattern '{pattern}': {ex.Message}");
        }
    }

    return errors;
}
```

---

## Conclusion

The CursorOps codebase has **17 identified issues** ranging from critical crashes and security vulnerabilities to minor performance concerns. The most urgent issues are:

1. **Index out of bounds** errors that will crash with edge case file sizes
2. **Path traversal vulnerability** that could expose sensitive files
3. **ReDoS vulnerability** that could hang the application
4. **Missing exception handling** throughout file operations

Addressing the critical and high severity issues should be the immediate priority before production deployment. The codebase would benefit from comprehensive unit and integration testing, particularly around file I/O, error handling, and edge cases.

The code is generally well-structured and readable, but needs hardening for production use with untrusted input and real-world file system scenarios.
