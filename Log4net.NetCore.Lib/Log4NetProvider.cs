using System.Collections.Concurrent;
using log4net.Appender;
using Microsoft.Extensions.Logging;

namespace Log4net.NetCore.Lib
{
    public class Log4NetProvider : ILoggerProvider
    {
        #region Fields
        private readonly IAppender[] _Appenders;
        private readonly ConcurrentDictionary<string, Log4NetLogger> _Loggers = new ConcurrentDictionary<string, Log4NetLogger>();
        #endregion

        public Log4NetProvider(IAppender[] appenders)
        {
            _Appenders = appenders;
        }

        #region Public Methods
        public ILogger CreateLogger(string categoryName)
        {
            return _Loggers.GetOrAdd(categoryName, CreateLoggerImplementation);
        }
        #endregion

        #region Helper Methods
        private Log4NetLogger CreateLoggerImplementation(string name)
        {
            return new Log4NetLogger(name, _Appenders);
        }
        #endregion

        public void Dispose()
        {
            _Loggers.Clear();
        }
    }
}
