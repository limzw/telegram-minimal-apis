using FluentValidation;
using MediatR;
using System.Text.Json;
using TelegramMinimalAPIs.Common;
using TelegramMinimalAPIs.Common.Database;
using TelegramMinimalAPIs.Common.Services.RuntimeUser;
using TelegramMinimalAPIs.Common.Utilities;

namespace TelegramMinimalAPIs.Features.ServiceUser
{
    public class LogoutServiceUser
    {
        public record LogoutServiceUserRequest(Dictionary<string, dynamic> keyValuePairs) : IRequest<LogoutServiceUserResponse>;

        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapPost("/api/service-user/logout", async (HttpRequest request, ISender mediator) =>
                {
                    var body = new StreamReader(request.Body);
                    string postData = await body.ReadToEndAsync();
                    Dictionary<string, dynamic> keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(postData) ?? new Dictionary<string, dynamic>();
                    var response = await mediator.Send(new LogoutServiceUserRequest(keyValuePairs));
                    return response.result;
                });
            }
        }
        public record LogoutServiceUserResponse(IResult result);

        public class LogoutServiceUserRequestHandler : IRequestHandler<LogoutServiceUserRequest, LogoutServiceUserResponse>
        {
            private AppDbContext _appDbContext;
            private RuntimeUserRegistry _runtimeUserRegistry;
            public LogoutServiceUserRequestHandler(RuntimeUserRegistry runtimeUserRegistry, AppDbContext appDbContext)
            {
                _runtimeUserRegistry = runtimeUserRegistry;
                _appDbContext = appDbContext;
            }

            public async Task<LogoutServiceUserResponse> Handle(LogoutServiceUserRequest request, CancellationToken ct)
            {
                string guid = HttpHelper.GetObjectValue(request.keyValuePairs["guid"]);

                RuntimeUser runtimeUser = _runtimeUserRegistry.Get(guid)!;
                runtimeUser.LogoutUser();
                InitialisationStatus status = await runtimeUser.WaitForStatusAsync();
                if (status == InitialisationStatus.PendingPhoneNumber)
                {
                    Common.Database.Entities.ServiceUser appUser = _appDbContext.ServiceUsers.First(user => user.Guid == guid);
                    _appDbContext.ServiceUsers.Remove(appUser);

                    await _appDbContext.SaveChangesAsync();

                    _runtimeUserRegistry.Remove(guid);
                    return new LogoutServiceUserResponse(Results.Ok());
                }

                return new LogoutServiceUserResponse(Results.BadRequest());
            }
        }

        public class LogoutServiceUserRequestValidator : AbstractValidator<LogoutServiceUserRequest>
        {
            public LogoutServiceUserRequestValidator()
            {
                RuleFor(request => request.keyValuePairs).NotEmpty().Must(dict => dict.ContainsKey("guid"));
            }
        }
    }
}
