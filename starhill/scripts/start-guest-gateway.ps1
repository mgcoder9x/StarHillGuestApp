[CmdletBinding()]
param(
    [ValidateRange(1024, 65535)]
    [int]$ListenPort = 18088,

    [string]$ApiUpstream = '127.0.0.1:18080',

    [switch]$SkipBuild
)

$nginxVersion = '1.28.3'
$nginxSha256 = 'aad7bf75d669ece7671688bfdf35f1093d6a30d2e62469405f4d55d8d82d5fd3'
$projectRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$webRoot = Join-Path $projectRoot 'web'
$guestDist = Join-Path $webRoot 'apps\guest-web\dist'
$templatePath = Join-Path $projectRoot 'deploy\nginx\guest-gateway.conf.template'
$runtimeRoot = Join-Path ([Environment]::GetFolderPath('UserProfile')) '.codex\runtimes\starhill-edge'
$nginxArchive = Join-Path $runtimeRoot "nginx-$nginxVersion.zip"
$nginxRoot = Join-Path $runtimeRoot "nginx-$nginxVersion"
$nginxExe = Join-Path $nginxRoot 'nginx.exe'
$runtimeConfig = Join-Path $nginxRoot 'conf\starhill-guest.conf'

New-Item -ItemType Directory -Path $runtimeRoot -Force | Out-Null

if (-not (Test-Path -LiteralPath $nginxArchive)) {
    Invoke-WebRequest -Uri "https://nginx.org/download/nginx-$nginxVersion.zip" -OutFile $nginxArchive -ErrorAction Stop
}

$actualHash = (Get-FileHash -LiteralPath $nginxArchive -Algorithm SHA256).Hash.ToLowerInvariant()
if ($actualHash -ne $nginxSha256) {
    throw "Nginx archive checksum mismatch. Expected $nginxSha256, got $actualHash."
}

if (-not (Test-Path -LiteralPath $nginxExe)) {
    Expand-Archive -LiteralPath $nginxArchive -DestinationPath $runtimeRoot -ErrorAction Stop
}

if (-not $SkipBuild) {
    Push-Location $webRoot
    try {
        & pnpm --filter '@starhill/guest-web' build
        if ($LASTEXITCODE -ne 0) {
            throw "Guest Web build failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}

if (-not (Test-Path -LiteralPath (Join-Path $guestDist 'index.html'))) {
    throw "Guest Web build output is missing at $guestDist."
}

$guestRootForNginx = $guestDist.Replace('\', '/')
$config = Get-Content -LiteralPath $templatePath -Raw
$config = $config.Replace('__STARHILL_LISTEN_PORT__', [string]$ListenPort)
$config = $config.Replace('__STARHILL_API_UPSTREAM__', $ApiUpstream)
$config = $config.Replace('__STARHILL_GUEST_ROOT__', $guestRootForNginx)
[IO.File]::WriteAllText($runtimeConfig, $config, [Text.UTF8Encoding]::new($false))

& $nginxExe -t -p "$nginxRoot/" -c 'conf/starhill-guest.conf'
if ($LASTEXITCODE -ne 0) {
    throw "Nginx configuration validation failed with exit code $LASTEXITCODE."
}

$pidPath = Join-Path $nginxRoot 'logs\starhill-guest.pid'
$nginxRunning = $false
if (Test-Path -LiteralPath $pidPath) {
    $nginxPid = 0
    $pidText = Get-Content -LiteralPath $pidPath -Raw
    if (-not [string]::IsNullOrWhiteSpace($pidText) -and [int]::TryParse($pidText.Trim(), [ref]$nginxPid)) {
        $nginxRunning = [bool](Get-Process -Id $nginxPid -ErrorAction SilentlyContinue)
    }
}

if ($nginxRunning) {
    & $nginxExe -p "$nginxRoot/" -c 'conf/starhill-guest.conf' -s reload
    if ($LASTEXITCODE -ne 0) {
        throw "Nginx reload failed with exit code $LASTEXITCODE."
    }
}
else {
    Start-Process -FilePath $nginxExe `
        -ArgumentList @('-p', "$nginxRoot/", '-c', 'conf/starhill-guest.conf') `
        -WorkingDirectory $nginxRoot `
        -WindowStyle Hidden
}

$healthUri = "http://127.0.0.1:$ListenPort/healthz"
$ready = $false
for ($attempt = 0; $attempt -lt 20; $attempt += 1) {
    try {
        $health = Invoke-WebRequest -Uri $healthUri -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        if ($health.StatusCode -eq 200) {
            $ready = $true
            break
        }
    }
    catch {
        Start-Sleep -Milliseconds 250
    }
}

if (-not $ready) {
    throw "Nginx did not become healthy at $healthUri."
}

Write-Output "StarHill guest gateway is ready at http://127.0.0.1:$ListenPort"
