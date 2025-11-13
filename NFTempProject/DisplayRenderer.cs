using System;
using System.Diagnostics;
using Iot.Device.Ssd13xx;

namespace NFTempProject
{
    internal class DisplayRenderer
    {
        private readonly Ssd1306 _display;
        private readonly TemperatureState _state;

        public DisplayRenderer(Ssd1306 display, TemperatureState state)
        {
            _display = display;
            _state = state;
        }

        public void Refresh()
        {
            try
            {
                string line1 = $"Preferred: {_state.Preferred:F1} C";
                string line2 = $"Current:    {_state.Actual:F1} C";
                string line3 = $"Status: {_state.Classify()}";

                _display.ClearScreen();
                _display.DrawString(0, 0, line1);
                _display.DrawString(0, 10, line2);
                _display.DrawString(0, 20, line3);
                _display.Display();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Display error: {ex}");
            }
        }
    }
}