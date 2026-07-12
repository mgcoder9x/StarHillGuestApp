@echo off
REM ============================================================================
REM  vp = verify platform. LAUNCHER CỐ ĐỊNH của BASE (command-governance AD-062).
REM  Sống trong platform/ để base tự-chứa. KHÔNG viết logic ở đây — ủy quyền tools\verify.ps1.
REM  Dùng: vp            (all: build 0-warning + validate-ci + test)
REM        vp build|ci|test|journal
REM ============================================================================
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0..\tools\verify.ps1" %*
exit /b %ERRORLEVEL%
