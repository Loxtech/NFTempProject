using System.Device.Gpio;
using System.Device.I2c;
using Iot.Device.Ssd13xx;
using Iot.Device.Ssd13xx.Samples;
using nanoFramework.Hardware.Esp32;

namespace NFTempProject.Initialization
{
    internal static class HardwareInitializer
    {
        public static void Initialize(out GpioController gpio, out Ssd1306 display)
        {
            // I2C/UART pin assignment
            Configuration.SetPinFunction(32, DeviceFunction.COM3_RX);
            Configuration.SetPinFunction(14, DeviceFunction.COM3_TX);
            Configuration.SetPinFunction(21, DeviceFunction.I2C1_DATA);
            Configuration.SetPinFunction(22, DeviceFunction.I2C1_CLOCK);

            gpio = new GpioController();

            var i2c = I2cDevice.Create(new I2cConnectionSettings(1, Ssd1306.DefaultI2cAddress));
            display = new Ssd1306(i2c, Ssd13xx.DisplayResolution.OLED128x32)
            {
                Font = new BasicFont()
            };
            display.ClearScreen();
            display.Display();
        }
    }
}