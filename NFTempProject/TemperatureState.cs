using System;
using nanoFramework.Runtime.Events;
using nanoFramework.Runtime.Native;
namespace NFTempProject
{
    internal class TemperatureState
    {
        public double Preferred { get; private set; }
        public double Actual { get; private set; }
        public double Sensor { get; private set; }
        public double EqualTolerance { get; }

        public TemperatureState(double preferred, double initialActual, double tolerance)
        {
            Preferred = preferred;
            Actual = initialActual;
            Sensor = initialActual;
            EqualTolerance = tolerance;
        }

        public void AdjustActual(double delta)
        { 
            Actual = (int)((Actual + delta) * 10) / 10.0;
        }

        public void SetFromSensor(double sensorValue)
        {
            Sensor = sensorValue;
            Actual = sensorValue;
        }

        public void UseLastSensorAsActual()
        {
            Actual = Sensor;
        }

        public string Classify()
        {
            if (Math.Abs(Actual - Preferred) <= EqualTolerance) return "Equal";
            if (Actual < Preferred - EqualTolerance) return "Cold";
            return "Warm";
        }
    }
}