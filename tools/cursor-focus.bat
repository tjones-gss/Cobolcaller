@echo off
REM ============================================================================
REM CursorOps Helper Script: COBOL Focus
REM
REM Purpose: Generate full Markdown context for a COBOL program and its call graph
REM Usage: cursor-focus <PROGRAM-ID> [DEPTH]
REM
REM Examples:
REM   cursor-focus SCR100
REM   cursor-focus SCR100 3
REM   cursor-focus UTIL001 1
REM
REM Output: Creates <PROGRAM-ID>_context.md in current directory
REM ============================================================================

REM Check if program name was provided
if "%~1"=="" (
  echo ERROR: Program name required
  echo.
  echo Usage: cursor-focus ^<PROGRAM-ID^> [DEPTH]
  echo.
  echo Examples:
  echo   cursor-focus SCR100
  echo   cursor-focus SCR100 3
  exit /b 1
)

REM Set program name from first argument
set "PROGRAM=%~1"

REM Set depth (default to 2 if not provided)
set "DEPTH=%~2"
if "%DEPTH%"=="" set "DEPTH=2"

REM Set output file name
set "OUTPUT=%PROGRAM%_context.md"

echo ============================================================================
echo CursorOps: Generating context for COBOL program %PROGRAM%
echo Depth: %DEPTH%
echo Output: %OUTPUT%
echo ============================================================================
echo.

REM Call cursorops with the parameters
cursorops cobol focus --entry "%PROGRAM%" --depth %DEPTH% --out "%OUTPUT%"

REM Check if command succeeded
if %ERRORLEVEL% EQU 0 (
  echo.
  echo ============================================================================
  echo SUCCESS: Context file created
  echo.
  echo Next steps:
  echo   1. Open %OUTPUT% in Cursor
  echo   2. Use as context for AI-assisted development
  echo   3. Reference call graph when making changes
  echo ============================================================================
) else (
  echo.
  echo ERROR: Failed to generate context file
  echo Check that:
  echo   1. Program %PROGRAM% exists in your source tree
  echo   2. CursorOps config points to correct source directory
  echo   3. COBOL file patterns match your file extensions
  exit /b 1
)
