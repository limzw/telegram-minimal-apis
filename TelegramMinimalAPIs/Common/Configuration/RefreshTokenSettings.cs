namespace TelegramMinimalAPIs.Common.Configuration
{
    public class RefreshTokenSettings
    {
        public int RefreshTokenExpiryValue { get; set; }
        public string RefreshTokenExpiryValueType { get; set; }
        public int RefreshCookieExpiryOffsetValue { get; set; }
        public string RefreshCookieExpiryOffsetValueType { get; set; }

    }
}
