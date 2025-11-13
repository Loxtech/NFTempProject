using Iot.Device.Ssd13xx;
using System.Device.Gpio;

namespace NFTempProject.Initialization
{
    internal static class TemperatureInitializer
    {
        public static void Initialize(
            GpioController gpio,
            Ssd1306 display,
            out TemperatureState tempState,
            out LedIndicator leds,
            out DisplayRenderer renderer,
            double preferred = 21.0,
            double initialActual = 21.0,
            double tolerance = 0.5)
        {
            tempState = new TemperatureState(preferred, initialActual, tolerance);
            leds = new LedIndicator(gpio, tempState);
            leds.InitializePins();
            renderer = new DisplayRenderer(display, tempState);
        }
    }
}