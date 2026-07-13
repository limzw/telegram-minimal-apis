namespace FlutterBackendCSharp.Common.Database.Entities
{
    public class WebUser
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public DateTime DateModified { get; set; }
        public int LoginTryCount { get; set; }
        public string Role { get; set; }
    }
}
