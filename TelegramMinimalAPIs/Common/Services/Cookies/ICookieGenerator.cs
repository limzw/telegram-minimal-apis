using System.Security.Claims;

namespace TelegramMinimalAPIs.Common.Services.Cookies
{
    public interface ICookieGenerator
    {
        public ICookie GenerateCookie(CookieType cookieType, Claim[]? claims);
    }
}
