using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenPrestige
{
    public class ProcessItem
    {
        public int Pid { get; set; }
        public string ProcessName { get; set; }
        public string Title { get; set; }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Title))
            {
                return string.Format("{0}.exe (PID: {1})", ProcessName, Pid);
            }
            return string.Format("{0}.exe (PID: {1}) - {2}", ProcessName, Pid, Title);
        }
    }

    public static class ProcessInjector
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out uint lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetModuleHandleA(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        private const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_RESERVE = 0x2000;
        private const uint PAGE_EXECUTE_READWRITE = 0x40;

        public static List<ProcessItem> GetRunningJavaProcesses(bool allProcesses = false)
        {
            var list = new List<ProcessItem>();
            try
            {
                Process[] processes = Process.GetProcesses();
                foreach (Process p in processes)
                {
                    try
                    {
                        string name = p.ProcessName.ToLowerInvariant();
                        bool match = allProcesses || name.Contains("javaw") || name.Contains("java");
                        if (match)
                        {
                            list.Add(new ProcessItem
                            {
                                Pid = p.Id,
                                ProcessName = p.ProcessName,
                                Title = p.MainWindowTitle
                            });
                        }
                    }
                    catch { }
                }
            }
            catch { }

            // Sort: javaw first, then by PID descending
            list.Sort((a, b) =>
            {
                bool aJavaw = a.ProcessName.IndexOf("javaw", StringComparison.OrdinalIgnoreCase) >= 0;
                bool bJavaw = b.ProcessName.IndexOf("javaw", StringComparison.OrdinalIgnoreCase) >= 0;
                if (aJavaw && !bJavaw) return -1;
                if (!aJavaw && bJavaw) return 1;
                return b.Pid.CompareTo(a.Pid);
            });

            return list;
        }

        public static string EnsureDllExtracted(Action<string> log)
        {
            // 1. Check local directory next to exe
            string localDll = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "prestige_offline.dll");
            if (File.Exists(localDll) && new FileInfo(localDll).Length > 1000000)
            {
                return Path.GetFullPath(localDll);
            }

            // 2. LocalAppData dedicated storage (foolproof location writable by standard user)
            string localAppDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenPrestige");
            string persistentDll = Path.Combine(localAppDir, "prestige_offline.dll");
            if (File.Exists(persistentDll) && new FileInfo(persistentDll).Length > 1000000)
            {
                return Path.GetFullPath(persistentDll);
            }

            // 3. Extract directly from embedded resources into LocalAppData
            try
            {
                log(Variables.LogDllUnpacking);
                if (ResourceHelper.ExtractResourceToFile("prestige_offline.dll", persistentDll))
                {
                    log(string.Format(Variables.LogDllUnpacked, new FileInfo(persistentDll).Length));
                    return Path.GetFullPath(persistentDll);
                }
            }
            catch (Exception ex)
            {
                log(string.Format(Variables.LogDllExtractError, ex.Message));
            }

            // 4. Fallback to %TEMP%
            string tempDll = Path.Combine(Path.GetTempPath(), "prestige_offline.dll");
            try
            {
                if (ResourceHelper.ExtractResourceToFile("prestige_offline.dll", tempDll))
                {
                    return Path.GetFullPath(tempDll);
                }
            }
            catch (Exception ex)
            {
                log(string.Format(Variables.LogDllExtractError, ex.Message));
            }

            return null;
        }

        public static bool Inject(int pid, Action<string> log)
        {
            string dllPath = EnsureDllExtracted(log);
            if (string.IsNullOrEmpty(dllPath) || !File.Exists(dllPath))
            {
                log(Variables.LogDllMissing);
                return false;
            }

            log(string.Format(Variables.LogInjecting, pid, dllPath));

            IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, pid);
            if (hProcess == IntPtr.Zero)
            {
                int err = Marshal.GetLastWin32Error();
                log(string.Format(Variables.LogOpenProcessFailed, err));
                return false;
            }

            try
            {
                byte[] bytes = Encoding.ASCII.GetBytes(dllPath + "\0");
                IntPtr allocMem = VirtualAllocEx(hProcess, IntPtr.Zero, bytes.Length, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
                if (allocMem == IntPtr.Zero)
                {
                    int err = Marshal.GetLastWin32Error();
                    log(string.Format(Variables.LogVirtualAllocFailed, err));
                    return false;
                }

                IntPtr written;
                if (!WriteProcessMemory(hProcess, allocMem, bytes, bytes.Length, out written))
                {
                    int err = Marshal.GetLastWin32Error();
                    log(string.Format(Variables.LogWriteMemFailed, err));
                    return false;
                }

                IntPtr kernel32 = GetModuleHandleA("kernel32.dll");
                if (kernel32 == IntPtr.Zero)
                {
                    log(Variables.LogGetModuleFailed);
                    return false;
                }

                IntPtr loadLibrary = GetProcAddress(kernel32, "LoadLibraryA");
                if (loadLibrary == IntPtr.Zero)
                {
                    log(Variables.LogGetProcFailed);
                    return false;
                }

                uint threadId = 0;
                IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibrary, allocMem, 0, out threadId);
                if (hThread == IntPtr.Zero)
                {
                    int err = Marshal.GetLastWin32Error();
                    log(string.Format(Variables.LogRemoteThreadFailed, err));
                    return false;
                }

                log(string.Format(Variables.LogRemoteThreadCreated, threadId));
                WaitForSingleObject(hThread, 10000);
                CloseHandle(hThread);

                log(Variables.LogInjectSuccess);
                return true;
            }
            finally
            {
                CloseHandle(hProcess);
            }
        }
    }
}
