$ErrorActionPreference = "Stop"
$mitm = "C:\Users\imget\AppData\Local\Packages\PythonSoftwareFoundation.Python.3.13_qbz5n2kfra8p0\LocalCache\local-packages\Python313\Scripts\mitmdump.exe"
$cer  = "C:\Users\imget\.mitmproxy\mitmproxy-ca-cert.cer"
$addon= "C:\Users\imget\Downloads\prestige_cap\capture_addon.py"
$crlDir = "C:\Users\imget\Downloads\prestige_cap\crl"
$leaf = "$crlDir\leaf-combined.pem"
$py   = "python"

if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)) {
    Write-Host "[!] Not elevated. Right-click PowerShell -> Run as administrator, then re-run this." -ForegroundColor Red
    exit 1
}

Write-Host "[*] Trusting intercept CA (temporary)..." -ForegroundColor Cyan
certutil -addstore -f Root "$cer" | Out-Null

Write-Host "[*] Starting local CRL server on 127.0.0.1:8081 (application/pkix-crl) ..." -ForegroundColor Cyan
$crlProc = Start-Process -FilePath $py -ArgumentList "$crlDir\crl_server.py" -WorkingDirectory $crlDir -PassThru -WindowStyle Hidden

Start-Sleep -Seconds 1
Write-Host "[*] Starting reverse proxy on 127.0.0.1:443 -> api.prestigeclient.vip" -ForegroundColor Cyan
Write-Host "[*] Now inject prestige_capture.dll into Minecraft (HWID auto-auth)." -ForegroundColor Yellow
Write-Host "[*] Watch for [CAP nn] POST ... -> 200 lines. When client is loaded, Ctrl+C here." -ForegroundColor Yellow
Write-Host ""
try {
    & $mitm --mode reverse:https://api.prestigeclient.vip -p 443 --listen-host 127.0.0.1 `
        --set keep_host_header=true --set upstream_cert=false --set http2=false `
        --certs api.prestigeclient.vip="$leaf" -s "$addon"
}
finally {
    if ($crlProc -and -not $crlProc.HasExited) { Stop-Process -Id $crlProc.Id -Force }
    Write-Host "[*] CRL server stopped." -ForegroundColor Cyan
}
