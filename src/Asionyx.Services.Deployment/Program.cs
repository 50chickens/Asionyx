using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog.Web;
using Asionyx.Library.Core;

var builder = Host.CreateDefaultBuilder(args)
    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureAppConfiguration((ctx, cfg) => {
        cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    })
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.ConfigureServices((context, services) =>
        {
            services.AddControllers();
            services.AddOptions();
        });

        webBuilder.Configure((ctx, app) =>
        {
            var env = ctx.HostingEnvironment;
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage();
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        });
    })
    .ConfigureLogging((context, logging) => { /* logging configured by NLog */ })
    .UseNLog();

builder.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    // Register core implementations here. Keep it minimal for the scaffold.
    containerBuilder.RegisterType<LocalSystemConfigurator>().As<ISystemConfigurator>().SingleInstance();
});

var appHost = builder.Build();
// On startup, attempt to start HelloWorld service via the systemd emulator CLI if available
try
{
    var systemdCli = Environment.GetEnvironmentVariable("ASIONYX_SYSTEMD_CLI") ?? "../Asionyx.Services.Deployment.SystemD/Asionyx.Services.Deployment.SystemD";
    // Try to run CLI: start Asionyx.Services.HelloWorld
    var pi = new System.Diagnostics.ProcessStartInfo(systemdCli, "start Asionyx.Services.HelloWorld") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
    var p = System.Diagnostics.Process.Start(pi);
    if (p != null)
    {
        var outp = p.StandardOutput.ReadToEndAsync();
        var err = p.StandardError.ReadToEndAsync();
        p.WaitForExit(2000);
        if (!p.HasExited) p.Kill();
        Console.WriteLine($"systemd-cli output: {outp.Result}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to invoke systemd CLI: {ex.Message}");
}
appHost.Run();

// Minimal implementation for demo - in real project this would be in another file
public class LocalSystemConfigurator : ISystemConfigurator
{
    public string GetInfo() => "Asionyx deployment service - local system configurator";
    public void ApplyConfiguration(string json) { /* apply config - scaffold */ }
}
