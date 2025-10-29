# CursorOps Agent

## Role

CursorOps is a local context-engineering assistant designed for ERP modernization at Global Shop Solutions (GSS). It facilitates AI-assisted development by managing reusable prompts, team rules, COBOL call graphs, and context packaging for the Cursor editor.

## Context

The GSS development team works with a mixed technology stack:
- **Legacy Systems**: Fujitsu COBOL, ACU-COBOL
- **Mid-tier**: VB.NET applications
- **Modern**: C# .NET applications
- **Development Tools**: Cursor editor, Monday.com, Git

CursorOps bridges the gap between legacy COBOL systems and modern development practices by providing:
- Static analysis of COBOL call graphs
- Context packaging for AI-assisted development
- Standardized prompts and team rules
- Offline, local operation (no cloud dependencies)

## Skills

### 1. Context Engineering
- Generate Markdown context packages combining source files, rules, and call graphs
- Truncate large files intelligently (head/tail preservation)
- Include syntax highlighting for COBOL, C#, and VB.NET
- Package multiple files into cohesive context for Cursor

### 2. COBOL Call Graph Analysis
- Static regex-based tracing of COBOL CALL statements
- Breadth-first search traversal of program dependencies
- Multi-depth analysis (configurable depth limits)
- Output formats: JSON, Markdown, ASCII tree
- Identify missing programs and circular dependencies
- Generate statistics on call complexity

### 3. Prompt Management
- Maintain library of reusable prompt templates:
  - **Refactor**: Code improvement and modernization
  - **Summary**: Program analysis and documentation
  - **BugFix**: Systematic bug investigation and resolution
  - **Documentation**: Comprehensive code documentation
- List available prompts
- Retrieve specific prompt content
- Extensible template system

### 4. Team Rules Injection
- Provide quick access to team coding standards
- Support for COBOL, VB.NET, and C# guidelines
- Version control best practices
- AI-assisted development guidelines
- Security and testing standards

### 5. File System Operations
- Search for COBOL files by pattern (*.cbl, *.cob)
- Index PROGRAM-ID to file path mappings
- Resolve relative and absolute paths
- Handle Windows file system conventions

### 6. Command-Line Interface
- Slash-style subcommands (rules, prompt, context, cobol)
- Colorized console output (green=success, yellow=warning, red=error)
- Help text for all commands
- Demo mode for learning and verification

## Technical Capabilities

### COBOL Analysis
```
Input: Entry program name, depth limit
Process:
  1. Index all COBOL files in configured root directory
  2. Extract PROGRAM-ID from each file
  3. Build program name → file path mapping
  4. Starting from entry program, extract CALL statements
  5. Recursively traverse called programs up to depth limit
  6. Build adjacency graph of dependencies
Output: JSON graph, Markdown documentation, ASCII tree
```

### Context Packaging
```
Input: List of source files, optional call graph
Process:
  1. Load team rules from configuration
  2. Read each source file
  3. Apply truncation if file exceeds MaxFileLines
  4. Include call graph if provided
  5. Format with Markdown and syntax highlighting
Output: Single Markdown file for Cursor context
```

### Configuration-Driven
All behavior controlled by `config/cursorops.json`:
- Source code root directory
- File patterns for COBOL, VB, C#
- Truncation limits
- CALL statement regex patterns
- Rules and prompts paths

## Usage Examples

### Generate COBOL Call Graph
```bash
cursorops cobol trace --entry SCR100 --depth 2 --graph callgraph.json --tree
```

### Create Full Context for COBOL Program
```bash
cursorops cobol focus --entry SCR100 --depth 2 --out scr100_context.md
```

### Package Multiple Files
```bash
cursorops context pack --files prog1.cbl utils.cs dao.vb --out context.md
```

### Inject Team Rules
```bash
cursorops rules inject --out rules.md
```

### List and Use Prompts
```bash
cursorops prompt list
cursorops prompt pick Refactor
```

## Integration with Cursor

### Typical Workflow
1. **Identify scope**: Determine which COBOL program(s) need work
2. **Generate context**: Use `cobol focus` to create comprehensive context
3. **Load prompt**: Use `prompt pick` to get appropriate prompt template
4. **Open in Cursor**: Load context.md and prompt in Cursor
5. **AI-assisted development**: Use Cursor with full context and standardized prompts
6. **Validate**: Review AI suggestions against team rules and call graph

### Best Practices
- Always generate call graph for COBOL changes to understand impact
- Include team rules in context for standards compliance
- Use appropriate prompt template for task type
- Verify AI suggestions against existing patterns
- Test changes considering full call graph

## Constraints and Limitations

### Static Analysis Only
- COBOL analysis is regex-based (not semantic parsing)
- Cannot detect dynamic CALL statements (e.g., CALL from variables)
- No runtime analysis or execution tracing
- No data flow analysis

### File System Dependent
- Requires access to source code on local file system
- Depends on PROGRAM-ID matching conventions
- File patterns must match actual extensions
- Windows-centric (but .NET Core portable)

### No External Dependencies
- Runs entirely offline (by design)
- No API calls to cloud services
- No database connections
- Configuration file required for operation

### Language Support
- **Full support**: COBOL (call graph + context)
- **Context only**: C#, VB.NET
- **Future**: Call graph for .NET languages

## Extensibility

CursorOps is designed to be extended:

### Planned Enhancements
- **GUI Layer**: Avalonia/WinUI for visual call graph navigation
- **Monday.com Integration**: Sync context to project management tickets
- **VB.NET/C# Call Graphs**: Extend static analysis to .NET
- **D3.js Visualization**: Interactive web-based graph viewer
- **Git Integration**: Auto-context generation for commits
- **Semantic COBOL Parsing**: More accurate analysis

### Custom Prompts
Add new `.md` files to `prompts/` directory for team-specific templates.

### Custom Call Patterns
Extend `CallPatterns` in configuration for non-standard COBOL syntax.

## Environment

- **Platform**: Windows (primarily), Linux/Mac via .NET Core
- **Runtime**: .NET 8.0
- **Dependencies**: System.CommandLine (CLI parsing)
- **Storage**: Local file system only
- **Output**: Markdown, JSON, console text

## Security

- **No network access**: Completely offline
- **No credential storage**: No authentication required
- **Read-only source**: Does not modify source files
- **Output control**: User specifies all output paths
- **Configuration**: Plain JSON (no secrets)

## Support

CursorOps is maintained by the GSS development team for internal use. It serves as a bridge between legacy COBOL systems and modern AI-assisted development practices, enabling the team to leverage Cursor's capabilities while maintaining standards and understanding complex call graphs.

---

**Version**: 1.0.0
**Maintained By**: GSS Development Team
**Purpose**: Local context engineering for ERP modernization
**License**: Internal Use Only
