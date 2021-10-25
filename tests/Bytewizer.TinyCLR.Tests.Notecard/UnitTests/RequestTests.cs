using System;
using System.Collections;

using Bytewizer.TinyCLR.Assertions;
using Bytewizer.TinyCLR.Drivers.Blues.Notecard;

namespace Bytewizer.TinyCLR.Tests.Notecard
{
    public class RequestTests : NotecardFixture
    {
        public RequestTests() : base() { }

        public void RandomRequest()
        {
            var request = new JsonRequest("card.random");

            Assert.AreEqual(
                request.ToJson(),
                "{\"req\":\"card.random\"}\n"
                );

            var results = _notecard.Request(request);

            StringAssert.Contains(results.Response, "\"count\"");
            Assert.True(results.IsSuccess);
        }

        public void RequestWithArguments()
        {
            var request = new JsonRequest("hub.log");
            request.Add("text", "something has gone wrong");
            request.Add("alert", true);

            Assert.AreEqual(
                request.ToJson(),
                "{\"req\":\"hub.log\",\"text\":\"something has gone wrong\",\"alert\":true}\n"
                );

            var results = _notecard.Request(request);

            Assert.True(results.IsSuccess);
        }

        public void RequestWithCapitalization()
        {
            var request = new JsonRequest("Card.Version");

            Assert.AreEqual(
                request.ToJson(),
                "{\"req\":\"card.version\"}\n"
                );

            var results = _notecard.Request(request);

            StringAssert.Contains(results.Response, "Blues Wireless");
            Assert.True(results.IsSuccess);
        }

        public void RequestWithBody()
        {
            var body = new JsonObject();
            body.Add("temp", 35.5);
            body.Add("humid", 56.23);

            var request = new JsonRequest("note.add");
            request.Add("body", body);
            request.Add("sync", true);

            Assert.AreEqual(
                request.ToJson(),
                "{\"req\":\"note.add\",\"body\":{\"temp\":35.5,\"humid\":56.23},\"sync\":true}\n"
                );

            var results = _notecard.Request(request);

            StringAssert.Contains(results.Response, "\"total\"");
            Assert.True(results.IsSuccess);
        }

        public void RequestWithBracketsArrayList()
        {
            var files = new ArrayList();
            files.Add("data.qi");
            files.Add("my-settings.db");

            var request = new JsonRequest("card.attn");
            request.Add("mode", "arm,files");
            request.Add("files", files);

            Assert.AreEqual(
                request.ToJson(),
                "{\"req\":\"card.attn\",\"mode\":\"arm,files\",\"files\":[\"data.qi\",\"my-settings.db\"]}\n"
                );

            var results = _notecard.Request(request);

            StringAssert.Contains(results.Response, "{}\r\n");
            Assert.True(results.IsSuccess);
        }

        public void EnsureSuccess()
        {
            Assert.Throws(typeof(Exception), () =>
            {
                var request = new JsonRequest("hub.notvalid");
                var results = _notecard.Request(request).EnsureSuccess();
            });
        }
    }
}