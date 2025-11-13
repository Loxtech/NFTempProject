using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace NFTempProject.Logging
{
    internal sealed class TemperatureLogService : IDisposable
    {
        private readonly TemperatureState _state;
        private readonly string _logFilePath;
        private Timer _timer;
        private readonly bool _silent;

        public string LogFilePath => _logFilePath;

        // silent = true suppresses all normal output (only errors will appear).
        public TemperatureLogService(TemperatureState state, int periodMs = 60_000, int initialDelayMs = 5_000, bool silent = true)
        {
            _state = state;
            _silent = silent;

            _logFilePath = ChooseLogPath();
            EnsureLogFileExists(_logFilePath);

            _timer = new Timer(Tick, null, initialDelayMs, periodMs);

            if (!_silent)
            {
                Debug.WriteLine($"[TemperatureLogService] Started. File={_logFilePath} interval={periodMs}ms");
            }
        }

        private static string ChooseLogPath()
        {
            var roots = new[] { "I:\\", "D:\\", "C:\\" };
            foreach (var root in roots)
            {
                try
                {
                    if (Directory.Exists(root))
                    {
                        return root + "temp-log.jsonl";
                    }
                }
                catch { }
            }
            return "temp-log.jsonl";
        }

        private static void EnsureLogFileExists(string path)
        {
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (!File.Exists(path))
                {
                    using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        // create empty file
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TemperatureLogService] EnsureLogFileExists error: {ex}");
            }
        }

        private void Tick(object _)
        {
            try
            {
                var actual = _state.Actual;
                var json = "{\"utc\":\"" + UtcNowIso8601() + "\",\"Temperature\":" + FormatOneDecimal(actual) + "}";
                AppendLine(_logFilePath, json);

                if (!_silent)
                {
                    Debug.WriteLine("[TemperatureLogService] Logged: " + json);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TemperatureLogService] Tick error: {ex}");
            }
        }

        private static void AppendLine(string path, string line)
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(line + "\r\n");
                using (var fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read))
                {
                    fs.Write(bytes, 0, bytes.Length);
                    fs.Flush();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TemperatureLogService] Append error: {ex}");
            }
        }

        private static string UtcNowIso8601()
        {
            var dt = DateTime.UtcNow;
            return dt.Year.ToString("D4") + "-" +
                   dt.Month.ToString("D2") + "/" +
                   dt.Day.ToString("D2") + " " +
                   dt.Hour.ToString("D2") + ":" +
                   dt.Minute.ToString("D2") + ":" +
                   dt.Second.ToString("D2") + " ";
        }

        private static string FormatOneDecimal(double value)
        {
            int tenths = (int)Math.Round(value * 10);
            int sign = tenths < 0 ? -1 : 1;
            tenths *= sign;
            int whole = tenths / 10;
            int frac = tenths % 10;
            return (sign < 0 ? "-" : "") + whole.ToString() + "." + frac.ToString();
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _timer = null;
            if (!_silent)
            {
                Debug.WriteLine("[TemperatureLogService] Stopped.");
            }
        }
    }
}