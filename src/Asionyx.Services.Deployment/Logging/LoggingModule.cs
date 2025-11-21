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
            // Read logging preferences from Solution1.Library.Logging per requirements
            var libLogSection = _configuration.GetSection("Solution1.Library.Logging");
            var levelStr = libLogSection["LogLevel"] ?? "Debug";
            var verbose = libLogSection.GetValue<bool?>("Verbose") ?? false;
            var nlogMinLevel = NLog.LogLevel.FromString(levelStr);

            var cfg = new LoggingConfiguration();

            var nlogSection = _configuration.GetSection("NLog");

            // Console target
            var consoleEnabled = nlogSection.GetValue<bool?>("Console:Enabled") ?? true;
            if (consoleEnabled)
            {
                var consoleTarget = new ColoredConsoleTarget("console");
                var consoleLayout = new JsonLayout { IncludeAllProperties = true, RenderEmptyObject = false };
                consoleLayout.Attributes.Add(new JsonAttribute("timestamp", "${longdate}"));
                consoleLayout.Attributes.Add(new JsonAttribute("level", "${level:upperCase=true}"));
                consoleLayout.Attributes.Add(new JsonAttribute("logger", "${logger}"));
                consoleLayout.Attributes.Add(new JsonAttribute("correlationId", "${event-properties:item=CorrelationId}"));
                consoleLayout.Attributes.Add(new JsonAttribute("message", "${message}"));
                consoleLayout.Attributes.Add(new JsonAttribute("exception", "${exception:format=toString}"));
                consoleTarget.Layout = consoleLayout;
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
                    Layout = new JsonLayout { IncludeAllProperties = true, RenderEmptyObject = false }
                };
                var fileLayout = (JsonLayout)fileTarget.Layout;
                fileLayout.Attributes.Add(new JsonAttribute("timestamp", "${longdate}"));
                fileLayout.Attributes.Add(new JsonAttribute("level", "${level:upperCase=true}"));
                fileLayout.Attributes.Add(new JsonAttribute("logger", "${logger}"));
                fileLayout.Attributes.Add(new JsonAttribute("correlationId", "${event-properties:item=CorrelationId}"));
                fileLayout.Attributes.Add(new JsonAttribute("message", "${message}"));
                fileLayout.Attributes.Add(new JsonAttribute("exception", "${exception:format=toString}"));
                cfg.AddTarget(fileTarget);
                cfg.AddRule(nlogMinLevel, NLog.LogLevel.Fatal, fileTarget);
            }

            NLog.LogManager.Configuration = cfg;

            // Register generic ILog<T> -> NLogLogger<T>
                 builder.RegisterGeneric(typeof(Asionyx.Library.Core.NLogLoggerCore<>))
                     .As(typeof(ILog<>))
                     .SingleInstance();
        }
    }
}
