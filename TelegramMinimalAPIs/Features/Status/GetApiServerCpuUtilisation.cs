using TelegramMinimalAPIs.Common;
using MediatR;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TelegramMinimalAPIs.Features.Status
{
    public class GetApiServerCpuUtilisation
    {
        public record GetApiServerCpuUtilisationRequest : IRequest<GetApiServerCpuUtilisationResponse>;

        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapGet("/api/status/cpu-utilisation", async Task<IResult> (ISender mediator) =>
                {
                    var response = await mediator.Send(new GetApiServerCpuUtilisationRequest());
                    return response.result;
                }).RequireAuthorization();
            }
        }

        public record GetApiServerCpuUtilisationResponse(IResult result);

        public class GetApiServerCpuUtilisationRequestHandler : IRequestHandler<GetApiServerCpuUtilisationRequest, GetApiServerCpuUtilisationResponse>
        {
            public async Task<GetApiServerCpuUtilisationResponse> Handle(GetApiServerCpuUtilisationRequest request, CancellationToken cancellationToken)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var processCpu = new PerformanceCounter("Process",
                                        "% Processor Time",
                                        Process.GetCurrentProcess().ProcessName);

                    processCpu.NextValue(); // Initialize (always returns 0)
                    await Task.Delay(1000);

                    float usage = processCpu.NextValue() / Environment.ProcessorCount;

                    var startTime = Process.GetCurrentProcess().StartTime;
                    TimeSpan runTime = DateTime.Now - startTime;

                    return new GetApiServerCpuUtilisationResponse(Results.Ok(new { CpuUsage = usage, RunTime = (int)runTime.TotalSeconds }));
                }
                else
                {
                    try
                    {
                        Process appProcess = Process.GetCurrentProcess();
                        var startCpu = appProcess.TotalProcessorTime;
                        var startWall = DateTime.UtcNow;

                        await Task.Delay(500);
                        appProcess.Refresh();

                        var endCpu = appProcess.TotalProcessorTime;
                        var endWall = DateTime.UtcNow;

                        var cpuUsedMs = (endCpu - startCpu).TotalMilliseconds;
                        var wallMs = (endWall - startWall).TotalMilliseconds;

                        // Divide by core count to get a 0–100% scale.
                        // Without this, a fully-loaded 4-core process reports 400%.
                        var usage = cpuUsedMs / (Environment.ProcessorCount * wallMs) * 100;

                        var startTime = Process.GetCurrentProcess().StartTime;
                        TimeSpan runTime = DateTime.Now - startTime;
                        return new GetApiServerCpuUtilisationResponse(Results.Ok(new { CpuUsage = usage, RunTime = (int)runTime.TotalSeconds }));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"CPU Utilisation Error: {ex.Message}");
                        return new GetApiServerCpuUtilisationResponse(Results.BadRequest("Not implemented"));
                    }
                }
            }
        }
    }
}
