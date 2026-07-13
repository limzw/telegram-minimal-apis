namespace FlutterBackendCSharp.Common.Configuration
{
    public class JwtSettings
    {
        public string JwtSecretKey { get; set; }
        public string JwtIssuer { get; set; }
        public string JwtAudience { get; set; }
        public int JwtExpiryMinutes { get; set; }
    }
}
