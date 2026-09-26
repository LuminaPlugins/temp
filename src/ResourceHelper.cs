using System;
using System.IO;
using System.Reflection;

namespace OpenPrestige
{
    public static class ResourceHelper
    {
        public static byte[] GetResourceBytes(string name)
        {
            using (Stream stream = GetResourceStream(name))
            {
                if (stream == null) return null;
                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }

        public static Stream GetResourceStream(string name)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string[] resourceNames = asm.GetManifestResourceNames();

            foreach (string res in resourceNames)
            {
                if (res.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                    res.EndsWith("." + name, StringComparison.OrdinalIgnoreCase) ||
                    res.EndsWith(name, StringComparison.OrdinalIgnoreCase))
                {
                    return asm.GetManifestResourceStream(res);
                }
            }
            return null;
        }

        public static string GetResourceString(string name)
        {
            byte[] bytes = GetResourceBytes(name);
            if (bytes == null) return null;
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        public static bool ExtractResourceToFile(string name, string outputPath)
        {
            try
            {
                using (Stream stream = GetResourceStream(name))
                {
                    if (stream == null) return false;

                    string dir = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        stream.CopyTo(fs);
                    }
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
