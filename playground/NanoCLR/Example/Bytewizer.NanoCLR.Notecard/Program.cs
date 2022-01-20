using System;
using System.Diagnostics;

using nanoFramework.Hardware.Esp32;

using Bytewizer.NanoCLR.Drivers.Blues.Notecard;

namespace Bytewizer.NanoCLR.Notecard
{
    public class Program
    {
        public static void Main()
        {
            // Set i2cBus1 configuration for the Adafruit ESP32 HUZZAH
            Configuration.SetPinFunction(23, DeviceFunction.I2C1_DATA);
            Configuration.SetPinFunction(22, DeviceFunction.I2C1_CLOCK);

            // Create notecard controller I2cBus1 for Feather
            var notecard = new NotecardController(1);

            var request = new JsonRequest("card.voltage");
            for (int x = 0; x < 100; x++)
            {
                var results = notecard.Request(request);
                if (results.IsSuccess)
                {
                    Debug.WriteLine(results.Response);
                }
                else
                {
                    Debug.WriteLine(results.Response);
                    break;
                }
            }

            Debug.WriteLine("Completed");
        }
    }
}
