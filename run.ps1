param([string]$ProcessName = "javaw")
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$dll  = Join-Path $root "prestige_offline.dll"
$cer  = Join-Path $root "mitmproxy-ca-cert.cer"
$log  = Join-Path $root "replay.log"
$py   = "python"

function Fail($m){ Write-Host "[!] $m" -ForegroundColor Red; Read-Host "Press Enter to exit"; exit 1 }

# if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltinRole]::Administrator)) {
#    Fail "Run this as Administrator (right-click > Run as administrator)."
# }
# if (-not (Test-Path $dll)) { Fail "Missing $dll" }
# if (-not (Test-Path $cer)) { Fail "Missing $cer" }

Add-Type @"
using System; using System.Runtime.InteropServices;
public class Inj {
  [DllImport("kernel32.dll",SetLastError=true)] public static extern IntPtr OpenProcess(uint a,bool i,int p);
  [DllImport("kernel32.dll",SetLastError=true)] public static extern IntPtr VirtualAllocEx(IntPtr h,IntPtr a,int s,uint t,uint p);
  [DllImport("kernel32.dll",SetLastError=true)] public static extern bool WriteProcessMemory(IntPtr h,IntPtr a,byte[] b,int s,out int w);
  [DllImport("kernel32.dll",SetLastError=true)] public static extern IntPtr CreateRemoteThread(IntPtr h,IntPtr a,uint s,IntPtr f,IntPtr p,uint c,out uint t);
  [DllImport("kernel32.dll",SetLastError=true)] public static extern uint WaitForSingleObject(IntPtr h,uint m);
  [DllImport("kernel32.dll",SetLastError=true)] public static extern bool CloseHandle(IntPtr h);
  [DllImport("kernel32.dll",SetLastError=true)] public static extern IntPtr GetModuleHandleA(string n);
  [DllImport("kernel32.dll",SetLastError=true)] public static extern IntPtr GetProcAddress(IntPtr m,string n);
}
"@

Write-Host "[*] Clearing any stale offline servers..." -ForegroundColor Cyan
Get-CimInstance Win32_Process -Filter "Name LIKE 'python%'" -ErrorAction SilentlyContinue |
  Where-Object { $_.CommandLine -match 'replay_server\.py|crl_server\.py' } |
  ForEach-Object { try { Stop-Process -Id $_.ProcessId -Force -ErrorAction Stop; Write-Host "    killed stale server pid $($_.ProcessId)" -ForegroundColor DarkGray } catch {} }
Get-NetTCPConnection -State Listen -LocalPort 443,8081 -ErrorAction SilentlyContinue |
  Select-Object -ExpandProperty OwningProcess -Unique |
  ForEach-Object { try { Stop-Process -Id $_ -Force -ErrorAction Stop; Write-Host "    killed stale pid $_" -ForegroundColor DarkGray } catch {} }
for ($i=0; $i -lt 20; $i++) {
    $busy = Get-NetTCPConnection -State Listen -LocalPort 443 -ErrorAction SilentlyContinue
    if (-not $busy) { break }
    Start-Sleep -Milliseconds 300
}

Write-Host "[*] Trusting local certificate..." -ForegroundColor Cyan
certutil -addstore -f Root "$cer" | Out-Null

Write-Host "[*] Starting offline server..." -ForegroundColor Cyan
if (Test-Path $log) { Remove-Item $log -Force -ErrorAction SilentlyContinue }
$crl = Start-Process $py -ArgumentList "`"$root\crl\crl_server.py`"" -WorkingDirectory "$root\crl" -PassThru -WindowStyle Hidden
$env:REPLAY_PORT = "443"
$rep = Start-Process $py -ArgumentList "`"$root\replay_server.py`"" -WorkingDirectory $root -PassThru -WindowStyle Hidden

$ok=$false
for ($i=0; $i -lt 40; $i++) {
    try { $t=New-Object Net.Sockets.TcpClient; $t.Connect("127.0.0.1",443); $t.Close(); $ok=$true; break } catch { Start-Sleep -Milliseconds 300 }
}
if (-not $ok) { Fail "Replay server did not start on :443 (Python installed? port free?)" }
Write-Host "[+] Offline server ready on https://127.0.0.1:443" -ForegroundColor Green

Write-Host "[*] Waiting for Minecraft ($ProcessName)... launch it now." -ForegroundColor Yellow
$proc=$null
while (-not $proc) { $proc = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue | Select-Object -First 1; if (-not $proc){ Start-Sleep -Milliseconds 800 } }
Write-Host "[+] Found $ProcessName PID $($proc.Id). Injecting..." -ForegroundColor Green
$h = [Inj]::OpenProcess(0x1F0FFF,$false,$proc.Id)
if ($h -eq [IntPtr]::Zero) { Fail "OpenProcess failed." }
$bytes = [Text.Encoding]::ASCII.GetBytes(((Resolve-Path $dll).Path) + "`0")
$mem = [Inj]::VirtualAllocEx($h,[IntPtr]::Zero,$bytes.Length,0x3000,0x40)
$w=0; [Inj]::WriteProcessMemory($h,$mem,$bytes,$bytes.Length,[ref]$w) | Out-Null
$load = [Inj]::GetProcAddress([Inj]::GetModuleHandleA("kernel32.dll"),"LoadLibraryA")
$tid=0; $th = [Inj]::CreateRemoteThread($h,[IntPtr]::Zero,0,$load,$mem,0,[ref]$tid)
if ($th -eq [IntPtr]::Zero) { Fail "CreateRemoteThread failed." }
[Inj]::WaitForSingleObject($th,10000) | Out-Null
[Inj]::CloseHandle($th) | Out-Null; [Inj]::CloseHandle($h) | Out-Null
Write-Host "[+] Injected. Prestige is loading offline..." -ForegroundColor Green
Write-Host ""
Write-Host "===== LIVE OFFLINE SERVER (each line = a request the client made) =====" -ForegroundColor Magenta
Write-Host "(Keep this window open while playing. Press Ctrl+C to stop.)" -ForegroundColor DarkGray
Write-Host ""

try {
    for ($i=0; $i -lt 20 -and -not (Test-Path $log); $i++) { Start-Sleep -Milliseconds 300 }
    Get-Content $log -Wait -Tail 100
}
finally {
    if ($crl -and -not $crl.HasExited) { Stop-Process -Id $crl.Id -Force }
    if ($rep -and -not $rep.HasExited) { Stop-Process -Id $rep.Id -Force }
    Write-Host "`n[*] Offline server stopped." -ForegroundColor Cyan
}
