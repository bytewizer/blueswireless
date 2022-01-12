using System;
using System.Threading;
using System.Diagnostics;

using Bytewizer.TinyCLR.Drivers.Blues.Notecard;

using GHIElectronics.TinyCLR.Pins;
using GHIElectronics.TinyCLR.Devices.I2c;
using GHIElectronics.TinyCLR.Native;

namespace Bytewizer.TinyCLR.Notecard
{
    class Program
    {
        static void Main()
        {
            // replace your this with your project uid
            var productUID = "com.bytewizer.trice:notepath";

            // Setup I2c bus for Fez Feather
            var controller = I2cController.FromName(SC20100.I2cBus.I2c1);
            var notecard = new NotecardController(controller);

            // Restore the notecard to default settings and deregister (this only needs to be done once)
            var request = new JsonRequest("card.restore");
            request.Add("delete", true);

            //Debug.WriteLine(notecard.Request(request).Response);
            //Thread.Sleep(10000); // Wait 10 seconds for notecard to reload

            // Set product id with json request 
            request = new JsonRequest("hub.set");
            request.Add("mode", "periodic");
            request.Add("product", productUID);

            var results = notecard.Request(request);
            if (!results.IsSuccess)
            {
                throw new Exception("Faild to register product uid");
            }

            // Enable card location module
            request = new JsonRequest("card.location.mode");
            request.Add("mode", "periodic");
            request.Add("seconds", 3600);

            Debug.WriteLine(notecard.Request(request).Response);

            // Enable card location tracking
            request = new JsonRequest("card.location.track");
            request.Add("start", true);
            request.Add("hours", 1);
            request.Add("heartbeat", true);

            Debug.WriteLine(notecard.Request(request).Response);

            // Create a json message body and include with note
            var body = new JsonObject();
            body.Add("manufacture", DeviceInformation.ManufacturerName);
            body.Add("device", DeviceInformation.DeviceName);

            request = new JsonRequest("note.add");
            request.Add("body", body);
            request.Add("sync", true);

            Debug.WriteLine(notecard.Request(request).Response);
        }
    }
}