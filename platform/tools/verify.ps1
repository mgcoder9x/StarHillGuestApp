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
    scaffold -> generate/restore/build/test the module template in a disposable tree
    all      -> build + ci + scaffold + web + test  (mặc định)

  Exit code: 0 = mọi bước OK; 1 = có bước fail.
#>
[CmdletBinding()]
param(
    [ValidateSet('all', 'build', 'ci', 'scaffold', 'test', 'web', 'journal', 'ops', 'release')]
    [string]$Scope = 'all'
)

$ErrorActionPreference = 'Stop'
$OutputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
# $PSScriptRoot = platform\tools → Platform = platform\ ; RepoRoot = gốc checkout chứa `.github/`.
$Platform = Split-Path -Parent $PSScriptRoot
$RepoRoot = Split-Path -Parent $Platform
$Solution = Join-Path $Platform 'Platform.slnx'
$ArchProj = Join-Path $Platform 'tests\Bedrock.ArchitectureTests\Bedrock.ArchitectureTests.csproj'
$ValidateCi = Join-Path $Platform 'tests\validate_ci.py'
$Web = Join-Path $Platform 'web'

$results = [System.Collections.Generic.List[object]]::new()

function Invoke-Step {
    param([string]$Name, [scriptblock]$Body)
    Write-Host ''
    Write-Host "==== [vp] $Name ====" -ForegroundColor Cyan
    $global:LASTEXITCODE = 0
    try {
        & $Body
        $code = if ($null -eq $LASTEXITCODE) { 0 } else { [int]$LASTEXITCODE }
    }
    catch {
        $code = if ($_.Exception -is [System.Management.Automation.CommandNotFoundException]) { 127 } else { 1 }
        Write-Host ("[vp] {0}: {1}" -f $Name, $_.Exception.Message) -ForegroundColor Red
    }
    $ok = ($code -eq 0)
    $results.Add([pscustomobject]@{ Step = $Name; Ok = $ok; Code = $code })
    if ($ok) { Write-Host "[vp] $Name -> OK" -ForegroundColor Green }
    else { Write-Host "[vp] $Name -> FAILED (exit $code)" -ForegroundColor Red }
}

function Add-BlockedStep {
    param([string]$Name, [string]$Reason)
    Write-Host ''
    Write-Host "==== [vp] $Name ====" -ForegroundColor Cyan
    Write-Host "[vp] $Name -> BLOCKED ($Reason)" -ForegroundColor Red
    $results.Add([pscustomobject]@{ Step = $Name; Ok = $false; Code = 125 })
}

function Resolve-NativeCommand {
    param([Parameter(Mandatory = $true)][string]$Name)

    $command = Get-Command $Name -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -eq $command) {
        throw [System.Management.Automation.CommandNotFoundException]::new(
            "Native command '$Name' was not found in PATH; verification stops fail-closed.")
    }

    return $command.Source
}

function Step-Build {
    $dotnet = Resolve-NativeCommand 'dotnet'
    & $dotnet restore $Solution --locked-mode
    if ($LASTEXITCODE -ne 0) { return }
    # Build ĐÚNG `-c Release` để khớp Step-TestNoBuild (`test -c Release --no-build`). Trước đây build Debug (mặc
    # định) còn test chạy Release → `--no-build` phải dựa vào artifact Release CŨ (stale); project mới chưa từng
    # build Release (vd Bedrock.ReferenceHost.Tests) → "dll not found" = FAIL giả, gate không tất định. Fix tận gốc.
    & $dotnet build $Solution --no-restore -c Release -clp:ErrorsOnly
}
function Step-Ci {
    $python = Get-Command python -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -ne $python) {
        & $python.Source --version *> $null
        if ($LASTEXITCODE -eq 0) {
            & $python.Source $ValidateCi
            return
        }
    }

    $py = Get-Command py -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -ne $py) {
        & $py.Source -3 --version *> $null
        if ($LASTEXITCODE -eq 0) {
            & $py.Source -3 $ValidateCi
            return
        }
    }

    throw [System.Management.Automation.CommandNotFoundException]::new(
        "Python 3 was not found via 'python' or 'py -3'.")
}

function Step-Operations {
    $python = Resolve-NativeCommand 'python'
    # Absolute path (Join-Path $Platform) — cwd-independent. Trước đây dùng relative `tools/...` nên chạy `vp all`
    # từ repo-root resolve nhầm sang root `tools/` (rỗng) → FAIL giả. Khớp pattern Step-Scaffold/module-template.
    & $python (Join-Path $Platform 'tools\validate-operations.py')
}
function Step-Release {
    $python = Resolve-NativeCommand 'python'
    & $python (Join-Path $Platform 'tools\validate-release.py')
}
function Step-TestFull {
    $dotnet = Resolve-NativeCommand 'dotnet'
    & $dotnet test $Solution -c Release --nologo -p:RestoreLockedMode=true
}
function Step-Web {
    $pnpm = Resolve-NativeCommand 'pnpm'
    Push-Location $Web
    try {
        & $pnpm install --frozen-lockfile
        if ($LASTEXITCODE -ne 0) { return }
        & $pnpm verify
    }
    finally {
        Pop-Location
    }
}
function Step-Scaffold {
    $python = Get-Command python -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -ne $python) {
        & $python.Source (Join-Path $Platform 'tools\verify-module-template.py')
        return
    }

    $py = Get-Command py -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -ne $py) {
        & $py.Source -3 (Join-Path $Platform 'tools\verify-module-template.py')
        return
    }

    throw [System.Management.Automation.CommandNotFoundException]::new(
        "Python 3 was not found via 'python' or 'py -3'.")
}
function Step-TestNoBuild {
    $dotnet = Resolve-NativeCommand 'dotnet'
    & $dotnet test $Solution -c Release --no-build --nologo
}
function Step-Journal {
    $dotnet = Resolve-NativeCommand 'dotnet'
    & $dotnet test $ArchProj --nologo --filter 'FullyQualifiedName~JournalConsistencyTests'
}

switch ($Scope) {
    'build' { Invoke-Step 'build (0-warning)' ${function:Step-Build} }
    'ci' { Invoke-Step 'validate-ci' ${function:Step-Ci} }
    'scaffold' { Invoke-Step 'module-scaffold' ${function:Step-Scaffold} }
    'ops' { Invoke-Step 'operations (alerts + dashboard)' ${function:Step-Operations} }
    'release' { Invoke-Step 'release contract' ${function:Step-Release} }
    'test' { Invoke-Step 'test (full suite)' ${function:Step-TestFull} }
    'web' { Invoke-Step 'web (frontend base)' ${function:Step-Web} }
    'journal' { Invoke-Step 'journal-consistency' ${function:Step-Journal} }
    'all' {
        Invoke-Step 'build (0-warning)' ${function:Step-Build}
        $buildOk = $results[$results.Count - 1].Ok
        Invoke-Step 'validate-ci' ${function:Step-Ci}
        Invoke-Step 'module-scaffold' ${function:Step-Scaffold}
        Invoke-Step 'operations (alerts + dashboard)' ${function:Step-Operations}
        Invoke-Step 'release contract' ${function:Step-Release}
        Invoke-Step 'web (frontend base)' ${function:Step-Web}
        if ($buildOk) {
            Invoke-Step 'test (full suite, --no-build)' ${function:Step-TestNoBuild}
        }
        else {
            Add-BlockedStep 'test (full suite, --no-build)' 'build failed'
        }
    }
}

Write-Host ''
Write-Host '==== [vp] SUMMARY ====' -ForegroundColor Cyan
foreach ($r in $results) {
    $tag = if ($r.Ok) { 'OK  ' } else { 'FAIL' }
    Write-Host ("  {0}  {1}" -f $tag, $r.Step)
}
if ($results.Where({ -not $_.Ok }).Count -gt 0) { exit 1 } else { exit 0 }
