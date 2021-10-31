using System;
using System.Text;
using System.Diagnostics;

using GHIElectronics.TinyCLR.Pins;
using GHIElectronics.TinyCLR.Devices.Spi;
using GHIElectronics.TinyCLR.Devices.Gpio;
using GHIElectronics.TinyCLR.Devices.Network;

namespace Bytewizer.TinyCLR.Notecard.Terminal
{
    public class NetworkProvider
    {
        private static bool _initialized;
        private static readonly object _lock = new object();

        public static NetworkController Controller { get; private set; }

        public static void Initialize()
        {
            SettingsProvider.Initialize();

            Initialize(
                SettingsProvider.Flash.Ssid,
                SettingsProvider.Flash.Password,
                SettingsProvider.Flash.Mode
                );
        }

        public static void Initialize(string ssid, string password, WiFiMode mode = WiFiMode.Station)
        {
            if (_initialized)
                return;

            lock (_lock)
            {
                if (_initialized)
                    return;

                var enablePin = GpioController.GetDefault().OpenPin(SC20100.GpioPin.PA8);
                enablePin.SetDriveMode(GpioPinDriveMode.Output);
                enablePin.Write(GpioPinValue.High);

                Controller = NetworkController.FromName(SC20100.NetworkController.ATWinc15x0);

                Controller.SetCommunicationInterfaceSettings(new SpiNetworkCommunicationInterfaceSettings()
                {
                    SpiApiName = SC20100.SpiBus.Spi3,
                    GpioApiName = SC20100.GpioPin.Id,
                    InterruptPin = GpioController.GetDefault().OpenPin(SC20100.GpioPin.PB12),
                    InterruptEdge = GpioPinEdge.FallingEdge,
                    InterruptDriveMode = GpioPinDriveMode.InputPullUp,
                    ResetPin = GpioController.GetDefault().OpenPin(SC20100.GpioPin.PB13),
                    ResetActiveState = GpioPinValue.Low,
                    SpiSettings = new SpiConnectionSettings()
                    {
                        ChipSelectLine = GpioController.GetDefault().OpenPin(SC20100.GpioPin.PD15),
                        ClockFrequency = 4000000,
                        Mode = SpiMode.Mode0,
                        ChipSelectType = SpiChipSelectType.Gpio,
                        ChipSelectHoldTime = TimeSpan.FromTicks(10),
                        ChipSelectSetupTime = TimeSpan.FromTicks(10)
                    }
                });

                Controller.SetInterfaceSettings(new WiFiNetworkInterfaceSettings()
                {
                    Ssid = ssid,
                    Password = password,
                    Mode = mode,
                    //MulticastDnsEnable = true
                });

                Controller.SetAsDefaultController();
                Controller.NetworkAddressChanged += NetworkAddressChanged;

                try
                {
                    Controller.Enable();
                    //MulticastDns.Start(mdns, TimeSpan.FromSeconds(1 * 60 * 60));
                    _initialized = true;
                }
                catch
                {
                    throw new Exception("Networking failed verify ssid and password");
                }
            }
        }

        public static void Set(string ssid, string password, WiFiMode mode)
        {
            SettingsProvider.Initialize();
            SettingsProvider.Flash.Ssid = ssid;
            SettingsProvider.Flash.Password = password;
            SettingsProvider.Flash.Mode = mode;
            SettingsProvider.Write();
        }

        public static string Info(NetworkController controller)
        {
            var ipProperties = controller.GetIPProperties();

            var sb = new StringBuilder();

            sb.Append($"Interface Address: {ipProperties.Address} ");
            sb.Append($"Subnet: {ipProperties.SubnetMask} ");
            sb.Append($"Gateway: {ipProperties.GatewayAddress} ");

            for (int i = 0; i < ipProperties.DnsAddresses.Length; i++)
            {
                var address = ipProperties.DnsAddresses[i].GetAddressBytes();
                if (address[0] != 0)
                {
                    sb.Append($"DNS: {ipProperties.DnsAddresses[i]} ");
                }
            }

            return sb.ToString();
        }

        private static void NetworkAddressChanged(
            NetworkController sender,
            NetworkAddressChangedEventArgs e)
        {
            var ipProperties = sender.GetIPProperties();
            var address = ipProperties.Address.GetAddressBytes();

            if (address != null && address[0] != 0 && address.Length > 0)
            {
                Debug.WriteLine(Info(sender));
            }
        }
    }
}