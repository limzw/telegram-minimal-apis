namespace FlutterBackendCSharp.Common.Database.Entities
{
    public class AppUptime
    {
        public int Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime LastUpdated { get; set; }
        public TimeSpan TotalRuntime { get; set; }
    }
}
