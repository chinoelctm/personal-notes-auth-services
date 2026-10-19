using PersonalNotes.Auth.Application.Commands.Auth.Login;
using PersonalNotes.Auth.Domain.Exceptions;
using PersonalNotes.Auth.Tests.Fakes;

namespace PersonalNotes.Auth.Tests.Application
{
    public class LoginHandlerTests
    {
        [Fact]
        public async Task ValidCredentials_IssueTokensAndStoreOnlyTheRefreshHash()
        {
            var fx = new AuthFixture();

            var result = await fx.LoginHandler().Handle(new LoginCommand("joaquinmarquez", AuthFixture.Password), default);

            Assert.Equal(AuthFixture.Username, result.User.Username);
            Assert.Equal(15 * 60, result.Tokens.AccessTokenExpiresIn);
            Assert.Equal(7 * 24 * 3600, result.Tokens.RefreshTokenExpiresIn);

            var session = Assert.Single(fx.Sessions.Sessions);
            Assert.NotEqual(result.Tokens.RefreshToken, session.Token);
            Assert.Equal(fx.Hasher.HashToken(result.Tokens.RefreshToken), session.Token);
            Assert.Equal(fx.Time.UtcNow.AddDays(7), session.ExpiresAt);
        }

        [Fact]
        public async Task WrongPasswordOrUnknownUser_AreUnauthorized_WithoutTokens()
        {
            var fx = new AuthFixture();
            var handler = fx.LoginHandler();

            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                handler.Handle(new LoginCommand(AuthFixture.Username, "otra"), default));
            await Assert.ThrowsAsync<UnauthorizedException>(() =>
                handler.Handle(new LoginCommand("nadie", AuthFixture.Password), default));

            Assert.Empty(fx.Sessions.Sessions);
        }

        [Fact]
        public async Task TooManyFailures_LockTheAccount_EvenWithTheRightPassword_UntilTheLockExpires()
        {
            var fx = new AuthFixture();
            var handler = fx.LoginHandler();

            await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(new LoginCommand(AuthFixture.Username, "x"), default));
            await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(new LoginCommand(AuthFixture.Username, "x"), default));
            await Assert.ThrowsAsync<LockedException>(() => handler.Handle(new LoginCommand(AuthFixture.Username, "x"), default));
            await Assert.ThrowsAsync<LockedException>(() =>
                handler.Handle(new LoginCommand(AuthFixture.Username, AuthFixture.Password), default));

            fx.Time.UtcNow = fx.Time.UtcNow.AddMinutes(16);
            var result = await handler.Handle(new LoginCommand(AuthFixture.Username, AuthFixture.Password), default);

            Assert.Equal(AuthFixture.Username, result.User.Username);
            Assert.Equal(0, fx.User.PasswordLock.FailedAttempts);
        }
    }
}
