using Serilog.Core;
using Serilog.Events;

namespace TelegramMinimalAPIs.Common.Services.Loggers
{
    public class UserActivitiySink : ILogEventSink
    {
        public void Emit(LogEvent logEvent)
        {
            //checks if UserId is passed in, if not return
            if (!logEvent.Properties.TryGetValue("UserId", out var userIdProp))
                return;

            var userId = userIdProp.ToString().Trim('"');
            var directory = Path.Combine(AppContext.BaseDirectory, "logs", "users", userId);
            Directory.CreateDirectory(directory);

            var filePath = Path.Combine(directory, "activity.txt");
            var message = logEvent.RenderMessage();
            File.AppendAllText(filePath, $"{logEvent.Timestamp:yyyy-MM-dd HH:mm:ss} {message}{Environment.NewLine}");
        }
    }
}
