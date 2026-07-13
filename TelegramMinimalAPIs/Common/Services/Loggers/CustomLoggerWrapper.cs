using Serilog.Context;

namespace TelegramMinimalAPIs.Common.Services.Loggers
{
    public enum Database
    {
        APILOGS,
        OVERVIEWLOGS,
        SERVICEUSERSACTIVITY
    }

    public class CustomLoggerWrapper
    {
        private readonly ILogger<CustomLoggerWrapper> _logger;
        public CustomLoggerWrapper(ILogger<CustomLoggerWrapper> logger)
        {
            _logger = logger;
        }

        public void Log(Database databaseType, LogLevel logLevel, string message)
        {
            if (!_logger.IsEnabled(logLevel))
            {
                return;
            }

            using (LogContext.PushProperty("database", Enum.GetName(typeof(Database), databaseType)))
            {
                _logger.Log(logLevel, message);
            }
        }

        public void LogServiceUserActivity(LogLevel logLevel, string phoneNumber, string message)
        {
            if (!_logger.IsEnabled(logLevel))
            {
                return;
            }

            using (LogContext.PushProperty("database", Database.SERVICEUSERSACTIVITY.ToString()))
            using (LogContext.PushProperty("phoneNumber", phoneNumber))
            {
                _logger.Log(logLevel, message);

            }
            ;
        }
    }
}
