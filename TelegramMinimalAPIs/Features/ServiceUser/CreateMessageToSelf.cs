using FluentValidation;
using TelegramMinimalAPIs.Common;
using TelegramMinimalAPIs.Common.Services.RuntimeUser;
using TelegramMinimalAPIs.Common.Utilities;
using MediatR;
using System.Text.Json;

namespace TelegramMinimalAPIs.Features.ServiceUser
{
    public class CreateMessageToSelf
    {
        public record CreateMessageToSelfRequest(Dictionary<string, dynamic> keyValuePairs) : IRequest<CreateMessageToSelfResponse>;
        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapPost("/api/service-user/send-message-to-self", async (HttpRequest request, ISender mediator) =>
                {
                    var body = new StreamReader(request.Body);
                    string postData = await body.ReadToEndAsync();
                    Dictionary<string, dynamic> keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(postData) ?? new Dictionary<string, dynamic>();
                    var response = await mediator.Send(new CreateMessageToSelfRequest(keyValuePairs));
                    return response.result;
                });
            }
        }

        public record CreateMessageToSelfResponse(IResult result);

        public class CreateMessageToSelfRequestHandler : IRequestHandler<CreateMessageToSelfRequest, CreateMessageToSelfResponse>
        {
            private RuntimeUserRegistry _runtimeUserRegistry;
            public CreateMessageToSelfRequestHandler(RuntimeUserRegistry runtimeUserRegistry)
            {
                _runtimeUserRegistry = runtimeUserRegistry;
            }
            public async Task<CreateMessageToSelfResponse> Handle(CreateMessageToSelfRequest request, CancellationToken cancellationToken)
            {
                string guid = HttpHelper.GetObjectValue(request.keyValuePairs["guid"]);
                string msg = HttpHelper.GetObjectValue(request.keyValuePairs["msg"]);

                RuntimeUser runtimeUser = _runtimeUserRegistry.Get(guid)!;
                if (runtimeUser != null)
                {
                    bool messageStatus = await runtimeUser.SendMessageToSelf(msg);
                    return new CreateMessageToSelfResponse(Results.Ok(messageStatus));
                }

                return new CreateMessageToSelfResponse(Results.BadRequest());
            }
        }

        public class CreateMessageToSelfRequestValidator : AbstractValidator<CreateMessageToSelfRequest>
        {
            public CreateMessageToSelfRequestValidator()
            {
                RuleFor(request => request.keyValuePairs).NotEmpty().Must(dict => dict.ContainsKey("guid") && dict.ContainsKey("msg"));
            }
        }
    }
}
