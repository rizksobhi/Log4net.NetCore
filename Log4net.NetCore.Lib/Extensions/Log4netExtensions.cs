using Microsoft.Extensions.Logging;

namespace Log4net.NetCore.Lib.Extensions
{
    public static class Log4netExtensions
    {
        public static ILoggerFactory AddLog4Net(this ILoggerFactory factory, string connectionString, string logFilePath)
        {
            factory.AddProvider(new Log4NetProvider(connectionString, logFilePath));
            return factory;
        }
    }
}
