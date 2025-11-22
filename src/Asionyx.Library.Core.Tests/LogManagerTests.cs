using System;
using NUnit.Framework;
using Asionyx.Library.Core;

namespace Asionyx.Library.Core.Tests
{
    [TestFixture]
    public class LogManagerTests
    {
        [Test]
        public void GetLogger_ReturnsLoggerInstance()
        {
            var logger = LogManager.GetLogger<LogManagerTests>();
            Assert.IsNotNull(logger);
            Assert.IsInstanceOf<ILog<LogManagerTests>>(logger);
        }
    }
}