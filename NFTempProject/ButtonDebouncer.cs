using System;

namespace NFTempProject
{
    internal static class ButtonDebouncer
    {
        private static long _downTicks;
        private static long _upTicks;
        private static long _resetTicks;

        private const int DebounceMs = 200;

        private static bool IsDebounced(ref long lastTicks)
        {
            long now = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            if (now - lastTicks < DebounceMs) return true;
            lastTicks = now;
            return false;
        }

        public static bool Down() => IsDebounced(ref _downTicks);
        public static bool Up() => IsDebounced(ref _upTicks);
        public static bool Reset() => IsDebounced(ref _resetTicks);
    }
}