namespace FlutterBackendCSharp.Common.Services.RuntimeUser
{
    public class RuntimeUserInitialisationParams
    {
        public int ApiId { get; private set; }
        public string ApiHash { get; private set; }
        public string DatabasePath { get; private set; }
        public string PhoneNumber { get; private set; }
        public RuntimeUserInitialisationParams(int apiId, string apiHash, string databasePath, string phoneNumber = "")
        {
            ApiId = apiId;
            ApiHash = apiHash;
            DatabasePath = databasePath;
            PhoneNumber = phoneNumber;
        }
    }
}
