using System;
using System.Text;

using GHIElectronics.TinyCLR.Devices.Uart;
using GHIElectronics.TinyCLR.Devices.Gpio;

namespace Bytewizer.TinyCLR.Drivers.Blues.Notecard.Diagnostics
{
    public sealed class NotecardLogger : IDisposable
    {
        private readonly GpioPin enablePin;
        private readonly UartController uartController;
        private readonly GpioController gpioController;

        public NotecardLogger(UartController uartController, int enablePin)
            : this(uartController, new UartSetting()
            {
                BaudRate = 115200,
                DataBits = 8,
                Parity = UartParity.None,
                StopBits = UartStopBitCount.One,
                Handshaking = UartHandshake.None
            }, enablePin)
        {
        }

        public NotecardLogger(UartController uartController, UartSetting uartSettings, int enablePin)
        {
            this.uartController = uartController;
            this.uartController.SetActiveSettings(uartSettings);
            this.uartController.Enable();
            this.uartController.DataReceived += this.UartController_DataReceived;

            this.gpioController = GpioController.GetDefault();

            this.enablePin = this.gpioController.OpenPin(enablePin);
            this.enablePin.SetDriveMode(GpioPinDriveMode.Output);
            this.enablePin.Write(GpioPinValue.High);
        }

        public void Enable() => this.enablePin.Write(GpioPinValue.High);

        public void Disable() => this.enablePin.Write(GpioPinValue.Low);

        public void TraceOn()
        {
            Enable();

            var writeBuffer = Encoding.UTF8.GetBytes("{\"req\":\"card.trace\",\"mode\":\"on\"}\n");
            this.uartController.Write(writeBuffer);
        }

        public void TraceOff()
        {
            var writeBuffer = Encoding.UTF8.GetBytes("{\"req\":\"card.trace\",\"mode\":\"off\"}\n");
            this.uartController.Write(writeBuffer);

            Disable();
        }

        public void Dispose()
        {
            this.enablePin.Dispose();
            this.uartController.Dispose();
            this.gpioController.Dispose();
        }

        public event MessageAvailableEventHandler MessageAvailable;
        public delegate void MessageAvailableEventHandler(string message);

        private string TempData { set; get; } = string.Empty;

        private void UartController_DataReceived(UartController sender, DataReceivedEventArgs e)
        {
            var rxBuffer = new byte[e.Count];
            var bytesReceived = this.uartController.Read(rxBuffer, 0, e.Count);
            var dataStr = Encoding.UTF8.GetString(rxBuffer, 0, bytesReceived);

            string[] lines = dataStr.Split(new char[] { '\n' });
            if (lines.Length == 0)
            {
                this.TempData += dataStr;
            }
            else
            {
                this.TempData += lines[0];
                MessageAvailable?.Invoke(this.TempData.Trim());
                this.TempData = lines[1];
            }
        }
    }
}