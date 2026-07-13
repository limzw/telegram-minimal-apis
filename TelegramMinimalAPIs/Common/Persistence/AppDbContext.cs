using FlutterBackendCSharp.Common.Database.Entities;
using FlutterBackendCSharp.Common.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlutterBackendCSharp.Common.Database
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<ApiLog> ApiLogs { get; set; }
        public DbSet<OverviewLog> OverviewLogs { get; set; }
        public DbSet<AppUptime> AppUptime { get; set; }
        public DbSet<ServiceUser> ServiceUsers { get; set; }
        public DbSet<ServiceUserActivity> ServiceUsersActivity { get; set; }
        public DbSet<WebUser> WebUsers { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<IdempotencyKey> IdempotencyKeys { get; set; }
    }
}
