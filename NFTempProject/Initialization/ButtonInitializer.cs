using System.Device.Gpio;

namespace NFTempProject.Initialization
{
    internal static class ButtonInitializer
    {
        public static void WireButtons(
            GpioController gpio,
            PinChangeEventHandler onDown,
            PinChangeEventHandler onUp,
            PinChangeEventHandler onReset)
        {
            gpio.OpenPin(PinConfig.BtnDown, PinMode.InputPullUp);
            gpio.OpenPin(PinConfig.BtnUp, PinMode.InputPullUp);
            gpio.OpenPin(PinConfig.BtnReset, PinMode.InputPullUp);

            gpio.RegisterCallbackForPinValueChangedEvent(PinConfig.BtnDown, PinEventTypes.Falling, onDown);
            gpio.RegisterCallbackForPinValueChangedEvent(PinConfig.BtnUp, PinEventTypes.Falling, onUp);

            // Register BOTH edges in one registration to avoid overriding previous handler
            gpio.RegisterCallbackForPinValueChangedEvent(
                PinConfig.BtnReset,
                PinEventTypes.Falling | PinEventTypes.Rising,
                onReset);
        }
    }
}