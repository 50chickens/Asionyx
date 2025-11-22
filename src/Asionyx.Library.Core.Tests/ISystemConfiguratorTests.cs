using NUnit.Framework;
using Asionyx.Library.Core;

namespace Asionyx.Library.Core.Tests
{
    [TestFixture]
    public class ISystemConfiguratorTests
    {
        private class DummyConfigurator : ISystemConfigurator
        {
            public bool Called { get; private set; }
            public string GetInfo() => "info";
            public void ApplyConfiguration(string json) => Called = true;
        }

        [Test]
        public void ApplyConfiguration_CallsMethod()
        {
            var conf = new DummyConfigurator();
            Assert.IsFalse(conf.Called);
            conf.ApplyConfiguration("{}");
            Assert.IsTrue(conf.Called);
        }
        [Test]
        public void GetInfo_ReturnsInfo()
        {
            var conf = new DummyConfigurator();
            Assert.AreEqual("info", conf.GetInfo());
        }
    }
}