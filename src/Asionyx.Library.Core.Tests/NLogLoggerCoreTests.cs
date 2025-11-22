using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Asionyx.Library.Core;

namespace Asionyx.Library.Core.Tests
{
    [TestFixture]
    public class NLogLoggerCoreTests
    {
        private NLogLoggerCore<NLogLoggerCoreTests> _logger;

        [SetUp]
        public void SetUp()
        {
            _logger = new NLogLoggerCore<NLogLoggerCoreTests>();
        }

        [Test]
        public void Debug_Info_Warn_Error_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _logger.Debug("debug"));
            Assert.DoesNotThrow(() => _logger.Info("info"));
            Assert.DoesNotThrow(() => _logger.Warn("warn"));
            Assert.DoesNotThrow(() => _logger.Error("error"));
            Assert.DoesNotThrow(() => _logger.Error(new Exception("ex"), "error with ex"));
        }

        [Test]
        public void Info_WithPayload_DoesNotThrow()
        {
            var payload = new { Foo = "bar", Value = 42 };
            Assert.DoesNotThrow(() => _logger.Info(payload));
            Assert.DoesNotThrow(() => _logger.Info("msg", payload));
        }

        [Test]
        public void CreateEvent_NullPayload_DoesNotThrow()
        {
            // Use reflection to call private CreateEvent
            var method = typeof(NLogLoggerCore<NLogLoggerCoreTests>).GetMethod("CreateEvent", BindingFlags.NonPublic | BindingFlags.Instance);
            var evt = method.Invoke(_logger, new object[] { NLog.LogLevel.Info, null, "msg" });
            Assert.IsNotNull(evt);
        }

        [Test]
        public void CreateEvent_WithPayload_HandlesProperties()
        {
            var payload = new { Foo = "bar", Value = 42 };
            var method = typeof(NLogLoggerCore<NLogLoggerCoreTests>).GetMethod("CreateEvent", BindingFlags.NonPublic | BindingFlags.Instance);
            var evt = (NLog.LogEventInfo)method.Invoke(_logger, new object[] { NLog.LogLevel.Info, payload, "msg" });
            Assert.AreEqual("bar", evt.Properties["Foo"]);
            Assert.AreEqual(42, evt.Properties["Value"]);
        }

        [Test]
        public void ToDictionary_HandlesPropertyException()
        {
            var loggerType = typeof(NLogLoggerCore<NLogLoggerCoreTests>);
            var toDict = loggerType.GetMethod("ToDictionary", BindingFlags.NonPublic | BindingFlags.Instance);
            var payload = new ExceptionPropertyObject();
            var dict = (IDictionary<string, object>)toDict.Invoke(_logger, new object[] { payload });
            Assert.IsTrue(dict.ContainsKey("Throwing"));
            Assert.IsNull(dict["Throwing"]);
        }

        private class ExceptionPropertyObject
        {
            public string Ok => "ok";
            public string Throwing => throw new Exception("fail");
        }
    }
}