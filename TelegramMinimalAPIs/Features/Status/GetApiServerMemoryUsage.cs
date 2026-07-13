using TelegramMinimalAPIs.Common;
using MediatR;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TelegramMinimalAPIs.Features.Status
{
    public class GetApiServerMemoryUsage
    {
        public record GetApiServerMemoryUsageRequest : IRequest<GetApiServerMemoryUsageResponse>;

        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapGet("/api/status/memory-usage", async Task<IResult> (ISender mediator) =>
                {
                    var response = await mediator.Send(new GetApiServerMemoryUsageRequest());
                    return response.result;
                }).RequireAuthorization();
            }
        }

        public record GetApiServerMemoryUsageResponse(IResult result);

        public class GetApiServerMemoryUsageRequestHandler : IRequestHandler<GetApiServerMemoryUsageRequest, GetApiServerMemoryUsageResponse>
        {
            public Task<GetApiServerMemoryUsageResponse> Handle(GetApiServerMemoryUsageRequest request, CancellationToken cancellationToken)
            {
                long memoryUsedInMb = 0;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    PerformanceCounter _performanceCounter = new PerformanceCounter("Process", "Working Set - Private", Process.GetCurrentProcess().ProcessName);
                    memoryUsedInMb = (long)(_performanceCounter.NextValue() / (1024 * 1024));
                }
                else
                {
                    using Process process = Process.GetCurrentProcess();
                    memoryUsedInMb = process.WorkingSet64 / (1024 * 1024);
                }

                return Task.FromResult(new GetApiServerMemoryUsageResponse(Results.Ok(new { MemoryUsage = memoryUsedInMb })));
            }
        }
    }
}
