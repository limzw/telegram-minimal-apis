using Serilog.Context;

namespace FlutterBackendCSharp.Common.Services.Loggers
{
    public class UserActivityLogger
    {
        private readonly ILogger<UserActivityLogger> _logger;
        public UserActivityLogger(ILogger<UserActivityLogger> logger)
        {
            _logger = logger;
        }
        public void LogActivity(string userId, string activity, string message)
        {
            using (LogContext.PushProperty("UserId", userId))
            {
                _logger.LogInformation($"{activity}: {message}");
            }
        }
    }
}
