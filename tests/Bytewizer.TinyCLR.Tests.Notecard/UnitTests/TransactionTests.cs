using System;

using Bytewizer.TinyCLR.Assertions;

namespace Bytewizer.TinyCLR.Tests.Notecard
{
    public class TransactionTests : NotecardFixture
    {
        public TransactionTests() 
            : base() { }

        public void TransactionWithEmptyRequest()
        {
            Assert.Throws(typeof(ArgumentNullException), () =>
            {
                var request = string.Empty;
                var response = _notecard.Transaction(request);
            });
        }

        public void TransactionWithInvalidRequest()
        {
            var request = "{ }";
            var response = _notecard.Transaction(request);

            StringAssert.Contains(response, "\"err\"");
        }

        public void TransactionSyncStatus()
        {
            var request = "{\"req\":\"hub.sync.status\"}";
            var response = _notecard.Transaction(request);

            StringAssert.Contains(response, "\"status\"");
        }

        public void TransactionWithOutNewLine()
        {
            var request = "{\"req\":\"card.version\"}";
            var response = _notecard.Transaction(request);

            StringAssert.Contains(response, "\"body\"");
        }

        public void TransactionWithEmptyResponse()
        {
            var request = "{\"req\":\"hub.sync\"}\r\n";
            var response = _notecard.Transaction(request);

            StringAssert.Contains(response, "{}\r\n");
        }
    }
}