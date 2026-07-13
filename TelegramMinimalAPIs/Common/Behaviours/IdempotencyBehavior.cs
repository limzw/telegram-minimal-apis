using TelegramMinimalAPIs.Common.Database;
using TelegramMinimalAPIs.Common.Database.Entities;
using TelegramMinimalAPIs.Common.ExceptionHandlers;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace TelegramMinimalAPIs.Common.Behaviours
{
    public class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private AppDbContext _appDbContext;
        public IdempotencyBehavior(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (request is not IIdempotentRequest idempotentRequest)
            {
                return await next();
            }

            var existing = await _appDbContext.IdempotencyKeys.FirstOrDefaultAsync(k => k.Key == idempotentRequest.IdempotencyKey);
            if (existing != null)
            {
                if (existing.Status == "Completed")
                {
                    return JsonSerializer.Deserialize<TResponse>(existing.Response)!;
                }

                throw new IdempotencyException("existing call is in process...");
            }

            IdempotencyKey newIdempKey = new IdempotencyKey();
            newIdempKey.Key = idempotentRequest.IdempotencyKey;
            newIdempKey.DateTimeCreated = DateTime.UtcNow;
            newIdempKey.Status = "InProgress";

            await _appDbContext.IdempotencyKeys.AddAsync(newIdempKey);
            await _appDbContext.SaveChangesAsync();

            var response = await next();

            existing = await _appDbContext.IdempotencyKeys.FirstAsync(k => k.Key == idempotentRequest.IdempotencyKey);
            existing.Status = "Completed";
            existing.Response = JsonSerializer.Serialize(response);
            await _appDbContext.SaveChangesAsync();

            return response;
        }
    }
}
