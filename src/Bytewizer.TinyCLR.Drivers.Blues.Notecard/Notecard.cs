using System;
using System.Text;
using System.Threading;
using System.Collections;
using System.Diagnostics;

#if NanoCLR

using System.Device.I2c;

namespace Bytewizer.NanoCLR.Drivers.Blues.Notecard

#else

using GHIElectronics.TinyCLR.Devices.I2c;

namespace Bytewizer.TinyCLR.Drivers.Blues.Notecard
#endif
{
    /// <summary>
    /// Configures the <see cref="NotecardController"/> to use the I2C bus for communication with the host.
    /// </summary>
    public sealed class NotecardController : IDisposable
    {
        const int I2C_ADDRESS = 0x17;
        const int I2C_DELAY_MS = 2;
        const int REQUEST_HEADER_LEN = 2;
        const int POLLING_TIMEOUT_MS = 5000;
        const int POLLING_DELAY_MS = 50;

        private readonly I2cDevice i2cDevice;

        private readonly byte[] emptyBuffer = new byte[2];
        private readonly byte[] pollBuffer = new byte[2];

        private readonly object requestLock = new object();

#if NanoCLR

        public NotecardController(int i2cBus)
            : this(new I2cConnectionSettings(
                    i2cBus,
                    I2C_ADDRESS,
                    I2cBusSpeed.StandardMode)
                  )
        {}

        public NotecardController(I2cConnectionSettings i2cSettings)
        {          
            try
            {       
                this.i2cDevice = I2cDevice.Create(i2cSettings);
                this.Reset();
            }
            catch (Exception ex)
            {
                throw new Exception("Notecard not responding", ex);
            }
        }
#else

        /// <summary>
        /// Initializes a default instance of the <see cref="NotecardController"/> class.
        /// </summary>
        /// <param name="i2cController">The i2c controller to use.</param>
        public NotecardController(I2cController i2cController)
            : this(i2cController, new I2cConnectionSettings(I2C_ADDRESS)
            {
                BusSpeed = 100000,
                AddressFormat = I2cAddressFormat.SevenBit
            })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotecardController"/> class.
        /// </summary>
        /// <param name="i2cController">The i2c controller to use.</param>
        /// <param name="i2cSettings">The i2c controller settings to use.</param>
        public NotecardController(I2cController i2cController, I2cConnectionSettings i2cSettings)
        {
            try
            {
                this.i2cDevice = i2cController.GetDevice(i2cSettings);
                this.Reset();
            }
            catch (Exception ex)
            {
                throw new Exception("Notecard not responding", ex);
            }
        }

#endif

        /// <summary>
        /// Pro-actively frees resources owned by this instance.
        /// </summary>
        public void Dispose()
            => this.i2cDevice.Dispose();

        /// <summary>
        /// Validate request removing whitespace and checking for starting and ending brackets.
        /// Thows <see cref="InvalidOperationException"/> if validation fails.
        /// </summary>
        public bool ValidateRequest { get; set; } = true;

        /// <summary>
        /// Sends a request for processing on the notecard.
        /// </summary>
        /// <param name="noteRequest">A <see cref=" JsonRequest"/> request.</param>
        /// <returns>A <see cref="RequestResults"/> populated with the request response.</returns>
        public RequestResults Request(JsonRequest noteRequest)
            => new RequestResults(this.Transaction(noteRequest));

        /// <summary>
        /// Sends a request for processing on the notecard.
        /// </summary>
        /// <param name="json">A <see cref="string"/> request formated as json.</param>
        /// <returns>A <see cref="RequestResults"/> populated with the request response.</returns>
        public RequestResults Request(string json)
            => new RequestResults(this.Transaction(json));

        /// <summary>
        /// Sends a transaction request for processing on the notecard.
        /// </summary>
        /// <param name="noteRequest">A <see cref=" JsonRequest"/> request.</param>
        /// <returns>A <see cref="string"/> populated with the json response.</returns>
        public string Transaction(JsonRequest noteRequest)
            => this.Transaction(noteRequest.ToJson());

        /// <summary>
        /// Sends a transaction request for processing on the notecard.
        /// </summary>
        /// <param name="json">A <see cref="string"/> request formated as json.</param>
        /// <returns>A <see cref="string"/> populated with the json response.</returns>
        public string Transaction(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new ArgumentNullException(nameof(json));
            }

            // Notecard doesn't support parallel request/responses
            lock (requestLock)
            {
                if (ValidateRequest)
                {
                    // Remove any whitespaces from request string
                    json = RemoveWhitespace(json);

                    if (!IsValidJson(json))
                    {
                        throw new InvalidOperationException("Invalid json request");
                    }
                }

                // Verify string ends with a newline character and if not add it
                if (!json.EndsWith("\n"))
                {
                    json += "\n";
                }

                // Encode request string
                var requestBytes = Encoding.UTF8.GetBytes(json);

                // Write request
                this.i2cDevice.WriteJoinBytes(new byte[] { (byte)json.Length }, requestBytes);

                // Notecard i2c commands can not be received more quickly then 1 ms appart.
                Thread.Sleep(I2C_DELAY_MS);

                // Create buffers
                var dataBuffer = new byte[0];
                var writeBuffer = new byte[2];

                while (true)
                {
                    // Poll for request to be processed
                    this.WaitForData(POLLING_TIMEOUT_MS, out var bytesAvailable);

                    // The minimum bytes returned should be "{}\r\n"
                    if (bytesAvailable < 4)
                    {
                        break;
                    }

                    // Write a two byte buffer with the second byte as the number of bytes available
                    writeBuffer[1] = bytesAvailable;
                    var readBuffer = new byte[bytesAvailable + REQUEST_HEADER_LEN];
                    this.i2cDevice.WriteRead(writeBuffer, readBuffer);

                    // Check to make sure the second byte matches the number of bytes requested
                    if (bytesAvailable != readBuffer[1])
                    {
                        throw new Exception("Unexpected i2c protocol byte count");
                    }

                    // Resize and update data buffer
                    var newbuffer = new byte[dataBuffer.Length + readBuffer.Length - REQUEST_HEADER_LEN];
                    Array.Copy(dataBuffer, newbuffer, dataBuffer.Length);
                    Array.Copy(readBuffer, REQUEST_HEADER_LEN, newbuffer, dataBuffer.Length, readBuffer.Length - REQUEST_HEADER_LEN);
                    dataBuffer = newbuffer;

                    // Exit loop when \n (newline) is received as last byte
                    if (readBuffer[readBuffer.Length - 1] == 0x0A)
                    {
                        break;
                    }
                }

                // Encode response string
                var response = Encoding.UTF8.GetString(dataBuffer, 0, dataBuffer.Length);

                // Verify response string is valid json
                if (IsValidJson(response))
                {
                    return response;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Resets the I2C port by draining away any left over data in the buffer.
        /// </summary>
        public void Reset()
        {
            // Notecard doesn't support parallel request/responses
            lock (requestLock)
            {
                while (true)
                {
                    this.WaitForData(100, out var bytesAvailable);

                    if (bytesAvailable > 0)
                    {
                        var buffer = new byte[bytesAvailable];

                        // Drain away any data left over in buffer
                        this.i2cDevice.WriteRead(new byte[] { 0x00, bytesAvailable }, buffer);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        internal static bool IsError(string json)
        {
            if (IsValidJson(json) && json.StartsWith("{\"err\":"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static bool IsValidJson(string json)
        {
            if (json.StartsWith("{") && json.EndsWith("}\n")
                || json.StartsWith("{") && json.EndsWith("}\r\n"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static string RemoveWhitespace(string json)
        {
            int j = 0;
            char[] str = new char[json.Length];

            for (int i = 0; i < json.Length; ++i)
            {
                char temp = json[i];

                if (!temp.IsWhiteSpace())
                {
                    str[j] = temp;
                    ++j;
                }
            }

            return $"{new string(str, 0, j)}\n";
        }

        private void WaitForData(int timeout, out byte bytesAvailable)
        {

            var startTicks = DateTime.UtcNow.Ticks;

            pollBuffer[0] = 0;
            bytesAvailable = 0;

            do
            {
                if ((DateTime.UtcNow.Ticks - startTicks) / TimeSpan.TicksPerMillisecond > timeout)
                {
                    return;
                }

                // Write an empty two byte buffer to poll for new data
                this.i2cDevice.WriteRead(this.emptyBuffer, pollBuffer);

                // The first byte in the buffer indicates the bytes available
                bytesAvailable = pollBuffer[0];

                Thread.Sleep(POLLING_DELAY_MS);

            } while (bytesAvailable == 0);
        }
    }

    /// <summary>
    ///  Represents a response from the notecard.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay, nq}")]
    public class RequestResults
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestResults" /> class.
        /// </summary>
        /// <param name="response">The json response message.</param>
        public RequestResults(string response)
        {
            this.Response = response;

            if (string.IsNullOrEmpty(response))
            {
                this.Response = "{\"err\":\"null or empty response\"}\r\n";
                return;
            }

            if (response.Contains("{}\r\n"))
            {
                this.IsSuccess = true;
                return;
            }

            if (NotecardController.IsError(response))
            {
                return;
            }

            this.IsSuccess = true;
        }

        /// <summary>
        /// Throws an exception if the <see cref="IsSuccess"/> property for the json response is <c>false</c>"/>.
        /// </summary>
        public RequestResults EnsureSuccess()
        {
            if (this.IsSuccess)
            {
                return this;
            }

            throw new Exception(this.Response);
        }

        /// <summary>
        /// A value that indicates if the notcard response was successful.
        /// </summary>
        public bool IsSuccess { get; private set; }

        /// <summary>
        /// Gets the json response message.
        /// </summary>
        /// <value>The json response message.</value>
        public string Response { get; private set; }

        ///	<summary>
        ///	Debugger display for this object.
        ///	</summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebuggerDisplay
        {
            get { return $"{Response}";}
        }
    }

    /// <summary>
    ///  Represents a notecard command request.
    /// </summary>
    public class JsonRequest : JsonObject
    {
        /// <summary>
        /// Creates a new command object for processing on the notecard.
        /// </summary>
        /// <param name="req">The command name (i.e. 'note.add').</param>
        public JsonRequest(string req)
            => this.NoteRequests.Add($"\"req\":\"{req.ToLower()}\"");

        /// <summary>
        /// Adds a command argument to <see cref="JsonRequest"/> object.
        /// </summary>
        /// <param name="argument">The argument name.</param>
        /// <param name="value">The argument value.</param>
        public void Add(string argument, JsonObject value)
            => this.NoteRequests.Add($"\"{argument.ToLower()}\":{value.ToJson()}");

        /// <summary>
        /// Returns a <see cref="string"/> that represents the <see cref="JsonRequest"/> object.
        /// </summary>
        public override string ToJson()
            => $"{base.ToJson()}\n";
    }

    /// <summary>
    ///  Represents a notecard json request object.
    /// </summary>
    public class JsonObject
    {
        protected readonly ArrayList NoteRequests = new ArrayList();

        /// <summary>
        /// Adds an argument and value to the <see cref="JsonObject"/>.
        /// </summary>
        /// <param name="argument">The argument name.</param>
        /// <param name="value">The argument value.</param>
        public void Add(string argument, string value)
            => this.NoteRequests.Add($"\"{argument.ToLower()}\":\"{value}\"");

        /// <summary>
        /// Adds an argument and value to the <see cref="JsonObject"/>.
        /// </summary>
        /// <param name="argument">The argument name.</param>
        /// <param name="value">The argument value.</param>
        public void Add(string argument, int value)
            => this.NoteRequests.Add($"\"{argument.ToLower()}\":{value}");

        /// <summary>
        /// Adds an argument and value to the <see cref="JsonObject"/>.
        /// </summary>
        /// <param name="argument">The argument name.</param>
        /// <param name="value">The argument value.</param>
        public void Add(string argument, long value)
            => this.NoteRequests.Add($"\"{argument.ToLower()}\":{value}");

        /// <summary>
        /// Adds an argument and value to the <see cref="JsonObject"/>.
        /// </summary>
        /// <param name="argument">The argument name.</param>
        /// <param name="value">The argument value.</param>
        public void Add(string argument, float value)
            => this.NoteRequests.Add($"\"{argument.ToLower()}\":{value}");

        /// <summary>
        /// Adds an argument and value to the <see cref="JsonObject"/>.
        /// </summary>
        /// <param name="argument">The argument name.</param>
        /// <param name="value">The argument value.</param>
        public void Add(string argument, double value)
            => this.NoteRequests.Add($"\"{argument.ToLower()}\":{value}");

        /// <summary>
        /// Adds an argument and value to the <see cref="JsonObject"/>.
        /// </summary>
        /// <param name="argument">The argument name.</param>
        /// <param name="value">The argument value.</param>
        public void Add(string argument, bool value)
            => this.NoteRequests.Add($"\"{argument.ToLower()}\":{value}");

        /// <summary>
        /// Adds an argument and value to the <see cref="JsonObject"/>.
        /// </summary>
        /// <param name="argument">The argument name.</param>
        /// <param name="value">The argument value.</param>
        public void Add(string argument, ArrayList value)
        {
            var sb = new StringBuilder();

            sb.Append($"\"{argument.ToLower()}\":");

            sb.Append("[");
            for (var i = 0; i < value.Count; i++)
            {

                if (value[i].GetType() == typeof(string))
                {
                    sb.Append($"\"{value[i]}\"");
                }
                else
                {
                    sb.Append($"{value[i]}");
                }

                if (i < value.Count - 1)
                {
                    sb.Append(",");
                }
            }
            sb.Append("]");

            this.NoteRequests.Add(sb.ToString());
        }

        /// <summary>
        /// Removes all elements from the <see cref="JsonObject"/>.
        /// </summary>
        public void Clear()
            => this.NoteRequests.Clear();

        /// <summary>
        /// Gets the number of elements contained in the <see cref="JsonObject"/>.
        /// </summary>
        public int Count
            => this.NoteRequests.Count;

        /// <summary>
        /// Returns a string that represents the <see cref="JsonObject"/>.
        /// </summary>
        public virtual string ToJson()
        {
            var sb = new StringBuilder();
            var count = this.NoteRequests.Count;

            sb.Append("{");
            for (var i = 0; i < count; i++)
            {
                sb.Append(this.NoteRequests[i]);
                if (i < count - 1)
                {
                    sb.Append(",");
                }
            }
            sb.Append("}");

            return sb.ToString();
        }
    }

    /// <summary>
    /// Contains extension methods for <see cref="string"/> object.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Determines whether the beginning of this string instance matches the specified string.
        /// </summary>
        /// <param name="source">The source string.</param>
        /// <param name="value">The string to compare.</param>
        /// <returns><c>true</c> if value matches the beginning of this string; otherwise, <c>false.</c></returns>
        public static bool StartsWith(this string source, string value)
            => source.ToLower().IndexOf(value.ToLower()) == 0;

        /// <summary>
        /// Determines whether the end of this string instance matches the specified string.
        /// </summary>
        /// <param name="source">The source string.</param>
        /// <param name="value">The string to compare.</param>
        /// <returns><c>true</c> if value matches the beginning of this string; otherwise, <c>false.</c></returns>
        public static bool EndsWith(this string source, string value)
            => source.ToLower().IndexOf(value.ToLower()) == source.Length - value.Length;

        /// <summary>
        /// Returns a value indicating whether a specified substring occurs within this string.
        /// </summary>
        /// <param name="source">The source string.</param>
        /// <param name="value">The string to seek.</param>
        /// <returns><c>true</c> if the value parameter occurs within this string, or if value is the empty string (""); otherwise, <c>false.</c></returns>
        public static bool Contains(this string source, string value)
            => source.ToLower().IndexOf(value.ToLower()) >= 0;
    }

    internal static class CharExtensions
    {
        internal static bool IsWhiteSpace(this char source)
        {
            return (source == ' ' || source == '\t' || source == '\n' || source == '\r' || source == '\v');
        }
    }

    internal static class I2cExtensions
    {
        internal static void WriteJoinBytes(this I2cDevice device, byte[] first, byte[] second)
        {
            var buffer = new byte[first.Length + second.Length];

            Array.Copy(first, buffer, first.Length);
            Array.Copy(second, 0, buffer, first.Length, second.Length);

            device.Write(buffer);
        }
    }
}