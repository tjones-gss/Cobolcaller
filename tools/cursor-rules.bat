@echo off
REM ============================================================================
REM CursorOps Helper Script: Inject Team Rules
REM
REM Purpose: Output team development rules for use in Cursor
REM Usage: cursor-rules [--out output.md]
REM
REM Examples:
REM   cursor-rules
REM   cursor-rules --out rules.md
REM
REM Output: Prints rules to console or saves to file
REM ============================================================================

echo ============================================================================
echo CursorOps: Team Development Rules
echo ============================================================================
echo.

REM Forward all arguments to cursorops
cursorops rules inject %*

REM Check if command succeeded
if %ERRORLEVEL% EQU 0 (
  echo.
  echo ============================================================================
  echo Tip: Copy these rules into Cursor context for AI-assisted development
  echo ============================================================================
) else (
  echo.
  echo ERROR: Failed to retrieve rules
  exit /b 1
)
