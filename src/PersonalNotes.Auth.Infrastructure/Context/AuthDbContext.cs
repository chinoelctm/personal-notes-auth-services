using MongoDB.Driver;
using PersonalNotes.Auth.Domain.Entities;

namespace PersonalNotes.Auth.Infrastructure.Context
{
    public sealed class AuthDbContext
    {
        public IMongoDatabase Database { get; }

        public IMongoCollection<User> Users { get; }
        public IMongoCollection<RefreshSession> RefreshSessions { get; }

        public AuthDbContext(IMongoClient client, string databaseName)
        {
            Database = client.GetDatabase(databaseName);

            Users = Database.GetCollection<User>("users");
            RefreshSessions = Database.GetCollection<RefreshSession>("refreshSessions");
        }

        public async Task EnsureIndexesAsync(CancellationToken ct = default)
        {
            await EnsureUserIndexesAsync(ct);
            await EnsureRefreshSessionIndexesAsync(ct);
        }

        private async Task EnsureUserIndexesAsync(CancellationToken ct)
        {
            var indexes = new[]
            {
                new CreateIndexModel<User>(
                    Builders<User>.IndexKeys.Ascending(x => x.NormalizedUsername),
                    new CreateIndexOptions { Name = "ux_users_username", Unique = true }
                )
            };

            await Users.Indexes.CreateManyAsync(indexes, ct);
        }

        private async Task EnsureRefreshSessionIndexesAsync(CancellationToken ct)
        {
            var indexes = new[]
            {
                new CreateIndexModel<RefreshSession>(
                    Builders<RefreshSession>.IndexKeys.Ascending(x => x.Token),
                    new CreateIndexOptions { Name = "ux_refresh_token", Unique = true }
                ),
                new CreateIndexModel<RefreshSession>(
                    Builders<RefreshSession>.IndexKeys.Ascending(x => x.UserId),
                    new CreateIndexOptions { Name = "ix_refresh_userId" }
                ),
                new CreateIndexModel<RefreshSession>(
                    Builders<RefreshSession>.IndexKeys.Ascending(x => x.ExpiresAt),
                    new CreateIndexOptions { Name = "ix_refresh_expiresAt" }
                )
            };

            await RefreshSessions.Indexes.CreateManyAsync(indexes, ct);
        }
    }
}
