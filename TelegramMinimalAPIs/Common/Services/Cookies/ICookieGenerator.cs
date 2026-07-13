using System.Security.Claims;

namespace FlutterBackendCSharp.Common.Services.Cookies
{
    public interface ICookieGenerator
    {
        public ICookie GenerateCookie(CookieType cookieType, Claim[]? claims);
    }
}
