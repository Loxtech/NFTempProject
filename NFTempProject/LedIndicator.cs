using System;
using System.Device.Gpio;

namespace NFTempProject
{
    internal class LedIndicator
    {
        private readonly GpioController _gpio;
        private readonly TemperatureState _state;

        public LedIndicator(GpioController gpio, TemperatureState state)
        {
            _gpio = gpio;
            _state = state;
        }

        public void InitializePins()
        {
            _gpio.OpenPin(PinConfig.BlueLed, PinMode.Output);
            _gpio.OpenPin(PinConfig.GreenLed, PinMode.Output);
            _gpio.OpenPin(PinConfig.RedLed, PinMode.Output);
            Set(false, false, false);
        }

        private void Set(bool blue, bool green, bool red)
        {
            _gpio.Write(PinConfig.BlueLed, blue ? PinValue.High : PinValue.Low);
            _gpio.Write(PinConfig.GreenLed, green ? PinValue.High : PinValue.Low);
            _gpio.Write(PinConfig.RedLed, red ? PinValue.High : PinValue.Low);
        }

        public void Refresh()
        {
            double actual = _state.Actual;
            double pref = _state.Preferred;
            double tol = _state.EqualTolerance;

            if (Math.Abs(actual - pref) <= tol)
            {
                Set(false, true, false);        // Equal = green
            }
            else if (actual < pref - tol)
            {
                Set(true, false, false);        // Cold = blue
            }
            else
            {
                Set(false, false, true);        // Warm = red
            }
        }
    }
}