using PersonalNotes.Auth.Domain.Entities;
using PersonalNotes.Auth.Domain.Exceptions;

namespace PersonalNotes.Auth.Tests.Domain
{
    public class UserTests
    {
        private static readonly DateTime Now = new(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void Create_NormalizesUsername_AndDefaultsNameToUsername()
        {
            var user = User.Create(" JoaquinMarquez ", null, "hash", Now);

            Assert.Equal(("JoaquinMarquez", "joaquinmarquez", "JoaquinMarquez"),
                (user.Username, user.NormalizedUsername, user.Name));
            Assert.True(user.IsActive);
        }

        [Fact]
        public void Create_RejectsMissingUsernameOrPassword()
        {
            Assert.Throws<DomainException>(() => User.Create(" ", "x", "hash", Now));
            Assert.Throws<DomainException>(() => User.Create("user", "x", "", Now));
            Assert.Throws<DomainException>(() => User.Create(new string('u', User.MaxUsernameLength + 1), "x", "hash", Now));
        }

        [Fact]
        public void SuccessfulLogin_ResetsFailedAttempts()
        {
            var user = User.Create("user", "x", "hash", Now);
            user.RegisterFailedPasswordAttempt(5, TimeSpan.FromMinutes(15), Now);

            user.RegisterSuccessfulLogin(Now);

            Assert.Equal(0, user.PasswordLock.FailedAttempts);
            Assert.Equal(Now, user.LastLoginOn);
        }
    }
}
