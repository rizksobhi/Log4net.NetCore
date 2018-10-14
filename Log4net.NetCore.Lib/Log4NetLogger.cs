using System;
using System.Reflection;
using log4net;
using log4net.Appender;
using log4net.Repository;
using Microsoft.Extensions.Logging;

namespace Log4net.NetCore.Lib
{
    internal class Log4NetLogger : Microsoft.Extensions.Logging.ILogger
    {
        #region Fields
        private readonly string _Name;
        private readonly ILog _Log;
        private static ILoggerRepository _LoggerRepository;
        #endregion

        public Log4NetLogger(string name, IAppender[] appenders)
        {
            _Name = name;

            if (_LoggerRepository != null)
                _Log = LogManager.GetLogger(_LoggerRepository.Name, name);

            if (_Log == null)
            {
                _LoggerRepository = LogManager.CreateRepository(Assembly.GetEntryAssembly(), typeof(log4net.Repository.Hierarchy.Hierarchy));
                _Log = LogManager.GetLogger(_LoggerRepository.Name, name);
                log4net.Config.BasicConfigurator.Configure(_LoggerRepository, appenders);
            }
        }

        #region Public Methods
        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Critical:
                    return _Log.IsFatalEnabled;
                case LogLevel.Debug:
                case LogLevel.Trace:
                    return _Log.IsDebugEnabled;
                case LogLevel.Error:
                    return _Log.IsErrorEnabled;
                case LogLevel.Information:
                    return _Log.IsInfoEnabled;
                case LogLevel.Warning:
                    return _Log.IsWarnEnabled;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel));
            }
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            Log(logLevel, state, exception, formatter);
        }

        private void Log<TState>(LogLevel logLevel, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            string message = null;

            if (null != formatter)
            {
                message = formatter(state, exception);
            }

            if (!string.IsNullOrEmpty(message) || exception != null)
            {
                switch (logLevel)
                {
                    case LogLevel.Critical:
                        _Log.Fatal(message, exception);
                        break;
                    case LogLevel.Debug:
                    case LogLevel.Trace:
                        _Log.Debug(message, exception);
                        break;
                    case LogLevel.Error:
                        _Log.Error(message, exception);
                        break;
                    case LogLevel.Information:
                        _Log.Info(message, exception);
                        break;
                    case LogLevel.Warning:
                        _Log.Warn(message, exception);
                        break;
                    default:
                        _Log.Warn($"Encountered unknown log level {logLevel}, writing out as Info.");
                        _Log.Info(message, exception);
                        break;
                }
            }
        }
        #endregion
    }
}
