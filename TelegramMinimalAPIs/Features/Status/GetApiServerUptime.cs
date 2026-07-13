using TelegramMinimalAPIs.Common;
using TelegramMinimalAPIs.Common.Database;
using MediatR;

namespace TelegramMinimalAPIs.Features.Status
{
    public class GetApiServerUptime
    {
        public record GetApiServerUptimeRequest : IRequest<GetApiServerUptimeResponse>;

        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapGet("/api/status/get-api-server-uptime", async Task<IResult> (ISender mediator) =>
                {
                    var response = await mediator.Send(new GetApiServerUptimeRequest());

                    return response.result;
                });
            }
        }

        public record GetApiServerUptimeResponse(IResult result);

        public class GetApiServerUptimeHandler : IRequestHandler<GetApiServerUptimeRequest, GetApiServerUptimeResponse>
        {
            AppDbContext _appDbContext;
            public GetApiServerUptimeHandler(AppDbContext appDbContext)
            {
                _appDbContext = appDbContext;
            }

            public async Task<GetApiServerUptimeResponse> Handle(GetApiServerUptimeRequest request, CancellationToken ct)
            {
                var appUptime = _appDbContext.AppUptime.OrderByDescending(uptime => uptime.Id).FirstOrDefault();
                return new GetApiServerUptimeResponse(appUptime == null ? Results.BadRequest() : Results.Ok(new { Uptime = appUptime.TotalRuntime.TotalSeconds }));
            }
        }
    }
}
