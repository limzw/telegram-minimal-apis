using FluentValidation;
using MediatR;
using System.Text.Json;
using TelegramMinimalAPIs.Common;
using TelegramMinimalAPIs.Common.Database;
using TelegramMinimalAPIs.Common.Services.RuntimeUser;
using TelegramMinimalAPIs.Common.Utilities;

namespace TelegramMinimalAPIs.Features.ServiceUser
{
    public class AuthenticateTwoFactorAuthServiceUser
    {
        public record AuthenticateTwoFactorAuthServiceUserRequest(Dictionary<string, dynamic> keyValuePairs) : IRequest<AuthenticateTwoFactorAuthServiceUserResponse>;

        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapPost("/api/service-user/authenticate-two-factor", async (HttpRequest request, ISender mediator) =>
                {
                    var body = new StreamReader(request.Body);
                    string postData = await body.ReadToEndAsync();
                    Dictionary<string, dynamic> keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(postData) ?? new Dictionary<string, dynamic>();
                    var response = await mediator.Send(new AuthenticateTwoFactorAuthServiceUserRequest(keyValuePairs));
                    return response.result;
                });
            }
        }
        public record AuthenticateTwoFactorAuthServiceUserResponse(IResult result);

        public class AuthenticateTwoFactorAuthServiceUserHandler : IRequestHandler<AuthenticateTwoFactorAuthServiceUserRequest, AuthenticateTwoFactorAuthServiceUserResponse>
        {
            private readonly AppDbContext _appDbContext;
            private readonly RuntimeUserRegistry _runtimeUserRegistry;
            private readonly ILogger<AuthenticateTwoFactorAuthServiceUserHandler> _logger;

            public AuthenticateTwoFactorAuthServiceUserHandler(AppDbContext appDbContext, ILogger<AuthenticateTwoFactorAuthServiceUserHandler> logger, RuntimeUserRegistry runtimeUserRegistry)
            {
                _appDbContext = appDbContext;
                _logger = logger;
                _runtimeUserRegistry = runtimeUserRegistry;
            }
            public async Task<AuthenticateTwoFactorAuthServiceUserResponse> Handle(AuthenticateTwoFactorAuthServiceUserRequest request, CancellationToken cancellationToken)
            {
                //check runtime client list for client
                string guid = HttpHelper.GetObjectValue(request.keyValuePairs["guid"]);
                string code = HttpHelper.GetObjectValue(request.keyValuePairs["code"]);

                RuntimeUser? runtimeUser = _runtimeUserRegistry.Get(guid);

                if (runtimeUser != null)
                {
                    runtimeUser.AuthenticateTwoFactorAuthCode(code);
                    InitialisationStatus status = await runtimeUser.WaitForStatusAsync();
                    if (status == InitialisationStatus.Ready)
                    {
                        Common.Database.Entities.ServiceUser appUser = _appDbContext.ServiceUsers.First(user => user.Guid == guid);
                        appUser.UserId = runtimeUser.UserId.ToString();
                        _logger.LogInformation($"UserId: {appUser.UserId}");
                        appUser.IsActive = true;
                        appUser.IsAuthenticated = true;

                        await _appDbContext.SaveChangesAsync();
                        return new AuthenticateTwoFactorAuthServiceUserResponse(Results.Ok());
                    }
                    else
                    {
                        return new AuthenticateTwoFactorAuthServiceUserResponse(Results.BadRequest(new { Error = runtimeUser.ErrorMessage }));
                    }
                }

                return new AuthenticateTwoFactorAuthServiceUserResponse(Results.BadRequest(new { Error = "Unknown Error" }));
            }
        }

        public class AuthenticateTwoFactorAuthServiceUserRequestValidator : AbstractValidator<AuthenticateTwoFactorAuthServiceUserRequest>
        {
            public AuthenticateTwoFactorAuthServiceUserRequestValidator()
            {
                RuleFor(request => request.keyValuePairs).NotEmpty().Must(dict => dict.ContainsKey("guid") && dict.ContainsKey("code"));
            }
        }
    }
}
