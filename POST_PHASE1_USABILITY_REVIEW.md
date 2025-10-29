# CursorOps Post-Phase 1 Usability Review

**Reviewer:** DX Specialist
**Date:** October 29, 2024
**Version Reviewed:** 1.0.0 (Post-Phase 1)
**Perspective:** COBOL developer new to .NET CLI tools

---

## Executive Summary

**Overall DX Score: 6.5/10**

CursorOps has a solid technical foundation after Phase 1 security fixes, but the developer experience needs significant improvement. The tool is secure and stable, but not yet user-friendly for its primary audience: COBOL developers with limited .NET experience.

### Key Findings

**Strengths:**
- ✅ Comprehensive security fixes (path traversal, ReDoS)
- ✅ Excellent README documentation (550+ lines)
- ✅ Color-coded console output
- ✅ Well-structured command hierarchy
- ✅ Good error handling (technically)

**Critical Issues:**
- ❌ **No logging system** - errors and warnings disappear after console scrolls
- ❌ **Error messages lack actionable guidance** - "what do I do now?"
- ❌ **Configuration validation happens too late** - users don't know if config is wrong until commands fail
- ❌ **No verbose/debug mode** for troubleshooting
- ❌ **Missing first-time user quickstart guide**

**High-Priority Issues:**
- ⚠️ Generic error messages without context
- ⚠️ Technical jargon unfamiliar to COBOL developers ("path traversal", "ReDoS", "regex timeout")
- ⚠️ No feedback during long operations (file indexing)
- ⚠️ Default configuration assumes specific environment (C:\GSS\Source)
- ⚠️ No config validation command

---

## 1. Configuration Analysis

### File: `config/cursorops.json`

#### Strengths
- ✅ Well-commented JSON with clear inline documentation
- ✅ Sensible defaults for most settings
- ✅ Flexible regex pattern system
- ✅ Clear structure and organization

#### Critical Issues

**Issue 1.1: Environment-Specific Default Path**
```json
"RootDirectory": "C:\\GSS\\Source"
```
**Problem:** Hard-coded to one specific environment. New users will see errors immediately.

**Recommendation:**
```json
"RootDirectory": "." // Default to current directory with comment explaining how to set it
```

**Better Documentation:**
```json
// REQUIRED: Set this to your COBOL source code directory
// Examples:
//   Windows: "C:\\MyCompany\\Source" or "C:\\GSS\\Source"
//   Relative: "." (current directory) or "..\\Source"
// TIP: Run 'cursorops config validate' to verify this path exists
"RootDirectory": "."
```

**Issue 1.2: No Configuration Validation**

**Problem:** Users don't know if their config is correct until they run a command and it fails.

**Recommendation:** Add `cursorops config validate` command:
```
$ cursorops config validate

Validating configuration...
✓ Config file found: C:\...\config\cursorops.json
✗ RootDirectory not found: C:\GSS\Source
  → Create this directory or update config to point to your COBOL source
✓ RulesFile found: rules\rules.md
✓ PromptsPath found: prompts\ (4 templates)
✓ COBOL patterns: 4 patterns configured
  - *.cbl, *.cob, *.CBL, *.COB
✓ Call patterns: 3 patterns compiled successfully
  - CALL\s+['\"](?<prog>\w+)['\"]
  - CALL\s+(?<prog>\w+)
  - CALL\s+['\"](?<prog>[A-Z0-9-]+)['\"]

Configuration status: 1 error, 0 warnings
Fix the errors above before running commands.
```

**Issue 1.3: Regex Patterns Are Intimidating**

**Current:**
```json
"CallPatterns": [
  "CALL\\s+['\"](?<prog>\\w+)['\"]",
  "CALL\\s+(?<prog>\\w+)",
  "CALL\\s+['\"](?<prog>[A-Z0-9-]+)['\"]"
]
```

**Problem:** COBOL developers may not understand regex. Comments don't explain pattern matching behavior.

**Recommendation:** Add pattern testing command and better comments:
```json
// Patterns for detecting COBOL CALL statements (regex)
// Each pattern must include (?<prog>...) to capture the program name
// TIPS:
//   - Test patterns: cursorops config test-pattern "your pattern"
//   - Common COBOL: CALL "PROGNAME" or CALL PROGNAME
//   - Add custom patterns if your code uses variables: CALL WS-PROG-NAME
"CallPatterns": [
  "CALL\\s+['\"](?<prog>\\w+)['\"]",     // Matches: CALL "PROG123" or CALL 'PROG123'
  "CALL\\s+(?<prog>\\w+)",               // Matches: CALL PROG123 (no quotes)
  "CALL\\s+['\"](?<prog>[A-Z0-9-]+)['\"]"  // Matches: CALL "PROG-NAME" (with hyphens)
]
```

#### Medium-Priority Issues

**Issue 1.4: MaxFileLines/Trim Configuration Not Intuitive**

Users don't understand the relationship between MaxFileLines (500), TrimHeadLines (50), and TrimTailLines (50).

**Current behavior:**
- Files > 500 lines show first 50 + last 50 = 100 total lines
- Files between 100-500 lines: shows all
- Files < 100 lines: shows all

**Recommendation:** Better documentation:
```json
// File truncation for large files in context packs
// If a file exceeds MaxFileLines, it will be truncated to show:
//   - First TrimHeadLines (from beginning)
//   - Last TrimTailLines (from end)
//   - A note showing how many lines were omitted
// Example: MaxFileLines=500, Trim=50 each → shows 100 lines total (50+50)
// TIP: Increase MaxFileLines if you need more context for large programs
"MaxFileLines": 500,
"TrimHeadLines": 50,
"TrimTailLines": 50
```

---

## 2. Error Message Quality Analysis

### Current State: Technically Correct, Not User-Friendly

**Total console outputs analyzed:** 117 across 3 files

#### Critical Error Message Issues

**Issue 2.1: No Actionable Guidance**

**Example 1: File Not Found**
```csharp
// Current (Program.cs:78)
ConsoleHelper.WriteError($"Rules file not found: {rulesPath}");
```

**Problem:** Tells WHAT failed, not WHY or HOW TO FIX.

**Recommended:**
```csharp
ConsoleHelper.WriteError($"Rules file not found: {rulesPath}");
ConsoleHelper.WriteInfo("Solution:");
ConsoleHelper.WriteInfo($"  1. Create the file: {rulesPath}");
ConsoleHelper.WriteInfo($"  2. Or update config: config/cursorops.json -> RulesFile");
ConsoleHelper.WriteInfo($"  3. Or use a different command that doesn't need rules");
```

**Example 2: No Programs Found**
```csharp
// Current (Program.cs:574, 634)
ConsoleHelper.WriteWarning($"No programs found in call graph for '{entry}'");
```

**Problem:** User doesn't know if it's a config issue, wrong program name, or something else.

**Recommended:**
```csharp
ConsoleHelper.WriteWarning($"No programs found in call graph for '{entry}'");
ConsoleHelper.WriteInfo("Possible causes:");
ConsoleHelper.WriteInfo($"  1. Program '{entry}' doesn't exist in: {_config.RootDirectory}");
ConsoleHelper.WriteInfo($"  2. PROGRAM-ID in source doesn't match '{entry}' (check exact name)");
ConsoleHelper.WriteInfo($"  3. File pattern mismatch (check CobolFilePatterns in config)");
ConsoleHelper.WriteInfo($"  4. No COBOL files found (wrong RootDirectory?)");
ConsoleHelper.WriteInfo("");
ConsoleHelper.WriteInfo($"Troubleshooting:");
ConsoleHelper.WriteInfo($"  - Verify config: cursorops config validate");
ConsoleHelper.WriteInfo($"  - List programs: cursorops cobol list (if implemented)");
ConsoleHelper.WriteInfo($"  - Check indexing: {_programToFile.Count} programs were indexed");
```

**Issue 2.2: Technical Jargon for COBOL Developers**

**Example 1: Security Errors**
```csharp
// Current (SecurityHelper.cs:233)
ConsoleHelper.WriteError("Invalid prompt name: path traversal detected");
```

**Problem:** "Path traversal" is a security term. COBOL developers think in business logic terms.

**Recommended:**
```csharp
ConsoleHelper.WriteError("Invalid prompt name: cannot use paths or '..' in name");
ConsoleHelper.WriteInfo("Use only the prompt name without folders.");
ConsoleHelper.WriteInfo("Example: 'Refactor' (not '../Refactor' or 'prompts/Refactor')");
```

**Example 2: ReDoS Warnings**
```csharp
// Current (CobolCallGraphBuilder.cs:236, 284)
ConsoleHelper.WriteWarning($"Regex timeout in {filePath} - possible ReDoS pattern");
```

**Problem:** "ReDoS" means nothing to COBOL developers.

**Recommended:**
```csharp
ConsoleHelper.WriteWarning($"Pattern matching took too long in {filePath} (timeout)");
ConsoleHelper.WriteInfo("This usually means:");
ConsoleHelper.WriteInfo("  1. The file has unusual CALL statement format");
ConsoleHelper.WriteInfo("  2. A pattern in config/cursorops.json is too complex");
ConsoleHelper.WriteInfo("  3. The file is extremely large");
ConsoleHelper.WriteInfo("File will be skipped. To investigate, use --verbose mode.");
```

**Issue 2.3: Permission Errors Not Windows-Specific**

**Example:**
```csharp
// Current (Program.cs:89)
ConsoleHelper.WriteError($"Permission denied reading rules file: {ex.Message}");
```

**Problem:** Doesn't explain common Windows permission issues.

**Recommended:**
```csharp
ConsoleHelper.WriteError($"Permission denied: Cannot read {rulesPath}");
ConsoleHelper.WriteInfo("Common causes on Windows:");
ConsoleHelper.WriteInfo("  1. File is open in another program (Excel, editor, etc.)");
ConsoleHelper.WriteInfo("  2. You don't have Read permission for this file");
ConsoleHelper.WriteInfo("  3. File is on a network drive that's disconnected");
ConsoleHelper.WriteInfo("  4. Antivirus is blocking access");
ConsoleHelper.WriteInfo($"Try: Right-click file → Properties → Security tab");
```

#### Medium-Priority Error Issues

**Issue 2.4: Warnings Are Fire-and-Forget**

**Example:**
```csharp
// Current (CobolCallGraphBuilder.cs:81, 90, 97)
ConsoleHelper.WriteWarning($"Pattern missing 'prog' capture group, skipping: {patternStr}");
ConsoleHelper.WriteWarning($"Invalid regex pattern, skipping: {patternStr} - {ex.Message}");
ConsoleHelper.WriteWarning("No valid call patterns configured! CALL extraction will not work.");
```

**Problem:** These warnings are CRITICAL but look like casual warnings. They disappear when console scrolls.

**Recommendation:**
- Use ERROR color for critical warnings
- Add these to a warnings log file
- Add flag to treat warnings as errors: `--strict`

**Issue 2.5: No Progress Feedback for Long Operations**

**Example:**
```csharp
// Current (CobolCallGraphBuilder.cs:169-208)
private void IndexCobolFiles()
{
    // ...searches through entire directory tree silently
}
```

**Problem:** On large codebases (1000+ files), this takes 10-30 seconds with no feedback. User thinks it's frozen.

**Recommended:**
```csharp
private void IndexCobolFiles()
{
    ConsoleHelper.WriteInfo($"Indexing COBOL files in: {rootDir}");
    ConsoleHelper.WriteInfo("This may take a moment for large codebases...");

    int filesProcessed = 0;
    // ... during processing ...
    if (filesProcessed % 100 == 0)
    {
        Console.Write($"\rProcessed {filesProcessed} files..."); // Inline progress
    }

    Console.WriteLine(); // New line after progress
    ConsoleHelper.WriteSuccess($"Indexed {_programToFile.Count} COBOL programs");
}
```

---

## 3. Documentation Completeness

### Current Documentation: Extensive but Gaps for First-Time Users

**Files Reviewed:**
- README.md (719 lines) ✅ Excellent
- DEPLOYMENT.md (152 lines) ✅ Good
- PHASE1_CHANGELOG.md (583 lines) ✅ Technical, not user-facing

#### Strengths
- ✅ Comprehensive command reference
- ✅ Good examples for each command
- ✅ Troubleshooting section exists
- ✅ Configuration table with descriptions
- ✅ Multiple workflow examples

#### Critical Documentation Gaps

**Gap 3.1: No "First 5 Minutes" Quickstart**

**Current:** README starts with overview, then prerequisites, then installation.

**Problem:** New users want to see it work FIRST, then learn details.

**Recommendation:** Create **QUICKSTART.md** (separate file):

```markdown
# CursorOps - 5 Minute Quickstart

## What is CursorOps?
A tool that helps you understand COBOL programs by showing:
- Which programs call which other programs (call graph)
- Combined context of multiple files for AI analysis
- Team coding rules and prompt templates

## See It Work (1 minute)

1. Download and run:
   ```bash
   cd src/CursorOps/bin/Release/net8.0
   cursorops demo
   ```

2. You'll see examples of:
   - Team rules
   - Available prompts
   - Call graph tree visualization
   - Context package structure

## Try Your First Command (2 minutes)

1. Update config with your source path:
   ```bash
   notepad config\cursorops.json
   ```
   Change: `"RootDirectory": "C:\\Your\\COBOL\\Path"`

2. See your COBOL programs:
   ```bash
   cursorops cobol list
   ```

3. Trace a program:
   ```bash
   cursorops cobol trace --entry YOUR_PROGRAM --tree
   ```

## Common First-Time Issues

### "No programs found"
- Check RootDirectory in config points to COBOL files
- Verify file extensions match CobolFilePatterns (*.cbl, *.cob)

### "Config file not found"
- Run from: src/CursorOps/bin/Release/net8.0/
- Or set PATH to this directory

### "Entry program not found"
- Use exact PROGRAM-ID from your COBOL source
- Names are case-insensitive: SCR100 = scr100

## Next Steps
- Read full README.md for all commands
- Customize prompts/ and rules/ for your team
- See TROUBLESHOOTING.md for common issues
```

**Gap 3.2: No Visual Examples**

**Problem:** README has ASCII tree examples, but users don't see actual output examples for errors, warnings, colored output.

**Recommendation:** Add **EXAMPLES.md** with screenshots or actual terminal output:

```markdown
# CursorOps - Output Examples

## Successful Command Examples

### Example 1: Successful Trace
```
$ cursorops cobol trace --entry SCR100 --depth 2 --tree

Tracing COBOL call graph from 'SCR100' (depth: 2)...
Indexed 1,247 COBOL programs in C:\GSS\Source

Call Graph Statistics:
  Total Programs: 6
  Found Programs: 6
  Missing Programs: 0
  Total CALL Statements: 8
  Maximum Depth: 2

Call Tree:
└── SCR100 [scr100.cbl]
    ├── UTIL001 [util001.cbl]
    │   └── LOGGER [logger.cbl]
    └── DBACCESS [dbaccess.cbl]
        ├── DBREAD [dbread.cbl]
        └── DBWRITE [dbwrite.cbl]
```

## Error Examples (And How to Fix)

### Error 1: Wrong RootDirectory
```
$ cursorops cobol trace --entry SCR100

Root directory not found: C:\GSS\Source
→ FIX: Update config/cursorops.json -> RootDirectory
```

### Error 2: Program Not Found
```
$ cursorops cobol trace --entry SCR999

Indexed 1,247 COBOL programs in C:\GSS\Source
No programs found in call graph for 'SCR999'

Possible causes:
  1. Program 'SCR999' doesn't exist
  2. PROGRAM-ID doesn't match 'SCR999'
  3. File pattern mismatch
→ FIX: Check exact PROGRAM-ID name in your .cbl file
```
```

**Gap 3.3: No Separate Troubleshooting Guide**

**Current:** Troubleshooting section in README (lines 515-551) is good but buried.

**Recommendation:** Extract to **TROUBLESHOOTING.md** with these additions:

```markdown
# CursorOps Troubleshooting Guide

## Quick Diagnostics

### Check Your Installation
```bash
cursorops demo                    # Should show examples
cursorops config validate         # Should check config (if implemented)
cursorops prompt list             # Should show 4 prompts
```

### Check Your Configuration
```bash
# Verify these paths exist:
dir config\cursorops.json         # Config file
dir rules\rules.md                # Rules file
dir prompts                       # Prompts directory
dir C:\GSS\Source                 # Your RootDirectory from config
```

## Common Error Scenarios

### Scenario 1: "Cannot find cursorops.exe"
**Symptoms:**
- "cursorops is not recognized as internal or external command"

**Diagnosis:**
1. Are you in the correct directory?
   ```bash
   cd src\CursorOps\bin\Release\net8.0
   ```

2. Did the build succeed?
   ```bash
   dotnet build -c Release
   ```

3. Check if executable exists:
   ```bash
   dir cursorops.exe
   ```

**Solutions:**
- Run from correct directory
- Add directory to PATH (see README Installation section)
- Create an alias: `doskey cursorops=C:\path\to\cursorops.exe $*`

### Scenario 2: "No COBOL files indexed"
**Symptoms:**
- "Indexed 0 COBOL programs"
- "No programs found in call graph"

**Diagnosis:**
1. Check RootDirectory path:
   ```json
   // config/cursorops.json
   "RootDirectory": "C:\\GSS\\Source"  // Does this exist?
   ```

2. Check file patterns:
   ```json
   "CobolFilePatterns": ["*.cbl", "*.cob", "*.CBL", "*.COB"]
   ```

3. Manually verify COBOL files exist:
   ```bash
   dir /s C:\GSS\Source\*.cbl
   ```

**Solutions:**
- Update RootDirectory to correct path
- Add your file extensions to CobolFilePatterns (e.g., "*.cobol")
- Check for permission issues on directory

### Scenario 3: "CALL statements not detected"
**Symptoms:**
- Programs found but show "Calls: None"
- Call tree shows single program with no children

**Diagnosis:**
1. Check CALL syntax in your COBOL:
   ```cobol
   CALL "PROGNAME"      ← Detected by default patterns
   CALL PROGNAME        ← Detected by default patterns
   CALL WS-PROG-NAME    ← NOT detected (variable name)
   ```

2. Check CallPatterns in config

**Solutions:**
- Add custom patterns for your CALL syntax
- Use --verbose to see pattern matching details
- Test pattern: `cursorops config test-pattern "your regex"`

## Performance Issues

### Slow Indexing (> 1 minute)
**Causes:**
- Very large codebase (> 10,000 files)
- Network drive
- Antivirus scanning

**Solutions:**
- Use local drive instead of network
- Add exception in antivirus for cursorops.exe
- Index subset: update RootDirectory to specific subdirectory

### Regex Timeout Warnings
**Symptoms:**
- "Pattern matching took too long (timeout)"
- Files skipped during processing

**Causes:**
- Extremely large files (> 10,000 lines)
- Complex regex patterns in config

**Solutions:**
- Simplify CallPatterns in config
- Increase timeout (future feature)
- Split large files if possible

## Windows-Specific Issues

### Path Separator Confusion
**Wrong:**
```json
"RootDirectory": "C:/GSS/Source"  ← Unix style
```

**Correct:**
```json
"RootDirectory": "C:\\GSS\\Source"  ← Windows (escaped)
```

### File Locked by Excel/Editor
**Error:** "Permission denied: file is in use"

**Solution:** Close file in other programs before running cursorops

### Spaces in Paths
**Wrong:**
```bash
cursorops context pack --files C:\My Files\prog.cbl
```

**Correct:**
```bash
cursorops context pack --files "C:\My Files\prog.cbl"
```

## Still Having Issues?

1. Run with verbose mode (if implemented): `cursorops --verbose cobol trace ...`
2. Check log file (if implemented): `logs/cursorops.log`
3. Run demo to verify installation: `cursorops demo`
4. Contact GSS dev team with:
   - Command you ran
   - Error message (exact text)
   - Config file (config/cursorops.json)
   - Output of `cursorops demo`
```

**Gap 3.4: No FAQ Section**

**Recommendation:** Add **FAQ.md**:

```markdown
# CursorOps - Frequently Asked Questions

## General Questions

### Q: What is CursorOps?
A: A command-line tool that helps you analyze COBOL programs and create context packages for AI-assisted development with Cursor editor.

### Q: Do I need to know .NET or C# to use this?
A: No. You just run commands like `cursorops cobol trace --entry PROG`. You only need .NET installed (like you need Java installed to run Java programs).

### Q: Does this modify my COBOL source code?
A: No. CursorOps is read-only. It only reads your files and creates reports/context documents.

### Q: Does this connect to the internet or cloud?
A: No. Everything runs locally on your machine. No data is sent anywhere.

## Configuration Questions

### Q: Where is the config file?
A: `config/cursorops.json` relative to the cursorops.exe location.

### Q: What is RootDirectory?
A: The top-level folder containing your COBOL source code. CursorOps searches recursively for all *.cbl files under this folder.

### Q: Can I use multiple RootDirectory values?
A: Not currently. Use the highest common parent directory. Example: If you have C:\Source\COBOL and C:\Source\VB, use C:\Source.

### Q: What is MaxFileLines for?
A: Large files (> 500 lines by default) are truncated in context packages to avoid overwhelming Cursor's AI. You see the first 50 and last 50 lines.

## COBOL Questions

### Q: What is a "call graph"?
A: A tree showing which COBOL programs call which other programs. Example:
```
SCR100 calls UTIL001
UTIL001 calls LOGGER
```

### Q: Why does it say "No programs found"?
A: Common causes:
1. Wrong RootDirectory in config
2. Your PROGRAM-ID doesn't match the name you typed
3. File extensions don't match CobolFilePatterns

### Q: What does "depth" mean?
A: How many levels deep to trace calls.
- Depth 0: Just the entry program
- Depth 1: Entry + programs it directly calls
- Depth 2: Entry + direct calls + their calls
- Depth 3+: Keep going deeper

### Q: Why don't I see all CALL statements?
A: CursorOps uses regex patterns to find CALL statements. If your code uses variables (CALL WS-PROGRAM-NAME), you need to add custom patterns to config.

### Q: Does this work with dynamic CALL statements?
A: No. This is *static analysis* using regex patterns. It only finds CALL "literal" or CALL literal, not CALL variable-name.

## Command Questions

### Q: What's the difference between 'trace' and 'focus'?
A:
- `trace`: Shows call graph statistics and tree (quick, small output)
- `focus`: Shows call graph PLUS full source code for all programs (comprehensive, large output for Cursor)

### Q: Can I trace multiple programs at once?
A: Not directly. Run separate commands or create a script:
```batch
cursorops cobol trace --entry PROG1 --tree > prog1_trace.txt
cursorops cobol trace --entry PROG2 --tree > prog2_trace.txt
```

### Q: How do I save output to a file?
A: Use `--out` or `>` redirect:
```bash
cursorops rules inject --out rules.md
cursorops cobol trace --entry PROG > output.txt
```

## Error Questions

### Q: What does "path traversal detected" mean?
A: Security error. You tried to use `..` or `/` in a name. Example:
```bash
cursorops prompt pick ../../../etc/passwd  ← Security risk
cursorops prompt pick Refactor             ← Correct
```

### Q: What does "Regex timeout" warning mean?
A: A pattern matching operation took too long (> 2 seconds) and was stopped. Usually means a file has unusual syntax or a config pattern is too complex. File will be skipped.

### Q: What does "Permission denied" mean?
A: Windows won't let cursorops read the file. Common causes:
- File is open in Excel or another program
- File is read-protected
- Network drive disconnected

## Troubleshooting Questions

### Q: How do I verify my installation is working?
A: Run `cursorops demo`. You should see formatted output showing examples.

### Q: How do I validate my config?
A: Currently no built-in validator. Verify manually:
1. Check RootDirectory exists: `dir "C:\GSS\Source"`
2. Check rules exist: `dir rules\rules.md`
3. Check prompts exist: `dir prompts\*.md`

### Q: Where are log files?
A: Currently no log files. All output goes to console only. (Feature request for Phase 2)

### Q: How do I see more details for debugging?
A: Currently no verbose mode. (Feature request for Phase 2)

## Advanced Questions

### Q: Can I customize the prompts?
A: Yes! Add `.md` files to `prompts/` folder. They'll appear in `cursorops prompt list`.

### Q: Can I customize team rules?
A: Yes! Edit `rules/rules.md` with your team's coding standards.

### Q: Can I add custom CALL patterns?
A: Yes! Edit `config/cursorops.json` -> CallPatterns. Each pattern must have `(?<prog>...)` capture group.

### Q: Can I trace VB.NET or C# call graphs?
A: Not yet. COBOL only in version 1.0. VB.NET/C# support planned for future.

### Q: Can I integrate this with Monday.com or our ticketing system?
A: Not yet. Currently exports to Markdown files only. Integration planned for future.
```

---

## 4. Help Text Quality

### Current Help Text: Auto-Generated from System.CommandLine

#### Analysis Method
Inspected command definitions in Program.cs:
- Line 29: Root command description
- Lines 47-65: Rules command
- Lines 137-163: Prompt command
- Lines 299-330: Context command
- Lines 495-558: COBOL command
- Lines 788-794: Demo command

#### Strengths
- ✅ Consistent format across all commands
- ✅ Required vs optional parameters clear
- ✅ Default values shown
- ✅ Aliases documented (e.g., --out, -o)

#### Issues

**Issue 4.1: No Examples in Help Text**

**Current:**
```bash
$ cursorops cobol trace --help

Description:
  Trace COBOL call graph from entry program

Usage:
  cursorops cobol trace [options]

Options:
  --entry, -e <entry> (REQUIRED)    Entry program name (PROGRAM-ID)
  --depth, -d <depth>               Maximum depth to trace [default: 2]
  --graph, -g <graph>               Output call graph JSON file
  --tree, -t                        Show ASCII tree view [default: False]
```

**Problem:** No examples showing actual usage.

**Recommendation:** Add examples to command descriptions:

```csharp
var traceCommand = new Command("trace",
@"Trace COBOL call graph from entry program

Examples:
  cursorops cobol trace --entry SCR100 --tree
  cursorops cobol trace --entry PROG001 --depth 3 --graph output.json
  cursorops cobol trace -e UTIL100 -d 2 -g callgraph.json --tree
");
```

**Issue 4.2: No Explanation of Key Concepts**

**Example:** Users don't understand what "depth" means from help text alone.

**Recommendation:** Enhance option descriptions:

```csharp
new Option<int>(
    aliases: new[] { "--depth", "-d" },
    description: @"Maximum depth to trace (0=entry only, 1=entry+direct calls, 2=2 levels, etc.)
    Example: depth 2 means entry program -> programs it calls -> programs they call",
    getDefaultValue: () => 2)
```

**Issue 4.3: No Link to Full Documentation**

**Recommendation:** Add footer to all commands:

```csharp
var rootCommand = new RootCommand(
@"CursorOps - Local context engineering and COBOL call-graph analysis for Cursor editor

For full documentation, examples, and troubleshooting:
  README.md - Complete user guide
  TROUBLESHOOTING.md - Common issues and solutions
  Or visit: <internal wiki URL if available>
");
```

---

## 5. Logging Strategy Recommendations

### Current State: No Logging

**Problem:** All output goes to console only. When things go wrong, there's no persistent record of what happened.

**Critical for:**
- Debugging user issues remotely
- Understanding regex timeout warnings
- Tracking performance issues
- Security audit trail

### Recommended Logging Strategy

#### Level 1: Basic File Logging (High Priority)

**Implementation:**
```csharp
public static class Logger
{
    private static string? _logFilePath;
    private static readonly object _logLock = new object();

    public static void Initialize()
    {
        var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(logDir);

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _logFilePath = Path.Combine(logDir, $"cursorops_{timestamp}.log");

        Log("INFO", "CursorOps started");
        Log("INFO", $"Version: {Assembly.GetExecutingAssembly().GetName().Version}");
        Log("INFO", $"Config: {configPath}");
    }

    public static void Log(string level, string message)
    {
        if (_logFilePath == null) return;

        lock (_logLock)
        {
            var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
            File.AppendAllText(_logFilePath, entry + Environment.NewLine);
        }
    }

    public static void LogError(string message, Exception? ex = null)
    {
        Log("ERROR", message);
        if (ex != null)
        {
            Log("ERROR", $"  Exception: {ex.GetType().Name}");
            Log("ERROR", $"  Message: {ex.Message}");
            Log("ERROR", $"  Stack: {ex.StackTrace}");
        }
    }
}
```

**Usage:**
```csharp
// In Main()
Logger.Initialize();

// Everywhere errors occur
try
{
    content = File.ReadAllText(rulesPath);
}
catch (UnauthorizedAccessException ex)
{
    Logger.LogError($"Permission denied: {rulesPath}", ex);
    ConsoleHelper.WriteError($"Permission denied reading rules file: {ex.Message}");
    return;
}

// Log important operations
Logger.Log("INFO", $"Indexing COBOL files in: {rootDir}");
Logger.Log("INFO", $"Indexed {_programToFile.Count} programs");

// Log warnings
ConsoleHelper.WriteWarning($"Pattern missing 'prog' capture group: {patternStr}");
Logger.Log("WARN", $"Pattern missing capture group: {patternStr}");
```

**Log Location:**
- `logs/cursorops_20241029_143052.log`
- Keep last 10 logs, delete older

**Benefits:**
- Persistent record of all operations
- Can investigate issues after the fact
- Security audit trail
- Performance metrics (timing)

#### Level 2: Verbose Mode (Medium Priority)

**Add --verbose flag:**
```csharp
var globalOptions = new Option<bool>(
    aliases: new[] { "--verbose", "-v" },
    description: "Show detailed output for troubleshooting");

rootCommand.AddGlobalOption(globalOptions);
```

**Usage:**
```csharp
public static bool VerboseMode { get; set; }

// In handlers
if (VerboseMode)
{
    ConsoleHelper.WriteInfo($"Resolved path: {absolutePath}");
    ConsoleHelper.WriteInfo($"Canonical path: {canonicalPath}");
    ConsoleHelper.WriteInfo($"Pattern match: {pattern} -> {match.Success}");
}

Logger.Log("DEBUG", $"Pattern: {pattern}, Match: {match.Success}");
```

**Example:**
```bash
$ cursorops --verbose cobol trace --entry SCR100

[Verbose] Config loaded: C:\...\config\cursorops.json
[Verbose] RootDirectory: C:\GSS\Source (exists: True)
[Verbose] CobolFilePatterns: *.cbl, *.cob, *.CBL, *.COB
[Verbose] Searching: C:\GSS\Source\*.cbl
[Verbose]   Found: C:\GSS\Source\prog1.cbl
[Verbose]   Found: C:\GSS\Source\prog2.cbl
[Verbose] Searching: C:\GSS\Source\*.cob
[Verbose]   Found: C:\GSS\Source\util.cob
[Verbose] Total files found: 3
[Verbose] Extracting PROGRAM-ID from: prog1.cbl
[Verbose]   Program-ID: PROG001
[Verbose] Extracting PROGRAM-ID from: prog2.cbl
[Verbose]   Program-ID: SCR100
...
```

#### Level 3: Performance Logging (Low Priority)

**Track timing for optimization:**
```csharp
public class OperationTimer : IDisposable
{
    private string _operation;
    private Stopwatch _sw;

    public OperationTimer(string operation)
    {
        _operation = operation;
        _sw = Stopwatch.StartNew();
        Logger.Log("PERF", $"{operation} - Started");
    }

    public void Dispose()
    {
        _sw.Stop();
        Logger.Log("PERF", $"{operation} - Completed in {_sw.ElapsedMilliseconds}ms");
    }
}

// Usage
using (new OperationTimer("Index COBOL files"))
{
    IndexCobolFiles();
}
```

**Benefits:**
- Identify slow operations
- Justify performance improvements
- Track improvements over time

---

## 6. User-Facing Terminology Consistency

### Analysis of Terminology Across Codebase

**Terms Used:** Analyzed all console output (117 occurrences)

#### Generally Consistent Terms ✅
- "COBOL call graph" - used consistently
- "context package/pack" - used consistently
- "prompt template" - used consistently
- "team rules" - used consistently
- "entry program" - used consistently
- "depth" - used consistently

#### Inconsistent or Confusing Terms ⚠️

**Issue 6.1: "Focus" Command Name**

**Usage:**
```bash
cursorops cobol focus --entry PROG
```

**Problem:** "Focus" is abstract. Users don't understand what it does vs "trace".

**User Feedback Likely:**
- "What does 'focus' mean?"
- "When do I use 'trace' vs 'focus'?"
- "Is focus like 'zoom in'?"

**Recommendation:** Consider renaming or adding aliases:
```bash
cursorops cobol context --entry PROG      # More descriptive
cursorops cobol package --entry PROG      # Clearer
cursorops cobol focus --entry PROG        # Keep for backwards compatibility

# Or add description to help:
"Generate comprehensive Markdown context package (call graph + source code)"
```

**Issue 6.2: Mixed Metaphors in Command Names**

Commands use different metaphors:
- `rules inject` - medical/injection metaphor
- `prompt pick` - selection metaphor
- `context pack` - packaging metaphor
- `cobol trace` - debugging metaphor
- `cobol focus` - photography metaphor

**Not wrong, but lacks cohesion.**

**Minor recommendation:** Could unify around "get/show" verbs:
- `rules show` (inject is more colorful though)
- `prompt show <name>` (pick is more interactive though)

**Lower priority:** Current names are descriptive enough.

**Issue 6.3: Technical Terms Needing Glossary**

Terms that may confuse COBOL developers:

| Term | Where Used | May Confuse Users | Recommend |
|------|------------|-------------------|-----------|
| "Path traversal" | Security errors | Yes - security jargon | "Cannot use '..' or paths" |
| "ReDoS" | Regex timeout warnings | Yes - security jargon | "Pattern matching timeout" |
| "Canonical path" | Internal, not user-facing | N/A | Keep internal |
| "Regex" | Config comments | Maybe | Add "pattern" synonym |
| "Capture group" | Config comments | Maybe | Add examples |
| "Static analysis" | Documentation | Maybe | Add "reads code without running it" |
| "Breadth-first" | Code comments | N/A | Not user-facing |

**Recommendation:** Add glossary to README or FAQ:

```markdown
## Glossary

**Call Graph**: A tree diagram showing which programs call which other programs.

**Context Package**: A Markdown document combining source code, team rules, and call graphs for AI analysis.

**Depth**: How many levels deep to trace in a call graph. Depth 1 = entry + direct calls.

**Entry Program**: The starting program for call graph analysis (usually a screen program like SCR100).

**Pattern / Regex**: A template for matching text. Used to find CALL statements in COBOL code.

**Program-ID**: The name of a COBOL program defined in "PROGRAM-ID. name" in the source.

**Static Analysis**: Analyzing code by reading it (not running it). Faster but limited to literal CALL statements.

**Truncation**: Shortening large files to show only first/last sections.
```

---

## 7. Common User Errors and Prevention

### Predicted Common Errors (Based on UX Analysis)

#### Error Scenario 1: Wrong Working Directory

**User Action:**
```bash
C:\Users\JohnDoe> cursorops demo
```

**Error:**
```
Config file not found: C:\Users\JohnDoe\config\cursorops.json. Using defaults.
Rules file not found: C:\Users\JohnDoe\rules\rules.md
(Rules file not found - create rules/rules.md)
Prompts directory not found
```

**Why It Happens:** Users run cursorops from anywhere, expecting it to work.

**Current Prevention:** None - relies on PATH or running from build directory.

**Recommendations:**

1. **Better error message:**
```csharp
if (!File.Exists(configPath))
{
    ConsoleHelper.WriteError($"Config file not found: {configPath}");
    ConsoleHelper.WriteInfo("You may be in the wrong directory.");
    ConsoleHelper.WriteInfo($"Current directory: {Directory.GetCurrentDirectory()}");
    ConsoleHelper.WriteInfo($"Expected config at: {configPath}");
    ConsoleHelper.WriteInfo("");
    ConsoleHelper.WriteInfo("Solutions:");
    ConsoleHelper.WriteInfo("  1. Run from: src\\CursorOps\\bin\\Release\\net8.0\\");
    ConsoleHelper.WriteInfo("  2. Or add that path to your PATH environment variable");
    ConsoleHelper.WriteInfo("  3. Or use: cd /d " + AppContext.BaseDirectory);
    return 1;
}
```

2. **Add --config option:**
```bash
cursorops --config "C:\path\to\config\cursorops.json" cobol trace --entry PROG
```

3. **Search for config in parent directories:**
```csharp
// Look for config in current dir, then parent, then grandparent
var searchDir = Directory.GetCurrentDirectory();
for (int i = 0; i < 3; i++)
{
    var testPath = Path.Combine(searchDir, "config", "cursorops.json");
    if (File.Exists(testPath))
    {
        configPath = testPath;
        break;
    }
    searchDir = Directory.GetParent(searchDir)?.FullName;
    if (searchDir == null) break;
}
```

#### Error Scenario 2: Wrong PROGRAM-ID Case

**User Action:**
```bash
cursorops cobol trace --entry scr100
```

**COBOL Source:**
```cobol
PROGRAM-ID. SCR100.
```

**Current Behavior:** May or may not work depending on indexing logic.

**Recommendation:** Make search case-insensitive:

```csharp
// In IndexCobolFiles
_programToFile[programId.ToUpperInvariant()] = file;

// In BuildGraph
entryProgram = entryProgram.ToUpperInvariant();
```

**And add feedback:**
```csharp
ConsoleHelper.WriteInfo($"Searching for program: {entryProgram} (case-insensitive)");
```

#### Error Scenario 3: File Extensions Not Matching

**User Setup:**
- COBOL files: `*.COBOL`, `*.CBL`
- Config: `"CobolFilePatterns": ["*.cbl", "*.cob"]`

**User Action:**
```bash
cursorops cobol trace --entry PROG001
```

**Error:**
```
Indexed 0 COBOL programs in C:\Source
No programs found in call graph for 'PROG001'
```

**Why It Happens:** Pattern `*.cbl` doesn't match `*.COBOL`.

**Current Prevention:** None - users must deduce this from "Indexed 0 programs".

**Recommendations:**

1. **Show what files were searched:**
```csharp
Logger.Log("INFO", $"Searching for: {string.Join(", ", _config.CobolFilePatterns)}");
if (VerboseMode)
{
    ConsoleHelper.WriteInfo($"Searching for files matching: {string.Join(", ", _config.CobolFilePatterns)}");
}
```

2. **Warn if no files found:**
```csharp
if (_programToFile.Count == 0)
{
    ConsoleHelper.WriteWarning($"No COBOL files found in: {rootDir}");
    ConsoleHelper.WriteInfo("Searched for patterns: " + string.Join(", ", _config.CobolFilePatterns));
    ConsoleHelper.WriteInfo("");
    ConsoleHelper.WriteInfo("Troubleshooting:");
    ConsoleHelper.WriteInfo("  1. Verify RootDirectory in config is correct");
    ConsoleHelper.WriteInfo("  2. Check if your file extensions match CobolFilePatterns");
    ConsoleHelper.WriteInfo($"  3. Manually verify files exist: dir /s \"{rootDir}\\*.cbl\"");
}
```

#### Error Scenario 4: Output File Already Open

**User Action:**
```bash
cursorops cobol focus --entry SCR100 --out context.md
# User has context.md open in Excel or Word
```

**Error:**
```
Failed to write file: The process cannot access the file 'context.md' because it is being used by another process.
```

**Why It Happens:** Windows file locking.

**Current Prevention:** None - generic IOException caught.

**Recommendation:** Specific handling for file-in-use:

```csharp
catch (IOException ex)
{
    if (ex.Message.Contains("being used by another process"))
    {
        ConsoleHelper.WriteError($"Cannot write to '{safePath}' - file is open in another program");
        ConsoleHelper.WriteInfo("Solution: Close the file in Excel, Word, or any editor, then try again.");
    }
    else
    {
        ConsoleHelper.WriteError($"Failed to write file: {ex.Message}");
    }
    return;
}
```

#### Error Scenario 5: Depth Misunderstanding

**User Action:**
```bash
cursorops cobol trace --entry MAINPROG --depth 10
```

**Expectation:** "Show me everything!"

**Reality:** May trace thousands of programs, take minutes, create huge output.

**Current Prevention:** None.

**Recommendations:**

1. **Warn for large depths:**
```csharp
if (depth > 5)
{
    ConsoleHelper.WriteWarning($"Warning: Depth {depth} may trace many programs and take a while.");
    ConsoleHelper.WriteInfo("Typical depths: 1-3 for most use cases");
    Console.Write("Continue? (y/n): ");
    var response = Console.ReadLine();
    if (response?.ToLower() != "y")
    {
        ConsoleHelper.WriteInfo("Cancelled");
        return;
    }
}
```

2. **Add progress indicator:**
```csharp
Console.Write($"\rProcessing depth {currentDepth} - {visited.Count} programs so far...");
```

#### Error Scenario 6: Regex Pattern Typos

**User Action:** Edit config:
```json
"CallPatterns": [
  "CALL\\s+['\"](?<PROG>\\w+)['\"]"  // Wrong: capture group should be lowercase "prog"
]
```

**Current Behavior:** Warning at runtime:
```
Pattern missing 'prog' capture group, skipping: CALL\s+['\"](?<PROG>\w+)['\"]
```

**Problem:** Users see this warning ONLY when they run a trace, not when they edit config.

**Recommendations:**

1. **Add config validation command:**
```bash
cursorops config validate

Validating configuration...
✗ CallPatterns[0]: Capture group must be named 'prog' (found 'PROG')
  Pattern: CALL\s+['\"](?<PROG>\w+)['\"]
  Fix: Change (?<PROG>...) to (?<prog>...)
```

2. **Add config test command:**
```bash
cursorops config test-pattern "CALL\s+['\"](?<prog>\w+)['\"]"

Testing pattern: CALL\s+['\"](?<prog>\w+)['\"]

Test against sample COBOL:
  "CALL 'PROG001'" → Match: PROG001 ✓
  "CALL PROG002"  → No match
  "CALL \"UTIL\"" → Match: UTIL ✓

Pattern is valid.
```

---

## 8. Quick Wins for Better UX

### Immediate Improvements (1-2 hours each)

#### Quick Win 1: Add Version Command
```csharp
var versionCommand = new Command("version", "Show CursorOps version information");
versionCommand.SetHandler(() =>
{
    var version = Assembly.GetExecutingAssembly().GetName().Version;
    ConsoleHelper.WriteInfo($"CursorOps CLI Version {version}");
    ConsoleHelper.WriteInfo($".NET Runtime: {Environment.Version}");
    ConsoleHelper.WriteInfo($"Platform: {Environment.OSVersion}");
});
rootCommand.AddCommand(versionCommand);
```

**Benefit:** Users can report version in bug reports.

#### Quick Win 2: Add Config Info Command
```csharp
var configCommand = new Command("config", "Show current configuration");
var infoCommand = new Command("info", "Display loaded configuration");
infoCommand.SetHandler(() =>
{
    ConsoleHelper.WriteInfo("Current Configuration:");
    Console.WriteLine($"  Config File: {configPath}");
    Console.WriteLine($"  Root Directory: {_config.RootDirectory}");
    Console.WriteLine($"    (Exists: {Directory.Exists(_config.RootDirectory)})");
    Console.WriteLine($"  Rules File: {_config.RulesFile}");
    Console.WriteLine($"    (Exists: {File.Exists(Path.Combine(AppContext.BaseDirectory, _config.RulesFile))})");
    Console.WriteLine($"  Prompts Path: {_config.PromptsPath}");
    Console.WriteLine($"    (Exists: {Directory.Exists(Path.Combine(AppContext.BaseDirectory, _config.PromptsPath))})");
    Console.WriteLine($"  Max File Lines: {_config.MaxFileLines}");
    Console.WriteLine($"  COBOL Patterns: {string.Join(", ", _config.CobolFilePatterns)}");
    Console.WriteLine($"  Call Patterns: {_config.CallPatterns.Count} configured");
});
configCommand.AddCommand(infoCommand);
rootCommand.AddCommand(configCommand);
```

**Benefit:** Users can verify config loaded correctly.

#### Quick Win 3: Improve "No Programs Found" Error
```csharp
// Replace lines 574 and 634 in Program.cs
if (graph.Count == 0)
{
    ConsoleHelper.WriteWarning($"No programs found in call graph for '{entry}'");
    ConsoleHelper.WriteInfo("");
    ConsoleHelper.WriteInfo("Possible causes:");
    ConsoleHelper.WriteInfo($"  1. Program '{entry}' doesn't exist in {_config.RootDirectory}");
    ConsoleHelper.WriteInfo($"  2. PROGRAM-ID in source doesn't match '{entry}'");
    ConsoleHelper.WriteInfo($"  3. File patterns don't match your files");
    ConsoleHelper.WriteInfo("");
    ConsoleHelper.WriteInfo("Troubleshooting:");
    ConsoleHelper.WriteInfo($"  - Indexed: {builder.GetIndexedCount()} programs");
    ConsoleHelper.WriteInfo($"  - Run: cursorops cobol list (to see all programs)");
    return;
}
```

#### Quick Win 4: Add List Programs Command
```csharp
var listCommand = new Command("list", "List all indexed COBOL programs");
listCommand.SetHandler(() =>
{
    var builder = new CobolCallGraphBuilder(_config);
    builder.IndexCobolFiles(); // Expose this as public

    var programs = builder.GetIndexedPrograms(); // Add this method

    if (programs.Count == 0)
    {
        ConsoleHelper.WriteWarning("No COBOL programs found");
        ConsoleHelper.WriteInfo($"Searched in: {_config.RootDirectory}");
        ConsoleHelper.WriteInfo($"Patterns: {string.Join(", ", _config.CobolFilePatterns)}");
        return;
    }

    ConsoleHelper.WriteInfo($"Found {programs.Count} COBOL programs:");
    foreach (var kvp in programs.OrderBy(p => p.Key))
    {
        Console.WriteLine($"  {kvp.Key,-20} → {kvp.Value}");
    }
});
cobolCommand.AddCommand(listCommand);
```

**Benefit:** Users can see what programs are available before tracing.

#### Quick Win 5: Add Progress Indicator for Indexing
```csharp
// In CobolCallGraphBuilder.IndexCobolFiles()
ConsoleHelper.WriteInfo($"Indexing COBOL files in: {rootDir}");
Console.Write("Scanning");

foreach (var pattern in _config.CobolFilePatterns)
{
    // ...
    foreach (var file in files)
    {
        Console.Write(".");  // Progress dots
        // ...
    }
}

Console.WriteLine(); // New line after dots
ConsoleHelper.WriteSuccess($"Indexed {_programToFile.Count} COBOL programs");
```

**Benefit:** Visual feedback during long operations.

#### Quick Win 6: Add Elapsed Time to Statistics
```csharp
// In BuildGraph
var stopwatch = Stopwatch.StartNew();
// ... build graph ...
stopwatch.Stop();

// In GetStatistics
return $@"
Call Graph Statistics:
  Total Programs: {totalPrograms}
  Found Programs: {foundPrograms}
  Missing Programs: {missingPrograms}
  Total CALL Statements: {totalCalls}
  Maximum Depth: {maxDepth}
  Time Taken: {elapsed.TotalSeconds:F2}s
";
```

**Benefit:** Users know if performance is acceptable.

---

## 9. Sample Improved Error Messages

### Before & After Examples

#### Example 1: File Not Found

**BEFORE:**
```
Rules file not found: C:\CursorOps\bin\Release\net8.0\rules\rules.md
```

**AFTER:**
```
✗ Rules file not found

  Expected location: C:\CursorOps\bin\Release\net8.0\rules\rules.md

  Solutions:
  1. Create the file:
     > notepad "C:\CursorOps\bin\Release\net8.0\rules\rules.md"

  2. Update config to point to a different file:
     > notepad config\cursorops.json
     Change: "RulesFile": "path/to/your/rules.md"

  3. Use a command that doesn't need rules:
     > cursorops prompt list
     > cursorops context pack --files yourfile.cbl
```

#### Example 2: No Programs Found

**BEFORE:**
```
No programs found in call graph for 'SCR999'
```

**AFTER:**
```
⚠ No programs found in call graph for 'SCR999'

Possible causes:
  1. Program 'SCR999' doesn't exist
     → Check if SCR999.cbl exists in your source directory

  2. PROGRAM-ID doesn't match 'SCR999'
     → Open the .cbl file and verify: PROGRAM-ID. SCR999
     → Check for exact match (case-insensitive but spelling matters)

  3. File pattern mismatch
     → Current patterns: *.cbl, *.cob, *.CBL, *.COB
     → Your files may use different extensions (*.cobol, *.cpy, etc.)

  4. Wrong root directory
     → Currently searching: C:\GSS\Source
     → Verify this is correct in config\cursorops.json

Diagnostics:
  ✓ Indexed 1,247 programs from 1,247 files
  ✗ 'SCR999' not found in index

Troubleshooting:
  • See all indexed programs: cursorops cobol list
  • Validate config: cursorops config info
  • Check specific file: dir C:\GSS\Source\SCR999.*
```

#### Example 3: Permission Denied

**BEFORE:**
```
Permission denied reading prompt file: Access to the path is denied.
```

**AFTER:**
```
✗ Permission denied: Cannot read prompt file

  File: C:\CursorOps\prompts\Refactor.md

  Common causes on Windows:

  1. File is open in another program
     → Close the file in Notepad, Word, Excel, or any editor

  2. You don't have Read permission
     → Right-click file → Properties → Security tab
     → Verify your user account has "Read" permission

  3. File is on a network drive that's disconnected
     → Check if the drive is accessible: dir C:\CursorOps\prompts

  4. Antivirus is blocking access
     → Temporarily disable antivirus or add exception for cursorops.exe

  Need help? Contact your IT administrator with this error message.
```

#### Example 4: Path Traversal Security Error

**BEFORE:**
```
Invalid prompt name: path traversal detected
```

**AFTER:**
```
✗ Security error: Invalid prompt name

What happened:
  You tried to use '..' or a path in the prompt name.
  This is blocked for security reasons.

What you entered:
  ../../etc/passwd

What you should enter:
  Just the prompt name without any path or folders

Examples:
  ✓ cursorops prompt pick Refactor       (correct)
  ✓ cursorops prompt pick Summary        (correct)
  ✗ cursorops prompt pick ../Refactor    (blocked)
  ✗ cursorops prompt pick prompts/Refactor (blocked)

To see available prompts:
  cursorops prompt list
```

#### Example 5: Regex Timeout Warning

**BEFORE:**
```
Regex timeout in C:\Source\PROG001.cbl - possible ReDoS pattern
```

**AFTER:**
```
⚠ Pattern matching timeout in PROG001.cbl

What happened:
  A pattern took longer than 2 seconds to process this file.
  The file has been skipped to avoid freezing the tool.

This usually means:
  1. The file has unusual CALL statement format
  2. The file is extremely large (> 10,000 lines)
  3. A pattern in config is too complex or inefficient

Impact:
  PROG001.cbl will not be included in the call graph.
  This may affect accuracy if other programs call PROG001.

What you can do:
  • Review CALL statements in PROG001.cbl manually
  • Check CallPatterns in config\cursorops.json
  • Simplify regex patterns if you edited them
  • Contact support if this happens frequently

Technical details (for developers):
  Regex timeout: 2000ms
  File: C:\Source\PROG001.cbl
  This is a ReDoS (Regular Expression Denial of Service) protection
```

#### Example 6: Config Load Failed

**BEFORE:**
```
Failed to load config: Unexpected character encountered while parsing value
Using default configuration.
```

**AFTER:**
```
✗ Configuration file has errors

File: C:\CursorOps\config\cursorops.json
Error: Unexpected character encountered while parsing value

This usually means:
  • Missing comma between items
  • Extra comma at end of list
  • Unescaped backslash in paths (use \\ not \)
  • Comments in wrong format (must be // not #)

Example of valid config:
  {
    "RootDirectory": "C:\\GSS\\Source",  ← Note: \\ not \
    "CobolFilePatterns": [
      "*.cbl",     ← Comma here
      "*.cob"      ← No comma on last item
    ],
    "MaxFileLines": 500   ← No comma on last property
  }

Tips:
  • Use a JSON validator: https://jsonlint.com/
  • Copy from backup: config\cursorops.json.bak
  • Reset to defaults: del config\cursorops.json (will recreate)

Fallback:
  Using default configuration for now.
  Some commands may not work correctly.
```

---

## 10. Prioritized Recommendations

### Phase 2A: Critical UX Fixes (1-2 days)

1. **Implement basic file logging**
   - Log all errors, warnings, operations
   - Location: `logs/cursorops_TIMESTAMP.log`
   - Estimate: 3 hours

2. **Improve error messages (top 5)**
   - "No programs found" → Add diagnostics
   - "Config not found" → Add solutions
   - "Permission denied" → Add Windows-specific help
   - "Path traversal" → Remove jargon
   - "Regex timeout" → Explain in plain English
   - Estimate: 4 hours

3. **Add progress indicators**
   - "Scanning..." with dots during indexing
   - "Processing depth X..." during tracing
   - Estimate: 2 hours

4. **Add quick diagnostic commands**
   - `cursorops version` → Show version info
   - `cursorops config info` → Show loaded config
   - `cursorops cobol list` → List all programs
   - Estimate: 3 hours

**Total Phase 2A: 12 hours / 1.5 days**

### Phase 2B: Documentation Updates (1 day)

5. **Create QUICKSTART.md**
   - 5-minute getting started guide
   - First-time user focused
   - Estimate: 2 hours

6. **Create TROUBLESHOOTING.md**
   - Extract from README, expand
   - Add all error scenarios from this review
   - Estimate: 3 hours

7. **Create FAQ.md**
   - 20-30 common questions
   - COBOL developer focused
   - Estimate: 2 hours

8. **Update README with glossary**
   - Define technical terms
   - Add to existing README
   - Estimate: 1 hour

**Total Phase 2B: 8 hours / 1 day**

### Phase 2C: Advanced UX Features (2-3 days)

9. **Implement --verbose mode**
   - Global flag
   - Detailed operation logging
   - Estimate: 4 hours

10. **Add config validation command**
    - `cursorops config validate`
    - Check all paths, patterns, settings
    - Estimate: 3 hours

11. **Add config test-pattern command**
    - `cursorops config test-pattern "regex"`
    - Test regex against sample COBOL
    - Estimate: 2 hours

12. **Improve help text with examples**
    - Add examples to all commands
    - Add footer linking to docs
    - Estimate: 2 hours

13. **Add confirmation for risky operations**
    - Depth > 5 warning
    - Large file count warning
    - Estimate: 2 hours

**Total Phase 2C: 13 hours / 1.5 days**

### Phase 2D: Polish (1 day)

14. **Add timing/performance metrics**
    - Show elapsed time in statistics
    - Log performance data
    - Estimate: 2 hours

15. **Better default config**
    - Change RootDirectory to "."
    - Improve comments
    - Estimate: 1 hour

16. **Case-insensitive program search**
    - SCR100 = scr100 = Scr100
    - Estimate: 1 hour

17. **Search for config in parent dirs**
    - Don't require exact directory
    - Estimate: 2 hours

**Total Phase 2D: 6 hours / 1 day**

---

## 11. Long-Term UX Improvements (Phase 3+)

### Future Considerations (Not Immediate)

- **Interactive mode**: Menu-driven UI for non-technical users
- **Config wizard**: `cursorops config setup` guided configuration
- **Auto-detect patterns**: Analyze actual CALL syntax in codebase
- **Performance profiling**: Identify slow files/patterns
- **Plugin system**: Extensible for custom analyzers
- **GUI wrapper**: Electron or Avalonia app
- **Integrated documentation**: `cursorops help trace --examples`
- **Autocomplete**: Shell completion scripts (bash, PowerShell)

---

## Conclusion

CursorOps has excellent technical foundations after Phase 1 security fixes, but **developer experience is the critical gap** preventing smooth adoption by COBOL developers.

### Summary of Priorities

| Priority | Category | Effort | Impact | ROI |
|----------|----------|--------|--------|-----|
| 🔴 Critical | Logging system | 3h | High | ⭐⭐⭐⭐⭐ |
| 🔴 Critical | Improve top 5 errors | 4h | High | ⭐⭐⭐⭐⭐ |
| 🔴 Critical | Progress indicators | 2h | High | ⭐⭐⭐⭐ |
| 🔴 Critical | Diagnostic commands | 3h | High | ⭐⭐⭐⭐ |
| 🟡 High | QUICKSTART.md | 2h | High | ⭐⭐⭐⭐ |
| 🟡 High | TROUBLESHOOTING.md | 3h | High | ⭐⭐⭐⭐ |
| 🟡 High | FAQ.md | 2h | Medium | ⭐⭐⭐ |
| 🟢 Medium | --verbose mode | 4h | Medium | ⭐⭐⭐ |
| 🟢 Medium | Config validation | 3h | Medium | ⭐⭐⭐ |
| 🔵 Low | Help text examples | 2h | Low | ⭐⭐ |

### Recommended Action Plan

**Week 1 (Phase 2A + 2B):**
- Implement critical fixes (logging, errors, progress, diagnostics)
- Create missing documentation (QUICKSTART, TROUBLESHOOTING, FAQ)
- **Result:** Usable by first-time COBOL developers with clear guidance

**Week 2 (Phase 2C):**
- Add advanced features (verbose mode, config validation)
- Polish help text and examples
- **Result:** Professional, production-ready tool

**Week 3 (Phase 2D):**
- Performance metrics and polish
- Better defaults
- **Result:** Excellent DX, ready for team rollout

**After Phase 2: DX Score 9/10** (from current 6.5/10)

---

**Report Complete**
