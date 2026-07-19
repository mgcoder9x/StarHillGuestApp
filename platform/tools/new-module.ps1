[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Z][A-Za-z0-9]*$')]
    [string] $Name,

    [ValidatePattern('^[a-z][a-z0-9.-]*$')]
    [string] $ModuleKey,

    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'
$ModuleKey = if ($ModuleKey) { $ModuleKey } else { $Name.ToLowerInvariant() }

$python = Get-Command python -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
$arguments = @((Join-Path $PSScriptRoot 'new_module.py'), '--name', $Name, '--module-key', $ModuleKey)
if ($SkipBuild) { $arguments += '--skip-build' }
if ($null -ne $python) {
    & $python.Source @arguments
    exit $LASTEXITCODE
}

$py = Get-Command py -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1
if ($null -ne $py) {
    & $py.Source -3 @arguments
    exit $LASTEXITCODE
}

throw 'Python 3 is required to run the cross-platform module scaffolder.'
