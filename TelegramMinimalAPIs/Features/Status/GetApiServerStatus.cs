using MediatR;
using TelegramMinimalAPIs.Common;

namespace TelegramMinimalAPIs.Features.Status
{
    public class GetApiServerStatus
    {
        public record GetApiServerStatusRequest : IRequest<GetApiServerStatusResponse>;

        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapGet("/api/status/get-api-server-status", async Task<IResult> (ISender mediator) =>
                {
                    var status = await mediator.Send(new GetApiServerStatusRequest());
                    return status.result;
                }).RequireAuthorization();
            }
        }

        public record GetApiServerStatusResponse(IResult result);

        public class GetApiServerStatusRequestHandler : IRequestHandler<GetApiServerStatusRequest, GetApiServerStatusResponse>
        {
            public Task<GetApiServerStatusResponse> Handle(GetApiServerStatusRequest request, CancellationToken ct)
            {
                return Task.FromResult(new GetApiServerStatusResponse(Results.Ok(new { Online = true })));
            }
        }
    }
}
