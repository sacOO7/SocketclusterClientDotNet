using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ScClient.Tests
{
    [TestClass]
    public class ParserTest
    {
        [TestMethod]
        public void ShouldReturnIsAuthenticated()
        {
            Parser.MessageType messageType = Parser.Parse("data", 1, null, null);
            Assert.AreEqual(Parser.MessageType.Isauthenticated, messageType);
        }

        [TestMethod]
        public void ShouldReturnPublished()
        {
            Parser.MessageType messageType = Parser.Parse("data", 34, null, null);
            Assert.AreEqual(Parser.MessageType.Ackreceive, messageType);
        }

        [TestMethod]
        public void ShouldReturnRemoveToken()
        {
            Parser.MessageType messageType = Parser.Parse("data", null, 34, "#publish");
            Assert.AreEqual(Parser.MessageType.Publish, messageType);
        }

        [TestMethod]
        public void ShouldReturnSetToken()
        {
            Parser.MessageType messageType = Parser.Parse("data", null, 2, "#removeAuthToken");
            Assert.AreEqual(Parser.MessageType.Removetoken, messageType);
        }

        [TestMethod]
        public void ShouldReturnEvent()
        {
            Parser.MessageType messageType = Parser.Parse("data", null, 12, "#setAuthToken");
            Assert.AreEqual(Parser.MessageType.Settoken, messageType);
        }

        [TestMethod]
        public void ShouldReturnAckReceive()
        {
            Parser.MessageType messageType = Parser.Parse("data", null, 67, "chat");
            Assert.AreEqual(Parser.MessageType.Event, messageType);
        }
    }
}