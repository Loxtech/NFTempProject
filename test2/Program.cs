using Iot.Device.Ds18b20;
using nanoFramework.Device.OneWire;
using nanoFramework.Hardware.Esp32;
using System;
using System.Diagnostics;
using System.Threading;

namespace test2
{
    public class Program
    {

        public static void Main()
        {
            Configuration.SetPinFunction(32, DeviceFunction.COM3_RX);
            Configuration.SetPinFunction(14, DeviceFunction.COM3_TX);
            ReadingFromOneSensor();
        }
        private static void ReadingFromOneSensor()
        {
            OneWireHost oneWire = new();

            Ds18b20 ds18b20 = new(oneWire, null, false, TemperatureResolution.VeryHigh)
            {
                IsAlarmSearchCommandEnabled = false
            };
            if (ds18b20.Initialize())
            {
                Console.WriteLine($"Is sensor parasite powered?:{ds18b20.IsParasitePowered}");
                string devAddrStr = "";
                foreach (var addrByte in ds18b20.Address)
                {
                    devAddrStr += addrByte.ToString("X2");
                }

                Console.WriteLine($"Sensor address:{devAddrStr}");

                while (true)
                {
                    if (!ds18b20.TryReadTemperature(out var currentTemperature))
                    {
                        Console.WriteLine("Can't read!");
                    }
                    else
                    {
                        Console.WriteLine($"Temperature: {currentTemperature.DegreesCelsius.ToString("F")}\u00B0C");
                    }

                    Thread.Sleep(5000);
                }
            }

            oneWire.Dispose();
        }
    }
}
