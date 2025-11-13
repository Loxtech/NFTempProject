using System;
using System.Device.Gpio;
using System.Device.I2c;
using System.Diagnostics;
using System.Threading;
using nanoFramework.Hardware.Esp32;
using Iot.Device.Ssd13xx;
using Iot.Device.Ssd13xx.Samples;

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
                InitializeHardware();
                InitializeTemperatureState();
                WireButtons();

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

        private static void InitializeHardware()
        {
            // I2C pin assignment (per board specifics)
            Configuration.SetPinFunction(32, DeviceFunction.COM3_RX);
            Configuration.SetPinFunction(14, DeviceFunction.COM3_TX);
            Configuration.SetPinFunction(21, DeviceFunction.I2C1_DATA);
            Configuration.SetPinFunction(22, DeviceFunction.I2C1_CLOCK);

            _gpio = new GpioController();

            var i2c = I2cDevice.Create(new I2cConnectionSettings(1, Ssd1306.DefaultI2cAddress));
            _display = new Ssd1306(i2c, Ssd13xx.DisplayResolution.OLED128x32)
            {
                Font = new BasicFont()
            };
            _display.ClearScreen();
            _display.Display();
        }

        private static void InitializeTemperatureState()
        {
            _tempState = new TemperatureState(preferred: 21.0, initialActual: 21.0, tolerance: 0.25);
            _leds = new LedIndicator(_gpio, _tempState);
            _leds.InitializePins();
            _displayRenderer = new DisplayRenderer(_display, _tempState);
        }

        private static void WireButtons()
        {
            _gpio.OpenPin(PinConfig.BtnDown, PinMode.InputPullUp);
            _gpio.OpenPin(PinConfig.BtnUp, PinMode.InputPullUp);
            _gpio.OpenPin(PinConfig.BtnReset, PinMode.InputPullUp);

            _gpio.RegisterCallbackForPinValueChangedEvent(PinConfig.BtnDown, PinEventTypes.Falling, BtnDown);
            _gpio.RegisterCallbackForPinValueChangedEvent(PinConfig.BtnUp, PinEventTypes.Falling, BtnUp);
            _gpio.RegisterCallbackForPinValueChangedEvent(PinConfig.BtnReset, PinEventTypes.Falling, BtnReset);
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
