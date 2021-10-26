using System;

using GHIElectronics.TinyCLR.Data.Json;

using Bytewizer.TinyCLR.Drivers.Blues.Notecard;

namespace Bytewizer.TinyCLR.Tests.Notecard
{
    public static class ControllerExtensions
    {
        public static object Request(this NotecardController source, object objectRequest, Type responseType)
        {
            var jsonSettings = new JsonSerializationOptions() { Indented = false };
            var noteRequest = JsonConverter.Serialize(objectRequest).ToString(jsonSettings);

            source.ValidateRequest = false;
            var results = new JsonResults(source.Transaction(noteRequest));
            source.ValidateRequest = true;

            if (results.IsSuccess)
            {
                return JsonConverter.DeserializeObject(results.Response, responseType);
            }

            return null;
        }

        public static object Request(this NotecardController source, JsonRequest jsonRequest, Type type)
        {
            var results = source.Request(jsonRequest);
            if (results.IsSuccess)
            {
                return JsonConverter.DeserializeObject(results.Response, type);
            }

            return null;
        }

        public static JsonResults Request(this NotecardController source, JsonRequest jsonRequest)
        {
            return new JsonResults(source.Transaction(jsonRequest));
        }
    }
}