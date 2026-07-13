using FluentValidation;
using TelegramMinimalAPIs.Common;
using TelegramMinimalAPIs.Common.Database;
using TelegramMinimalAPIs.Common.Services.RuntimeUser;
using TelegramMinimalAPIs.Common.Utilities;
using MediatR;
using System.Text.Json;

namespace TelegramMinimalAPIs.Features.ServiceUser
{
    public class GetActiveSessionsServiceUser
    {
        public record GetActiveSessionsServiceUserRequest(Dictionary<string, dynamic> keyValuePairs) : IRequest<GetActiveSessionsServiceUserResponse>;

        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapPost("/api/service-user/get-active-sessions", async (HttpRequest request, ISender mediator) =>
                {
                    var body = new StreamReader(request.Body);
                    string postData = await body.ReadToEndAsync();
                    Dictionary<string, dynamic> keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(postData) ?? new Dictionary<string, dynamic>();
                    var response = await mediator.Send(new GetActiveSessionsServiceUserRequest(keyValuePairs));
                    return response.result;
                });
            }
        }
        public record GetActiveSessionsServiceUserResponse(IResult result);

        public class GetActiveSessionsServiceUserHandler : IRequestHandler<GetActiveSessionsServiceUserRequest, GetActiveSessionsServiceUserResponse>
        {
            private readonly AppDbContext _appDbContext;
            private readonly RuntimeUserRegistry _runtimeUserRegistry;
            private readonly ILogger<GetActiveSessionsServiceUserHandler> _logger;

            public GetActiveSessionsServiceUserHandler(AppDbContext appDbContext, RuntimeUserRegistry runtimeUserRegistry, ILogger<GetActiveSessionsServiceUserHandler> logger)
            {
                _appDbContext = appDbContext;
                _runtimeUserRegistry = runtimeUserRegistry;
                _logger = logger;
            }
            public async Task<GetActiveSessionsServiceUserResponse> Handle(GetActiveSessionsServiceUserRequest request, CancellationToken cancellationToken)
            {
                string guid = HttpHelper.GetObjectValue(request.keyValuePairs["guid"]);
                RuntimeUser? runtimeUser = _runtimeUserRegistry.Get(guid);
                if (runtimeUser != null)
                {
                    await runtimeUser.GetUserActiveSessions();
                    return new GetActiveSessionsServiceUserResponse(Results.Ok());
                }

                return new GetActiveSessionsServiceUserResponse(Results.BadRequest());
            }
        }

        public class GetActiveSessionsServiceUserRequestValidator : AbstractValidator<GetActiveSessionsServiceUserRequest>
        {
            public GetActiveSessionsServiceUserRequestValidator()
            {
                RuleFor(request => request.keyValuePairs).NotEmpty().Must(dict => dict.ContainsKey("guid"));
            }
        }
    }
}
