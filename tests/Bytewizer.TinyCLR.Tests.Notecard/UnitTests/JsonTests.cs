using System;
using System.Diagnostics;

using Bytewizer.TinyCLR.Assertions;
using Bytewizer.TinyCLR.Drivers.Blues.Notecard;

using GHIElectronics.TinyCLR.Data.Json;

namespace Bytewizer.TinyCLR.Tests.Notecard
{
    public class JsonTests : NotecardFixture
    {
        public JsonTests()
            : base() { }

        public void RequestWithJsonRequest()
        {
            var request = new JsonRequest("card.wireless");
            var response = (CardWirelessResponse)_notecard.Request(request, typeof(CardWirelessResponse));

            Assert.IsNotEmpty(response.status);
        }

        public void RequestWithJsonExtension()
        {
            var request = new CardWirelessRequest
            {
                mode = "auto"
            };

            var response = (CardWirelessResponse)_notecard.Request(request, typeof(CardWirelessResponse));

            Assert.IsNotEmpty(response.status);
        }

        public void RequestWithPrettyJson()
        {
            var request = new CardWirelessRequest
            {
                mode = "auto"
            };

            var noteRequest = JsonConverter.Serialize(request).ToString();
            var results = new JsonResults(_notecard.Transaction(noteRequest));

            if (results.IsSuccess)
            {
                var response = (CardWirelessResponse)JsonConverter.DeserializeObject(results.Response, typeof(CardWirelessResponse));
                Assert.IsNotEmpty(response.status);

                return;
            }

            Assert.Fail();
        }

        public void RequestWithOutPrettyJson()
        {
            var request = new CardWirelessRequest
            {
                mode = "auto"
            };

            var jsonSettings = new JsonSerializationOptions() { Indented = false };
            var noteRequest = JsonConverter.Serialize(request).ToString(jsonSettings);

            var results = new JsonResults(_notecard.Transaction(noteRequest));

            if (results.IsSuccess)
            {
                var response = (CardWirelessResponse)JsonConverter.DeserializeObject(results.Response, typeof(CardWirelessResponse));
                Assert.IsNotEmpty(response.status);

                return;
            }

            Assert.Fail();
        }
    }
}