# CursorOps CLI Tool

**Version:** 1.0.0
**Author:** Travis Jones
**Organization:** Global Shop Solutions (GSS)
**License:** Internal Use Only

---

## Overview

**CursorOps** is a Windows-based .NET 8 command-line tool designed for the GSS development team. It provides local, offline utilities for managing reusable prompts, team rules, context packets, and COBOL call-graph analysis for use with the Cursor editor.

### Key Features

- **Context Packaging**: Combine source files, team rules, and call graphs into Markdown documents
- **COBOL Call Graph Analysis**: Static regex-based tracing of COBOL program dependencies
- **Prompt Management**: Reusable prompt templates for common development tasks
- **Team Rules Injection**: Quick access to team coding standards and guidelines
- **Offline Operation**: No cloud dependencies, runs entirely on local machine
- **Cursor Integration**: Outputs optimized for Cursor editor context

### Supported Languages

- COBOL (ACU-COBOL, Fujitsu COBOL)
- VB.NET
- C#

---

## Prerequisites

- **Operating System**: Windows 10/11 or Windows Server
- **.NET SDK**: [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- **Development Environment**: Cursor editor (recommended)

---

## Installation

### 1. Clone or Download Repository

```bash
git clone <repository-url>
cd Cobolcaller
```

### 2. Configure Source Path

Edit `config/cursorops.json` and update the `RootDirectory` to point to your GSS source code:

```json
{
  "RootDirectory": "C:\\GSS\\Source",
  ...
}
```

### 3. Build the Project

```bash
cd src/CursorOps
dotnet build -c Release
```

### 4. Add to PATH (Optional)

For easier access, add the build output directory to your system PATH:

```
C:\path\to\Cobolcaller\src\CursorOps\bin\Release\net8.0
```

Or create an alias in your shell:

```bash
# PowerShell profile
Set-Alias cursorops "C:\path\to\Cobolcaller\src\CursorOps\bin\Release\net8.0\cursorops.exe"
```

---

## Quick Start

### Run the Demo

```bash
cursorops demo
```

This displays example output for all major features without modifying any files.

### Inject Team Rules

```bash
cursorops rules inject
```

Output team development rules to the console, or save to a file:

```bash
cursorops rules inject --out rules.md
```

### List Available Prompts

```bash
cursorops prompt list
```

### Use a Prompt Template

```bash
cursorops prompt pick Refactor
cursorops prompt pick Summary --out summary_prompt.md
```

---

## Command Reference

### Rules Commands

#### `cursorops rules inject`

Output team development rules to console or file.

**Options:**
- `--out, -o <path>` - Output file path (default: stdout)

**Examples:**
```bash
cursorops rules inject
cursorops rules inject --out team_rules.md
```

---

### Prompt Commands

#### `cursorops prompt list`

List all available prompt templates in the `prompts/` directory.

**Example:**
```bash
cursorops prompt list
```

**Output:**
```
Available prompt templates:
  - Refactor
  - Summary
  - BugFix
  - Documentation
```

#### `cursorops prompt pick <name>`

Output the content of a specific prompt template.

**Arguments:**
- `<name>` - Name of the prompt template (without .md extension)

**Options:**
- `--out, -o <path>` - Output file path (default: stdout)

**Examples:**
```bash
cursorops prompt pick Refactor
cursorops prompt pick Summary --out summary_prompt.md
```

---

### Context Commands

#### `cursorops context pack`

Combine source files, team rules, and optional call graph into a Markdown context document.

**Options:**
- `--files, -f <paths...>` - Source files to include (required)
- `--graph, -g <path>` - Optional call graph JSON file
- `--out, -o <path>` - Output file path (default: stdout)

**Examples:**
```bash
# Pack two files
cursorops context pack --files prog1.cbl utils.cs

# Pack with output file
cursorops context pack --files prog1.cbl prog2.cbl --out context.md

# Pack with call graph
cursorops context pack --files scr100.cbl --graph scr100_graph.json --out context.md
```

**Output Structure:**
1. Header with timestamp
2. Team development rules
3. Call graph (if provided)
4. Source files with syntax highlighting
5. Automatic truncation for large files

---

### COBOL Commands

#### `cursorops cobol trace`

Trace COBOL call graph from an entry program using regex-based static analysis.

**Options:**
- `--entry, -e <program>` - Entry program name (PROGRAM-ID) (required)
- `--depth, -d <number>` - Maximum depth to trace (default: 2)
- `--graph, -g <path>` - Output call graph JSON file
- `--tree, -t` - Show ASCII tree view

**Examples:**
```bash
# Trace with JSON output
cursorops cobol trace --entry SCR100 --depth 2 --graph callgraph.json

# Trace with tree visualization
cursorops cobol trace --entry SCR100 --depth 3 --tree

# Trace and save
cursorops cobol trace --entry UTIL001 --depth 1 --graph util001.json --tree
```

**ASCII Tree Output Example:**
```
└── SCR100 [scr100.cbl]
    ├── UTIL001 [util001.cbl]
    │   └── LOGGER [logger.cbl]
    └── DBACCESS [dbaccess.cbl]
        ├── DBREAD [dbread.cbl]
        └── DBWRITE [dbwrite.cbl]
```

**Statistics Output:**
```
Call Graph Statistics:
  Total Programs: 6
  Found Programs: 6
  Missing Programs: 0
  Total CALL Statements: 8
  Maximum Depth: 2
```

#### `cursorops cobol focus`

Generate a comprehensive Markdown context package for a COBOL program including:
- Team rules
- Call graph statistics and tree
- Source code for all programs in the call graph

**Options:**
- `--entry, -e <program>` - Entry program name (PROGRAM-ID) (required)
- `--depth, -d <number>` - Maximum depth to trace (default: 2)
- `--out, -o <path>` - Output Markdown file (default: `<PROGRAM>_context.md`)

**Examples:**
```bash
# Generate context with default output
cursorops cobol focus --entry SCR100 --depth 2

# Generate context with custom output file
cursorops cobol focus --entry SCR100 --depth 3 --out scr100_full_context.md
```

**Output File Structure:**
1. Header with program name and timestamp
2. Team development rules
3. Call graph statistics
4. ASCII call tree
5. Source code for all programs (organized by depth)

---

### Demo Command

#### `cursorops demo`

Display example output for all major features without modifying files. Useful for:
- Verifying installation
- Learning command syntax
- Understanding output formats

**Example:**
```bash
cursorops demo
```

---

## Helper Scripts

The `tools/` directory contains Windows batch scripts for common workflows.

### `cursor-focus.bat`

One-command context generation for COBOL programs.

**Usage:**
```batch
cursor-focus <PROGRAM-ID> [DEPTH]
```

**Examples:**
```batch
cursor-focus SCR100
cursor-focus SCR100 3
```

**Output:** Creates `<PROGRAM>_context.md` in the current directory.

### `cursor-trace.bat`

Quick call graph generation with tree view.

**Usage:**
```batch
cursor-trace <PROGRAM-ID> [DEPTH]
```

**Examples:**
```batch
cursor-trace SCR100
cursor-trace UTIL001 1
```

**Output:** Creates `<PROGRAM>_callgraph.json` and displays tree.

### `cursor-context.bat`

Simplified context packing.

**Usage:**
```batch
cursor-context <file1> <file2> ... [--out output.md]
```

**Examples:**
```batch
cursor-context prog1.cbl util.cs
cursor-context prog1.cbl prog2.cbl --out mycontext.md
```

### `cursor-rules.bat`

Quick access to team rules.

**Usage:**
```batch
cursor-rules [--out output.md]
```

---

## Configuration

Configuration is stored in `config/cursorops.json`.

### Configuration Properties

| Property | Type | Description | Default |
|----------|------|-------------|---------|
| `RootDirectory` | string | Path to source code root | `"."` |
| `CobolFilePatterns` | string[] | COBOL file extensions | `["*.cbl", "*.cob"]` |
| `VbFilePatterns` | string[] | VB.NET file extensions | `["*.vb"]` |
| `CsFilePatterns` | string[] | C# file extensions | `["*.cs"]` |
| `MaxFileLines` | number | Max lines before truncation | `500` |
| `TrimHeadLines` | number | Lines to show from file start | `50` |
| `TrimTailLines` | number | Lines to show from file end | `50` |
| `CallPatterns` | string[] | Regex patterns for CALL statements | See below |
| `RulesFile` | string | Path to rules Markdown file | `"rules/rules.md"` |
| `PromptsPath` | string | Directory containing prompt templates | `"prompts"` |

### COBOL Call Patterns

The `CallPatterns` array contains regex patterns for detecting COBOL CALL statements. Each pattern must include a named capture group `(?<prog>...)` to extract the called program name.

**Default Patterns:**
```json
[
  "CALL\\s+['\"](?<prog>\\w+)['\"]",      // CALL "PROGNAME"
  "CALL\\s+(?<prog>\\w+)",                // CALL PROGNAME
  "CALL\\s+['\"](?<prog>[A-Z0-9-]+)['\"]" // CALL with hyphens
]
```

**Custom Pattern Example:**
```json
"CALL\\s+WS-PROGRAM-NAME" // If using variable calls
```

---

## Typical Workflows

### Workflow 1: Understanding a COBOL Program

```bash
# 1. Generate call graph with tree view
cursorops cobol trace --entry SCR100 --depth 2 --tree

# 2. Generate full context
cursorops cobol focus --entry SCR100 --depth 2

# 3. Open SCR100_context.md in Cursor

# 4. Use Summary prompt
cursorops prompt pick Summary

# 5. Ask Cursor to summarize based on context
```

### Workflow 2: Refactoring with Context

```bash
# 1. Identify files to refactor
cursorops context pack --files old_prog.cbl utils.vb --out refactor_context.md

# 2. Get refactor prompt
cursorops prompt pick Refactor

# 3. Open refactor_context.md in Cursor

# 4. Paste refactor prompt and request changes
```

### Workflow 3: Bug Fix with Call Graph

```bash
# 1. Trace affected program
cursorops cobol focus --entry BUGGY_PROG --depth 3

# 2. Get bug fix prompt
cursorops prompt pick BugFix --out bugfix_prompt.md

# 3. Provide context to Cursor with call graph

# 4. Implement fix with full understanding of dependencies
```

### Workflow 4: Team Onboarding

```bash
# 1. Show demo
cursorops demo

# 2. Review team rules
cursorops rules inject

# 3. List available prompts
cursorops prompt list

# 4. Try a sample trace
cursorops cobol trace --entry SAMPLE_PROG --tree
```

---

## Customization

### Adding New Prompts

1. Create a new `.md` file in `prompts/` directory
2. Name it descriptively (e.g., `Performance.md`)
3. Write the prompt content in Markdown
4. Access with `cursorops prompt pick Performance`

**Example Prompt Template:**
```markdown
# Performance Optimization Prompt

## Objective
Identify and optimize performance bottlenecks in the provided code.

## Analysis
- Profile critical paths
- Identify inefficient algorithms
- Check database access patterns
- Review memory usage

## Deliverables
- List of bottlenecks with severity
- Recommended optimizations
- Estimated impact of each change
```

### Modifying Team Rules

Edit `rules/rules.md` to reflect your team's evolving standards. Changes are immediately available via `cursorops rules inject`.

### Extending COBOL Patterns

If your COBOL code uses non-standard CALL syntax:

1. Open `config/cursorops.json`
2. Add a new regex pattern to `CallPatterns`
3. Ensure it includes `(?<prog>...)` named capture group

**Example:**
```json
"PERFORM\\s+CALL-(?<prog>\\w+)" // Matches PERFORM CALL-PROGNAME
```

---

## Troubleshooting

### Issue: "Program not found" when tracing

**Cause:** The program file doesn't exist or isn't in the configured root directory.

**Solutions:**
1. Verify `RootDirectory` in `config/cursorops.json` points to your source code
2. Check that COBOL file patterns match your file extensions
3. Ensure the PROGRAM-ID in the source matches the entry program name

### Issue: No CALL statements detected

**Cause:** CALL syntax doesn't match configured regex patterns.

**Solutions:**
1. Review the CALL statement format in your COBOL code
2. Add a custom pattern to `CallPatterns` in configuration
3. Test the regex pattern against sample CALL statements

### Issue: Files truncated too aggressively

**Cause:** `MaxFileLines` is set too low for your files.

**Solutions:**
1. Increase `MaxFileLines` in `config/cursorops.json`
2. Adjust `TrimHeadLines` and `TrimTailLines` for better balance
3. Consider splitting large files for better maintainability

### Issue: "Config file not found"

**Cause:** Running from wrong directory or config file was moved.

**Solutions:**
1. Ensure `config/cursorops.json` exists relative to the executable
2. Run from the correct directory
3. Check that build copied config files (defined in `.csproj`)

---

## Architecture

### Project Structure

```
Cobolcaller/
├── src/
│   └── CursorOps/
│       ├── Program.cs              # Main CLI entry point and command handlers
│       ├── CursorOpsConfig.cs      # Configuration model and loader
│       ├── CobolCallGraphBuilder.cs # COBOL call graph analysis engine
│       └── CursorOps.csproj        # Project definition
├── config/
│   └── cursorops.json              # Configuration file
├── rules/
│   └── rules.md                    # Team development rules
├── prompts/
│   ├── Refactor.md                 # Refactoring prompt template
│   ├── Summary.md                  # Summary prompt template
│   ├── BugFix.md                   # Bug fix prompt template
│   └── Documentation.md            # Documentation prompt template
├── tools/
│   ├── cursor-focus.bat            # Helper script for COBOL focus
│   ├── cursor-trace.bat            # Helper script for call graph tracing
│   ├── cursor-context.bat          # Helper script for context packing
│   └── cursor-rules.bat            # Helper script for rules injection
└── README.md                       # This file
```

### Key Components

#### Program.cs
- CLI command definitions using `System.CommandLine`
- Command handlers for all subcommands
- Color-coded console output
- Markdown generation for context packing

#### CursorOpsConfig.cs
- Configuration model with JSON serialization
- Configuration loader with fallback to defaults
- Path resolution utilities
- Console helper for color-coded output

#### CobolCallGraphBuilder.cs
- COBOL file indexer (maps PROGRAM-ID to file paths)
- Regex-based CALL statement extractor
- Breadth-first search call graph builder
- JSON and Markdown serializers
- ASCII tree renderer

### Extension Points

The tool is designed for future enhancements:

- **GUI Layer**: Avalonia or WinUI frontend for visual call graph navigation
- **Monday.com Integration**: Sync context packages to tickets
- **VB.NET Analysis**: Extend call graph to VB.NET and C# projects
- **D3.js Visualization**: Interactive web-based call graph viewer
- **Git Integration**: Auto-generate context for changed files
- **CI/CD Integration**: Automated documentation generation

---

## Verification Checklist

Use this checklist to verify your installation:

- [ ] **Build Success**: Run `dotnet build -c Release` with no errors
- [ ] **Demo Works**: Run `cursorops demo` and see formatted output
- [ ] **Config Loads**: Edit `config/cursorops.json` and verify changes are used
- [ ] **Rules Inject**: Run `cursorops rules inject` and see team rules
- [ ] **Prompt List**: Run `cursorops prompt list` and see 4 prompts
- [ ] **Prompt Pick**: Run `cursorops prompt pick Refactor` and see content
- [ ] **Context Pack**: Run `cursorops context pack --files <any-file>` successfully
- [ ] **COBOL Trace** (if COBOL available): Run `cursorops cobol trace --entry <PROGRAM> --tree`
- [ ] **COBOL Focus** (if COBOL available): Run `cursorops cobol focus --entry <PROGRAM>`
- [ ] **Helper Scripts** (Windows only): Run `cursor-focus <PROGRAM>` or `cursor-rules`

---

## Development

### Building from Source

```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release

# Run without building
dotnet run -- demo

# Run specific command
dotnet run -- cobol trace --entry SCR100 --depth 2 --tree
```

### Code Style

- **File-scoped namespaces**: `namespace CursorOps;`
- **Nullable reference types**: Enabled for null safety
- **XML documentation**: All public APIs documented
- **Inline comments**: Complex logic explained for COBOL veterans
- **Modern C#**: Pattern matching, null-coalescing, LINQ where appropriate

### Adding New Commands

1. Create command in `Program.cs` using `System.CommandLine`
2. Add handler method with clear documentation
3. Update this README with command documentation
4. Add example to demo command
5. Create helper script in `tools/` if appropriate

---

## Support

For issues, questions, or feature requests:

1. Check this README's Troubleshooting section
2. Review configuration in `config/cursorops.json`
3. Run `cursorops demo` to verify installation
4. Contact the GSS development team

---

## License

**Internal Use Only**
Copyright (c) 2024 Global Shop Solutions
All rights reserved.

This tool is for internal use by Global Shop Solutions employees only. Distribution, modification, or use outside GSS is prohibited without explicit written permission.

---

## Changelog

### Version 1.0.0 (2024-10-29)

Initial release with:
- Team rules and prompt management
- Context packing for Cursor integration
- COBOL call graph analysis (static regex-based)
- Helper batch scripts
- Comprehensive documentation
- Demo command

---

## Acknowledgments

Built with:
- [.NET 8.0](https://dotnet.microsoft.com/)
- [System.CommandLine](https://github.com/dotnet/command-line-api)

Designed for use with:
- [Cursor Editor](https://cursor.sh/)
- [Monday.com](https://monday.com/)

---

**Maintained by:** GSS Development Team
**Last Updated:** October 2024
