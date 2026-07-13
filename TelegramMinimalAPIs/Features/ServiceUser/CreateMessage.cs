using FluentValidation;
using TelegramMinimalAPIs.Common;
using TelegramMinimalAPIs.Common.Services.RuntimeUser;
using TelegramMinimalAPIs.Common.Utilities;
using MediatR;
using System.Text.Json;

namespace TelegramMinimalAPIs.Features.ServiceUser
{
    public class CreateMessage
    {
        public record CreateMessageRequest(Dictionary<string, dynamic> keyValuePairs) : IRequest<CreateMessageResponse>;

        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapPost("/api/service-user/send-message", async (HttpRequest request, ISender mediator) =>
                {
                    var body = new StreamReader(request.Body);
                    string postData = await body.ReadToEndAsync();
                    Dictionary<string, dynamic> keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(postData) ?? new Dictionary<string, dynamic>();
                    var response = await mediator.Send(new CreateMessageRequest(keyValuePairs));
                    return response.result;
                });
            }
        }

        public record CreateMessageResponse(IResult result);

        public class CreateMessageRequestHandler : IRequestHandler<CreateMessageRequest, CreateMessageResponse>
        {
            private RuntimeUserRegistry _runtimeUserRegistry;
            public CreateMessageRequestHandler(RuntimeUserRegistry runtimeUserRegistry)
            {
                _runtimeUserRegistry = runtimeUserRegistry;
            }

            public async Task<CreateMessageResponse> Handle(CreateMessageRequest request, CancellationToken ct)
            {
                string guid = HttpHelper.GetObjectValue(request.keyValuePairs["guid"]);
                string msg = HttpHelper.GetObjectValue(request.keyValuePairs["payerDetails"]);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                RuntimeUser runtimeUser = _runtimeUserRegistry.Get(guid)!;

                List<RecipientInfo>? recipientsInfo = JsonSerializer.Deserialize<List<RecipientInfo>>(msg, options);
                if (runtimeUser != null)
                {
                    List<SendMessageStatus> sendMessageStatuses = await runtimeUser.SendMessageToUsers(recipientsInfo);
                    string jsonString = JsonSerializer.Serialize(sendMessageStatuses);
                    return new CreateMessageResponse(Results.Ok(jsonString));
                }

                return new CreateMessageResponse(Results.BadRequest());
            }
        }

        public class CreateMessageRequestValidator : AbstractValidator<CreateMessageRequest>
        {
            public CreateMessageRequestValidator()
            {
                RuleFor(request => request.keyValuePairs).NotEmpty().Must(dict => dict.ContainsKey("guid") && dict.ContainsKey("payerDetails"));
            }
        }

        public class SendMessageStatus
        {
            public string Contact { get; private set; }
            public bool IsSent { get; private set; }
            public string ErrorMsg { get; private set; }
            public string LastMessageSentAt { get; private set; }
            public SendMessageStatus(string contact, bool isSent, string errorMsg = "", string lastMessageSentAt = "")
            {
                Contact = contact;
                IsSent = isSent;
                ErrorMsg = errorMsg;
                LastMessageSentAt = lastMessageSentAt;
            }
        }

        public class RecipientInfo
        {
            public string Name { get; set; }
            public string Contact { get; set; }
            public string Message { get; set; }
        }
    }
}
