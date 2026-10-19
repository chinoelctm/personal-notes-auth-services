using Microsoft.Extensions.Logging.Abstractions;
using PersonalNotes.Auth.Application.Commands.Users.EnsureSeed;
using PersonalNotes.Auth.Domain.Entities;
using PersonalNotes.Auth.Tests.Fakes;

namespace PersonalNotes.Auth.Tests.Application
{
    public class EnsureSeedUserHandlerTests
    {
        private static EnsureSeedUserHandler HandlerFor(AuthFixture fx)
            => new(NullLogger<EnsureSeedUserHandler>.Instance, fx.Users, fx.Hasher, fx.Time);

        [Fact]
        public async Task CreatesTheUser_WhenItDoesNotExist()
        {
            var fx = new AuthFixture();
            var hash = fx.Hasher.HashPassword("otra-clave");

            var created = await HandlerFor(fx).Handle(new EnsureSeedUserCommand("Nuevo", "Nuevo", hash), default);

            Assert.True(created);
            Assert.Equal(hash, fx.Users.Users.Single(u => u.Username == "Nuevo").Password);
        }

        [Fact]
        public async Task DoesNotTouchAnExistingUserWithAValidPassword()
        {
            var fx = new AuthFixture();
            var original = fx.User.Password;

            var changed = await HandlerFor(fx).Handle(
                new EnsureSeedUserCommand(AuthFixture.Username, "x", fx.Hasher.HashPassword("otra-clave")), default);

            Assert.False(changed);
            Assert.Equal(original, fx.User.Password);
        }

        [Fact]
        public async Task RepairsAnExistingUserWhoseStoredPasswordIsNotABcryptHash_AndClearsTheLock()
        {
            var fx = new AuthFixture();
            // Así quedó en producción: un deploy arrancó con la clave privada en SeedUser__PasswordHash
            var broken = User.Create("JoaquinMarquez", "J", "-----BEGIN PRIVATE KEY-----\\nMIIE...", fx.Time.UtcNow);
            fx.Users.Users.Clear();
            fx.Users.Users.Add(broken);
            broken.RegisterFailedPasswordAttempt(5, TimeSpan.FromMinutes(15), fx.Time.UtcNow);
            var hash = fx.Hasher.HashPassword(AuthFixture.Password);

            var repaired = await HandlerFor(fx).Handle(new EnsureSeedUserCommand("joaquinmarquez", "J", hash), default);

            Assert.True(repaired);
            Assert.Equal(hash, broken.Password);
            Assert.Equal(0, broken.PasswordLock.FailedAttempts);
            Assert.True(fx.Hasher.VerifyPassword(AuthFixture.Password, broken.Password));
        }

        [Theory]
        [InlineData("")]
        [InlineData("gtanos11")]
        [InlineData("-----BEGIN PRIVATE KEY-----\\nMIIE")]
        public async Task IgnoresAConfiguredValueThatIsNotABcryptHash(string configured)
        {
            var fx = new AuthFixture();
            fx.Users.Users.Clear();

            var created = await HandlerFor(fx).Handle(new EnsureSeedUserCommand(AuthFixture.Username, "J", configured), default);

            Assert.False(created);
            Assert.Empty(fx.Users.Users);
        }
    }
}
