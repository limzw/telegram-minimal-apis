namespace TelegramMinimalAPIs.Common.Database.Entities
{
    public class ServiceUser
    {
        public int Id { get; set; }
        public string Guid { get; set; }
        public string UserPhoneNumber { get; set; }
        public string Path { get; set; }
        public string? UserId { get; set; }
        public bool IsActive { get; set; }
        public bool IsAuthenticated { get; set; }

    }
}
