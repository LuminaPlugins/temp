using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace OpenPrestige
{
    static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        private static Mutex _singleInstanceMutex;

        [STAThread]
        static void Main()
        {
            try
            {
                SetProcessDPIAware();
            }
            catch { }

            bool createdNew;
            _singleInstanceMutex = new Mutex(true, "OpenPrestige_SingleInstance_Mutex", out createdNew);
            if (!createdNew)
            {
                MessageBox.Show(
                    Variables.SingleInstanceBody,
                    Variables.SingleInstanceTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.ThreadException += (s, e) =>
            {
                MessageBox.Show(
                    "Unhandled error: " + e.Exception.Message + "\n\n" + e.Exception.StackTrace,
                    "OpenPrestige Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                Exception ex = e.ExceptionObject as Exception;
                string msg = ex != null ? ex.Message + "\n\n" + ex.StackTrace : "Unknown unhandled error.";
                MessageBox.Show(msg, "OpenPrestige Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            using (SplashScreen splash = new SplashScreen())
            {
                splash.ShowDialog();
            }

            Application.Run(new MainForm());

            GC.KeepAlive(_singleInstanceMutex);
        }
    }
}
