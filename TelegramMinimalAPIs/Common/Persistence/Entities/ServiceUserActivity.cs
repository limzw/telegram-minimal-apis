namespace TelegramMinimalAPIs.Common.Persistence.Entities
{
    public class ServiceUserActivity
    {
        public int Id { get; set; }
        public string Guid { get; set; }
        public DateTime Timestamp { get; set; }
        public string Details { get; set; }
    }
}
