using System;
using Microsoft.Extensions.Logging;

namespace ComingSoonPlugin.Services
{
    /// <summary>
    /// Wraps Emby's native MediaBrowser.Model.Logging.ILogger as a Microsoft.Extensions.Logging
    /// ILogger, so the rest of the plugin can use the modern structured-logging API. Needed
    /// because Emby's DI container only registers the legacy ILogManager/ILogger singletons, not
    /// the generic ILogger&lt;T&gt; - types it instantiates directly (IScheduledTask,
    /// IServerEntryPoint, ...) must take ILogManager and build this adapter via GetLogger(name).
    /// </summary>
    public sealed class LegacyLoggerAdapter : ILogger
    {
        private readonly MediaBrowser.Model.Logging.ILogger _legacy;

        public LegacyLoggerAdapter(MediaBrowser.Model.Logging.ILogger legacy)
        {
            _legacy = legacy;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);

            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    _legacy.Debug(message);
                    break;
                case LogLevel.Information:
                    _legacy.Info(message);
                    break;
                case LogLevel.Warning:
                    _legacy.Warn(exception is null ? message : $"{message} :: {exception}");
                    break;
                case LogLevel.Error:
                    if (exception != null)
                    {
                        _legacy.ErrorException(message, exception);
                    }
                    else
                    {
                        _legacy.Error(message);
                    }
                    break;
                case LogLevel.Critical:
                    if (exception != null)
                    {
                        _legacy.FatalException(message, exception);
                    }
                    else
                    {
                        _legacy.Fatal(message);
                    }
                    break;
                default:
                    _legacy.Info(message);
                    break;
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
