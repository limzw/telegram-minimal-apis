using FlutterBackendCSharp.Common;
using FlutterBackendCSharp.Common.Database;
using FlutterBackendCSharp.Common.Database.Entities;
using FlutterBackendCSharp.Common.Services.Cookies;
using MediatR;
using System.Security.Claims;

namespace FlutterBackendCSharp.Features.WebUser
{
    public class RefreshMainUser
    {
        public record RefreshMainUserRequest(string refToken) : IRequest<RefreshMainUserResponse>;

        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapPost("/api/main-user/refresh", async Task<IResult> (HttpContext context, ISender mediator, IWebHostEnvironment environment, ILogger<RefreshMainUser> logger) =>
                {
                    var refreshToken = context.Request.Cookies["myRefreshToken"];
                    if (string.IsNullOrEmpty(refreshToken))
                    {
                        logger.LogInformation("Refresh token not found");
                        return Results.BadRequest(new
                        {
                            error = "invalid_grant",
                            error_description = "The provided authorization grant is invalid, expired, or revoked."
                        });
                    }

                    var response = await mediator.Send(new RefreshMainUserRequest(refreshToken));

                    //token has either been revoked (null) or has expired
                    if (response.accessCookie == null || response.refreshCookie == null)
                    {
                        logger.LogInformation("Refresh token has expired/been revoked");
                        context.Response.Cookies.Delete("myRefreshToken");
                        context.Response.Cookies.Delete("myToken");
                        return Results.BadRequest(new
                        {
                            error = "invalid_grant",
                            error_description = "The provided authorization grant is invalid, expired, or revoked."
                        });
                    }

                    context.Response.Cookies.Append("myToken", response.accessCookie.TokenString, response.accessCookie.Options);
                    context.Response.Cookies.Append("myRefreshToken", response.refreshCookie.TokenString, response.refreshCookie.Options);

                    return Results.Ok(new { Online = true });
                }).AllowAnonymous();
            }
        }

        public record RefreshMainUserResponse(Cookie? accessCookie, Cookie? refreshCookie);

        public class RefreshMainUserRequestHandler : IRequestHandler<RefreshMainUserRequest, RefreshMainUserResponse>
        {
            private AppDbContext _appDbContext;
            private ICookieGenerator _tokenGenerator;

            public RefreshMainUserRequestHandler(AppDbContext appDbContext, IConfiguration configuration, IWebHostEnvironment environment, ICookieGenerator tokenGenerator)
            {
                _appDbContext = appDbContext;
                _tokenGenerator = tokenGenerator;
            }
            public async Task<RefreshMainUserResponse> Handle(RefreshMainUserRequest request, CancellationToken cancellationToken)
            {
                RefreshToken? token = _appDbContext.RefreshTokens.FirstOrDefault(token => token.Token == request.refToken);

                if (token == null || token != null && DateTime.UtcNow > token.ExpiryDateTime)
                {
                    return new RefreshMainUserResponse(null, null);
                }

                var sAdmin = _appDbContext.WebUsers.First(sa => sa.Username == token.Username);
                _appDbContext.RefreshTokens.Remove(token);
                Claim[] claims =
                [
                    new Claim(ClaimTypes.Name, sAdmin.Username),
                    new Claim(ClaimTypes.Role, sAdmin.Role)
                ];

                Cookie newAccessCookie = _tokenGenerator.GenerateCookie(CookieType.ACCESS, claims) as Cookie;
                Cookie newRefreshCookie = _tokenGenerator.GenerateCookie(CookieType.REFRESH, null) as Cookie;

                RefreshToken newRefreshToken = new RefreshToken();
                newRefreshToken.Token = newRefreshCookie.TokenString;
                newRefreshToken.ExpiryDateTime = newRefreshCookie.TokenExpiryDateTime.Value;
                newRefreshToken.Username = sAdmin.Username;

                await _appDbContext.RefreshTokens.AddAsync(newRefreshToken);
                await _appDbContext.SaveChangesAsync();

                return new RefreshMainUserResponse(newAccessCookie, newRefreshCookie);
            }
        }
    }
}
