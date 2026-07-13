namespace TelegramMinimalAPIs.Common.Database.Entities
{
    public class IdempotencyKey
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string? Response { get; set; }
        public string Status { get; set; }
        public DateTime DateTimeCreated { get; set; }
    }
}
