$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Building Standalone OpenPrestige.exe  " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    $csc = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path $csc)) {
    Write-Error "Microsoft .NET C# Compiler (csc.exe) not found."
    exit 1
}

# Ensure server.pfx exists
$pfx = Join-Path $root "crl\server.pfx"
$pem = Join-Path $root "crl\leaf-combined.pem"
if (-not (Test-Path $pfx) -and (Test-Path $pem)) {
    Write-Host "[*] Exporting server.pfx from leaf-combined.pem..." -ForegroundColor Gray
    $openssl = "C:\Program Files\Git\usr\bin\openssl.exe"
    if (Test-Path $openssl) {
        & $openssl pkcs12 -export -out "$pfx" -in "$pem" -passout pass:
    }
}

# Ensure flows.zip exists
$flowsZip = Join-Path $root "flows.zip"
$flowsDir = Join-Path $root "flows"
if (-not (Test-Path $flowsZip) -and (Test-Path $flowsDir)) {
    Write-Host "[*] Compressing flows to flows.zip..." -ForegroundColor Gray
    Compress-Archive -Path "$flowsDir\*" -DestinationPath "$flowsZip" -Force
}

# Ensure Inter.ttf exists
$fontFile = Join-Path $root "Inter.ttf"
if (-not (Test-Path $fontFile)) {
    Write-Host "[*] Downloading Google Fonts Inter..." -ForegroundColor Gray
    curl.exe -L -s -o "$fontFile" "https://raw.githubusercontent.com/google/fonts/main/ofl/inter/Inter%5Bopsz%2Cwght%5D.ttf"
}

$outExe = Join-Path $root "OpenPrestige.exe"
if (Test-Path $outExe) {
    try {
        Remove-Item $outExe -Force -ErrorAction Stop
    } catch {
        Write-Host "[*] OpenPrestige.exe is currently running/locked. Outputting to OpenPrestige_Standalone.exe instead." -ForegroundColor Yellow
        $outExe = Join-Path $root "OpenPrestige_Standalone.exe"
        Remove-Item $outExe -Force -ErrorAction SilentlyContinue
    }
}

$refs = @(
    "System.dll",
    "System.Drawing.dll",
    "System.Windows.Forms.dll",
    "System.Core.dll",
    "System.IO.Compression.dll",
    "System.IO.Compression.FileSystem.dll"
) -join ","

$resources = @(
    "/res:`"$fontFile`",OpenPrestige.Inter.ttf",
    "/res:`"$pfx`",OpenPrestige.server.pfx",
    "/res:`"$root\crl\mitm.crl`",OpenPrestige.mitm.crl",
    "/res:`"$root\mitmproxy-ca-cert.cer`",OpenPrestige.mitmproxy-ca-cert.cer",
    "/res:`"$root\prestige_offline.dll`",OpenPrestige.prestige_offline.dll",
    "/res:`"$flowsZip`",OpenPrestige.flows.zip",
    "/res:`"$root\config_store\index.json`",OpenPrestige.index.json",
    "/res:`"$root\config_store\data.json`",OpenPrestige.data.json"
)

$srcFiles = (Get-ChildItem -Path (Join-Path $root "src") -Filter "*.cs").FullName
$manifest = Join-Path $root "src\app.manifest"

$args = @(
    "/target:winexe",
    "/platform:x64",
    "/optimize+",
    "/win32manifest:`"$manifest`"",
    "/out:`"$outExe`"",
    "/r:$refs"
) + $resources + ($srcFiles | ForEach-Object { "`"$_`"" })

Write-Host "[*] Compiling sources and bundling resources into OpenPrestige.exe..." -ForegroundColor Yellow

$proc = Start-Process -FilePath $csc -ArgumentList $args -NoNewWindow -PassThru -Wait

if ($proc.ExitCode -eq 0 -and (Test-Path $outExe)) {
    $sizeMb = [Math]::Round(((Get-Item $outExe).Length / 1MB), 2)
    Write-Host "[+] Build SUCCESSFUL!" -ForegroundColor Green
    Write-Host "    Output: $outExe ($sizeMb MB)" -ForegroundColor Green
} else {
    Write-Host "[!] Build FAILED with exit code $($proc.ExitCode)." -ForegroundColor Red
    exit 1
}
