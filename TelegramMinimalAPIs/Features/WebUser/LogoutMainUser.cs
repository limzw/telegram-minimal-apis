using FlutterBackendCSharp.Common;
using FlutterBackendCSharp.Common.Database;
using FlutterBackendCSharp.Common.Database.Entities;
using MediatR;

namespace FlutterBackendCSharp.Features.WebUser
{
    public class LogoutMainUser
    {
        public record LogoutMainUserRequest : IRequest<LogoutMainUserResponse>;

        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapPost("/api/main-user/logout", async (HttpContext context, IWebHostEnvironment environment, AppDbContext appDbContext) =>
                {
                    if (context.Request.Cookies.ContainsKey("myToken"))
                    {
                        context.Response.Cookies.Delete("myToken", new CookieOptions
                        {
                            HttpOnly = true,
                            SameSite = environment.IsDevelopment() ? SameSiteMode.None : SameSiteMode.Lax,
                            Secure = true,
                        });
                    }

                    if (context.Request.Cookies.ContainsKey("myRefreshToken"))
                    {
                        RefreshToken deleteToken = appDbContext.RefreshTokens.FirstOrDefault(token => token.Token == context.Request.Cookies["myRefreshToken"]);
                        appDbContext.RefreshTokens.Remove(deleteToken);
                        await appDbContext.SaveChangesAsync();

                        context.Response.Cookies.Delete("myRefreshToken", new CookieOptions
                        {
                            HttpOnly = true,
                            SameSite = environment.IsDevelopment() ? SameSiteMode.None : SameSiteMode.Lax,
                            Secure = true,
                        });
                    }

                    return Results.Ok();
                });
            }
        }

        public record LogoutMainUserResponse(IResult result);
    }
}
