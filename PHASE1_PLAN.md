# Phase 1 Implementation Plan: Critical Fixes

**Goal:** Fix critical security vulnerabilities and crash scenarios
**Timeline:** 2-3 days
**Status:** 🔴 In Progress

---

## Overview

Phase 1 addresses **13 critical issues** that prevent production deployment:
- 6 path traversal vulnerabilities (security)
- 2 ReDoS vulnerabilities (security/DoS)
- 4 index out of bounds (crashes)
- 11+ missing exception handlers (crashes)
- 11+ null safety issues (crashes)

---

## Task Breakdown

### Task 1: Create Security Utilities (30 minutes)

**File:** `src/CursorOps/SecurityHelper.cs` (new file)

**Implementation:**
```csharp
public static class SecurityHelper
{
    public static string ValidateOutputPath(string? outputPath, string baseDirectory)
    public static string ValidateInputName(string name)
    public static bool IsPathSafe(string path, string allowedDirectory)
}
```

**Features:**
- Path canonicalization
- Directory traversal detection
- Invalid character filtering
- Path validation against allowed directories

**Test Cases:**
- `../../etc/passwd`
- `C:\Windows\System32\config\sam`
- Valid relative paths
- Valid absolute paths
- Paths with null bytes
- Paths exceeding MAX_PATH

---

### Task 2: Secure All Output Parameters (45 minutes)

**Files to Modify:**
- `src/CursorOps/Program.cs` (6 locations)

**Locations:**
1. Line 84: `HandleRulesInject` - `File.WriteAllText(outputPath, content)`
2. Line 181: `HandlePromptPick` - `File.WriteAllText(outputPath, content)`
3. Line 326: `HandleContextPack` - `File.WriteAllText(outputPath, output)`
4. Line 435: `HandleCobolTrace` - `File.WriteAllText(graphPath, json)`
5. Line 554: `HandleCobolFocus` - `File.WriteAllText(finalOutputPath, output)`

**Changes Required:**
```csharp
// Before (vulnerable):
if (!string.IsNullOrEmpty(outputPath))
{
    File.WriteAllText(outputPath, content);
}

// After (secure):
if (!string.IsNullOrEmpty(outputPath))
{
    try
    {
        var safePath = SecurityHelper.ValidateOutputPath(outputPath, Directory.GetCurrentDirectory());
        File.WriteAllText(safePath, content);
        ConsoleHelper.WriteSuccess($"Written to: {safePath}");
    }
    catch (SecurityException ex)
    {
        ConsoleHelper.WriteError($"Security error: {ex.Message}");
        return;
    }
    catch (IOException ex)
    {
        ConsoleHelper.WriteError($"Failed to write file: {ex.Message}");
        return;
    }
}
```

---

### Task 3: Fix Prompt Pick Path Traversal (20 minutes)

**File:** `src/CursorOps/Program.cs`, Line 159-184

**Current Vulnerability:**
```csharp
var promptFile = Path.Combine(promptsPath, $"{name}.md");
```

**Fix Required:**
```csharp
private static void HandlePromptPick(string name, string? outputPath)
{
    // Validate name (no path separators)
    try
    {
        name = SecurityHelper.ValidateInputName(name);
    }
    catch (SecurityException ex)
    {
        ConsoleHelper.WriteError(ex.Message);
        return;
    }

    var promptsPath = Path.Combine(AppContext.BaseDirectory, _config.PromptsPath);
    var promptFile = Path.Combine(promptsPath, $"{name}.md");

    // Verify final path is within prompts directory
    if (!SecurityHelper.IsPathSafe(promptFile, promptsPath))
    {
        ConsoleHelper.WriteError("Invalid prompt name: path traversal detected");
        return;
    }

    // ... rest of method
}
```

---

### Task 4: Add Regex Timeout & Pre-compilation (1 hour)

**File:** `src/CursorOps/CobolCallGraphBuilder.cs`

**Changes:**
1. Add fields for pre-compiled patterns
2. Compile patterns in constructor with timeout
3. Validate patterns
4. Handle RegexMatchTimeoutException

**Implementation:**
```csharp
public class CobolCallGraphBuilder
{
    private readonly CursorOpsConfig _config;
    private readonly Dictionary<string, CallGraphNode> _graph = new();
    private readonly Dictionary<string, string> _programToFile = new();

    // NEW: Pre-compiled patterns
    private readonly Regex _programIdPattern;
    private readonly List<Regex> _compiledCallPatterns = new();
    private const int REGEX_TIMEOUT_MS = 2000;

    public CobolCallGraphBuilder(CursorOpsConfig config)
    {
        _config = config;

        // Pre-compile PROGRAM-ID pattern with timeout
        _programIdPattern = new Regex(
            @"PROGRAM-ID\.\s+(?:IS\s+)?(?<prog>\w+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled,
            TimeSpan.FromMilliseconds(REGEX_TIMEOUT_MS));

        // Compile and validate call patterns
        foreach (var patternStr in _config.CallPatterns)
        {
            try
            {
                var regex = new Regex(
                    patternStr,
                    RegexOptions.IgnoreCase | RegexOptions.Compiled,
                    TimeSpan.FromMilliseconds(REGEX_TIMEOUT_MS));

                // Verify required capture group
                if (!regex.GetGroupNames().Contains("prog"))
                {
                    ConsoleHelper.WriteWarning($"Pattern missing 'prog' group, skipping: {patternStr}");
                    continue;
                }

                _compiledCallPatterns.Add(regex);
            }
            catch (ArgumentException ex)
            {
                ConsoleHelper.WriteWarning($"Invalid regex pattern, skipping: {patternStr} - {ex.Message}");
            }
        }

        if (_compiledCallPatterns.Count == 0)
        {
            ConsoleHelper.WriteWarning("No valid call patterns configured!");
        }
    }
}
```

**Update ExtractProgramId:**
```csharp
private string? ExtractProgramId(string filePath)
{
    try
    {
        foreach (var line in File.ReadLines(filePath))
        {
            try
            {
                var match = _programIdPattern.Match(line);
                if (match.Success)
                {
                    return match.Groups["prog"].Value;
                }
            }
            catch (RegexMatchTimeoutException)
            {
                ConsoleHelper.WriteWarning($"Regex timeout in {filePath}");
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
```

**Update ExtractCalls:**
```csharp
private List<string> ExtractCalls(string filePath)
{
    var calls = new HashSet<string>();
    try
    {
        var content = File.ReadAllText(filePath);
        foreach (var pattern in _compiledCallPatterns)
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
                ConsoleHelper.WriteWarning($"Regex timeout in {filePath}");
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

---

### Task 5: Fix Index Out of Bounds (30 minutes)

**File:** `src/CursorOps/Program.cs`

**Locations:**
- Lines 287-299 (HandleContextPack)
- Lines 522-533 (HandleCobolFocus)

**Current Bug:**
```csharp
// Assumes file has at least TrimHeadLines
for (int i = 0; i < _config.TrimHeadLines; i++)
{
    sb.AppendLine(lines[i]); // CRASH if lines.Length < TrimHeadLines
}
```

**Fix:**
```csharp
if (lines.Length > _config.MaxFileLines)
{
    int actualHeadLines = Math.Min(_config.TrimHeadLines, lines.Length);
    int actualTailLines = Math.Min(_config.TrimTailLines, lines.Length - actualHeadLines);

    sb.AppendLine($"*File truncated: {lines.Length} lines (showing first {actualHeadLines} and last {actualTailLines})*");
    sb.AppendLine();
    sb.AppendLine($"```{extension}");

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
```

**Create Shared Method:**
```csharp
private static void AppendFileWithTruncation(StringBuilder sb, string filePath, string fileTitle, string? additionalInfo = null)
{
    if (!File.Exists(filePath))
    {
        sb.AppendLine($"### {fileTitle} (Not Found)");
        sb.AppendLine();
        return;
    }

    sb.AppendLine($"### {fileTitle}");
    sb.AppendLine($"Path: `{filePath}`");

    if (!string.IsNullOrEmpty(additionalInfo))
    {
        sb.AppendLine(additionalInfo);
    }

    sb.AppendLine();

    var lines = File.ReadAllLines(filePath);
    var extension = Path.GetExtension(filePath).TrimStart('.');

    if (lines.Length > _config!.MaxFileLines)
    {
        // Truncation logic above
    }
    else
    {
        // Include full file
        sb.AppendLine($"```{extension}");
        foreach (var line in lines)
        {
            sb.AppendLine(line);
        }
        sb.AppendLine("```");
    }

    sb.AppendLine();
}
```

---

### Task 6: Fix Null Safety (30 minutes)

**File:** `src/CursorOps/Program.cs`

**Current Issue:**
```csharp
private static CursorOpsConfig? _config;

// Later: using _config! everywhere (dangerous)
var rulesPath = Path.Combine(AppContext.BaseDirectory, _config!.RulesFile);
```

**Fix Option 1: Make Non-Nullable**
```csharp
private static CursorOpsConfig _config = null!;

static async Task<int> Main(string[] args)
{
    var configPath = Path.Combine(AppContext.BaseDirectory, "config", "cursorops.json");
    _config = CursorOpsConfig.Load(configPath);

    // Config.Load() always returns valid instance, but verify
    if (_config == null)
    {
        ConsoleHelper.WriteError("Failed to initialize configuration");
        return 1;
    }

    // ... rest of main
}
```

**Fix Option 2: Add Null Checks**
```csharp
private static void HandleRulesInject(string? outputPath)
{
    if (_config == null)
    {
        ConsoleHelper.WriteError("Configuration not initialized");
        return;
    }

    var rulesPath = Path.Combine(AppContext.BaseDirectory, _config.RulesFile);
    // ... rest of method
}
```

**Recommended:** Option 1 (cleaner, since Load() always returns valid config)

---

### Task 7: Add Exception Handling to All File Operations (1.5 hours)

**Files:** `src/CursorOps/Program.cs`, `src/CursorOps/CobolCallGraphBuilder.cs`

**Standard Pattern:**
```csharp
try
{
    var content = File.ReadAllText(filePath);
    // ... process content
}
catch (FileNotFoundException)
{
    ConsoleHelper.WriteError($"File not found: {filePath}");
    return; // or continue
}
catch (UnauthorizedAccessException)
{
    ConsoleHelper.WriteError($"Permission denied: {filePath}");
    return;
}
catch (IOException ex)
{
    ConsoleHelper.WriteError($"I/O error: {ex.Message}");
    return;
}
```

**Locations in Program.cs:**
- Line 74: `HandleRulesInject` - File.ReadAllText
- Line 84: `HandleRulesInject` - File.WriteAllText
- Line 171: `HandlePromptPick` - File.ReadAllText
- Line 181: `HandlePromptPick` - File.WriteAllText
- Line 246: `HandleContextPack` - File.ReadAllText (rules)
- Line 256: `HandleContextPack` - File.ReadAllText (graph)
- Line 277: `HandleContextPack` - File.ReadAllLines
- Line 309: `HandleContextPack` - File.ReadAllText
- Line 326: `HandleContextPack` - File.WriteAllText
- Line 435: `HandleCobolTrace` - File.WriteAllText
- Line 514: `HandleCobolFocus` - File.ReadAllLines
- Line 541: `HandleCobolFocus` - File.ReadAllText
- Line 554: `HandleCobolFocus` - File.WriteAllText

**Locations in CobolCallGraphBuilder.cs:**
- Line 136: IndexCobolFiles - Directory.GetFiles
- Line 168: ExtractProgramId - File.ReadLines
- Line 196: ExtractCalls - File.ReadAllText

---

## Implementation Order

### Day 1 (4-5 hours)
1. ✅ Create `SecurityHelper.cs` (30 min)
2. ✅ Secure all output parameters (45 min)
3. ✅ Fix prompt pick path traversal (20 min)
4. ✅ Fix index out of bounds (30 min)
5. ✅ Fix null safety (30 min)
6. ☕ Break
7. ✅ Add regex timeout (1 hour)

### Day 2 (3-4 hours)
8. ✅ Add exception handling - Part 1 (1 hour)
   - Program.cs: HandleRulesInject, HandlePromptPick
9. ✅ Add exception handling - Part 2 (1 hour)
   - Program.cs: HandleContextPack
10. ✅ Add exception handling - Part 3 (1 hour)
    - Program.cs: HandleCobolTrace, HandleCobolFocus
    - CobolCallGraphBuilder.cs: All methods

### Day 3 (2-3 hours)
11. ✅ Testing with edge cases (1.5 hours)
12. ✅ Documentation and changelog (1 hour)
13. ✅ Final verification (30 min)

---

## Testing Checklist

### Security Tests
- [ ] Path traversal: `cursorops prompt pick "../../../etc/passwd"`
- [ ] Path traversal: `cursorops rules inject --out "/etc/malicious"`
- [ ] ReDoS pattern: Add `(a+)+b` to config, trace large file
- [ ] Invalid regex: Add `(((` to config

### Edge Case Tests
- [ ] File with 0 lines
- [ ] File with 10 lines (< TrimHeadLines)
- [ ] File with 30 lines (< TrimHeadLines)
- [ ] File with exactly 50 lines (= TrimHeadLines)
- [ ] File with exactly 100 lines (= TrimHeadLines + TrimTailLines)
- [ ] File locked by another process
- [ ] File deleted during processing
- [ ] Permission denied on file
- [ ] Permission denied on directory
- [ ] Disk full during write

### Functionality Tests
- [ ] Demo command still works
- [ ] Rules inject works
- [ ] Prompt list works
- [ ] Prompt pick works
- [ ] Context pack works
- [ ] COBOL trace works (if COBOL available)
- [ ] COBOL focus works (if COBOL available)

---

## Success Criteria

Phase 1 is complete when:

1. ✅ All 6 path traversal vulnerabilities fixed
2. ✅ ReDoS vulnerability mitigated
3. ✅ All 4 index out of bounds bugs fixed
4. ✅ All 11+ null safety issues resolved
5. ✅ All 11+ file operations have exception handling
6. ✅ All security tests pass
7. ✅ All edge case tests pass
8. ✅ All functionality tests pass
9. ✅ Code review of changes
10. ✅ Documentation updated

---

## Risk Mitigation

### Risk: Breaking Existing Functionality
**Mitigation:**
- Test each fix individually
- Run demo command after each change
- Keep changes minimal and focused

### Risk: Incomplete Exception Handling
**Mitigation:**
- Use checklist to track all file operations
- Search for `File.` and `Directory.` in codebase
- Code review before commit

### Risk: Performance Regression
**Mitigation:**
- Pre-compiled regex is actually faster
- Minimal overhead from path validation
- Test with large COBOL project

---

## Deliverables

1. ✅ `src/CursorOps/SecurityHelper.cs` (new file)
2. ✅ `src/CursorOps/Program.cs` (modified, security & exception handling)
3. ✅ `src/CursorOps/CobolCallGraphBuilder.cs` (modified, regex timeout)
4. ✅ `PHASE1_CHANGELOG.md` (new file, detailed changes)
5. ✅ Updated `README.md` (security notes)
6. ✅ Test results documentation

---

**Status:** Ready to begin implementation
**Next Action:** Create SecurityHelper.cs utility class
