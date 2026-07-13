namespace TelegramMinimalAPIs.Common.Database.Entities
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; }
        public DateTime ExpiryDateTime { get; set; }
        public string Username { get; set; }
    }
}
