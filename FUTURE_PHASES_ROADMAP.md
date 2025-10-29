# CursorOps Future Phases Roadmap

**Document Version:** 1.0
**Date:** October 29, 2024
**Based On:** Multi-agent review synthesis
**Planning Horizon:** 6-12 months

---

## Quick Reference

| Phase | Focus | Effort | Status | Priority |
|-------|-------|--------|--------|----------|
| Phase 1 | Security & Stability Fixes | 2-3 days | ✅ **Complete** | CRITICAL |
| Phase 2A | Code Quality & Quick Wins | 1.5-2 days | 🏗️ **In Progress** | HIGH |
| Phase 2B | Critical Security Testing | 3-4 days | ⏳ Planned | HIGH |
| Phase 2C | Documentation & Error Messages | 0.5-1 day | ⏳ Planned | MEDIUM |
| Phase 3A | Error Handling & Config Tests | 1.5-2 days | 📋 Backlog | MEDIUM |
| Phase 3B | CI/CD Pipeline | 1 day | 📋 Backlog | MEDIUM |
| Phase 3C | Advanced Documentation | 1 day | 📋 Backlog | LOW |
| Phase 4A | SOLID Refactoring | 2-3 days | 📋 Backlog | LOW |
| Phase 4B | Async/Await & Parallelism | 3-5 days | 📋 Backlog | LOW |
| Phase 4C | Advanced Features | 3-4 days | 📋 Backlog | LOW |
| Phase 5+ | Advanced Capabilities | 30-60 days | 💡 Future | OPTIONAL |

**Total Completed:** 4-5 days
**Total Planned (2-4):** 13-22 days
**Total Optional (5+):** 30-60 days

---

## Completed Phases

### ✅ Phase 1: Security & Stability Fixes (COMPLETE)

**Duration:** 2-3 days
**Completion Date:** October 29, 2024
**Documentation:** `PHASE1_CHANGELOG.md`

**Achievements:**
- Fixed 6 path traversal vulnerabilities (CVSS 9.1)
- Mitigated 2 ReDoS vulnerabilities (CVSS 7.5)
- Fixed 4 index out of bounds crashes
- Fixed 11+ null safety issues
- Added exception handling to 11+ file I/O operations
- Created SecurityHelper.cs (312 lines)

**Risk Reduction:** CRITICAL → LOW

---

### 🏗️ Phase 2A: Code Quality & Quick Wins (IN PROGRESS)

**Duration:** 1.5-2 days (12-16 hours)
**Start Date:** October 29, 2024
**Documentation:** `PHASE2A_PLAN.md`

**Goals:**
- Eliminate 200+ lines of code duplication
- 3x performance improvement (60s → 20s for 1,000 files)
- Add logging system
- Add progress reporting
- Add `config validate` command

**Deliverables:**
- New: FileOperations.cs (~250 lines)
- New: LoggingHelper.cs (~150 lines)
- New: ConfigCommand.cs (~200 lines)
- Modified: Program.cs (-150 lines)
- Modified: CobolCallGraphBuilder.cs (stream-based I/O)

**Success Criteria:**
- Build succeeds, all tests pass
- Performance: 3x faster indexing
- UX: Logging and progress reporting work
- No regressions from Phase 1

---

## Planned Phases (Near-Term)

### ⏳ Phase 2B: Critical Security Testing

**Duration:** 3-4 days (24-32 hours)
**Prerequisite:** Phase 2A complete
**Documentation:** See `POST_PHASE1_TESTING_ANALYSIS.md`
**Priority:** HIGH (validates Phase 1 fixes)

#### Objectives
Implement automated testing for all Phase 1 security and stability fixes.

#### Deliverables

**1. Test Project Infrastructure**
```
tests/
├── CursorOps.Tests/
│   ├── CursorOps.Tests.csproj
│   ├── Security/
│   │   ├── SecurityHelperTests.cs (18 test methods)
│   │   └── PathTraversalTests.cs (8 test methods)
│   ├── CallGraph/
│   │   ├── CobolCallGraphBuilderTests.cs
│   │   └── ReDoSProtectionTests.cs (5 test methods)
│   ├── Commands/
│   │   └── FileTruncationTests.cs (6 test methods)
│   └── TestData/
│       ├── cobol/ (test COBOL files)
│       ├── configs/ (test configurations)
│       └── malicious/ (attack test cases)
```

**2. Test Coverage**
- 29+ test methods (18 HIGH priority)
- 80%+ code coverage of security-critical code
- All Phase 1 fixes validated

**3. Test Categories**

**Security Tests (CRITICAL):**
- Path traversal attacks (8 scenarios)
  - `../../../etc/passwd`
  - Absolute paths outside allowed directories
  - Null byte injection
  - Reserved Windows device names (CON, PRN, AUX)
  - Symbolic link traversal
  - UNC path attacks
  - Mixed separators (\\ and /)
  - Encoded path attacks (%2e%2e%2f)

- ReDoS attacks (4 scenarios)
  - Catastrophic backtracking patterns
  - Nested quantifiers `(a+)+b`
  - Overlapping alternatives
  - Regex timeout validation

**Crash Prevention Tests (HIGH):**
- Index out of bounds (6 scenarios)
  - Empty files (0 lines)
  - Small files (< TrimHeadLines)
  - Files < total trim (< TrimHead + TrimTail)
  - Exact boundary cases
  - Large files (10,000+ lines)
  - Very large files (100,000+ lines)

**4. Frameworks & Tools**
- xUnit (test framework)
- FluentAssertions (readable assertions)
- Moq (mocking framework)
- Coverlet (code coverage)
- Bogus (test data generation)

#### Effort Breakdown
| Task | Hours |
|------|-------|
| Test project setup | 2-3 |
| SecurityHelperTests implementation | 8-10 |
| ReDoSProtectionTests implementation | 4-5 |
| FileTruncationTests implementation | 3-4 |
| Test data creation | 3-4 |
| Coverage analysis & gaps | 2-3 |
| Documentation | 2-3 |
| **Total** | **24-32** |

#### Success Criteria
- [ ] All 29+ tests pass
- [ ] 80%+ code coverage on security code
- [ ] No Phase 1 fixes regress
- [ ] CI pipeline runs tests automatically
- [ ] Coverage report generated

#### Team Requirements
- 2 engineers working in parallel
- 1 engineer on SecurityHelperTests + PathTraversalTests (2 days)
- 1 engineer on ReDoSProtectionTests + FileTruncationTests (2 days)

#### Risk Assessment
**Low Risk** - Tests don't change production code, only validate it

---

### ⏳ Phase 2C: Documentation & Error Messages

**Duration:** 0.5-1 day (4-8 hours)
**Prerequisite:** Phase 2A complete
**Documentation:** See `POST_PHASE1_USABILITY_REVIEW.md`
**Priority:** MEDIUM (improves first-time user experience)

#### Objectives
Improve user experience through better documentation and error messages.

#### Deliverables

**1. QUICKSTART.md (NEW)**
Target: New user productive in 5 minutes

```markdown
# CursorOps Quick Start

## Installation (2 minutes)
1. Download CursorOps.zip
2. Extract to C:\Tools\CursorOps
3. Add to PATH: setx PATH "%PATH%;C:\Tools\CursorOps"

## Configuration (2 minutes)
1. Edit config/cursorops.json
   - Set "RootDirectory" to your COBOL source folder
2. Validate: `cursorops config validate`

## First Commands (1 minute)
```bash
# See COBOL programs
cursorops cobol trace --entry MAINPROG --depth 1

# Create context for Cursor
cursorops cobol focus --entry MAINPROG --out context.md
```

Done! Open context.md in Cursor editor.
```

**2. TROUBLESHOOTING.md (NEW)**
Target: Common issues and solutions

**Sections:**
- Installation problems
- Configuration errors
- "RootDirectory not found"
- "No COBOL programs found"
- "Permission denied" errors
- "Regex timeout" warnings
- Performance issues (slow indexing)
- Log file location and interpretation

**3. Improved Error Messages**
Add "Next steps:" to all errors

**Before:**
```
Error: Security error: Path traversal detected
```

**After:**
```
Error: Security error: Path traversal detected: '../../../etc/passwd' resolves
outside allowed directory

Next steps:
  1. Use only file names without path separators (/, \)
  2. Or use relative paths within current directory (./subdir/file.md)
  3. Use absolute paths are restricted to current directory and subdirectories

For help: cursorops --help or see TROUBLESHOOTING.md
```

**4. Updated README.md**
- Add "What's New in Phase 2" section
- Add troubleshooting quick links
- Add performance expectations table
- Add logging information

**5. Verbose Mode Help**
```bash
$ cursorops --verbose --help

Global options:
  --verbose, -v    Enable detailed logging
                   Log file: logs/cursorops_YYYYMMDD_HHMMSS.log
                   Use for troubleshooting errors

  --help, -h       Show help information
  --version        Show version information
```

#### Effort Breakdown
| Task | Hours |
|------|-------|
| QUICKSTART.md | 1-2 |
| TROUBLESHOOTING.md | 2-3 |
| Error message improvements | 1-2 |
| README.md updates | 0.5-1 |
| **Total** | **4.5-8** |

#### Success Criteria
- [ ] New user completes QUICKSTART in < 5 minutes
- [ ] All common errors documented in TROUBLESHOOTING
- [ ] All error messages include "Next steps:"
- [ ] README updated with Phase 2 features

---

## Backlog Phases (Medium-Term)

### 📋 Phase 3A: Error Handling & Configuration Tests

**Duration:** 1.5-2 days (12-16 hours)
**Prerequisite:** Phase 2B complete
**Priority:** MEDIUM (completes test coverage)

#### Objectives
Achieve 90%+ overall code coverage through additional tests.

#### Deliverables

**1. ConfigLoadTests (5 test methods)**
- Valid configuration loading
- Invalid JSON syntax handling
- Missing configuration file
- Invalid regex patterns in config
- Invalid file paths in config

**2. Exception Handling Tests (5 test methods)**
- Locked file handling (file in use)
- Permission denied handling
- Disk full scenario
- Network path disconnection
- Path too long exception

**3. Integration Tests (3 test methods)**
- End-to-end: rules inject workflow
- End-to-end: COBOL trace workflow
- End-to-end: context pack workflow

#### Success Criteria
- [ ] 90%+ overall code coverage
- [ ] All error paths tested
- [ ] Integration tests validate full workflows

---

### 📋 Phase 3B: CI/CD Pipeline

**Duration:** 1 day (6-8 hours)
**Prerequisite:** Phase 2B complete
**Priority:** MEDIUM (enables automated quality gates)

#### Objectives
Automated testing and release process.

#### Deliverables

**1. GitHub Actions Workflow**
```yaml
name: CI/CD Pipeline

on: [push, pull_request]

jobs:
  test:
    strategy:
      matrix:
        os: [windows-latest, ubuntu-latest, macos-latest]

    runs-on: ${{ matrix.os }}

    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore -c Release

      - name: Test
        run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage"

      - name: Upload Coverage
        uses: codecov/codecov-action@v3
        with:
          files: ./coverage.cobertura.xml
```

**2. Code Coverage Enforcement**
- Minimum 80% coverage required
- PR blocks if coverage drops
- Coverage badge in README

**3. Release Automation**
- Automatic versioning (SemVer)
- ZIP packaging for Windows
- TAR.GZ packaging for Linux/macOS
- GitHub Releases with changelog

#### Success Criteria
- [ ] Tests run on every PR
- [ ] Multi-platform testing (Win/Linux/Mac)
- [ ] Code coverage tracked
- [ ] Releases automated

---

### 📋 Phase 3C: Advanced Documentation

**Duration:** 1 day (6-8 hours)
**Prerequisite:** None (can run in parallel)
**Priority:** LOW (nice-to-have for broader adoption)

#### Deliverables

**1. FAQ.md**
20+ common questions and answers

**Categories:**
- Installation and setup
- Configuration
- COBOL-specific questions
- Performance tuning
- Troubleshooting
- Advanced usage

**2. Getting Started Video (5-10 minutes)**
- Screencast showing installation to first use
- Hosted on YouTube or GitHub
- Linked from README

**3. EXAMPLES.md**
5+ real-world scenarios with complete workflows

**Scenarios:**
- Scenario 1: Tracing a COBOL batch job
- Scenario 2: Documenting a COBOL screen program
- Scenario 3: Refactoring a utility module
- Scenario 4: Analyzing copybook dependencies
- Scenario 5: Preparing context for AI-assisted COBOL → C# migration

**4. Architecture Diagram**
Visual representation of:
- Component architecture
- Data flow
- Call graph building process
- Configuration hierarchy

---

## Long-Term Phases (Future)

### 📋 Phase 4A: SOLID Refactoring

**Duration:** 2-3 days (16-24 hours)
**Prerequisite:** Phase 3A complete (need comprehensive tests)
**Priority:** LOW (code quality improvement)

#### Objectives
Improve code architecture through SOLID principles.

#### Deliverables

**1. Method Decomposition**
- Decompose HandleContextPack: 150 lines → 50 lines
- Decompose HandleCobolFocus: 155 lines → 50 lines
- Extract helper methods for each concern

**2. Command Handler Classes**
Extract to separate files:
- RulesHandler.cs (HandleRulesInject)
- PromptHandler.cs (HandlePromptList, HandlePromptPick)
- ContextHandler.cs (HandleContextPack)
- CobolHandler.cs (HandleCobolTrace, HandleCobolFocus)

**3. Dependency Injection**
Functional DI pattern:
```csharp
public interface IFileSystem
{
    bool FileExists(string path);
    string ReadAllText(string path);
    void WriteAllText(string path, string content);
}

public class RulesHandler
{
    private readonly CursorOpsConfig _config;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;

    public RulesHandler(CursorOpsConfig config, IFileSystem fileSystem, ILogger logger)
    {
        _config = config;
        _fileSystem = fileSystem;
        _logger = logger;
    }
}
```

**4. Constants Extraction**
- Extract magic numbers (2000ms regex timeout, etc.)
- Create Constants.cs file
- Document all constants

**5. File Reorganization**
```
src/CursorOps/
├── Program.cs (main entry point only)
├── Commands/
│   ├── RulesCommand.cs
│   ├── PromptCommand.cs
│   ├── ContextCommand.cs
│   ├── CobolCommand.cs
│   └── ConfigCommand.cs
├── Handlers/
│   ├── RulesHandler.cs
│   ├── PromptHandler.cs
│   ├── ContextHandler.cs
│   └── CobolHandler.cs
├── Core/
│   ├── CobolCallGraphBuilder.cs
│   ├── SecurityHelper.cs
│   ├── FileOperations.cs
│   └── CursorOpsConfig.cs
├── Infrastructure/
│   ├── LoggingHelper.cs
│   ├── ConsoleHelper.cs
│   └── Constants.cs
└── Abstractions/
    ├── IFileSystem.cs
    └── ILogger.cs
```

#### Success Criteria
- [ ] All methods < 50 lines
- [ ] No code duplication > 5 lines
- [ ] Testability improved (DI enabled)
- [ ] All tests still pass

---

### 📋 Phase 4B: Async/Await & Parallelism

**Duration:** 3-5 days (24-40 hours)
**Prerequisite:** Phase 4A complete
**Priority:** LOW (performance optimization for very large codebases)

#### Objectives
5-10x performance improvement through asynchronous I/O and parallelism.

#### Deliverables

**1. Async File I/O**
Convert all synchronous file operations:
```csharp
// Before
var content = File.ReadAllText(path);

// After
var content = await File.ReadAllTextAsync(path, cancellationToken);
```

**2. Parallel Processing**
```csharp
// Parallel file indexing
await Parallel.ForEachAsync(
    files,
    new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = cancellationToken },
    async (file, ct) =>
    {
        var programId = await ExtractProgramIdAsync(file, ct);
        if (!string.IsNullOrEmpty(programId))
        {
            _programToFile.TryAdd(programId, file);
        }
    });
```

**3. Cancellation Support**
- Ctrl+C graceful shutdown
- CancellationToken throughout
- Progress saved on cancel

**4. Async Command Handlers**
Update System.CommandLine to use async handlers:
```csharp
traceCommand.SetHandler(async (entry, depth, graphPath, showTree, token) =>
{
    await HandleCobolTraceAsync(entry, depth, graphPath, showTree, token);
}, ...);
```

**5. Benchmarking**
- Create BenchmarkDotNet suite
- Measure before/after performance
- Document improvements

#### Performance Targets
| Codebase Size | Before | After | Improvement |
|---------------|--------|-------|-------------|
| 100 files | 2-5 sec | 1-2 sec | 2-3x |
| 1,000 files | 60-120 sec | 10-20 sec | 5-6x |
| 10,000 files | 30+ min | 4-6 min | 5-10x |

#### Success Criteria
- [ ] 5x minimum performance improvement
- [ ] Cancellation works correctly
- [ ] No deadlocks or race conditions
- [ ] All tests pass (updated to async)

---

### 📋 Phase 4C: Advanced Features

**Duration:** 3-4 days (24-32 hours)
**Prerequisite:** Phase 4B complete
**Priority:** LOW (nice-to-have features)

#### Objectives
Add advanced capabilities for power users.

#### Deliverables

**1. File Content Caching**
LRU cache for frequently accessed files:
```csharp
public class FileCache
{
    private readonly int _maxCacheSize;
    private readonly LinkedList<CacheEntry> _lruList;
    private readonly Dictionary<string, LinkedListNode<CacheEntry>> _cache;

    public string? TryGet(string path, DateTime lastModified)
    {
        // Return cached content if file hasn't changed
    }

    public void Add(string path, string content, DateTime lastModified)
    {
        // Add to cache, evict LRU if full
    }
}
```

**Impact:** 3x faster for repeated operations (context pack after cobol trace)

**2. Incremental Indexing**
Only re-scan changed files:
```csharp
// Save index to .cursorops/index.json
// On next run, only process files newer than last index
```

**Impact:** 100x faster for repeated runs (2min → 1sec)

**3. Configuration Profiles**
```json
{
  "profiles": {
    "dev": {
      "RootDirectory": "C:\\Dev\\Source"
    },
    "test": {
      "RootDirectory": "C:\\Test\\Source"
    },
    "prod": {
      "RootDirectory": "\\\\NetworkShare\\Prod\\Source"
    }
  }
}
```

```bash
cursorops --profile prod cobol trace --entry MAIN
```

**4. Watch Mode**
Auto-regenerate on file changes:
```bash
cursorops cobol focus --entry MAIN --watch
# Regenerates context.md whenever COBOL files change
```

**5. Additional Output Formats**
- JSON output (in addition to Markdown)
- XML output
- HTML output with interactive navigation

#### Success Criteria
- [ ] Caching provides 3x speedup
- [ ] Incremental indexing provides 100x speedup
- [ ] Profiles work correctly
- [ ] Watch mode detects changes
- [ ] New output formats valid

---

### 💡 Phase 5+: Advanced Capabilities (Optional)

**Duration:** 30-60 days
**Priority:** OPTIONAL (only if user demand emerges)

#### Potential Features

**1. VB.NET Call Graph Analysis**
Extend to analyze VB.NET projects:
- Parse `.vb` and `.vbproj` files
- Extract method calls
- Generate VB.NET call graphs
- Cross-reference with COBOL

**Effort:** 10-15 days

**2. C# Call Graph Analysis**
Extend to analyze C# projects:
- Parse `.cs` and `.csproj` files
- Use Roslyn semantic analysis
- Extract method calls
- Generate C# call graphs

**Effort:** 10-15 days

**3. Cross-Language Call Graphs**
Trace COBOL → .NET interop:
- Identify COBOL programs that call .NET
- Identify .NET programs that call COBOL
- Generate unified call graph
- Visualize cross-language dependencies

**Effort:** 15-20 days

**4. Semantic COBOL Parser**
Replace regex with proper COBOL parser:
- Antlr4 grammar for COBOL
- Full AST generation
- Semantic analysis
- Handle multi-line CALL statements
- Handle dynamic CALLs

**Effort:** 20-30 days

**5. GUI Layer**
Desktop application:
- Avalonia (cross-platform)
- Visual call graph navigation
- Interactive configuration
- Drag-and-drop file selection
- Real-time preview

**Effort:** 30-40 days

**6. VS Code Extension**
Integrate with VS Code:
- Context menu integration
- Inline call graph visualization
- Hover tooltips for CALL statements
- Jump to definition for COBOL programs

**Effort:** 15-20 days

**7. Cursor Editor Native Plugin**
Native Cursor integration:
- Auto-generate context on file open
- Smart context selection based on cursor position
- Inline suggestions for COBOL modernization

**Effort:** 20-30 days

---

## Decision Framework

Use this framework to decide which phase to implement next:

### Priority Matrix

| Criteria | Weight | Phase 2 | Phase 3 | Phase 4 | Phase 5+ |
|----------|--------|---------|---------|---------|----------|
| User Impact | 40% | HIGH | MEDIUM | LOW | VERY LOW |
| Risk Reduction | 30% | HIGH | MEDIUM | LOW | VERY LOW |
| ROI (Impact/Effort) | 20% | HIGH | MEDIUM | LOW | VERY LOW |
| Technical Debt | 10% | HIGH | MEDIUM | HIGH | LOW |
| **Total Score** | | **9.0/10** | **6.5/10** | **4.5/10** | **2.0/10** |

### Recommended Sequence

**Immediate (This Session):**
- ✅ Phase 1: Security fixes (DONE)
- 🏗️ Phase 2A: Code quality & quick wins (IN PROGRESS)

**Next Session (User Testing):**
- ⏳ Phase 2B: Security testing (validate Phase 1)
- ⏳ Phase 2C: Documentation (improve UX)

**After Team Feedback (1-2 weeks):**
- 📋 Phase 3A: Additional tests (if needed)
- 📋 Phase 3B: CI/CD (if team adopts tool)
- 📋 Phase 3C: Advanced docs (if team requests)

**Long-Term (2-6 months):**
- 📋 Phase 4A: SOLID refactoring (if codebase grows)
- 📋 Phase 4B: Async/await (if performance issues)
- 📋 Phase 4C: Advanced features (if power users request)

**Future (6+ months):**
- 💡 Phase 5+: Only implement if specific user demand emerges

---

## Version Planning

| Version | Phases Included | Target Date | Status |
|---------|-----------------|-------------|--------|
| v1.0.0 | Phase 1 | Oct 29, 2024 | ✅ Released |
| v1.1.0 | Phase 2A | Nov 1, 2024 | 🏗️ In Progress |
| v1.2.0 | Phase 2B-2C | Nov 15, 2024 | ⏳ Planned |
| v1.3.0 | Phase 3A-3C | Dec 2024 | 📋 Backlog |
| v2.0.0 | Phase 4A-4C | Q1 2025 | 📋 Backlog |
| v3.0.0 | Phase 5+ | TBD | 💡 Future |

---

## Success Metrics

Track these metrics to guide phase prioritization:

**Adoption Metrics:**
- Number of team members using tool
- Commands executed per week
- Average session length

**Quality Metrics:**
- Bug reports per month
- Test coverage percentage
- Build success rate

**Performance Metrics:**
- Average indexing time
- Peak memory usage
- User-reported performance issues

**UX Metrics:**
- Support questions per month
- Time to first successful command
- User satisfaction score (1-10)

**Target After Phase 2:**
- Adoption: 5+ team members
- Quality: < 1 bug/month, 85%+ coverage
- Performance: < 30 sec for 1,000 files
- UX: < 2 questions/month, < 10 min to first success, 8+/10 satisfaction

**Target After Phase 3:**
- Adoption: 10+ team members
- Quality: < 0.5 bugs/month, 90%+ coverage
- Performance: < 20 sec for 1,000 files
- UX: < 1 question/month, < 5 min to first success, 9+/10 satisfaction

---

## Conclusion

This roadmap provides a clear path forward for CursorOps development. **Phase 2-3 (13-22 days) should be sufficient for production-grade quality.** Phase 4-5 are optional optimizations that may never be needed.

**Key Principle:** Ship early, iterate based on feedback, avoid over-engineering.

**Next Steps:**
1. Complete Phase 2A (this session)
2. Gather user feedback at work
3. Decide on Phase 2B-2C timeline based on feedback
4. Re-evaluate Phase 3-5 based on adoption and needs

