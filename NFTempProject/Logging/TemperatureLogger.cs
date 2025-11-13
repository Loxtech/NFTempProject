using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace NFTempProject.Logging
{
    internal sealed class TemperatureLogger : IDisposable
    {
        private readonly TemperatureState _state;
        private readonly string _logFolder;
        private readonly string _logFile;
        private Timer _timer;

        // Defaults for ESP32 internal flash (SPIFFS) on nanoFramework
        private const string DefaultFolder = "I:\\logs";
        private const string DefaultFileName = "temp.jsonl";

        public TemperatureLogger(TemperatureState state, string folder = DefaultFolder, string fileName = DefaultFileName)
        {
            _state = state;
            _logFolder = folder;
            _logFile = Path.Combine(folder, fileName);
        }

        // Start periodic logging. For testing use 60_000 (1 minute). For hourly use 3_600_000.
        public void Start(int periodMs = 60_000, int initialDelayMs = 5_000)
        {
            EnsureStorage();
            _timer = new Timer(OnTick, null, initialDelayMs, periodMs);
            Debug.WriteLine($"TemperatureLogger started. Writing to '{_logFile}' every {periodMs} ms.");
        }

        public void Stop()
        {
            _timer?.Dispose();
            _timer = null;
            Debug.WriteLine("TemperatureLogger stopped.");
        }

        private void EnsureStorage()
        {
            try
            {
                if (!Directory.Exists(_logFolder))
                {
                    Directory.CreateDirectory(_logFolder);
                }
                if (!File.Exists(_logFile))
                {
                    using var fs = new FileStream(_logFile, FileMode.Create, FileAccess.Write, FileShare.Read);
                    // Optionally write a header comment line explaining JSONL format
                    var header = Encoding.UTF8.GetBytes("// JSON Lines: one JSON object per line\r\n");
                    fs.Write(header, 0, header.Length);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"EnsureStorage error: {ex}");
            }
        }

        private void OnTick(object _)
        {
            try
            {
                // Snapshot values quickly to avoid partial reads
                var actual = _state.Actual;
                var utcIso = UtcNowIso8601();

                var json = "{\"utc\":\"" + utcIso + "\",\"actualC\":" + FormatDoubleOneDecimal(actual) + "}\r\n";

                using (var fs = new FileStream(_logFile, FileMode.Append, FileAccess.Write, FileShare.Read))
                {
                    var bytes = Encoding.UTF8.GetBytes(json);
                    fs.Write(bytes, 0, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TemperatureLogger tick error: {ex}");
            }
        }

        // Formats UTC as yyyy-MM-ddTHH:mm:ssZ (no dependency on custom format strings)
        private static string UtcNowIso8601()
        {
            var dt = DateTime.UtcNow;
            return dt.Year.ToString("D4") + "-" +
                   dt.Month.ToString("D2") + "-" +
                   dt.Day.ToString("D2") + "T" +
                   dt.Hour.ToString("D2") + ":" +
                   dt.Minute.ToString("D2") + ":" +
                   dt.Second.ToString("D2") + "Z";
        }

        // Culture-invariant one-decimal formatting without relying on globalization
        private static string FormatDoubleOneDecimal(double value)
        {
            int tenths = (int)Math.Round(value * 10, MidpointRounding.AwayFromZero);
            int sign = tenths < 0 ? -1 : 1;
            int abs = tenths * sign;
            int whole = abs / 10;
            int frac = abs % 10;
            return (sign < 0 ? "-" : "") + whole.ToString() + "." + frac.ToString();
        }

        public void Dispose() => Stop();
    }
}