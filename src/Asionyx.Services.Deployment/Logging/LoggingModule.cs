using Autofac;
using Autofac.Core;
using Asionyx.Library.Core;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using Microsoft.Extensions.Configuration;

namespace Asionyx.Services.Deployment.Logging
{
    public class LoggingModule : Module
    {
        private readonly IConfiguration _configuration;

        public LoggingModule(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void Load(ContainerBuilder builder)
        {
            // Configure NLog programmatically based on settings in appsettings.json
            var nlogSection = _configuration.GetSection("NLog");
            var minLevelStr = nlogSection["MinLevel"] ?? "Debug";
            var nlogMinLevel = NLog.LogLevel.FromString(minLevelStr);

            var cfg = new LoggingConfiguration();

            // Console target
            var consoleEnabled = nlogSection.GetValue<bool?>("Console:Enabled") ?? true;
            if (consoleEnabled)
            {
                var consoleTarget = new ColoredConsoleTarget("console")
                {
                    Layout = new JsonLayout
                    {
                        Attributes =
                        {
                            new JsonAttribute("timestamp", "${longdate}"),
                            new JsonAttribute("level", "${level:upperCase=true}"),
                            new JsonAttribute("logger", "${logger}"),
                            new JsonAttribute("correlationId", "${event-properties:item=CorrelationId}"),
                            new JsonAttribute("message", "${message}"),
                            new JsonAttribute("exception", "${exception:format=toString}")
                        }
                    }
                };
                cfg.AddTarget(consoleTarget);
                cfg.AddRule(nlogMinLevel, NLog.LogLevel.Fatal, consoleTarget);
            }

            // File target
            var fileEnabled = nlogSection.GetValue<bool?>("File:Enabled") ?? true;
            if (fileEnabled)
            {
                var fileName = nlogSection["File:FileName"] ?? "logs/asionyx.json";
                var fileTarget = new FileTarget("file")
                {
                    FileName = fileName,
                    KeepFileOpen = false,
                    Layout = new JsonLayout
                    {
                        Attributes =
                        {
                            new JsonAttribute("timestamp", "${longdate}"),
                            new JsonAttribute("level", "${level:upperCase=true}"),
                            new JsonAttribute("logger", "${logger}"),
                            new JsonAttribute("correlationId", "${event-properties:item=CorrelationId}"),
                            new JsonAttribute("message", "${message}"),
                            new JsonAttribute("exception", "${exception:format=toString}")
                        }
                    }
                };
                cfg.AddTarget(fileTarget);
                cfg.AddRule(nlogMinLevel, NLog.LogLevel.Fatal, fileTarget);
            }

            LogManager.Configuration = cfg;

            // Register generic ILog<T> -> NLogLogger<T>
            builder.RegisterGeneric(typeof(NLogLogger<>))
                   .As(typeof(ILog<>))
                   .SingleInstance();
        }
    }
}
