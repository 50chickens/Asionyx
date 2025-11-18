using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// If running on Linux with systemd available, this will enable systemd integration when the package is present.
try
{
	builder.Host.UseSystemd();
}
catch { /* optional: ignore if not available */ }

var app = builder.Build();

app.MapGet("/info", () => Results.Ok(new { Service = "Asionyx.Services.HelloWorld", Message = "Hello from HelloWorld service" }));

app.Run();
