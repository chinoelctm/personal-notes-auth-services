using PersonalNotes.Auth.Application.Commands.Auth.Login;
using PersonalNotes.Auth.Application.Commands.RefreshToken;
using PersonalNotes.Auth.Domain.Exceptions;
using PersonalNotes.Auth.Tests.Fakes;

namespace PersonalNotes.Auth.Tests.Application
{
    public class RefreshTokenHandlerTests
    {
        private static async Task<(AuthFixture Fx, string RefreshToken)> LoggedInAsync()
        {
            var fx = new AuthFixture();
            var login = await fx.LoginHandler().Handle(new LoginCommand(AuthFixture.Username, AuthFixture.Password), default);
            return (fx, login.Tokens.RefreshToken);
        }

        [Fact]
        public async Task Refresh_RotatesTheToken_AndTheOldOneCannotBeReused()
        {
            var (fx, refreshToken) = await LoggedInAsync();
            var handler = fx.RefreshHandler();

            var renewed = await handler.Handle(new RefreshTokenCommand(refreshToken), default);

            Assert.NotEqual(refreshToken, renewed.RefreshToken);
            Assert.Equal(2, fx.Sessions.Sessions.Count);
            Assert.True(fx.Sessions.Sessions[0].IsUsed);

            await Assert.ThrowsAsync<InvalidRefreshTokenException>(() =>
                handler.Handle(new RefreshTokenCommand(refreshToken), default));
            await handler.Handle(new RefreshTokenCommand(renewed.RefreshToken), default);
        }

        [Fact]
        public async Task Refresh_FailsAfterSevenDays()
        {
            var (fx, refreshToken) = await LoggedInAsync();

            fx.Time.UtcNow = fx.Time.UtcNow.AddDays(7).AddMinutes(1);

            await Assert.ThrowsAsync<InvalidRefreshTokenException>(() =>
                fx.RefreshHandler().Handle(new RefreshTokenCommand(refreshToken), default));
        }

        [Fact]
        public async Task Refresh_FailsForRevokedOrUnknownTokens_OrDisabledUsers()
        {
            var (fx, refreshToken) = await LoggedInAsync();
            var handler = fx.RefreshHandler();

            await Assert.ThrowsAsync<InvalidRefreshTokenException>(() =>
                handler.Handle(new RefreshTokenCommand("inventado"), default));

            fx.User.Disable(fx.Time.UtcNow);
            await Assert.ThrowsAsync<InvalidRefreshTokenException>(() =>
                handler.Handle(new RefreshTokenCommand(refreshToken), default));

            var (fx2, token2) = await LoggedInAsync();
            fx2.Sessions.Sessions[0].Revoke(fx2.Time.UtcNow);
            await Assert.ThrowsAsync<InvalidRefreshTokenException>(() =>
                fx2.RefreshHandler().Handle(new RefreshTokenCommand(token2), default));
        }
    }
}
