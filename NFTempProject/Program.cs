using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.Threading;
using Iot.Device.Ssd13xx;
using NFTempProject.Initialization;

namespace NFTempProject
{
    public class Program
    {
        // Core state/services
        private static TemperatureState _tempState;
        private static LedIndicator _leds;
        private static DisplayRenderer _displayRenderer;

        private static GpioController _gpio;
        private static Ssd1306 _display;

        public static void Main()
        {
            Debug.WriteLine("Main starting...");

            try
            {
                HardwareInitializer.Initialize(out _gpio, out _display);
                TemperatureInitializer.Initialize(_gpio, _display, out _tempState, out _leds, out _displayRenderer,
                    preferred: 21.0, initialActual: 21.0, tolerance: 0.5);
                ButtonInitializer.WireButtons(_gpio, BtnDown, BtnUp, BtnReset);

                // Initial sensor read
                if (!TryRefreshFromSensor())
                {
                    Debug.WriteLine("Initial sensor read failed; using defaults.");
                }

                // Initial UI
                RefreshUi();

                // Keep application alive
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
            Debug.WriteLine($"(Simulated) actual decreased -> {_tempState.Actual:F1}°C");
            RefreshUi();
        }

        private static void BtnUp(object sender, PinValueChangedEventArgs e)
        {
            if (ButtonDebouncer.Up()) return;
            _tempState.AdjustActual(0.5);
            Debug.WriteLine($"(Simulated) actual increased -> {_tempState.Actual:F1}°C");
            RefreshUi();
        }

        private static void BtnReset(object sender, PinValueChangedEventArgs e)
        {
            if (ButtonDebouncer.Reset()) return;

            if (!TryRefreshFromSensor())
            {
                _tempState.UseLastSensorAsActual();
                Debug.WriteLine("Reset: sensor read failed, using last sensor value.");
            }

            Debug.WriteLine($"(Reset) actual -> {_tempState.Actual:F1}°C");
            RefreshUi();
        }

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
