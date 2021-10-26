using GHIElectronics.TinyCLR.Data.Json;

using Bytewizer.TinyCLR.Drivers.Blues.Notecard;

namespace Bytewizer.TinyCLR.Tests.Notecard
{
    public class JsonResults : RequestResults
    {
        public JsonResults(string response) 
            : base(response) 
        { 
            if (IsSuccess)
            {
                Json = (JObject)JsonConverter.Deserialize(response);
            }
        }

        public JObject Json { get; private set; }
    }
}