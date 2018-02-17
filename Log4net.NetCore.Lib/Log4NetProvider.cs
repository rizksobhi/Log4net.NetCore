using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Log4net.NetCore.Lib
{
    public class Log4NetProvider : ILoggerProvider
    {
        #region Fields
        private readonly string _ConnectionString;
        private readonly string _LogFilePath;
        private readonly ConcurrentDictionary<string, Log4NetLogger> _Loggers = new ConcurrentDictionary<string, Log4NetLogger>();
        #endregion

        public Log4NetProvider(string connectionString, string logFilePath)
        {
            _ConnectionString = connectionString;
            _LogFilePath = logFilePath;
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
            return new Log4NetLogger(name, _ConnectionString, _LogFilePath);
        }
        #endregion

        public void Dispose()
        {
            _Loggers.Clear();
        }
    }
}
