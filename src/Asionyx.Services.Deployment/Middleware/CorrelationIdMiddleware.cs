using Asionyx.Library.Core;
using NLog;

namespace Asionyx.Services.Deployment.Middleware
{
    public class CorrelationIdMiddleware
    {
        private const string HeaderName = "X-Correlation-ID";
        private readonly RequestDelegate _next;
        private readonly ILog<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(RequestDelegate next, ILog<CorrelationIdMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            string correlationId = context.Request.Headers[HeaderName];
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = Guid.NewGuid().ToString("D");
                context.Request.Headers[HeaderName] = correlationId;
            }

            context.Response.Headers[HeaderName] = correlationId;

            // Set NLog MDLC so JsonLayout can include correlationId in structured logs.
            try
            {
                MappedDiagnosticsLogicalContext.Set("CorrelationId", correlationId);
            }
            catch { }

            try
            {
                await _next(context).ConfigureAwait(false);
            }
            finally
            {
                try { MappedDiagnosticsLogicalContext.Remove("CorrelationId"); } catch { }
            }
        }
    }

    public static class CorrelationIdMiddlewareExtensions
    {
        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        {
            return app.UseMiddleware<CorrelationIdMiddleware>();
        }
    }
}
