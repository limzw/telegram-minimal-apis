namespace FlutterBackendCSharp.Common.Database.Entities
{
    public class ApiLog
    {
        public int Id { get; set; }
        public string Method { get; set; }
        public string Endpoint { get; set; }
        public int StatusCode { get; set; }
        public int Duration { get; set; }
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
    }
}
