using MediatR;
using System.Text.Json;
using TelegramMinimalAPIs.Common;
using TelegramMinimalAPIs.Common.Database;
using TelegramMinimalAPIs.Common.Database.Entities;

namespace TelegramMinimalAPIs.Features.Logs
{
    public class GetLogs
    {
        public record GetApiLogsRequest(Dictionary<string, string> filters) : IRequest<GetLogsResponse<ApiLog>>;
        public record GetSystemLogsRequest(Dictionary<string, string> filters) : IRequest<GetLogsResponse<OverviewLog>>;

        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapGet("/api/logs", async Task<IResult> (HttpContext context, ISender mediator) =>
                {
                    var queryParams = context.Request.Query.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());
                    if (queryParams.ContainsKey("type"))
                    {
                        var otherParams = queryParams.Where(param => param.Key != "type").ToDictionary();
                        return queryParams["type"] switch
                        {
                            "system" => Results.Ok(
                                (await mediator.Send(new GetSystemLogsRequest(otherParams))).LogJsonStr
                            ),
                            "api" => Results.Ok(
                                (await mediator.Send(new GetApiLogsRequest(otherParams))).LogJsonStr
                            ),
                            _ => Results.BadRequest($"unknown log type: {queryParams["type"]}")
                        };
                    }

                    return Results.BadRequest();

                });
            }
        }

        public class GetApiLogsRequestHandler : IRequestHandler<GetApiLogsRequest, GetLogsResponse<ApiLog>>
        {
            private readonly AppDbContext _appDbContext;
            public GetApiLogsRequestHandler(AppDbContext appDbContext)
            {
                _appDbContext = appDbContext;
            }

            public Task<GetLogsResponse<ApiLog>> Handle(GetApiLogsRequest request, CancellationToken ct)
            {
                EndpointLogsFilter filter = DeserializeIntoObj<EndpointLogsFilter>(request.filters);
                var query = _appDbContext.ApiLogs.AsQueryable();

                if (filter.StartDate.HasValue)
                {
                    query = query.Where(log => log.Timestamp >= filter.StartDate);
                }

                if (filter.EndDate.HasValue)
                {
                    query = query.Where(log => log.Timestamp < filter.EndDate.Value.AddDays(1));
                }

                var endpointLogs = query.OrderBy(x => x.Timestamp).ToList();
                return Task.FromResult(GetLogsResponse<ApiLog>.From(endpointLogs));
            }
        }

        public class GetSystemLogsRequestHandler : IRequestHandler<GetSystemLogsRequest, GetLogsResponse<OverviewLog>>
        {
            private readonly AppDbContext _appDbContext;
            public GetSystemLogsRequestHandler(AppDbContext appDbContext)
            {
                _appDbContext = appDbContext;
            }

            public Task<GetLogsResponse<OverviewLog>> Handle(GetSystemLogsRequest request, CancellationToken ct)
            {
                OverviewLogsFilter filter = DeserializeIntoObj<OverviewLogsFilter>(request.filters);

                var query = _appDbContext.OverviewLogs.AsQueryable();
                if (filter.StartDate.HasValue)
                {
                    query = query.Where(log => log.Timestamp >= filter.StartDate);
                }

                if (filter.EndDate.HasValue)
                {
                    query = query.Where(log => log.Timestamp < filter.EndDate.Value.AddDays(1));
                }

                if (!string.IsNullOrEmpty(filter.Severity))
                {
                    if (filter.Severity != "all")
                    {
                        query = query.Where(log => log.Severity == filter.Severity.CapitaliseLetterAtIndex(0));
                    }
                }

                var overviewLogs = query.OrderBy(x => x.Timestamp).ToList();
                return Task.FromResult(GetLogsResponse<OverviewLog>.From(overviewLogs));
            }
        }

        public class GetLogsResponse<T>
        {
            public string LogJsonStr { get; set; } = "";
            public static GetLogsResponse<T> From(List<T> logData) => new GetLogsResponse<T>
            {
                LogJsonStr = JsonSerializer.Serialize(logData)
            };
        };

        public static T DeserializeIntoObj<T>(Dictionary<string, string> dictionary)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // ← case sensitive
            };
            string jsonString = JsonSerializer.Serialize(dictionary);
            return JsonSerializer.Deserialize<T>(jsonString, options)!;
        }
    }

    public static class StringExtensions
    {
        public static string CapitaliseLetterAtIndex(this string value, int index)
        {
            if (index >= value.Length || index < 0)
            {
                return value;
            }

            return (index > 0 ? value.Substring(0, index - 1) : "") + char.ToUpper(value[index]) + value.Substring(index + 1);
        }
    }


    public class OverviewLogsFilter
    {
        public string? Severity { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

    public class EndpointLogsFilter
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
