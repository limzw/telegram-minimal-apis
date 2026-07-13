using FlutterBackendCSharp.Common;
using FlutterBackendCSharp.Common.Database;
using FlutterBackendCSharp.Common.Services.RuntimeUser;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FlutterBackendCSharp.Features.ServiceUser
{
    public class GetOnlineServiceUsers
    {
        public record GetOnlineServiceUsersRequest : IRequest<GetOnlineServiceUsersResponse>;

        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapGet("/api/service-user/online", async Task<IResult> (ISender mediator) =>
                {
                    var response = await mediator.Send(new GetOnlineServiceUsersRequest());
                    return Results.Ok(new { Count = response.onlineUsersCount, OnlineUsers = response.onlineUsersJsonStr });
                }).RequireAuthorization();
            }
        }

        public record GetOnlineServiceUsersResponse(int onlineUsersCount, string onlineUsersJsonStr);

        public class GetOnlineServiceUsersHandler : IRequestHandler<GetOnlineServiceUsersRequest, GetOnlineServiceUsersResponse>
        {
            RuntimeUserRegistry _runtimeUserRegistry;
            AppDbContext _appDbContext;
            public GetOnlineServiceUsersHandler(RuntimeUserRegistry runtimeUserRegistry, AppDbContext appDbContext)
            {
                _runtimeUserRegistry = runtimeUserRegistry;
                _appDbContext = appDbContext;
            }
            public async Task<GetOnlineServiceUsersResponse> Handle(GetOnlineServiceUsersRequest request, CancellationToken ct)
            {
                int clientCount = _runtimeUserRegistry.GetActiveRuntimeUsers();

                var onlineUsers = await _appDbContext.ServiceUsers.ToListAsync();

                List<FormattedServiceUser> formattedOnlineUsers = onlineUsers.Select(user => new FormattedServiceUser(user.Id, user.Guid, user.IsActive, user.IsAuthenticated)).ToList();

                string onlineUsersJsonStr = JsonSerializer.Serialize(formattedOnlineUsers);
                return new GetOnlineServiceUsersResponse(clientCount, onlineUsersJsonStr);
            }
        }
    }

    public class FormattedServiceUser
    {
        public int Id { get; set; }
        public string Guid { get; set; }
        public bool IsActive { get; set; }
        public bool IsAuthenticated { get; set; }

        public FormattedServiceUser(int id, string guid, bool isActive, bool isAuthenticated)
        {
            Id = id;
            Guid = guid;
            IsActive = isActive;
            IsAuthenticated = isAuthenticated;
        }
    }
}
