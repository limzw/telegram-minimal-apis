using MediatR;
using System.Diagnostics;
using TelegramMinimalAPIs.Common;

namespace TelegramMinimalAPIs.Features.Status
{
    public class GetApiServerVersion
    {
        public record GetApiServerVersionRequest : IRequest<GetApiServerVersionResponse>;

        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapGet("/api/status/version", async Task<IResult> (ISender mediator) =>
                {
                    var response = await mediator.Send(new GetApiServerVersionRequest());
                    return response.result;
                }).RequireAuthorization();
            }
        }

        public record GetApiServerVersionResponse(IResult result);

        public class GetApiServerVersionHandler : IRequestHandler<GetApiServerVersionRequest, GetApiServerVersionResponse>
        {
            public Task<GetApiServerVersionResponse> Handle(GetApiServerVersionRequest request, CancellationToken cancellationToken)
            {
                string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
                FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(path);
                FileInfo fileInfo = new FileInfo(path);

                return Task.FromResult(new GetApiServerVersionResponse(Results.Ok(new { AppName = fileVersionInfo.ProductName, ServerVersion = fileVersionInfo.FileVersion, DateModified = fileInfo.LastWriteTime.ToString("dd-MM-yyyy") })));
            }
        }
    }
}
