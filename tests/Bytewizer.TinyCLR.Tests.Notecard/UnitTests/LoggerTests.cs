using System;
using System.Diagnostics;

using Bytewizer.TinyCLR.Assertions;
using Bytewizer.TinyCLR.Drivers.Blues.Notecard;

namespace Bytewizer.TinyCLR.Tests.Notecard
{
    public class LoggerTests : NotecardFixture
    {
        public LoggerTests() 
            : base() 
        {
            _notecardLogger.MessageAvailable += NotecardLogger_DataReceived;
        }

        public void LoggerEnable()
        {
            _notecardLogger.TraceOn();
            _notecard.Request(new JsonRequest("hub.sync"));
            _notecardLogger.TraceOff();
        }

        private void NotecardLogger_DataReceived(string message)
        {
            StringAssert.Contains(message, ":");
            Debug.WriteLine(message);
        }
    }
}