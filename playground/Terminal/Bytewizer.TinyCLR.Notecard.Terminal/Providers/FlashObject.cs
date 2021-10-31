using System;

using GHIElectronics.TinyCLR.Devices.Network;

namespace Bytewizer.TinyCLR.Notecard.Terminal
{
    [Serializable]
    public class FlashObject
    {
        public FlashObject()
        {
            Ssid = "ssid";
            Password = "password";
            Mode = WiFiMode.Station;
        }

        public string Ssid { get; set; }
        public string Password { get; set; }
        public WiFiMode Mode { get; set; }
    }
}