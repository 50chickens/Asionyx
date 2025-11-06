using System;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Asionyx.Library.Asio;
using Asionyx.Library.Core;

var builder = Host.CreateDefaultBuilder(args)
   .ConfigureAppConfiguration((context, cfg) =>
   {
       cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
   })
   .ConfigureWebHostDefaults(webBuilder =>
   {
       webBuilder.ConfigureKestrel(serverOptions => { /* use defaults or configure as needed */ })
           .ConfigureServices(services =>
           {
               // Minimal services required for a Kestrel-hosted control-plane
               services.AddEndpointsApiExplorer();
               services.AddSwaggerGen();

               // Cross-platform DI: register IAsioFactory with OS-specific selection.
               // - On Windows prefer real drivers (if available), fall back to TestAsioFactory.
               // - On non-Windows use TestAsioFactory with fakes by default.
               services.AddSingleton<IAsioFactory>(sp =>
               {
                   var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                   if (isWindows)
                   {
                       // Attempt to use real drivers when present; TestAsioFactory will choose real drivers if available.
                       try
                       {
                           return new TestAsioFactory(forceFake: false) as IAsioFactory;
                       }
                       catch
                       {
                           return new TestAsioFactory(forceFake: true);
                       }
                   }
                   else
                   {
                       // Non-Windows: use fake factory to avoid platform-specific dependencies
                       return new TestAsioFactory(forceFake: true);
                   }
               });
           })
           .Configure(app =>
           {
               var env = app.ApplicationServices.GetRequiredService<IHostEnvironment>();
               if (env.IsDevelopment())
               {
                   app.UseSwagger();
                   app.UseSwaggerUI();
               }

               app.UseRouting();

               app.UseEndpoints(endpoints =>
               {
                   endpoints.MapGet("/health", async context =>
                   {
                       context.Response.ContentType = "text/plain";
                       await context.Response.WriteAsync("OK");
                   });

                   endpoints.MapGet("/weatherforecast", async context =>
                   {
                       var summaries = new[]
                       {
                           "Freezing","Bracing","Chilly","Cool","Mild","Warm","Balmy","Hot","Sweltering","Scorching"
                       };

                       var forecast = Enumerable.Range(1, 5).Select(index =>
                           new WeatherForecast(
                               DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                               Random.Shared.Next(-20, 55),
                               summaries[Random.Shared.Next(summaries.Length)]
                           )).ToArray();

                       context.Response.ContentType = "application/json";
                       await context.Response.WriteAsJsonAsync(forecast);
                   }).WithDisplayName("GetWeatherForecast");
               });
           });
   });

var host = builder.Build();
await host.RunAsync();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
   public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
