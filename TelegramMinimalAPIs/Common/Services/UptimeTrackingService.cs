using System.Diagnostics;
using TelegramMinimalAPIs.Common.Database;
using TelegramMinimalAPIs.Common.Database.Entities;

namespace TelegramMinimalAPIs.Common.Services
{
    public class UptimeTrackingService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly DateTime _startTime;
        private bool _isFirstWrite = true;
        private int _setUptimeInterval = 5;
        public UptimeTrackingService(IServiceScopeFactory scopeFactory)
        {
            _startTime = Process.GetCurrentProcess().StartTime.ToUniversalTime();
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await UpdateUptime();
                await Task.Delay(TimeSpan.FromSeconds(_setUptimeInterval), stoppingToken); // update every 30 seconds
            }
        }

        private async Task UpdateUptime()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (_isFirstWrite)
            {
                db.AppUptime.Add(new AppUptime
                {
                    StartTime = _startTime,
                    LastUpdated = DateTime.UtcNow,
                    TotalRuntime = DateTime.UtcNow - _startTime
                });

                _isFirstWrite = false;
            }
            else
            {
                var uptime = db.AppUptime.OrderByDescending(uptime => uptime.Id).First();
                uptime.LastUpdated = DateTime.UtcNow;
                uptime.TotalRuntime = DateTime.UtcNow - _startTime;
            }


            await db.SaveChangesAsync();
        }
    }
}
