using NUnit.Framework;
using Asionyx.Library.Core;

namespace Asionyx.Library.Core.Tests
{
    [TestFixture]
    public class ILogTests
    {
        private class DummyLogger : ILog<ILogTests>
        {
            public string LastMessage { get; private set; }
            public void Debug(string message) => LastMessage = message;
            public void Info(string message) => LastMessage = message;
            public void Warn(string message) => LastMessage = message;
            public void Error(string message) => LastMessage = message;
            public void Error(System.Exception ex, string message) => LastMessage = message + ex.Message;
        }

        [Test]
        public void AllMethods_AreCallable()
        {
            var logger = new DummyLogger();
            logger.Debug("debug");
            Assert.AreEqual("debug", logger.LastMessage);
            logger.Info("info");
            Assert.AreEqual("info", logger.LastMessage);
            logger.Warn("warn");
            Assert.AreEqual("warn", logger.LastMessage);
            logger.Error("error");
            Assert.AreEqual("error", logger.LastMessage);
            logger.Error(new System.Exception("ex"), "error");
            Assert.IsTrue(logger.LastMessage.Contains("error"));
            Assert.IsTrue(logger.LastMessage.Contains("ex"));
        }
    }
}