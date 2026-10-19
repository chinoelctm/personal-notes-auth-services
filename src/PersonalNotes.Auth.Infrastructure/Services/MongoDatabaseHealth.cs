using MongoDB.Bson;
using MongoDB.Driver;
using PersonalNotes.Auth.Domain.Services;
using PersonalNotes.Auth.Infrastructure.Context;

namespace PersonalNotes.Auth.Infrastructure.Services
{
    public class MongoDatabaseHealth : IDatabaseHealth
    {
        private readonly AuthDbContext _context;

        public MongoDatabaseHealth(AuthDbContext context)
        {
            _context = context;
        }

        public async Task<bool> PingAsync(CancellationToken ct = default)
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(TimeSpan.FromSeconds(5));

                await _context.Database.RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1),
                    cancellationToken: timeout.Token);
                return true;
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                return false;
            }
        }
    }
}
