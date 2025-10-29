@echo off
REM ============================================================================
REM CursorOps Helper Script: COBOL Call Graph Trace
REM
REM Purpose: Generate call graph JSON for a COBOL program
REM Usage: cursor-trace <PROGRAM-ID> [DEPTH]
REM
REM Examples:
REM   cursor-trace SCR100
REM   cursor-trace SCR100 3
REM
REM Output: Creates <PROGRAM-ID>_callgraph.json in current directory
REM ============================================================================

REM Check if program name was provided
if "%~1"=="" (
  echo ERROR: Program name required
  echo.
  echo Usage: cursor-trace ^<PROGRAM-ID^> [DEPTH]
  echo.
  echo Examples:
  echo   cursor-trace SCR100
  echo   cursor-trace SCR100 3
  exit /b 1
)

REM Set program name from first argument
set "PROGRAM=%~1"

REM Set depth (default to 2 if not provided)
set "DEPTH=%~2"
if "%DEPTH%"=="" set "DEPTH=2"

REM Set output file name
set "OUTPUT=%PROGRAM%_callgraph.json"

echo ============================================================================
echo CursorOps: Tracing call graph for COBOL program %PROGRAM%
echo Depth: %DEPTH%
echo Output: %OUTPUT%
echo ============================================================================
echo.

REM Call cursorops with the parameters and show tree
cursorops cobol trace --entry "%PROGRAM%" --depth %DEPTH% --graph "%OUTPUT%" --tree

REM Check if command succeeded
if %ERRORLEVEL% EQU 0 (
  echo.
  echo ============================================================================
  echo SUCCESS: Call graph created
  echo.
  echo Files created:
  echo   - %OUTPUT% (JSON format for programmatic use)
  echo.
  echo Use this graph to:
  echo   - Understand program dependencies
  echo   - Plan impact analysis for changes
  echo   - Generate full context with cursor-focus
  echo ============================================================================
) else (
  echo.
  echo ERROR: Failed to generate call graph
  exit /b 1
)
