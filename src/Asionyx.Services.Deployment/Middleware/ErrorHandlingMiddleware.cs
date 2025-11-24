using System.Net;
using Asionyx.Library.Core;
using Asionyx.Library.Shared.Diagnostics;
using Asionyx.Services.Deployment.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Asionyx.Services.Deployment.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILog<ErrorHandlingMiddleware> _logger;
        private readonly IAppDiagnostics _diagnostics;

        public ErrorHandlingMiddleware(RequestDelegate next, ILog<ErrorHandlingMiddleware> logger, IAppDiagnostics diagnostics)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Log full exception for diagnostics and operator inspection
                try
                {
                    var name = $"exception_{DateTime.UtcNow:yyyyMMddHHmmssfff}";
                    var diagObj = new
                    {
                        Timestamp = DateTime.UtcNow,
                        Exception = ex.ToString(),
                        Path = context.Request?.Path.Value,
                        Method = context.Request?.Method,
                        Headers = context.Request?.Headers
                    };
                    await _diagnostics.WriteAsync(name, diagObj).ConfigureAwait(false);
                }
                catch
                {
                    // Swallow diagnostics write errors to avoid masking original exception
                }

                // Minimum information to return to clients
                var errorDto = new ErrorDto
                {
                    Error = "Internal server error",
                    Detail = null
                };

                // Log at error level (structured logger will pick up correlation id from scope)
                _logger.Error(ex, $"Unhandled exception while processing request {context.Request?.Path}");

                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json; charset=utf-8";
                var payload = JsonConvert.SerializeObject(errorDto);
                await context.Response.WriteAsync(payload).ConfigureAwait(false);
            }
        }
    }

    public static class ErrorHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ErrorHandlingMiddleware>();
        }
    }
}
