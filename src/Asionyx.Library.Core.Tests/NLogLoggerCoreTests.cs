using System;
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
    }
}