using FlutterBackendCSharp.Common;
using MediatR;
using System.Security.Claims;

namespace FlutterBackendCSharp.Features.WebUser
{
    public class GetMainUser
    {
        public record GetMainUserRequest : IRequest<GetMainUserResponse>;

        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapGet("/api/main-user/me", (HttpContext context) =>
                {
                    var username = context.User.Claims.Where(claim => claim.Type == ClaimTypes.Name).First().Value;
                    var role = context.User.Claims.Where(claim => claim.Type == ClaimTypes.Role).First().Value;

                    return Results.Ok(new { username, role });
                }).RequireAuthorization();
            }
        }

        public record GetMainUserResponse();
    }
}
