using Asionyx.Library.Core;
using Autofac;

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
            // Configure NLog using IConfiguration only. Do not set any log levels in code;
            // all filtering and levels must be defined in appsettings.json (or other IConfiguration sources).
            // Use NLog.Extensions.Logging to translate the IConfiguration NLog section into NLog configuration.
            var nlogConfig = new NLog.Extensions.Logging.NLogLoggingConfiguration(_configuration.GetSection("NLog"));
            NLog.LogManager.Configuration = nlogConfig;

            // Register generic ILog<T> -> LoggerAdapter<T> which delegates to Microsoft.Extensions.Logging.ILogger<T>
            // This centralizes logging on ILogger<T> while keeping NLog as the provider.
            builder.RegisterGeneric(typeof(Asionyx.Library.Core.LoggerAdapter<>))
                .As(typeof(ILog<>))
                .InstancePerDependency();
        }
    }
}
