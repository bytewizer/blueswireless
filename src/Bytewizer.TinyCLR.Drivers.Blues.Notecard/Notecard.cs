using System;
using System.Text;
using System.Threading;
using System.Collections;

using GHIElectronics.TinyCLR.Devices.I2c;

namespace Bytewizer.TinyCLR.Drivers.Blues.Notecard {

    public sealed class NotecardController : IDisposable {

        const int I2C_ADDRESS = 0x17;
        const int REQUEST_HEADER_LEN = 2;
        const int POLLING_TIMEOUT_MS = 2500;
        const int POLLING_DELAY_MS = 25;

        private readonly I2cDevice i2cDevice;

        private readonly byte[] emptyBuffer = new byte[2];
        private readonly byte[] pollBuffer = new byte[2];


        public NotecardController(I2cController i2cController)
            : this(i2cController, new I2cConnectionSettings(I2C_ADDRESS) {
                BusSpeed = 100000,
                AddressFormat = I2cAddressFormat.SevenBit
            }) {
        }

        public NotecardController(I2cController i2cController, I2cConnectionSettings i2cSettings) {
            
            this.i2cDevice = i2cController.GetDevice(i2cSettings);
            
            try {
                this.Reset();
            }
            catch (Exception ex)
            {
                throw new Exception("Notecard not responding", ex);
            }
        }

        public void Dispose()
            => this.i2cDevice.Dispose();

        public RequestResults Request(JsonRequest noteRequest)
            => new RequestResults(this.Transaction(noteRequest));

        public string Transaction(JsonRequest request)
            => this.Transaction(request.ToJson());

        public string Transaction(string json) {

            if (string.IsNullOrEmpty(json)) {
                throw new ArgumentNullException(nameof(json));
            }

            // Verify string ends with a newline character and if not add it
            if (!json.EndsWith("\n")) {
                json += "\n";
            }

            // Encode request string
            var requestBytes = Encoding.UTF8.GetBytes(json);

            // Write request
            this.i2cDevice.WriteJoinBytes(new byte[] { (byte)json.Length }, requestBytes);

            // Create buffers
            var dataBuffer = new byte[0];
            var writeBuffer = new byte[2];

            while (true) {

                // Poll for request to be processed
                this.WaitForData(POLLING_TIMEOUT_MS, out var bytesAvailable);

                // The minimum bytes returned should be "{}\r\n"
                if (bytesAvailable < 4) {
                    break;
                }

                // Write a two byte buffer with the second byte as the number of bytes available
                writeBuffer[1] = bytesAvailable;
                var readBuffer = new byte[bytesAvailable + REQUEST_HEADER_LEN];
                this.i2cDevice.WriteRead(writeBuffer, readBuffer);

                // Check to make sure the second byte matches the number of bytes requested
                if (bytesAvailable != readBuffer[1]) {
                    throw new Exception("Unexpected i2c protocol byte count");
                }

                // Resize and update data buffer
                var newbuffer = new byte[dataBuffer.Length + readBuffer.Length - REQUEST_HEADER_LEN];
                Array.Copy(dataBuffer, newbuffer, dataBuffer.Length);
                Array.Copy(readBuffer, REQUEST_HEADER_LEN, newbuffer, dataBuffer.Length, readBuffer.Length - REQUEST_HEADER_LEN);
                dataBuffer = newbuffer;

                // Exit loop when \n (newline) is received as last byte
                if (readBuffer[readBuffer.Length - 1] == 0x0A) {
                    break;
                }
            }

            // Encode response string
            var response = Encoding.UTF8.GetString(dataBuffer);

            // Verify response string is valid json
            if (IsValidJson(response)) {
                return response;
            }
            else {
                return null;
            }
        }

        public void Reset() {

            while (true) {

                this.WaitForData(100, out var bytesAvailable);

                if (bytesAvailable > 0) {
                    var buffer = new byte[bytesAvailable];

                    // Drain away any data left over in buffer
                    this.i2cDevice.WriteRead(new byte[] { 0x00, bytesAvailable }, buffer);
                }
                else {
                    break;
                }
            }
        }

        internal static bool IsError(string json) {
            if (IsValidJson(json) && json.StartsWith("{\"err\":")) {
                return true;
            }
            else {
                return false;
            }
        }

        internal static bool IsValidJson(string json) {
            if (json.StartsWith("{") && json.EndsWith("}\n")
                || json.StartsWith("{") && json.EndsWith("}\r\n")) {
                return true;
            }
            else {
                return false;
            }
        }

        private void WaitForData(int timeout, out byte bytesAvailable) {

            var startTicks = DateTime.UtcNow.Ticks;
            
            pollBuffer[0] = 0;
            bytesAvailable = 0;

            do {
                var elapsed = (DateTime.UtcNow.Ticks - startTicks) / TimeSpan.TicksPerMillisecond;
                if (elapsed > timeout) {
                    return;
                }

                Thread.Sleep(POLLING_DELAY_MS);

                // Write an empty two byte buffer to poll for new data
                this.i2cDevice.WriteRead(this.emptyBuffer, pollBuffer);

                // The first byte in the buffer indicates the bytes available
                bytesAvailable = pollBuffer[0];

            } while (bytesAvailable == 0);
        }
    }

    public class RequestResults {

        public RequestResults(string response) {

            this.Response = response;

            if (string.IsNullOrEmpty(response)) {
                this.Response = "{\"err\":\"null or empty response\"}\r\n";
                return;
            }

            if (response.Contains("{}\r\n")) {
                this.IsSuccess = true;
                return;
            }

            if (NotecardController.IsError(response)) {
                return;
            }

            this.IsSuccess = true;
        }

        public RequestResults EnsureSuccess()
        {
            if (this.IsSuccess)
            {
                return this;
            }

            throw new Exception(this.Response);
        }

        public bool IsSuccess { get; private set; }

        public string Response { get; private set; }
    }

    public class JsonRequest : JsonObject {

        public JsonRequest(string req)
            => this.NoteRequests.Add($"\"req\":\"{req}\"");

        public void Add(string argument, JsonObject value)
            => this.NoteRequests.Add($"\"{argument}\":{value.ToJson()}");

        public override string ToJson()
            => $"{base.ToJson()}\n";
    }

    public class JsonObject {

        protected readonly ArrayList NoteRequests = new ArrayList();

        public void Add(string argument, string value)
            => this.NoteRequests.Add($"\"{argument}\":\"{value}\"");

        public void Add(string argument, int value)
            => this.NoteRequests.Add($"\"{argument}\":{value}");

        public void Add(string argument, long value)
            => this.NoteRequests.Add($"\"{argument}\":{value}");

        public void Add(string argument, float value)
            => this.NoteRequests.Add($"\"{argument}\":{value}");

        public void Add(string argument, double value)
            => this.NoteRequests.Add($"\"{argument}\":{value}");

        public void Add(string argument, bool value)
            => this.NoteRequests.Add($"\"{argument}\":{value}");

        public void Add(string argument, ArrayList value) {
            var sb = new StringBuilder();

            sb.Append($"\"{argument}\":");

            sb.Append("[");
            for (var i = 0; i < value.Count; i++) {

                if (value[i].GetType() == typeof(string)) {
                    sb.Append($"\"{value[i]}\"");
                }
                else {
                    sb.Append($"{value[i]}");
                }
                
                if (i < value.Count - 1) {
                    sb.Append(",");
                }
            }
            sb.Append("]");

            this.NoteRequests.Add(sb.ToString());
        }

        public void Clear()
            => this.NoteRequests.Clear();

        public int Count
            => this.NoteRequests.Count;

        public virtual string ToJson() {
            var sb = new StringBuilder();

            sb.Append("{");
            for (var i = 0; i < this.NoteRequests.Count; i++) {
                sb.Append(this.NoteRequests[i]);
                if (i < this.NoteRequests.Count - 1) {
                    sb.Append(",");
                }
            }
            sb.Append("}");

            return sb.ToString().ToLower();
        }
    }


    //public sealed class NotecardLogger : IDisposable {

    //    private readonly GpioController gpioController;
    //    private readonly UartController uartController;

    //    private readonly GpioPin enablePin;

    //    public NotecardLogger(UartController uartController, int enablePin)
    //        : this(uartController, new UartSetting() {
    //            BaudRate = 115200,
    //            DataBits = 8,
    //            Parity = UartParity.None,
    //            StopBits = UartStopBitCount.One,
    //            Handshaking = UartHandshake.None
    //        }, enablePin) {
    //    }

    //    public NotecardLogger(UartController uartController, UartSetting uartSettings, int enablePin) {

    //        this.uartController = uartController;
    //        this.uartController.SetActiveSettings(uartSettings);
    //        this.uartController.Enable();

    //        this.gpioController = GpioController.GetDefault();
    //        this.enablePin = this.gpioController.OpenPin(enablePin);
    //        this.enablePin.SetDriveMode(GpioPinDriveMode.Output);
    //        this.enablePin.Write(GpioPinValue.High);
    //    }

    //    public void Enable() => this.enablePin.Write(GpioPinValue.High);

    //    public void Disable() => this.enablePin.Write(GpioPinValue.Low);

    //    public void Dispose() {
    //        this.gpioController.Dispose();
    //        this.uartController.Dispose();
    //    }
    //}

    internal static class I2cExtensions {

        internal static void WriteJoinBytes(this I2cDevice device, byte[] first, byte[] second) {
            var firstLength = first.Length;
            var secondLength = second.Length;
            var buffer = new byte[firstLength + secondLength];

            Array.Copy(first, buffer, firstLength);
            Array.Copy(second, 0, buffer, firstLength, secondLength);

            device.Write(buffer);
        }
    }

    internal static class StringExtensions {

        internal static bool StartsWith(this string source, string value)
            => source.ToLower().IndexOf(value.ToLower()) == 0;

        internal static bool EndsWith(this string source, string value)
            => source.ToLower().IndexOf(value.ToLower()) == source.Length - value.Length;

        internal static bool Contains(this string source, string value)
            => source.ToLower().IndexOf(value.ToLower()) >= 0;
    }
}
