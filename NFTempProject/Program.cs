using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.Threading;
using Iot.Device.Ssd13xx;
using NFTempProject.Initialization;
using NFTempProject.Logging;
using NFTempProject.Time;

namespace NFTempProject
{
    public class Program
    {
        private static TemperatureState _tempState;
        private static LedIndicator _leds;
        private static DisplayRenderer _displayRenderer;
        private static TemperatureLogService _logService;

        private static GpioController _gpio;
        private static Ssd1306 _display;

        // Reset long-press detection
        private const int ResetLongPressMs = 5000; // 5 seconds
        private static long _resetPressStartMs = -1;

        public static void Main()
        {
            Debug.WriteLine("Thermostat Starting...");

            try
            {
                HardwareInitializer.Initialize(out _gpio, out _display);
                TemperatureInitializer.Initialize(_gpio, _display, out _tempState, out _leds, out _displayRenderer,
                    preferred: 21.0, initialActual: 21.0, tolerance: 0.5);

                ButtonInitializer.WireButtons(_gpio, BtnDown, BtnUp, BtnReset);

                // Manually set RTC if invalid (replace with your current UTC)
                if (!ClockService.IsDateTimeValid())
                {
                    ClockService.SetManualUtc(2025, 11, 14, 11, 57, 00, silent: true);
                }

                if (!TryRefreshFromSensor())
                {
                    Debug.WriteLine("Initial sensor read failed; using defaults.");
                }

                RefreshUi();

                // Start logging (1 minute for test; use 900_000 for every 15 minutes)
                _logService = new TemperatureLogService(_tempState, periodMs: 60_000, silent: true);

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unhandled exception: {ex}");
            }

            Thread.Sleep(Timeout.Infinite);
        }

        private static void BtnDown(object sender, PinValueChangedEventArgs e)
        {
            if (ButtonDebouncer.Down()) return;
            _tempState.AdjustActual(-0.5);
            Debug.WriteLine($"Temperature decreased -> {_tempState.Actual:F1}°C");
            RefreshUi();
        }

        private static void BtnUp(object sender, PinValueChangedEventArgs e)
        {
            if (ButtonDebouncer.Up()) return;
            _tempState.AdjustActual(0.5);
            Debug.WriteLine($"Temperature increased -> {_tempState.Actual:F1}°C");
            RefreshUi();
        }

        private static void BtnReset(object sender, PinValueChangedEventArgs e)
        {
            // Falling = button pressed (with PullUp), start timing (debounced)
            if (e.ChangeType == PinEventTypes.Falling)
            {
                if (ButtonDebouncer.Reset()) return;
                _resetPressStartMs = UtcMs();
                return;
            }

            // Rising = button released, decide action by press duration
            if (e.ChangeType == PinEventTypes.Rising)
            {
                if (_resetPressStartMs < 0)
                {
                    return; 
                }

                long elapsed = UtcMs() - _resetPressStartMs;
                _resetPressStartMs = -1;

                if (elapsed >= ResetLongPressMs)
                {
                    // Long press: shows log, does not perform reset
                    if (_logService != null)
                    {
                        LogInspector.DumpTail(_logService.LogFilePath, maxBytes: 512);
                    }
                    return;
                }

                // Short press: reset current temperature to sensor value
                if (!TryRefreshFromSensor())
                {
                    _tempState.UseLastSensorAsActual();
                    Debug.WriteLine("Reset: sensor read failed, using last sensor value.");
                }

                Debug.WriteLine($"(Reset Temperature) -> {_tempState.Actual:F1}°C");
                RefreshUi();
            }
        }

        private static long UtcMs() => DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

        private static bool TryRefreshFromSensor()
        {
            double value;
            var success = SensorReader.TryReadOnce(out value);
            if (success)
            {
                _tempState.SetFromSensor(value);
                return true;
            }
            return false;
        }

        private static void RefreshUi()
        {
            _leds.Refresh();
            _displayRenderer.Refresh();
        }
    }
}
