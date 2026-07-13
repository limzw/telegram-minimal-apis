using FlutterBackendCSharp.Common.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace FlutterBackendCSharp.Common.Services.Cookies
{
    public class CookieGenerator : ICookieGenerator
    {
        IWebHostEnvironment _environment;
        private readonly JwtSettings _jwtSettings;
        private readonly RefreshTokenSettings _refreshTokenSettings;

        public CookieGenerator(IWebHostEnvironment environment, IOptions<JwtSettings> jwtSettings, IOptions<RefreshTokenSettings> refreshTokenSettings)
        {
            _environment = environment;
            _jwtSettings = jwtSettings.Value;
            _refreshTokenSettings = refreshTokenSettings.Value;
        }

        public ICookie GenerateCookie(CookieType cookieType, Claim[]? claims)
        {
            DateTimeOffset dateTimeOffset = default; //time to be based off what was set in secrets.json/environment variable
            string tokenString = "";

            Cookie newCookie = new Cookie();
            switch (cookieType)
            {
                case CookieType.ACCESS:
                    dateTimeOffset = DateTime.UtcNow.AddMinutes(_jwtSettings.JwtExpiryMinutes);
                    tokenString = GenerateAccessToken(claims!);
                    break;

                case CookieType.REFRESH:
                    dateTimeOffset = GetRefreshCookieDateTimeOffset(_refreshTokenSettings, newCookie);
                    tokenString = GenerateRefreshToken();
                    break;
            }

            CookieOptions cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                SameSite = _environment.IsDevelopment() ? SameSiteMode.None : SameSiteMode.Lax,
                Secure = true,
                Expires = dateTimeOffset
            };

            newCookie.TokenString = tokenString;
            newCookie.Options = cookieOptions;

            return newCookie;
        }

        private DateTimeOffset GetRefreshCookieDateTimeOffset(RefreshTokenSettings refreshTokenSettings, Cookie newCookie)
        {
            DateTimeOffset dateTimeOffset = DateTime.UtcNow;
            int refreshTokenExpiryValue = refreshTokenSettings.RefreshTokenExpiryValue;
            switch (refreshTokenSettings.RefreshTokenExpiryValueType)
            {
                case "minute":
                    dateTimeOffset = dateTimeOffset.AddMinutes(refreshTokenExpiryValue);
                    break;

                case "hour":
                    dateTimeOffset = dateTimeOffset.AddHours(refreshTokenExpiryValue);
                    break;

                case "day":
                    dateTimeOffset = dateTimeOffset.AddDays(refreshTokenExpiryValue);
                    break;
            }
            newCookie.TokenExpiryDateTime = DateTime.SpecifyKind(dateTimeOffset.DateTime, DateTimeKind.Utc);

            int refreshCookieExpiryValue = refreshTokenSettings.RefreshCookieExpiryOffsetValue;
            switch (refreshTokenSettings.RefreshCookieExpiryOffsetValueType)
            {
                case "minute":
                    dateTimeOffset = dateTimeOffset.AddMinutes(refreshCookieExpiryValue);
                    break;

                case "hour":
                    dateTimeOffset = dateTimeOffset.AddHours(refreshCookieExpiryValue);
                    break;

                case "day":
                    dateTimeOffset = dateTimeOffset.AddDays(refreshCookieExpiryValue);
                    break;
            }

            return dateTimeOffset;
        }

        private string GenerateRefreshToken()
        {
            // generate 64 random bytes and convert to base64 string
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private string GenerateAccessToken(Claim[] claims)
        {
            string secretKey = _jwtSettings.JwtSecretKey;
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var credentials = new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256
            );

            SecurityTokenDescriptor descriptor = new SecurityTokenDescriptor
            {
                Issuer = _jwtSettings.JwtIssuer,
                Audience = _jwtSettings.JwtAudience,
                Subject = new ClaimsIdentity(claims),
                SigningCredentials = credentials,
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.JwtExpiryMinutes)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(descriptor);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public enum CookieType
    {
        ACCESS,
        REFRESH
    }
    public class Cookie : ICookie
    {
        public string TokenString { get; set; }
        public CookieOptions Options { get; set; }
        public DateTime? TokenExpiryDateTime { get; set; }
    }
}
