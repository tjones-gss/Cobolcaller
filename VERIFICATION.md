# CursorOps Verification Guide

This guide provides step-by-step verification procedures to confirm your CursorOps installation is working correctly.

---

## Prerequisites Check

Before beginning verification, ensure you have:

- [ ] Windows 10/11 or Windows Server
- [ ] .NET 8.0 SDK installed ([Download](https://dotnet.microsoft.com/download/dotnet/8.0))
- [ ] Git (for cloning the repository)
- [ ] Terminal access (Command Prompt or PowerShell)

**Verify .NET Installation:**
```bash
dotnet --version
```
Expected output: `8.0.x` or higher

---

## Phase 1: Build Verification

### Step 1: Navigate to Project Directory

```bash
cd path\to\Cobolcaller\src\CursorOps
```

### Step 2: Clean Build

```bash
dotnet clean
dotnet build -c Release
```

**Expected Result:**
- Build succeeds with no errors
- Output directory created: `bin/Release/net8.0/`
- Executable created: `bin/Release/net8.0/cursorops.exe`

**Verification Checklist:**
- [ ] Build completes without errors
- [ ] cursorops.exe exists in output directory
- [ ] config/, rules/, and prompts/ folders copied to output directory

---

## Phase 2: Configuration Verification

### Step 1: Check Configuration Files

Navigate to output directory:
```bash
cd bin\Release\net8.0
dir config
dir rules
dir prompts
```

**Expected Files:**
- `config/cursorops.json`
- `rules/rules.md`
- `prompts/Refactor.md`
- `prompts/Summary.md`
- `prompts/BugFix.md`
- `prompts/Documentation.md`

**Verification Checklist:**
- [ ] All configuration files present
- [ ] JSON file is valid (open in text editor)
- [ ] Markdown files have content

### Step 2: Customize Configuration

Edit `config/cursorops.json`:
```json
{
  "RootDirectory": "C:\\Your\\GSS\\Source\\Path",
  ...
}
```

**Verification Checklist:**
- [ ] RootDirectory points to actual source code location
- [ ] COBOL file patterns match your extensions
- [ ] No syntax errors in JSON

---

## Phase 3: Command Verification

Run all tests from the `bin\Release\net8.0` directory.

### Test 1: Demo Command

```bash
.\cursorops.exe demo
```

**Expected Output:**
- Formatted demo output showing all features
- Color-coded text (green, yellow, cyan)
- No error messages

**Verification Checklist:**
- [ ] Demo runs without errors
- [ ] All 5 demo sections display
- [ ] Colors display correctly

---

### Test 2: Rules Injection

```bash
.\cursorops.exe rules inject
```

**Expected Output:**
- Team rules Markdown content displayed
- Rules from `rules/rules.md` file

**Verification Checklist:**
- [ ] Rules display correctly
- [ ] No "file not found" errors

**Test 2a: Save to File**
```bash
.\cursorops.exe rules inject --out test_rules.md
type test_rules.md
```

**Verification Checklist:**
- [ ] File created successfully
- [ ] Content matches console output

---

### Test 3: Prompt Commands

**List Prompts:**
```bash
.\cursorops.exe prompt list
```

**Expected Output:**
```
Available prompt templates:
  - Refactor
  - Summary
  - BugFix
  - Documentation
```

**Verification Checklist:**
- [ ] All 4 prompts listed
- [ ] No errors

**Pick a Prompt:**
```bash
.\cursorops.exe prompt pick Refactor
```

**Expected Output:**
- Full content of Refactor.md displayed

**Verification Checklist:**
- [ ] Prompt content displays
- [ ] Markdown formatting intact

**Save Prompt to File:**
```bash
.\cursorops.exe prompt pick Summary --out test_summary.md
type test_summary.md
```

**Verification Checklist:**
- [ ] File created
- [ ] Content correct

---

### Test 4: Context Pack (Generic Files)

Create a test source file:
```bash
echo // Test C# file > test.cs
echo IDENTIFICATION DIVISION. > test.cbl
```

**Pack Single File:**
```bash
.\cursorops.exe context pack --files test.cs --out context_test.md
type context_test.md
```

**Expected Output:**
- Markdown file with:
  - Header with timestamp
  - Team rules section
  - Source files section
  - Syntax-highlighted code

**Verification Checklist:**
- [ ] Context file created
- [ ] Includes header
- [ ] Includes team rules
- [ ] Includes source file with syntax highlighting

**Pack Multiple Files:**
```bash
.\cursorops.exe context pack --files test.cs test.cbl --out context_multi.md
```

**Verification Checklist:**
- [ ] Both files included
- [ ] Separate sections for each file

---

### Test 5: COBOL Call Graph (If COBOL Source Available)

**Note:** This test requires actual COBOL source files. Skip if not available.

**Trace Call Graph:**
```bash
.\cursorops.exe cobol trace --entry YOUR_PROGRAM --depth 2 --tree
```

**Expected Output:**
- Statistics section
- ASCII tree showing call hierarchy
- JSON output (if --graph specified)

**Verification Checklist:**
- [ ] Program indexing completes
- [ ] Statistics display
- [ ] Tree structure shows calls
- [ ] No crashes on missing programs

**Generate Full Context:**
```bash
.\cursorops.exe cobol focus --entry YOUR_PROGRAM --depth 2
```

**Expected Output:**
- Message: "Generating context for COBOL program..."
- File created: `YOUR_PROGRAM_context.md`
- Success message with program count

**Verification Checklist:**
- [ ] Context file created
- [ ] Includes call graph
- [ ] Includes source code
- [ ] File is valid Markdown

---

## Phase 4: Helper Scripts Verification (Windows Only)

Navigate to the tools directory:
```bash
cd ..\..\..\..\..\tools
```

### Test Helper Scripts

**cursor-rules.bat:**
```batch
cursor-rules.bat
```

**Verification Checklist:**
- [ ] Displays team rules
- [ ] No errors

**cursor-focus.bat** (requires COBOL):
```batch
cursor-focus.bat YOUR_PROGRAM
```

**Verification Checklist:**
- [ ] Creates context file
- [ ] Shows success message

---

## Phase 5: Integration Testing

### Workflow Test: Complete COBOL Analysis

If you have COBOL source available:

```bash
cd ..\src\CursorOps\bin\Release\net8.0

# 1. Trace the call graph
.\cursorops.exe cobol trace --entry MAIN_PROG --depth 2 --tree --graph callgraph.json

# 2. Generate full context
.\cursorops.exe cobol focus --entry MAIN_PROG --depth 2 --out main_context.md

# 3. Verify output files exist
dir callgraph.json
dir main_context.md

# 4. Check content
type main_context.md
```

**Verification Checklist:**
- [ ] Call graph JSON created
- [ ] Context Markdown created
- [ ] Both files have valid content
- [ ] No programs missing from call graph

---

### Workflow Test: Context Packing with Call Graph

```bash
# Create context with call graph included
.\cursorops.exe context pack --files YOUR_FILE.cbl --graph callgraph.json --out full_context.md

# Verify
type full_context.md
```

**Verification Checklist:**
- [ ] Context includes call graph section
- [ ] JSON embedded in markdown code block
- [ ] Source file included

---

## Phase 6: Edge Cases and Error Handling

### Test Error Handling

**Missing File:**
```bash
.\cursorops.exe context pack --files nonexistent.cs
```
**Expected:** Warning message in yellow

**Invalid Program:**
```bash
.\cursorops.exe cobol trace --entry FAKE_PROG --depth 1
```
**Expected:** Warning or empty graph with message

**Invalid Command:**
```bash
.\cursorops.exe invalid-command
```
**Expected:** Error message with help suggestion

**Verification Checklist:**
- [ ] Errors don't crash the application
- [ ] Error messages are clear and helpful
- [ ] Color coding works (red for errors, yellow for warnings)

---

### Test File Truncation

Create a large test file (>500 lines based on default config):

```bash
# PowerShell
1..600 | ForEach-Object { "Line $_" } | Out-File -FilePath large_test.cs

# Pack it
.\cursorops.exe context pack --files large_test.cs --out truncated_test.md

# Check for truncation message
type truncated_test.md | findstr /C:"truncated"
```

**Verification Checklist:**
- [ ] Truncation message appears
- [ ] First 50 lines included (default TrimHeadLines)
- [ ] Last 50 lines included (default TrimTailLines)
- [ ] Omission indicator shows middle lines count

---

## Phase 7: PATH Integration (Optional)

### Add to System PATH

**Option 1: Temporary (Current Session)**
```bash
set PATH=%PATH%;C:\path\to\Cobolcaller\src\CursorOps\bin\Release\net8.0
```

**Option 2: Permanent (System)**
1. Open System Properties → Environment Variables
2. Add to PATH: `C:\path\to\Cobolcaller\src\CursorOps\bin\Release\net8.0`
3. Restart terminal

**Verification:**
```bash
# From any directory
cursorops demo
```

**Verification Checklist:**
- [ ] Command works from any directory
- [ ] No path errors

---

## Phase 8: Cursor Integration Test

### Test in Cursor Editor

1. Generate a context file:
```bash
cursorops cobol focus --entry YOUR_PROGRAM --depth 2
```

2. Open the generated `YOUR_PROGRAM_context.md` in Cursor

3. Use a prompt:
```bash
cursorops prompt pick Summary
```

4. In Cursor:
   - Load the context file
   - Paste the Summary prompt
   - Ask Cursor to analyze the program

**Verification Checklist:**
- [ ] Context loads in Cursor without errors
- [ ] Syntax highlighting works
- [ ] Call graph displays correctly
- [ ] Cursor can reference the context in responses

---

## Troubleshooting Common Issues

### Issue: "dotnet: command not found"
**Solution:** Install .NET 8 SDK from Microsoft

### Issue: "Config file not found"
**Solution:** Ensure config/ folder is in the same directory as cursorops.exe

### Issue: "No programs found"
**Solution:**
1. Check RootDirectory in config/cursorops.json
2. Verify COBOL file patterns match your extensions
3. Ensure PROGRAM-ID exists in COBOL files

### Issue: Colors not displaying
**Solution:**
1. Use Windows Terminal (not legacy cmd.exe)
2. Or use PowerShell 7+
3. Check terminal supports ANSI colors

### Issue: "Prompt not found"
**Solution:** Ensure prompts/ folder is in the same directory as cursorops.exe

---

## Success Criteria

Your installation is fully verified if:

- [x] Build completes without errors
- [x] Demo command works
- [x] Rules inject works
- [x] Prompt list shows all templates
- [x] Prompt pick retrieves content
- [x] Context pack creates valid Markdown
- [x] COBOL trace works (if COBOL available)
- [x] COBOL focus creates context (if COBOL available)
- [x] Helper scripts execute (Windows)
- [x] Error handling is graceful
- [x] File truncation works correctly
- [x] Cursor can load generated contexts

---

## Final Deployment Checklist

Before team-wide deployment:

- [ ] Build tested on target Windows version
- [ ] Configuration template updated for GSS paths
- [ ] Team rules reflect current standards
- [ ] All prompt templates reviewed
- [ ] COBOL call patterns tested against actual code
- [ ] Documentation reviewed for accuracy
- [ ] Helper scripts tested
- [ ] Installation guide provided to team
- [ ] Training session scheduled (optional)
- [ ] Feedback mechanism established

---

## Performance Benchmarks (Optional)

For large codebases, measure performance:

```bash
# Measure time for call graph (PowerShell)
Measure-Command { .\cursorops.exe cobol trace --entry LARGE_PROG --depth 3 --graph out.json }
```

**Expected Performance:**
- Small programs (<10 calls): < 1 second
- Medium programs (10-50 calls): < 5 seconds
- Large programs (50+ calls): < 15 seconds

**Note:** Times depend on disk I/O and number of source files.

---

## Support and Feedback

After verification, document any issues or suggestions:

1. **Issues:** Create issue tracker entries for bugs
2. **Feature Requests:** Note enhancement ideas
3. **Documentation:** Suggest README improvements
4. **Configuration:** Share optimized config settings

---

**Document Version:** 1.0
**Last Updated:** 2024-10-29
**Maintained By:** GSS Development Team
