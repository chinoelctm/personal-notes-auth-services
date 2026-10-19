using MongoDB.Driver;
using PersonalNotes.Auth.Domain.Entities;
using PersonalNotes.Auth.Domain.Repositories;
using PersonalNotes.Auth.Infrastructure.Context;

namespace PersonalNotes.Auth.Infrastructure.Persistence
{
    public class RefreshSessionRepository : IRefreshSessionRepository
    {
        private readonly IMongoCollection<RefreshSession> _refreshSessions;

        public RefreshSessionRepository(AuthDbContext context)
        {
            _refreshSessions = context.RefreshSessions;
        }

        public async Task<Guid> AddAsync(RefreshSession refreshSession, CancellationToken ct = default)
        {
            await _refreshSessions.InsertOneAsync(refreshSession, cancellationToken: ct).ConfigureAwait(false);
            return refreshSession.Id;
        }

        public async Task<RefreshSession?> GetAsync(string tokenHash, CancellationToken ct = default)
        {
            var filter = Builders<RefreshSession>.Filter.Eq(x => x.Token, tokenHash);

            return await _refreshSessions.Find(filter).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        }

        public async Task UpdateAsync(RefreshSession refreshSession, CancellationToken ct = default)
        {
            var filter = Builders<RefreshSession>.Filter.Eq(x => x.Id, refreshSession.Id);

            await _refreshSessions.ReplaceOneAsync(filter, refreshSession, cancellationToken: ct).ConfigureAwait(false);
        }

        public async Task DeleteRangeAsync(List<Guid> ids, CancellationToken ct = default)
        {
            var filter = Builders<RefreshSession>.Filter.In(x => x.Id, ids);

            await _refreshSessions.DeleteManyAsync(filter, ct).ConfigureAwait(false);
        }

        // Vencidos, o ya usados/revocados hace más de 15 min (el margen evita borrar uno recién rotado en una
        // carrera entre dos pestañas)
        public async Task<List<RefreshSession>> GetExpiredAsync(DateTime now, CancellationToken ct = default)
        {
            var limit = now.AddMinutes(-15);
            var builder = Builders<RefreshSession>.Filter;

            var filter = builder.Or(
                builder.Lte(x => x.ExpiresAt, now),
                builder.And(builder.Eq(x => x.IsUsed, true), builder.Lte(x => x.UsedAt, limit)),
                builder.Lte(x => x.RevokedAt, limit));

            return await _refreshSessions.Find(filter).ToListAsync(ct).ConfigureAwait(false);
        }
    }
}
