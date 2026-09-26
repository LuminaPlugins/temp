using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace OpenPrestige
{
    public static class CertManager
    {
        public static bool EnsureRootCaTrusted(Action<string> log)
        {
            try
            {
                byte[] certBytes = GetCertBytes();
                if (certBytes == null || certBytes.Length == 0)
                {
                    log(Variables.LogCertMissing);
                    return false;
                }

                X509Certificate2 caCert = new X509Certificate2(certBytes);
                string thumbprint = caCert.Thumbprint;

                if (IsCertInStore(StoreLocation.LocalMachine, thumbprint) ||
                    IsCertInStore(StoreLocation.CurrentUser, thumbprint))
                {
                    log(Variables.LogCertTrusted);
                    return true;
                }

                log(Variables.LogCertInstalling);

                try
                {
                    using (X509Store store = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
                    {
                        store.Open(OpenFlags.ReadWrite);
                        store.Add(caCert);
                        store.Close();
                    }
                    log(Variables.LogCertInstalledDirect);
                    return true;
                }
                catch (Exception ex)
                {
                    log(string.Format(Variables.LogCertStoreFailed, ex.Message));
                }

                string tempCertPath = Path.Combine(Path.GetTempPath(), "mitmproxy-ca-cert.cer");
                File.WriteAllBytes(tempCertPath, certBytes);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "certutil.exe",
                    Arguments = "-addstore -f Root \"" + tempCertPath + "\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (Process proc = Process.Start(psi))
                {
                    proc.WaitForExit(10000);
                    if (proc.ExitCode == 0)
                    {
                        log(Variables.LogCertInstalledUtil);
                        return true;
                    }
                    else
                    {
                        log(string.Format(Variables.LogCertUtilFailed, proc.ExitCode));
                    }
                }
            }
            catch (Exception ex)
            {
                log(string.Format(Variables.LogCertError, ex.Message));
            }
            return false;
        }

        private static bool IsCertInStore(StoreLocation location, string thumbprint)
        {
            try
            {
                using (X509Store store = new X509Store(StoreName.Root, location))
                {
                    store.Open(OpenFlags.ReadOnly);
                    X509Certificate2Collection matches = store.Certificates.Find(
                        X509FindType.FindByThumbprint, thumbprint, false);
                    store.Close();
                    return matches.Count > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        public static byte[] GetCertBytes()
        {
            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mitmproxy-ca-cert.cer");
            if (File.Exists(localPath))
            {
                return File.ReadAllBytes(localPath);
            }
            return ResourceHelper.GetResourceBytes("mitmproxy-ca-cert.cer");
        }
    }
}
