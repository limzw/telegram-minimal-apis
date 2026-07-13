using FluentValidation;
using MediatR;
using System.Security.Claims;
using System.Text.Json;
using TelegramMinimalAPIs.Common;
using TelegramMinimalAPIs.Common.Database;
using TelegramMinimalAPIs.Common.Database.Entities;
using TelegramMinimalAPIs.Common.Services.Cookies;

namespace TelegramMinimalAPIs.Features.WebUser
{
    public class CreateMainUser
    {
        public record CreateMainUserRequest(Dictionary<string, string> authDict, string idempotencyKey) : IRequest<CreateMainUserResponse>, IIdempotentRequest
        {
            public string IdempotencyKey => idempotencyKey;
        }

        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapPost("/api/main-user/create-new", async Task<IResult> (HttpContext context, ISender mediator) =>
                {
                    using var reader = new StreamReader(context.Request.Body);
                    var body = await reader.ReadToEndAsync();
                    Dictionary<string, string> userCreds = JsonSerializer.Deserialize<Dictionary<string, string>>(body);

                    if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var key) || string.IsNullOrWhiteSpace(key) || key == "null")
                        return Results.BadRequest("Idempotency-Key header is required.");

                    var response = await mediator.Send(new CreateMainUserRequest(userCreds, key));
                    if (response.accessCookie == null || response.refreshCookie == null)
                    {
                        if (!string.IsNullOrEmpty(response.message))
                        {
                            return Results.BadRequest(new { error = response.message });
                        }
                        return Results.BadRequest();
                    }

                    context.Response.Cookies.Append("myToken", response.accessCookie.TokenString, response.accessCookie.Options);
                    context.Response.Cookies.Append("myRefreshToken", response.refreshCookie.TokenString, response.refreshCookie.Options);

                    return Results.Created();
                });
            }
        }

        public record CreateMainUserResponse(Cookie? accessCookie, Cookie? refreshCookie, string message = "");

        public class CreateMainUserRequestHandler : IRequestHandler<CreateMainUserRequest, CreateMainUserResponse>
        {
            AppDbContext _appDbContext;
            private ICookieGenerator _cookieGenerator;

            public CreateMainUserRequestHandler(AppDbContext appDbContext, ICookieGenerator cookieGenerator)
            {
                _appDbContext = appDbContext;
                _cookieGenerator = cookieGenerator;
            }

            public async Task<CreateMainUserResponse> Handle(CreateMainUserRequest request, CancellationToken cancellationToken)
            {
                var existingAdmin = _appDbContext.WebUsers.FirstOrDefault(x => x.Username == request.authDict["username"]);
                if (existingAdmin != null)
                {
                    return new CreateMainUserResponse(null, null, "username_exists");
                }

                Common.Database.Entities.WebUser newWebUser = new Common.Database.Entities.WebUser();
                newWebUser.Username = request.authDict["username"];

                string unhashedPassword = request.authDict["password"];
                newWebUser.Password = BCrypt.Net.BCrypt.HashPassword(unhashedPassword);

                newWebUser.DateModified = DateTime.Now.ToUniversalTime();
                newWebUser.Name = request.authDict["username"];
                newWebUser.Role = "User";

                _appDbContext.WebUsers.Add(newWebUser);
                await _appDbContext.SaveChangesAsync();

                Claim[] claims =
                [
                    new Claim(ClaimTypes.Name, newWebUser.Username),
                    new Claim(ClaimTypes.Role, newWebUser.Role)
                ];

                Cookie accessCookie = _cookieGenerator.GenerateCookie(CookieType.ACCESS, claims) as Cookie;
                Cookie refreshCookie = _cookieGenerator.GenerateCookie(CookieType.REFRESH, null) as Cookie;

                RefreshToken refToken = new RefreshToken();
                refToken.Token = refreshCookie.TokenString;
                refToken.ExpiryDateTime = refreshCookie.TokenExpiryDateTime.Value;
                refToken.Username = newWebUser.Username;

                await _appDbContext.RefreshTokens.AddAsync(refToken);
                await _appDbContext.SaveChangesAsync();

                CreateMainUserResponse createMainUserResponse = new CreateMainUserResponse(accessCookie, refreshCookie);

                return createMainUserResponse;
            }
        }

        public class CreateMainUserRequestValidation : AbstractValidator<CreateMainUserRequest>
        {
            public CreateMainUserRequestValidation()
            {
                RuleFor(request => request.authDict).NotEmpty().Must(dict => dict.ContainsKey("username") && dict.ContainsKey("password"));
            }
        }
    }
}
