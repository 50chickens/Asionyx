using System.Reflection;
using NLog;

namespace Asionyx.Library.Core
{
    public class NLogLoggerCore<T> : ILog<T>
    {
        private readonly Logger _logger;

        public NLogLoggerCore()
        {
            _logger = NLog.LogManager.GetLogger(typeof(T).FullName ?? typeof(T).Name);
        }

        public void Debug(string message) => _logger.Debug(message);
        public void Info(string message) => _logger.Info(message);
        public void Warn(string message) => _logger.Warn(message);
        public void Error(string message) => _logger.Error(message);
        public void Error(Exception ex, string message) => _logger.Error(ex, message);

        // Structured logging: accept an object (anonymous or POCO) and record its properties as event properties
        public void Info(object payload)
        {
            var evt = CreateEvent(LogLevel.Info, payload, null);
            _logger.Log(evt);
        }

        public void Info(string message, object payload)
        {
            var evt = CreateEvent(LogLevel.Info, payload, message);
            _logger.Log(evt);
        }

        private LogEventInfo CreateEvent(LogLevel level, object? payload, string? message)
        {
            var evt = new LogEventInfo(level, _logger.Name, message ?? string.Empty);
            if (payload != null)
            {
                foreach (var kv in ToDictionary(payload))
                {
                    evt.Properties[kv.Key] = kv.Value ?? string.Empty;
                }
            }
            return evt;
        }

        private IDictionary<string, object?> ToDictionary(object payload)
        {
            var dict = new Dictionary<string, object?>();
            var t = payload.GetType();
            foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                try
                {
                    dict[p.Name] = p.GetValue(payload);
                }
                catch { dict[p.Name] = null; }
            }
            return dict;
        }
    }
}
