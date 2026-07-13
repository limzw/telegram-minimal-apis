using FluentValidation.Results;
using Microsoft.AspNetCore.Diagnostics;

namespace FlutterBackendCSharp.Common.ExceptionHandlers
{
    public class ValidationExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                error = "Validation exception caught"
            }, cancellationToken);

            return true;
        }
    }

    public class ValidationException : Exception
    {
        public ValidationException(List<ValidationFailure> failures)
        {
            foreach (var failure in failures)
            {
                Console.WriteLine($"Validation failed for {failure.PropertyName}: {failure.ErrorMessage}");
            }
        }
    }
}
