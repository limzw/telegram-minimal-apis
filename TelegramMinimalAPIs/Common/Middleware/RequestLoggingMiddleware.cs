using Serilog.Context;
using System.Diagnostics;
using System.Net;

namespace FlutterBackendCSharp.Common.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(
            RequestDelegate next,
            ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                await _next(context); // call the next middleware
            }
            finally
            {
                stopwatch.Stop();
                int statusCode = context.Response.StatusCode;

                //log down responses that do not return 200 
                if (statusCode != (int)HttpStatusCode.OK)
                {
                    var method = context.Request.Method;
                    var path = context.Request.Path.ToString();
                    var duration = stopwatch.ElapsedMilliseconds;

                    using (LogContext.PushProperty("database", "ApiLogs"))
                    using (LogContext.PushProperty("method", method))
                    using (LogContext.PushProperty("path", path))
                    using (LogContext.PushProperty("status_code", statusCode))
                    using (LogContext.PushProperty("duration", duration))
                    {
                        string errorMsg;
                        switch (statusCode)
                        {
                            case 400:
                                errorMsg = "Bad Request";
                                break;
                            case 401:
                                errorMsg = "Unauthorised";
                                break;
                            case 404:
                                errorMsg = "Not Found";
                                break;
                            default:
                                errorMsg = "Unknown";
                                break;
                        }
                        _logger.LogError(errorMsg);
                    }
                }
            }
        }
    }
}
