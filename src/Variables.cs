using System;

namespace OpenPrestige
{
    /// <summary>
    /// Centralized configuration for all text displayed across the GUI,
    /// splash screen, terminal logs, and system notifications.
    /// You can freely edit any of these strings here.
    /// </summary>
    public static class Variables
    {
        // ==========================================
        //  APPLICATION & BRANDING
        // ==========================================
        public static string AppTitle               = "rackelPrestige";
        public static string AppSubtitle            = "Reverse Engineered by rackel";
        public static string AuthorCredit           = "MADE BY RACKELDEVS";
        public static string VersionTag             = "v1.3";
        public static string WindowTitle            = "rackelPrestige v1.3";
        public static string FooterCredit           = "rackelPrestige • Native • free!!1!";

        // ==========================================
        //  SPLASH SCREEN
        // ==========================================
        public static string SplashTitle            = "rackelPrestige";
        public static string SplashSubtitle         = "Native Cracked Injector";
        public static string SplashBadge            = "MADE BY RACKELDEVS";
        public static string SplashStatusInit       = "Initializing libraries.";
        public static string SplashStatusFonts      = "Loading Fonts";
        public static string SplashStatusDll        = "Unpacking Nulled Prestige";
        public static string SplashStatusCert       = "Loading CA store";
        public static string SplashStatusEndpoints  = "Preparing network endpoints...";
        public static string SplashStatusReady      = "Ready bro.";

        // ==========================================
        //  SERVER STATUS BADGES
        // ==========================================
        public static string CrlBadgeOnline         = "● CRL :8081";
        public static string CrlBadgeOffline        = "○ CRL :8081";
        public static string ReplayBadgeOnline      = "● HTTPS :443";
        public static string ReplayBadgeOffline     = "○ HTTPS :443";

        // ==========================================
        //  TARGET PROCESS SECTION
        // ==========================================
        public static string ProcessSectionHeader   = "TARGET PROCESS";
        public static string NoProcessSelected      = "Select Minecraft process.";
        public static string BtnScanText            = "Scan";
        public static string BtnInjectText          = "Inject";

        public static string StatusScanning         = "Scanning for instances..";
        public static string StatusFoundProcesses   = "Found {0} processes";
        public static string StatusNoProcesses      = "No instances found.";
        public static string StatusInjecting        = "Injecting into PID {0} ({1})...";
        public static string StatusInjectSuccess    = "Successfully injected into PID {0}!";
        public static string StatusInjectFailed     = "Injection failed for PID {0}. See log below.";

        public static string MsgNoProcessSelectedTitle = "No Process Selected";
        public static string MsgNoProcessSelectedBody  = "Select a minecraft process from the dropdown first.";

        // ==========================================
        //  LIVE CONSOLE SECTION
        // ==========================================
        public static string LogSectionHeader       = "";
        public static string BtnClearLogText        = "Clear";

        // ==========================================
        //  THEMES
        // ==========================================
        public static string ThemeLabel             = "Select Theme:";

        // ==========================================
        //  LOGS & TERMINAL OUTPUT
        // ==========================================
        public static string LogStartup             = "[*] rackelPrestige (specially for op yash) initialized.";
        public static string LogFoundProcesses      = "[*] Found {0} Java process(es).";

        // Cert Logs
        public static string LogCertMissing         = "[!] CA Certificate not found in resources or on disk.";
        public static string LogCertTrusted         = "[+] Local certificate authority is already trusted.";
        public static string LogCertInstalling      = "[*] Installing local certificate authority into Root store...";
        public static string LogCertInstalledDirect = "[+] Certificate installed into LocalMachine Root store.";
        public static string LogCertStoreFailed     = "[*] Direct LocalMachine store add failed ({0}), trying certutil...";
        public static string LogCertInstalledUtil   = "[+] Certificate installed successfully via certutil.";
        public static string LogCertUtilFailed      = "[!] certutil failed with exit code {0}.";
        public static string LogCertError           = "[!] Error ensuring Root CA: {0}";

        // CRL Server Logs
        public static string LogCrlMissing          = "[!] Failed to load mitm.crl from disk or embedded resources.";
        public static string LogCrlListening        = "[+] CRL server listening on http://127.0.0.1:{0}";
        public static string LogCrlStartFailed      = "[!] Failed to start CRL server on :{0} - {1}";
        public static string LogCrlStopped          = "[*] CRL server stopped.";
        public static string LogCrlAcceptError      = "[!] CRL accept error: {0}";
        public static string LogCrlServed           = "[CRL] served {0} ({1} bytes)";

        // Replay Server Logs
        public static string LogCertLoaded          = "[*] Loaded SSL certificate: {0}";
        public static string LogCertLoadMissing     = "[!] server.pfx not found in disk or embedded resources!";
        public static string LogCertLoadError       = "[!] Error loading SSL certificate: {0}";
        public static string LogFlowsLoaded         = "[*] Loaded {0} endpoint routes.";
        public static string LogFlowsZipMissing     = "[!] flows.zip not found!";
        public static string LogFlowsZipError       = "[!] Error reading flows.zip: {0}";
        public static string LogConfigSaveError     = "[!] Error saving config store: {0}";
        public static string LogConfigCreated       = "[CONFIG] created id={0} name='{1}' total={2}";
        public static string LogConfigEdited        = "[CONFIG] edited id={0}";
        public static string LogConfigDeleted       = "[CONFIG] deleted id={0} total={1}";
        public static string LogReplayNoCert        = "[!] Cannot start ReplayServer: Certificate not loaded.";
        public static string LogReplayListening     = "[+] Offline replay server listening on https://127.0.0.1:{0}";
        public static string LogReplayStartFailed   = "[!] Failed to start ReplayServer on :{0} - {1}";
        public static string LogReplayStopped       = "[*] Replay server stopped.";
        public static string LogReplayAcceptError   = "[!] Replay accept error: {0}";
        public static string LogShutdownSignal      = "[*] Shutdown signal received.";
        public static string LogConfigError         = "[CFG-ERR] {0}: {1}";
        public static string LogRouteMiss           = "[MISS] {0} {1} reqlen={2}";
        public static string LogRouteServed         = "{0} {1} {2} -> {3} {4}b ({5}ms)";

        // Injection Logs
        public static string LogDllMissing          = "[!] Target DLL could not be found or extracted!";
        public static string LogInjecting           = "[*] Injecting DLL into PID {0}: {1}";
        public static string LogOpenProcessFailed   = "[!] OpenProcess failed (Win32 Error: {0}). Please run as Administrator if the target process is elevated.";
        public static string LogVirtualAllocFailed  = "[!] VirtualAllocEx failed (Win32 Error: {0}).";
        public static string LogWriteMemFailed      = "[!] WriteProcessMemory failed (Win32 Error: {0}).";
        public static string LogGetModuleFailed     = "[!] GetModuleHandleA(kernel32.dll) failed.";
        public static string LogGetProcFailed       = "[!] GetProcAddress(LoadLibraryA) failed.";
        public static string LogRemoteThreadFailed  = "[!] CreateRemoteThread failed (Win32 Error: {0}).";
        public static string LogRemoteThreadCreated = "[+] Remote injection thread {0} created successfully.";
        public static string LogInjectSuccess       = "[+] Injected. Prestige is loading offline...";
        public static string LogDllUnpacking        = "[*] Unpacking embedded Nulled prestige into standalone cache...";
        public static string LogDllUnpacked         = "[+] Native prestige unpacked successfully ({0} bytes).";
        public static string LogDllExtractError     = "[!] Error extracting DLL: {0}";

        // Single Instance
        public static string SingleInstanceTitle    = "rackelPrestige already running.";
        public static string SingleInstanceBody     = "Already running, close existing processes. Made specially for OP YASH if not u are a great gigadick.";
    }
}
