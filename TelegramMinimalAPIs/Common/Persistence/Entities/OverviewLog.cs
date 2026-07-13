namespace TelegramMinimalAPIs.Common.Database.Entities
{
    public class OverviewLog
    {
        public int Id { get; set; }
        public string Severity { get; set; }
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
    }
}
