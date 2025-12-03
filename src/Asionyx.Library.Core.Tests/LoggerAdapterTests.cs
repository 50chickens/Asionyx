using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Asionyx.Library.Core.Tests
{
    public class TestLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var msg = formatter(state, exception);
            Entries.Add((logLevel, msg));
        }
    }

    [TestFixture]
    public class LoggerAdapterTests
    {
        class Dummy { }

        [Test]
        public void LoggerAdapter_Delegates_Log_Calls_To_ILogger()
        {
            var testLogger = new TestLogger<Dummy>();
            var adapter = new LoggerAdapter<Dummy>(testLogger);

            adapter.Debug("dbg");
            adapter.Info("info");
            adapter.Warn("warn");
            adapter.Error("err");
            adapter.Error(new System.Exception("ex"), "errWithEx");

            Assert.That(testLogger.Entries.Count, Is.GreaterThanOrEqualTo(4));
            Assert.That(testLogger.Entries[0].Message, Is.EqualTo("dbg"));
            Assert.That(testLogger.Entries[1].Message, Is.EqualTo("info"));
        }
    }
}
