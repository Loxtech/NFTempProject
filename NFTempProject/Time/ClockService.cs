using System;
using System.Diagnostics;
using nanoFramework.Runtime.Native;

namespace NFTempProject.Time
{
    internal static class ClockService
    {
        // Consider time valid if not the default epoch.
        public static bool IsDateTimeValid() => DateTime.UtcNow.Year >= 2023;

        // Set system clock (expects UTC).
        // silent = true suppresses success output.
        public static void SetManualUtc(int year, int month, int day, int hour, int minute, int second, bool silent = true)
        {
            try
            {
                Rtc.SetSystemTime(new DateTime(year, month, day, hour, minute, second));
                if (!silent)
                {
                    Debug.WriteLine($"[ClockService] UTC set to: {ToIsoUtc(DateTime.UtcNow)}");
                }
            }
            catch (Exception ex)
            {
                // Keep error logging so failures aren’t hidden.
                Debug.WriteLine($"[ClockService] SetManualUtc error: {ex}");
            }
        }

        public static string ToIsoUtc(DateTime dt) =>
            dt.Year.ToString("D4") + "-" +
            dt.Month.ToString("D2") + "/" +
            dt.Day.ToString("D2") + " " +
            dt.Hour.ToString("D2") + ":" +
            dt.Minute.ToString("D2") + ":" +
            dt.Second.ToString("D2") + " ";
    }
}