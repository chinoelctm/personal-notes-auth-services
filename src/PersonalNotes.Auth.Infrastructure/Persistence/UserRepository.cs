using MongoDB.Driver;
using PersonalNotes.Auth.Domain.Entities;
using PersonalNotes.Auth.Domain.Repositories;
using PersonalNotes.Auth.Infrastructure.Context;

namespace PersonalNotes.Auth.Infrastructure.Persistence
{
    public class UserRepository : IUserRepository
    {
        private readonly IMongoCollection<User> _users;

        public UserRepository(AuthDbContext context)
        {
            _users = context.Users;
        }

        public async Task<Guid> AddAsync(User user, CancellationToken ct = default)
        {
            await _users.InsertOneAsync(user, cancellationToken: ct).ConfigureAwait(false);
            return user.Id;
        }

        public async Task UpdateAsync(User user, CancellationToken ct = default)
        {
            var filter = Builders<User>.Filter.Eq(x => x.Id, user.Id);

            await _users.ReplaceOneAsync(filter, user, cancellationToken: ct).ConfigureAwait(false);
        }

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var filter = Builders<User>.Filter.Eq(x => x.Id, id);

            return await _users.Find(filter).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        }

        public async Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
        {
            var filter = Builders<User>.Filter.Eq(x => x.NormalizedUsername, User.Normalize(username));

            return await _users.Find(filter).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        }
    }
}
