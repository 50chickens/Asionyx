using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Asionyx.Services.Deployment.Tests.Middleware
{
    [TestFixture]
    public class CorrelationIdMiddlewareTests
    {
        [Test]
        public async Task InvokeAsync_AddsCorrelationIdHeader_WhenMissing()
        {
            // Arrange
            var context = new DefaultHttpContext();
            RequestDelegate next = (ctx) => Task.CompletedTask;
            var middleware = new Asionyx.Services.Deployment.Middleware.CorrelationIdMiddleware(next, NullLogger<Asionyx.Services.Deployment.Middleware.CorrelationIdMiddleware>.Instance);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            Assert.That(context.Response.Headers.ContainsKey("X-Correlation-ID"), Is.True);
            var val = context.Response.Headers["X-Correlation-ID"].ToString();
            Assert.That(string.IsNullOrWhiteSpace(val), Is.False);
        }
    }
}
