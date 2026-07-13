using Microsoft.AspNetCore.Diagnostics;

namespace TelegramMinimalAPIs.Common.ExceptionHandlers
{
    public class IdempotencyExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is not IdempotencyException idempotencyException)
            {
                return false;
            }

            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                error = exception.Message
            }, cancellationToken);

            return true;
        }
    }

    public class IdempotencyException : Exception
    {
        public IdempotencyException(string key) : base($"A request with idempotency key '{key}' is already being processed.") { }
    }
}
