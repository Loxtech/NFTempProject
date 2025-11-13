using System;
using System.Diagnostics;
using nanoFramework.Device.OneWire;
using Iot.Device.Ds18b20;

namespace NFTempProject
{
    internal static class SensorReader
    {
        // Returns true if successful, and sets temperatureCelsius
        public static bool TryReadOnce(out double value)
        {
            value = 0;
            try
            {
                using (var oneWire = new OneWireHost())
                {
                    var sensor = new Ds18b20(oneWire, null, false, TemperatureResolution.VeryHigh)
                    {
                        IsAlarmSearchCommandEnabled = false
                    };

                    if (sensor.Initialize() && sensor.TryReadTemperature(out var temp))
                    {
                        value = temp.DegreesCelsius;
                        Debug.WriteLine($"Sensor read: {value:F2}°C");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Sensor read error: {ex}");
            }
            return false;
        }
    }
}