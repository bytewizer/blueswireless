using System;
using System.Diagnostics;

using Bytewizer.TinyCLR.Drivers.Blues.Notecard;

using GHIElectronics.TinyCLR.Pins;
using GHIElectronics.TinyCLR.Devices.I2c;

namespace Bytewizer.TinyCLR.Notecard
{
    class Program
    {
        static void Main()
        {
            // Setup I2c bus for Fez Feather
            var controller = I2cController.FromName(SC20100.I2cBus.I2c1);
            var notecard = new NotecardController(controller);

            // Set product id with json request (this only needs to be done once)
            var request1 = new JsonRequest("hub.set");
            request1.Add("product", "[your-product-uid]"); // replace your this with your project uid

            var results1 = notecard.Request(request1);
            if (results1.IsSuccess)
            {
                Debug.WriteLine(results1.Response);
            }

            // Create a json body object
            var body = new JsonObject();
            body.Add("temp", 35.5);
            body.Add("humid", 56.23);

            // Set note with json request and included the body message
            var request2 = new JsonRequest("note.add");
            request2.Add("body", body);
            request2.Add("sync", true);

            var results2 = notecard.Request(request2);

            if (results2.IsSuccess)
            {
                Debug.WriteLine(results2.Response);
            }
        }
    }
}
