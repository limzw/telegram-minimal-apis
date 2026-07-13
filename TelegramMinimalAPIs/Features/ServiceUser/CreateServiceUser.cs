using FluentValidation;
using TelegramMinimalAPIs.Common;
using TelegramMinimalAPIs.Common.Configuration;
using TelegramMinimalAPIs.Common.Database;
using TelegramMinimalAPIs.Common.Services.RuntimeUser;
using TelegramMinimalAPIs.Common.Utilities;
using MediatR;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace TelegramMinimalAPIs.Features.ServiceUser
{
    public class CreateServiceUser
    {
        public record CreateServiceUserRequest(Dictionary<string, dynamic> keyValuePairs) : IRequest<CreateServiceUserResponse>;

        public class Endpoint : IEndpoint
        {
            public void MapEndpoint(IEndpointRouteBuilder app)
            {
                app.MapPost("/api/service-user/create", async Task<IResult> (HttpRequest request, ISender mediator) =>
                {
                    var body = new StreamReader(request.Body);
                    string postData = await body.ReadToEndAsync();
                    Dictionary<string, dynamic> keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(postData) ?? new Dictionary<string, dynamic>();
                    var response = await mediator.Send(new CreateServiceUserRequest(keyValuePairs));
                    return response.result;
                });
            }
        }
        public record CreateServiceUserResponse(IResult result);

        public class CreateServiceUserRequestHandler : IRequestHandler<CreateServiceUserRequest, CreateServiceUserResponse>
        {
            private readonly AppDbContext _appDbContext;
            private readonly RuntimeUserRegistry _runtimeUserRegistry;
            private readonly ILogger<CreateServiceUserRequestHandler> _logger;
            private readonly TelegramSettings _telegramSettings;

            public CreateServiceUserRequestHandler(AppDbContext appDbContext, IConfiguration configuration, ILogger<CreateServiceUserRequestHandler> logger, RuntimeUserRegistry runtimeUserRegistry, IOptions<TelegramSettings> telegramSettings)
            {
                _appDbContext = appDbContext;
                _runtimeUserRegistry = runtimeUserRegistry;
                _logger = logger;
                _telegramSettings = telegramSettings.Value;
            }

            public async Task<CreateServiceUserResponse> Handle(CreateServiceUserRequest request, CancellationToken ct)
            {
                string phoneNumber = HttpHelper.GetObjectValue(request.keyValuePairs["phoneNumber"]);

                (string? guid, RuntimeUser? runtimeUser) = _runtimeUserRegistry.GetUsingPhoneNumber(phoneNumber);

                if (runtimeUser == null) //if no runtime user, means its a new login
                {
                    string guidStr = Guid.NewGuid().ToString();
                    string userDbPath = Path.Combine(_telegramSettings.DatabasePath, guidStr);

                    //try to initialise RuntimeUser first
                    RuntimeUserInitialisationParams userInitParams = new RuntimeUserInitialisationParams((int)_telegramSettings.ApiId, _telegramSettings.ApiHash, userDbPath, phoneNumber);

                    //add to runtime list here
                    RuntimeUser? newRuntimeUser = _runtimeUserRegistry.Register(guidStr, userInitParams);
                    if (newRuntimeUser != null)
                    {
                        newRuntimeUser.Initialise();
                        InitialisationStatus status = await newRuntimeUser.WaitForStatusAsync();
                        if (status == InitialisationStatus.PendingAuthorisationCode)
                        {
                            Common.Database.Entities.ServiceUser newServiceUser = new Common.Database.Entities.ServiceUser();
                            newServiceUser.Guid = guidStr;
                            newServiceUser.Path = userDbPath;
                            newServiceUser.UserPhoneNumber = phoneNumber;

                            await _appDbContext.AddAsync(newServiceUser);
                            int saveCount = await _appDbContext.SaveChangesAsync();

                            _logger.LogInformation("Saved app user");
                            return new CreateServiceUserResponse(Results.Ok(new { Guid = guidStr }));
                        }
                        else
                        {
                            string errorMsg = newRuntimeUser.ErrorMessage;
                            _runtimeUserRegistry.Remove(guidStr);
                            return new CreateServiceUserResponse(Results.BadRequest(new { Error = errorMsg }));
                        }
                    }
                }
                else
                {
                    return new CreateServiceUserResponse(Results.Ok(new { Guid = guid, Exists = true }));
                }

                return new CreateServiceUserResponse(Results.BadRequest(new { Error = "Unknown Error" }));
            }
        }

        public class CreateServiceUserRequestValidator : AbstractValidator<CreateServiceUserRequest>
        {
            public CreateServiceUserRequestValidator()
            {
                RuleFor(request => request.keyValuePairs).NotEmpty().Must(dict => dict.ContainsKey("phoneNumber"));
            }
        }
    }
}
