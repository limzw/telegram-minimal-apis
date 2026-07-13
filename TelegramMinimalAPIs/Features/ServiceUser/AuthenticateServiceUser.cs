using FluentValidation;
using FlutterBackendCSharp.Common;
using FlutterBackendCSharp.Common.Database;
using FlutterBackendCSharp.Common.Services.RuntimeUser;
using FlutterBackendCSharp.Common.Utilities;
using MediatR;
using System.Text.Json;

namespace FlutterBackendCSharp.Features.ServiceUser
{
    public class AuthenticateServiceUser
    {
        public record AuthenticateServiceUserRequest(Dictionary<string, dynamic> keyValuePairs) : IRequest<AuthenticateServiceUserResponse>;

        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapPost("/api/service-user/authenticate", async (HttpRequest request, ISender mediator) =>
                {
                    var body = new StreamReader(request.Body);
                    string postData = await body.ReadToEndAsync();
                    Dictionary<string, dynamic> keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(postData) ?? new Dictionary<string, dynamic>();
                    var response = await mediator.Send(new AuthenticateServiceUserRequest(keyValuePairs));
                    return response.result;
                });
            }
        }

        public record AuthenticateServiceUserResponse(IResult result);

        public class AuthenticateServiceUserRequestHandler : IRequestHandler<AuthenticateServiceUserRequest, AuthenticateServiceUserResponse>
        {
            private readonly AppDbContext _appDbContext;
            private readonly RuntimeUserRegistry _runtimeUserRegistry;

            public AuthenticateServiceUserRequestHandler(AppDbContext appDbContext, RuntimeUserRegistry runtimeUserRegistry)
            {
                _appDbContext = appDbContext;
                _runtimeUserRegistry = runtimeUserRegistry;
            }

            public async Task<AuthenticateServiceUserResponse> Handle(AuthenticateServiceUserRequest request, CancellationToken ct)
            {
                string guid = HttpHelper.GetObjectValue(request.keyValuePairs["guid"]);
                string code = HttpHelper.GetObjectValue(request.keyValuePairs["code"]);

                RuntimeUser? runtimeUser = _runtimeUserRegistry.Get(guid);

                if (runtimeUser != null)
                {
                    runtimeUser.AuthenticateCode(code);
                    InitialisationStatus status = await runtimeUser.WaitForStatusAsync();
                    if (status == InitialisationStatus.Ready)
                    {
                        Common.Database.Entities.ServiceUser appUser = _appDbContext.ServiceUsers.First(user => user.Guid == guid);
                        appUser.UserId = runtimeUser.UserId.ToString();
                        appUser.IsActive = true;
                        appUser.IsAuthenticated = true;

                        await _appDbContext.SaveChangesAsync();
                        return new AuthenticateServiceUserResponse(Results.Ok(new { PendingTwoFactorAuth = false }));
                    }
                    else if (status == InitialisationStatus.Pending2FAVerificationCode)
                    {
                        return new AuthenticateServiceUserResponse(Results.Ok(new { PendingTwoFactorAuth = true }));
                    }
                    else
                    {
                        return new AuthenticateServiceUserResponse(Results.BadRequest(new { Error = runtimeUser.ErrorMessage }));
                    }
                }

                return new AuthenticateServiceUserResponse(Results.BadRequest(new { Error = "Unknown Error" }));
            }
        }

        public class AuthenticateServiceUserRequestValidator : AbstractValidator<AuthenticateServiceUserRequest>
        {
            public AuthenticateServiceUserRequestValidator()
            {
                RuleFor(request => request.keyValuePairs).NotEmpty().Must(dict => dict.ContainsKey("guid") && dict.ContainsKey("code"));
            }
        }
    }
}
