using MediatR;
using Microsoft.EntityFrameworkCore;
using TelegramMinimalAPIs.Common;
using TelegramMinimalAPIs.Common.Database;

namespace TelegramMinimalAPIs.Features.Logs
{
    public class DeleteLogs
    {
        public record DeleteLogsRequest(string type, List<int> ids) : IRequest<DeleteLogsResponse>;

        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapPost("/api/logs", async Task<IResult> (ISender mediator, DeleteLogsRequest request) =>
                {
                    var response = await mediator.Send(request);
                    return response.result;
                });
            }
        }
        public record DeleteLogsResponse(IResult result);

        public class DeleteApiLogsRequestHandler : IRequestHandler<DeleteLogsRequest, DeleteLogsResponse>
        {
            private readonly AppDbContext _appDbContext;
            public DeleteApiLogsRequestHandler(AppDbContext appDbContext)
            {
                _appDbContext = appDbContext;
            }

            public async Task<DeleteLogsResponse> Handle(DeleteLogsRequest request, CancellationToken ct)
            {
                int deleteCount = 0;
                switch (request.type)
                {
                    case "api":
                        deleteCount = await _appDbContext.ApiLogs.Where(log => request.ids.Contains(log.Id)).ExecuteDeleteAsync();
                        break;

                    case "system":
                        deleteCount = await _appDbContext.OverviewLogs.Where(log => request.ids.Contains(log.Id)).ExecuteDeleteAsync();
                        break;
                }

                return new DeleteLogsResponse(deleteCount == request.ids.Count ? Results.Ok() : Results.BadRequest());
            }
        }
    }
}
