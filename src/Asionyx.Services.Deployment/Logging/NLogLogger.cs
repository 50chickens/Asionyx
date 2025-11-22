using Asionyx.Library.Core;

namespace Asionyx.Services.Deployment.Logging
{
    public class NLogLogger<T> : ILog<T>
    {
        private readonly NLog.Logger _logger;

        public NLogLogger()
        {
            _logger = NLog.LogManager.GetLogger(typeof(T).FullName ?? typeof(T).Name);
        }

        public void Debug(string message) => _logger.Debug(message);
        public void Info(string message) => _logger.Info(message);
        public void Warn(string message) => _logger.Warn(message);
        public void Error(string message) => _logger.Error(message);
        public void Error(Exception ex, string message) => _logger.Error(ex, message);
    }
}
