namespace FlutterBackendCSharp.Common.Services.Cookies
{
    public interface ICookie
    {
        public string TokenString { get; set; }
        public CookieOptions Options { get; set; }
    }
}
