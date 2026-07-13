namespace FlutterBackendCSharp.Common
{
    public interface IIdempotentRequest
    {
        string IdempotencyKey { get; }
    }
}
