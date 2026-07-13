using FluentValidation;
using FlutterBackendCSharp.Common;
using FlutterBackendCSharp.Common.Database;
using FlutterBackendCSharp.Common.Database.Entities;
using FlutterBackendCSharp.Common.Services.Cookies;
using FlutterBackendCSharp.Common.Services.Loggers;
using MediatR;
using System.Security.Claims;
using System.Text.Json;

namespace FlutterBackendCSharp.Features.WebUser
{
    public class AuthenticateMainUser
    {
        public record AuthenticateMainUserRequest(Dictionary<string, string> authDict, string? oldRefreshToken) : IRequest<AuthenticateMainUserResponse>;

        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapPost("/api/main-user/authenticate", async Task<IResult> (HttpContext context, ISender mediator, AppDbContext appDbContext, CustomLoggerWrapper logger) =>
                {
                    using var reader = new StreamReader(context.Request.Body);
                    var body = await reader.ReadToEndAsync();
                    Dictionary<string, string> userCreds = JsonSerializer.Deserialize<Dictionary<string, string>>(body);

                    string? existingRefreshToken = "";
                    if (context.Request.Cookies.TryGetValue("myRefreshToken", out string? token))
                    {
                        existingRefreshToken = token;
                    }

                    var response = await mediator.Send(new AuthenticateMainUserRequest(userCreds, existingRefreshToken));

                    context.Response.Cookies.Append("myToken", response.accessCookie.TokenString, response.accessCookie.Options);
                    context.Response.Cookies.Append("myRefreshToken", response.refreshCookie.TokenString, response.refreshCookie.Options);

                    return Results.Ok();
                });
            }
        }

        public record AuthenticateMainUserResponse(Cookie? accessCookie, Cookie? refreshCookie, string message = "");

        public class AuthenticateMainUserRequestHandler : IRequestHandler<AuthenticateMainUserRequest, AuthenticateMainUserResponse>
        {
            private AppDbContext _appDbContext;
            private ICookieGenerator _cookieGenerator;

            public AuthenticateMainUserRequestHandler(AppDbContext appDbContext, ICookieGenerator cookieGenerator)
            {
                _appDbContext = appDbContext;
                _cookieGenerator = cookieGenerator;
            }

            public async Task<AuthenticateMainUserResponse> Handle(AuthenticateMainUserRequest request, CancellationToken cancellationToken)
            {
                //check for username first
                var adminAccount = _appDbContext.WebUsers.FirstOrDefault(admin => admin.Username == request.authDict["username"]);
                if (adminAccount == null)
                {
                    return new AuthenticateMainUserResponse(null, null);
                }
                else
                {
                    //update existing unhashed passwords with hashed ones
                    if (request.authDict["password"] == adminAccount.Password)
                    {
                        adminAccount.Password = BCrypt.Net.BCrypt.HashPassword(adminAccount.Password);
                        await _appDbContext.SaveChangesAsync();
                    }
                    else if (!BCrypt.Net.BCrypt.Verify(request.authDict["password"], adminAccount.Password))
                    {
                        adminAccount.LoginTryCount += 1;
                        await _appDbContext.SaveChangesAsync();

                        return new AuthenticateMainUserResponse(null, null, "invalid_password");
                    }
                }

                if (adminAccount.LoginTryCount > 0)
                {
                    //reset LoginTryCount on successful login
                    adminAccount.LoginTryCount = 0;
                }

                if (!string.IsNullOrEmpty(request.oldRefreshToken))
                {
                    var token = _appDbContext.RefreshTokens.FirstOrDefault(token => token.Token == request.oldRefreshToken);
                    if (token != null)
                    {
                        _appDbContext.RefreshTokens.Remove(token);
                    }
                }

                Claim[] claims =
                [
                    new Claim(ClaimTypes.Name, adminAccount.Username),
                    new Claim(ClaimTypes.Role, adminAccount.Role)
                ];

                Cookie accessCookie = _cookieGenerator.GenerateCookie(CookieType.ACCESS, claims) as Cookie;
                Cookie refreshCookie = _cookieGenerator.GenerateCookie(CookieType.REFRESH, null) as Cookie;

                RefreshToken refToken = new RefreshToken();
                refToken.Token = refreshCookie.TokenString;
                refToken.ExpiryDateTime = refreshCookie.TokenExpiryDateTime.Value;
                refToken.Username = adminAccount.Username;

                await _appDbContext.RefreshTokens.AddAsync(refToken);
                await _appDbContext.SaveChangesAsync();

                return new AuthenticateMainUserResponse(accessCookie, refreshCookie);
            }
        }

        public class AuthenticateMainUserRequestValidation : AbstractValidator<AuthenticateMainUserRequest>
        {
            public AuthenticateMainUserRequestValidation()
            {
                RuleFor(request => request.authDict).NotEmpty().Must(dict => dict.ContainsKey("username") && dict.ContainsKey("password"));
            }
        }
    }
}
