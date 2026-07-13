#Requires -Version 5.1
<#
  Launcher xác minh CỐ ĐỊNH của BASE (command-governance AD-062). Sống TRONG platform/ để base tự-chứa.
  Mọi logic build/test/validate sống Ở ĐÂY — cần thêm bước → sửa file NÀY (không đổi tên lệnh `vp`).
  Gọi qua: platform\scripts\vp.cmd [scope]

  Scope:
    build    -> dotnet build Platform.slnx (cổng 0-warning; TreatWarningsAsErrors)
    ci       -> Python 3 platform\tests\validate_ci.py (tự chọn python hoặc Windows py -3)
    test     -> dotnet test Platform.slnx (test Docker SKIP mềm khi thiếu Docker — fixture lazy-build N-067)
    journal  -> JournalConsistencyTests (anti-drift INV-1..5)
    all      -> build + ci + test  (mặc định)

  Exit code: 0 = mọi bước OK; 1 = có bước fail.
#>
[CmdletBinding()]
param(
    [ValidateSet('all', 'build', 'ci', 'test', 'journal')]
    [string]$Scope = 'all'
)

$ErrorActionPreference = 'Continue'
# $PSScriptRoot = platform\tools → Platform = platform\ ; RepoRoot = gốc checkout (chứa .github/, resort-qr/...).
$Platform = Split-Path -Parent $PSScriptRoot
$RepoRoot = Split-Path -Parent $Platform
$Solution = Join-Path $Platform 'Platform.slnx'
$ArchProj = Join-Path $Platform 'tests\Bedrock.ArchitectureTests\Bedrock.ArchitectureTests.csproj'
$ValidateCi = Join-Path $Platform 'tests\validate_ci.py'

$results = [System.Collections.Generic.List[object]]::new()

function Invoke-Step {
    param([string]$Name, [scriptblock]$Body)
    Write-Host ''
    Write-Host "==== [vp] $Name ====" -ForegroundColor Cyan
    $global:LASTEXITCODE = 0
    & $Body
    $code = $LASTEXITCODE
    $ok = ($code -eq 0)
    $results.Add([pscustomobject]@{ Step = $Name; Ok = $ok; Code = $code })
    if ($ok) { Write-Host "[vp] $Name -> OK" -ForegroundColor Green }
    else { Write-Host "[vp] $Name -> FAILED (exit $code)" -ForegroundColor Red }
}

function Step-Build { dotnet build $Solution -clp:ErrorsOnly }
function Step-Ci {
    $python = Get-Command python -ErrorAction SilentlyContinue
    if ($null -ne $python) {
        & python --version *> $null
        if ($LASTEXITCODE -eq 0) {
            & python $ValidateCi
            return
        }
    }

    $py = Get-Command py -ErrorAction SilentlyContinue
    if ($null -ne $py) {
        & py -3 --version *> $null
        if ($LASTEXITCODE -eq 0) {
            & py -3 $ValidateCi
            return
        }
    }

    Write-Error "Không tìm thấy Python 3 khả dụng qua 'python' hoặc 'py -3'."
    $global:LASTEXITCODE = 127
}
function Step-TestFull { dotnet test $Solution --nologo }
function Step-TestNoBuild { dotnet test $Solution --no-build --nologo }
function Step-Journal { dotnet test $ArchProj --nologo --filter 'FullyQualifiedName~JournalConsistencyTests' }

switch ($Scope) {
    'build' { Invoke-Step 'build (0-warning)' ${function:Step-Build} }
    'ci' { Invoke-Step 'validate-ci' ${function:Step-Ci} }
    'test' { Invoke-Step 'test (full suite)' ${function:Step-TestFull} }
    'journal' { Invoke-Step 'journal-consistency' ${function:Step-Journal} }
    'all' {
        Invoke-Step 'build (0-warning)' ${function:Step-Build}
        Invoke-Step 'validate-ci' ${function:Step-Ci}
        Invoke-Step 'test (full suite, --no-build)' ${function:Step-TestNoBuild}
    }
}

Write-Host ''
Write-Host '==== [vp] SUMMARY ====' -ForegroundColor Cyan
foreach ($r in $results) {
    $tag = if ($r.Ok) { 'OK  ' } else { 'FAIL' }
    Write-Host ("  {0}  {1}" -f $tag, $r.Step)
}
if ($results.Where({ -not $_.Ok }).Count -gt 0) { exit 1 } else { exit 0 }
