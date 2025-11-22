using NUnit.Framework;
using Asionyx.Library.Core;
using System.Threading.Tasks;

namespace Asionyx.Library.Core.Tests
{
    [TestFixture]
    public class IApiKeyServiceTests
    {
        private class DummyApiKeyService : IApiKeyService
        {
            public Task<string> EnsureApiKeyAsync() => Task.FromResult("dummy");
            public bool Validate(string key) => key == "dummy";
        }

        [Test]
        public async Task EnsureApiKeyAsync_ReturnsDummy()
        {
            var svc = new DummyApiKeyService();
            Assert.AreEqual("dummy", await svc.EnsureApiKeyAsync());
        }

        [Test]
        public void Validate_ReturnsTrueForDummy()
        {
            var svc = new DummyApiKeyService();
            Assert.IsTrue(svc.Validate("dummy"));
            Assert.IsFalse(svc.Validate("notdummy"));
        }
    }
}