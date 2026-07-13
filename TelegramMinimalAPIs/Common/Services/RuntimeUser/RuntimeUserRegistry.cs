using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using TelegramMinimalAPIs.Common.Configuration;
using TelegramMinimalAPIs.Common.Database;
using TelegramMinimalAPIs.Common.Database.Entities;
using TelegramMinimalAPIs.Common.Services.Loggers;

namespace TelegramMinimalAPIs.Common.Services.RuntimeUser
{
    public class RuntimeUserRegistry : IDisposable
    {
        private readonly ConcurrentDictionary<string, RuntimeUser> _runtimes = new ConcurrentDictionary<string, RuntimeUser>();
        private readonly TelegramSettings _telegramSettings;
        private readonly CustomLoggerWrapper _runtimeUserLogger;

        public RuntimeUserRegistry(IOptions<TelegramSettings> telegramSettings, CustomLoggerWrapper runtimeUserLogger)
        {
            _telegramSettings = telegramSettings.Value;
            _runtimeUserLogger = runtimeUserLogger;
        }

        public async Task InitialiseAllActiveUsers(AppDbContext appDbContext)
        {
            try
            {
                List<ServiceUser> serviceUsers = appDbContext.ServiceUsers.Where(user => user.IsActive || !user.IsAuthenticated).ToList();
                foreach (ServiceUser user in serviceUsers)
                {
                    RuntimeUserInitialisationParams runtimeUserInitialisationParams = new RuntimeUserInitialisationParams((int)_telegramSettings.ApiId,
                                                                                                    _telegramSettings.ApiHash, user.Path,
                                                                                                     user.UserPhoneNumber);
                    RuntimeUser currUser = Register(user.Guid, runtimeUserInitialisationParams)!;
                    currUser.Initialise();
                    InitialisationStatus status = await currUser.WaitForStatusAsync();
                }
            }
            catch (Exception ex)
            {
                _runtimeUserLogger.Log(Loggers.Database.OVERVIEWLOGS, LogLevel.Error, $"{nameof(InitialiseAllActiveUsers)} : {ex.Message}");
            }
        }

        public RuntimeUser? Register(string guidStr, RuntimeUserInitialisationParams runtimeUserInitParams)
        {
            return _runtimes.GetOrAdd(guidStr, new RuntimeUser(runtimeUserInitParams, _runtimeUserLogger));
        }

        public (string? guid, RuntimeUser?) GetUsingPhoneNumber(string phoneNumber)
        {
            var runtimeKvp = _runtimes.FirstOrDefault(kvp => kvp.Value.PhoneNumber == phoneNumber);
            if (runtimeKvp.Value != null)
            {
                return (runtimeKvp.Key, runtimeKvp.Value);
            }

            return (null, null);
        }

        public RuntimeUser? Get(string guid)
        {
            return _runtimes.TryGetValue(guid, out var runtimeUser) ? runtimeUser : null;
        }

        public void Remove(string guid)
        {
            var removeUser = _runtimes.FirstOrDefault(user => user.Key == guid);
            if (!removeUser.Equals(default(KeyValuePair<string, RuntimeUser>)))
            {
                removeUser.Value.Dispose();
                _runtimes.Remove(guid, out var removedUser);
            }
        }

        public int GetActiveRuntimeUsers()
        {
            return _runtimes.Values.Count;
        }

        public void Dispose()
        {

        }
    }
}
