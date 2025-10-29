@echo off
REM ============================================================================
REM CursorOps Helper Script: Context Pack
REM
REM Purpose: Combine multiple source files into a Markdown context package
REM Usage: cursor-context <file1> <file2> ... [--out output.md]
REM
REM Examples:
REM   cursor-context prog1.cbl util.cs
REM   cursor-context prog1.cbl prog2.cbl --out mycontext.md
REM
REM Output: Creates context.md (or specified output file)
REM ============================================================================

REM Check if at least one file was provided
if "%~1"=="" (
  echo ERROR: At least one source file required
  echo.
  echo Usage: cursor-context ^<file1^> ^<file2^> ... [--out output.md]
  echo.
  echo Examples:
  echo   cursor-context prog1.cbl util.cs
  echo   cursor-context prog1.cbl prog2.cbl --out mycontext.md
  exit /b 1
)

echo ============================================================================
echo CursorOps: Creating context package
echo ============================================================================
echo.

REM Forward all arguments to cursorops
cursorops context pack --files %*

REM Check if command succeeded
if %ERRORLEVEL% EQU 0 (
  echo.
  echo ============================================================================
  echo SUCCESS: Context package created
  echo.
  echo The package includes:
  echo   - Team development rules
  echo   - Source files with syntax highlighting
  echo   - Truncation for large files
  echo ============================================================================
) else (
  echo.
  echo ERROR: Failed to create context package
  exit /b 1
)
