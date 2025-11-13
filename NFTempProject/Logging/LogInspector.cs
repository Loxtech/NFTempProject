using System;
using System.Diagnostics;
using System.IO;

namespace NFTempProject.Logging
{
    internal static class LogInspector
    {
        // Dumps the last maxBytes of the log to Debug output (safe on constrained devices)
        public static void DumpTail(string path, int maxBytes = 512)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Debug.WriteLine($"[LogInspector] File not found: {path}");
                    return;
                }

                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    long len = fs.Length;
                    long start = len > maxBytes ? len - maxBytes : 0;
                    if (start > 0) fs.Seek(start, SeekOrigin.Begin);

                    byte[] buf = new byte[len - start];
                    int read = fs.Read(buf, 0, buf.Length);
                    var text = new string(System.Text.Encoding.UTF8.GetChars(buf, 0, read));

                    Debug.WriteLine("[LogInspector] ===== Log begin =====");
                    Debug.WriteLine(text);
                    Debug.WriteLine("[LogInspector] ===== Log end =====");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LogInspector] Error reading '{path}': {ex}");
            }
        }
    }
}