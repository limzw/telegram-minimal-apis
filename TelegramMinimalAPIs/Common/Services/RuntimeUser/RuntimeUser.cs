using TelegramMinimalAPIs.Common.Services.Loggers;
using TdLib;
using TdLib.Bindings;
using static TelegramMinimalAPIs.Features.ServiceUser.CreateMessage;
using static TdLib.TdApi;
using static TdLib.TdApi.InputMessageContent;

namespace TelegramMinimalAPIs.Common.Services.RuntimeUser
{
    public class RuntimeUser : IDisposable
    {
        private TdClient _myTdClient;
        private readonly CustomLoggerWrapper _runtimeUserLogger;

        static int _apiId;
        static string _apiHash = string.Empty;

        string _databasePath = string.Empty;
        string _phoneNumber = string.Empty;
        public string PhoneNumber => _phoneNumber;
        long _userId;
        public long UserId => _userId;

        public InitialisationStatus InitialisationStatus { get; private set; }
        public string ErrorMessage { get; private set; }

        private TaskCompletionSource<InitialisationStatus>? _pendingStatus;

        public RuntimeUser(RuntimeUserInitialisationParams runtimeUserInitParams, CustomLoggerWrapper runtimeUserLogger)
        {
            _runtimeUserLogger = runtimeUserLogger;

            InitialisationStatus = InitialisationStatus.PendingPhoneNumber;

            _apiId = runtimeUserInitParams.ApiId;
            _apiHash = runtimeUserInitParams.ApiHash;
            _databasePath = runtimeUserInitParams.DatabasePath;
            _phoneNumber = runtimeUserInitParams.PhoneNumber;
        }

        public void Initialise()
        {
            _myTdClient = new TdClient();
            _myTdClient.Bindings.SetLogVerbosityLevel(TdLogLevel.Warning);
            _myTdClient.UpdateReceived += OnUpdateReceived;
        }

        private async void OnUpdateReceived(object? sender, Update update)
        {
            try
            {
                await Task.Run(() => HandleUpdate(update));
            }
            catch (TdException ex)
            {
                ErrorMessage = ex.Message;

                if (ex.Message != "PHONE_CODE_INVALID") //invalid phone number
                {
                    _pendingStatus?.SetResult(InitialisationStatus);
                    _runtimeUserLogger.LogServiceUserActivity(LogLevel.Error, PhoneNumber, ErrorMessage);
                }
            }
        }

        private async Task HandleUpdate(Update update)
        {
            switch (update)
            {
                case Update.UpdateAuthorizationState { AuthorizationState: AuthorizationState.AuthorizationStateWaitTdlibParameters }:
                    await _myTdClient.ExecuteAsync(new SetTdlibParameters
                    {
                        ApiId = _apiId, // Get from my.telegram.org
                        ApiHash = _apiHash, // Get from my.telegram.org
                        UseTestDc = false,
                        DatabaseDirectory = _databasePath,
                        UseFileDatabase = true,
                        UseMessageDatabase = true,
                        UseSecretChats = true,
                        SystemLanguageCode = "en",
                        DeviceModel = "PC",
                        ApplicationVersion = "1.0",
                    });
                    break;

                case Update.UpdateAuthorizationState { AuthorizationState: AuthorizationState.AuthorizationStateWaitPhoneNumber }:
                    await _myTdClient.SetAuthenticationPhoneNumberAsync(_phoneNumber);
                    break;

                case Update.UpdateAuthorizationState { AuthorizationState: AuthorizationState.AuthorizationStateWaitCode }:
                    _pendingStatus?.SetResult(InitialisationStatus.PendingAuthorisationCode);
                    InitialisationStatus = InitialisationStatus.PendingAuthorisationCode;
                    break;

                case Update.UpdateAuthorizationState { AuthorizationState: AuthorizationState.AuthorizationStateReady }:
                    if (_userId == 0)
                    {
                        User newUser = await _myTdClient.ExecuteAsync(new GetMe());
                        _userId = newUser.Id;
                    }
                    InitialisationStatus = InitialisationStatus.Ready;
                    _pendingStatus?.SetResult(InitialisationStatus.Ready);
                    break;

                case Update.UpdateAuthorizationState { AuthorizationState: AuthorizationState.AuthorizationStateWaitPassword }:
                    InitialisationStatus = InitialisationStatus.Pending2FAVerificationCode;
                    _pendingStatus?.SetResult(InitialisationStatus.Pending2FAVerificationCode);
                    break;

                case Update.UpdateAuthorizationState { AuthorizationState: AuthorizationState.AuthorizationStateClosed }:
                    _myTdClient.Dispose();
                    _pendingStatus?.SetResult(InitialisationStatus.PendingPhoneNumber);
                    InitialisationStatus = InitialisationStatus.PendingPhoneNumber;
                    break;
            }
        }

        public async Task<InitialisationStatus> WaitForStatusAsync()
        {
            _pendingStatus = new TaskCompletionSource<InitialisationStatus>();

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            using var registration = cts.Token.Register(() => _pendingStatus.TrySetCanceled());

            try
            {
                return await _pendingStatus.Task;
            }
            catch (Exception ex)
            {
                _runtimeUserLogger.LogServiceUserActivity(LogLevel.Error, PhoneNumber, $"{Enum.GetName(typeof(InitialisationStatus), InitialisationStatus)}: {ex.Message}");
                return InitialisationStatus;
            }
            finally
            {
                _pendingStatus = null;
            }
        }

        public async void AuthenticateCode(string code)
        {
            try
            {
                await _myTdClient.CheckAuthenticationCodeAsync(code);
            }
            catch (TdException ex)
            {
                ErrorMessage = ex.Message;
                _runtimeUserLogger.LogServiceUserActivity(LogLevel.Error, PhoneNumber, $"{nameof(AuthenticateCode)}: {ex.Message}");
                _pendingStatus?.SetResult(InitialisationStatus);
            }
        }

        public async void AuthenticateTwoFactorAuthCode(string code)
        {
            try
            {
                await _myTdClient.CheckAuthenticationPasswordAsync(code);
            }
            catch (TdException ex)
            {
                ErrorMessage = ex.Message;
                _runtimeUserLogger.LogServiceUserActivity(LogLevel.Error, PhoneNumber, $"{nameof(AuthenticateTwoFactorAuthCode)}: {ex.Message}");
                _pendingStatus?.SetResult(InitialisationStatus);
            }
        }

        public async Task<List<SendMessageStatus>> SendMessageToUsers(List<RecipientInfo> recipientsInfos)
        {
            List<SendMessageStatus> sendMessageStatuses = new List<SendMessageStatus>();
            if (recipientsInfos.Count > 0)
            {
                List<long> userIds = new List<long>();
                foreach (var recipientInfo in recipientsInfos)
                {
                    //remove any empty spaces in contact number
                    string modContact = recipientInfo.Contact.Replace(" ", string.Empty);
                    User? searchedUser = null;

                    try
                    {
                        searchedUser = await _myTdClient!.ExecuteAsync(new SearchUserByPhoneNumber
                        {
                            PhoneNumber = modContact,
                        });
                    }
                    catch (TdException)
                    {
                        sendMessageStatuses.Add(new SendMessageStatus(modContact, false, "cannot find user on telegram"));
                        continue;
                    }

                    Chat? existingChat = null;
                    userIds.Add(searchedUser.Id);
                    try
                    {
                        existingChat = await _myTdClient!.ExecuteAsync(new GetChat() { ChatId = searchedUser.Id });

                    }
                    catch (Exception ex)
                    {
                        //GetChat throws an exception if no chat found so catch here to initialise a chat
                        existingChat = await _myTdClient!.ExecuteAsync(new CreatePrivateChat() { UserId = searchedUser.Id });
                    }

                    if (existingChat != null)
                    {
                        Message sendMessageStatus = await _myTdClient.ExecuteAsync(new SendMessage()
                        {
                            ChatId = searchedUser.Id,
                            InputMessageContent = new InputMessageText()
                            {
                                Text = new FormattedText { Text = $"{recipientInfo.Name} : {recipientInfo.Message}" }
                            }
                        });

                        if (sendMessageStatus != null)
                        {
                            if (sendMessageStatus.SendingState.GetType() == typeof(MessageSendingState.MessageSendingStateFailed))
                            {
                                sendMessageStatuses.Add(new SendMessageStatus(modContact, false, "failed to send message"));
                            }
                            else
                            {
                                sendMessageStatuses.Add(new SendMessageStatus(modContact, true, lastMessageSentAt: DateTime.UtcNow.ToString("o")));
                            }
                        }
                    }
                }
            }

            return sendMessageStatuses;
        }

        public async Task<bool> SendMessageToSelf(string message)
        {
            User? self = null;
            try
            {
                self = await _myTdClient!.ExecuteAsync(new SearchUserByPhoneNumber
                {
                    PhoneNumber = _phoneNumber,
                });
            }
            catch (TdException ex)
            {
                _runtimeUserLogger.LogServiceUserActivity(LogLevel.Error, PhoneNumber, $"{nameof(SendMessageToSelf)}: {ex.Message}");
            }

            Chat? existingChat = null;

            try
            {
                existingChat = await _myTdClient!.ExecuteAsync(new GetChat() { ChatId = self.Id });

            }
            catch (Exception ex)
            {
                //GetChat throws an exception if no chat found so catch here to initialise a chat
                existingChat = await _myTdClient!.ExecuteAsync(new CreatePrivateChat() { UserId = self.Id });
            }

            if (existingChat != null)
            {
                Message sendMessageStatus = await _myTdClient.ExecuteAsync(new SendMessage()
                {
                    ChatId = self.Id,
                    InputMessageContent = new InputMessageText()
                    {
                        Text = new FormattedText { Text = message }
                    }
                });

                if (sendMessageStatus != null)
                {
                    if (sendMessageStatus.SendingState.GetType() != typeof(MessageSendingState.MessageSendingStateFailed))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public async Task<bool> GetUserActiveSessions()
        {
            try
            {
                Sessions sessions = await _myTdClient.ExecuteAsync(new GetActiveSessions());
                foreach (Session session in sessions.Sessions_)
                {
                    Console.WriteLine($"{session.ApplicationName}: {session.IsCurrent}");

                }
                return true;
            }
            catch (TdException ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async void LogoutUser()
        {
            await _myTdClient.ExecuteAsync(new LogOut());

        }

        public void Dispose()
        {
            _myTdClient.UpdateReceived -= OnUpdateReceived;
        }
    }
}
