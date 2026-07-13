using MediatR;
using System.Diagnostics;

namespace FlutterBackendCSharp.Common.Behaviours
{
    public class LoggingBehaviour<TRequest, TReponse> : IPipelineBehavior<TRequest, TReponse>
    {
        private readonly ILogger<LoggingBehaviour<TRequest, TReponse>> _logger;
        public LoggingBehaviour(ILogger<LoggingBehaviour<TRequest, TReponse>> logger)
        {
            _logger = logger;
        }
        public async Task<TReponse> Handle(TRequest request, RequestHandlerDelegate<TReponse> next, CancellationToken cancellationToken)
        {
            var name = typeof(TRequest).Name;
            var sw = Stopwatch.StartNew();

            _logger.LogInformation("Handling {RequestName}", name);

            var response = await next(); // continues down the pipeline

            _logger.LogInformation("Handled {RequestName} in {Ms}ms", name, sw.ElapsedMilliseconds);

            return response;
        }
    }
}
