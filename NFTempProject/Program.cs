using Iot.Device.Ssd13xx;
using Iot.Device.Ssd13xx.Samples;
using nanoFramework.Hardware.Esp32;
using System.Device.Gpio;
using System.Device.I2c;
using System.Diagnostics;
using System.Threading;
using System;


namespace NFTempProject
{
    public class Program
    {
        // Pin mapping (GPIO numbers as provided)
        const int PinBlueLed = 26;   // D26
        const int PinGreenLed = 25;  // D25
        const int PinRedLed = 33;    // D33

        const int PinDs18b20 = 32;   // D32 (OneWire)
        const int PinBtnDown = 5;    // D5
        const int PinBtnUp = 18;     // D18
        const int PinBtnReset = 19;  // D19

        // Temperatures
        static double preferredTemp = 21.0; // hardcoded preferred start value (never changed by buttons)
        static double actualTemp = 21.0;    // displayed "actual" temperature (adjustable with buttons)
        static double sensorTemp = 21.0;    // raw DS18B20 reading (used for reset)

        // Tolerance to consider "identical"
        const double EqualTolerance = 0.25;

        // instantiate inside Main to avoid exceptions during type/module initialization
        static GpioController gpio;
        static Ssd1306 display;

        // For button debouncing
        static long lastBtnDownTicks = 0;
        static long lastBtnUpTicks = 0;
        static long lastBtnResetTicks = 0;
        const int debounceMs = 200;

        // track first successful sensor read so we can initialize actualTemp
        static bool actualInitialized = false;

        public static void Main()
        {
            Debug.WriteLine("Main starting...");

            try
            {
                Configuration.SetPinFunction(21, DeviceFunction.I2C1_DATA);
                Configuration.SetPinFunction(22, DeviceFunction.I2C1_CLOCK);

                gpio = new GpioController();

                Debug.WriteLine("Starting NFTempProject...");

                var i2c = I2cDevice.Create(new I2cConnectionSettings(1, Ssd1306.DefaultI2cAddress));
                display = new Ssd1306(i2c, Ssd13xx.DisplayResolution.OLED128x32);
                display.Font = new BasicFont();
                display.ClearScreen();
                display.Display();

                gpio.OpenPin(PinBlueLed, PinMode.Output);
                gpio.OpenPin(PinGreenLed, PinMode.Output);
                gpio.OpenPin(PinRedLed, PinMode.Output);
                SetAllLeds(false, false, false);

                gpio.OpenPin(PinBtnDown, PinMode.InputPullUp);
                gpio.OpenPin(PinBtnUp, PinMode.InputPullUp);
                gpio.OpenPin(PinBtnReset, PinMode.InputPullUp);

                gpio.RegisterCallbackForPinValueChangedEvent(PinBtnDown, PinEventTypes.Falling, BtnDown_Callback);
                gpio.RegisterCallbackForPinValueChangedEvent(PinBtnUp, PinEventTypes.Falling, BtnUp_Callback);
                gpio.RegisterCallbackForPinValueChangedEvent(PinBtnReset, PinEventTypes.Falling, BtnReset_Callback);

                gpio.OpenPin(PinDs18b20, PinMode.Input);

                // start web control panel (call after display and gpio are initialized)
                WebControlPanel.Start(
                    () => actualTemp,
                    () => preferredTemp,
                    action =>
                    {
                        // this lambda runs inside Program class so it can call private static helpers
                        if (action == "up") actualTemp += 0.5;
                        else if (action == "down") actualTemp -= 0.5;
                        else if (action == "reset") actualTemp = sensorTemp;

                        UpdateLedsForTemperature(actualTemp, preferredTemp);
                        RefreshDisplay(actualTemp, preferredTemp);
                    });

                while (true)
                {

                    while (true)
                    {
                        if (TryReadTemperatureDs18b20(out double read))
                        {
                            sensorTemp = read;
                            if (!actualInitialized)
                            {
                                actualTemp = sensorTemp;
                                actualInitialized = true;
                            }
                        }

                        UpdateLedsForTemperature(actualTemp, preferredTemp);
                        RefreshDisplay(actualTemp, preferredTemp);

                        Thread.Sleep(1500);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unhandled exception in Main: {ex}");
            }

            Debug.WriteLine("Main finished — sleeping indefinitely to keep app alive.");
            Thread.Sleep(Timeout.Infinite);
        }

        // Buttons now adjust displayed actualTemp only; preferredTemp remains constant.
        static void BtnDown_Callback(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            if (IsDebounced(ref lastBtnDownTicks)) return;
            actualTemp -= 0.5;
            Debug.WriteLine($"Actual temp decreased -> {actualTemp:F1}°C");
        }

        static void BtnUp_Callback(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            if (IsDebounced(ref lastBtnUpTicks)) return;
            actualTemp += 0.5;
            Debug.WriteLine($"Actual temp increased -> {actualTemp:F1}°C");
        }

        // Reset button sets displayed actualTemp back to sensor reading
        static void BtnReset_Callback(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            if (IsDebounced(ref lastBtnResetTicks)) return;
            actualTemp = sensorTemp;
            Debug.WriteLine($"Actual temp reset to sensor -> {actualTemp:F1}°C");
        }

        static bool IsDebounced(ref long lastTicks)
        {
            long now = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            if (now - lastTicks < debounceMs) return true;
            lastTicks = now;
            return false;
        }

        static void SetAllLeds(bool blue, bool green, bool red)
        {
            gpio.Write(PinBlueLed, blue ? PinValue.High : PinValue.Low);
            gpio.Write(PinGreenLed, green ? PinValue.High : PinValue.Low);
            gpio.Write(PinRedLed, red ? PinValue.High : PinValue.Low);
        }

        static void UpdateLedsForTemperature(double actual, double pref)
        {
            if (Math.Abs(actual - pref) <= EqualTolerance)
            {
                // identical -> green
                SetAllLeds(false, true, false);
            }
            else if (actual < pref - EqualTolerance)
            {
                // too cold -> blue
                SetAllLeds(true, false, false);
            }
            else // actual > pref + tolerance
            {
                // too warm -> red
                SetAllLeds(false, false, true);
            }
        }

        static void RefreshDisplay(double actual, double pref)
        {
            try
            {
                if (display == null)
                {
                    Debug.WriteLine("Display is null - cannot draw.");
                    return;
                }

                // Build the three lines requested
                string line1 = $"Preferred: {pref:F1} C";
                string line2 = $"Current:    {actual:F1} C";
                string statusText = Math.Abs(actual - pref) <= EqualTolerance ? "Equal" :
                                    (actual < pref - EqualTolerance ? "Cold" : "Warm");
                string line3 = $"Status: {statusText}";

                // Redraw
                display.ClearScreen();
                display.DrawString(0, 0, line1);
                display.DrawString(0, 10, line2);
                display.DrawString(0, 20, line3);
                display.Display();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Display error: {ex}");
            }
        }

        #region DS18B20 OneWire minimal driver (single device, Skip ROM)
        // Returns true and sets temp when read succeeds; false on failure.
        static bool TryReadTemperatureDs18b20(out double temp)
        {
            temp = double.NaN;
            try
            {
                // Start conversion (Skip ROM + Convert T)
                if (!OneWireReset()) return false;
                OneWireWriteByte(0xCC); // Skip ROM
                OneWireWriteByte(0x44); // Convert T

                // Max conversion time for 12-bit ~750ms
                Thread.Sleep(750);

                // Read scratchpad
                if (!OneWireReset()) return false;
                OneWireWriteByte(0xCC); // Skip ROM
                OneWireWriteByte(0xBE); // Read Scratchpad

                byte[] data = new byte[9];
                for (int i = 0; i < 9; i++)
                {
                    data[i] = OneWireReadByte();
                }

                int raw = data[0] | (data[1] << 8);

                double t;
                if ((raw & 0x8000) != 0)
                {
                    raw = (raw ^ 0xFFFF) + 1;
                    t = -raw / 16.0;
                }
                else
                {
                    t = raw / 16.0;
                }

                temp = t;
                return true;
            }
            catch
            {
                return false;
            }
        }

        static bool OneWireReset()
        {
            var pin = PinDs18b20;

            gpio.SetPinMode(pin, PinMode.Output);
            gpio.Write(pin, PinValue.Low);
            DelayMicroseconds(480);

            gpio.Write(pin, PinValue.High);
            gpio.SetPinMode(pin, PinMode.Input);
            DelayMicroseconds(70);

            var presence = gpio.Read(pin) == PinValue.Low;

            DelayMicroseconds(410);
            return presence;
        }

        static void OneWireWriteByte(byte data)
        {
            for (int i = 0; i < 8; i++)
            {
                OneWireWriteBit((data >> i) & 0x01);
            }
        }

        static byte OneWireReadByte()
        {
            byte result = 0;
            for (int i = 0; i < 8; i++)
            {
                int bit = OneWireReadBit();
                result |= (byte)(bit << i);
            }
            return result;
        }

        static void OneWireWriteBit(int bit)
        {
            var pin = PinDs18b20;
            gpio.SetPinMode(pin, PinMode.Output);
            gpio.Write(pin, PinValue.Low);
            if (bit == 1)
            {
                DelayMicroseconds(6);
                gpio.Write(pin, PinValue.High);
                gpio.SetPinMode(pin, PinMode.Input);
                DelayMicroseconds(64);
            }
            else
            {
                DelayMicroseconds(60);
                gpio.Write(pin, PinValue.High);
                gpio.SetPinMode(pin, PinMode.Input);
                DelayMicroseconds(10);
            }
        }

        static int OneWireReadBit()
        {
            var pin = PinDs18b20;
            gpio.SetPinMode(pin, PinMode.Output);
            gpio.Write(pin, PinValue.Low);
            DelayMicroseconds(6);
            gpio.Write(pin, PinValue.High);
            gpio.SetPinMode(pin, PinMode.Input);
            DelayMicroseconds(9);
            var val = gpio.Read(pin);
            DelayMicroseconds(55);
            return val == PinValue.High ? 1 : 0;
        }

        static void DelayMicroseconds(int microseconds)
        {
            if (microseconds <= 0) return;
            long start = DateTime.UtcNow.Ticks;
            long target = start + microseconds * 10L; // 1 microsecond = 10 ticks (100ns)
            while (DateTime.UtcNow.Ticks < target) ;
        }
        #endregion
    }
}
