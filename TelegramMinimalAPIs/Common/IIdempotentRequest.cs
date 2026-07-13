namespace TelegramMinimalAPIs.Common
{
    public interface IIdempotentRequest
    {
        string IdempotencyKey { get; }
    }
}
