using Microsoft.VisualStudio.TestTools.UnitTesting;
using ScClient.Models;

namespace ScClient.Tests
{
    [TestClass]
    public class ParserTest
    {
        [TestMethod]
        public void ShouldReturnIsAuthenticated()
        {
            var messageEvent = new MessageEvent
            {
                Event = null,
                Data = "data",
                Cid = null,
                Error = null,
                Rid = 1
            };
            Parser.MessageType messageType = Parser.Parse(messageEvent);
            Assert.AreEqual(Parser.MessageType.Isauthenticated, messageType);
        }

        [TestMethod]
        public void ShouldReturnPublished()
        {
            var messageEvent = new MessageEvent
            {
                Event = null,
                Data = "data",
                Cid = null,
                Error = null,
                Rid = 34
            };

            Parser.MessageType messageType = Parser.Parse(messageEvent);
            Assert.AreEqual(Parser.MessageType.Ackreceive, messageType);
        }

        [TestMethod]
        public void ShouldReturnRemoveToken()
        {
            var messageEvent = new MessageEvent
            {
                Event = "#publish",
                Data = "data",
                Cid = 34,
                Error = null,
                Rid = null
            };

            Parser.MessageType messageType = Parser.Parse(messageEvent);
            Assert.AreEqual(Parser.MessageType.Publish, messageType);
        }

        [TestMethod]
        public void ShouldReturnSetToken()
        {
            var messageEvent = new MessageEvent
            {
                Event = "#removeAuthToken",
                Data = "data",
                Cid = 2,
                Error = null,
                Rid = null
            };
            Parser.MessageType messageType = Parser.Parse(messageEvent);
            Assert.AreEqual(Parser.MessageType.Removetoken, messageType);
        }

        [TestMethod]
        public void ShouldReturnEvent()
        {
            var messageEvent = new MessageEvent
            {
                Event = "#setAuthToken",
                Data = "data",
                Cid = 12,
                Error = null,
                Rid = null
            };
            Parser.MessageType messageType = Parser.Parse(messageEvent);
            Assert.AreEqual(Parser.MessageType.Settoken, messageType);
        }

        [TestMethod]
        public void ShouldReturnAckReceive()
        {
            var messageEvent = new MessageEvent
            {
                Event = "chat",
                Data = "data",
                Cid = 67,
                Error = null,
                Rid = null
            };
            Parser.MessageType messageType = Parser.Parse(messageEvent);
            Assert.AreEqual(Parser.MessageType.Event, messageType);
        }
    }
}