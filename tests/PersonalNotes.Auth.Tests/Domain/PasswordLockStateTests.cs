using PersonalNotes.Auth.Domain.ValueObjects;

namespace PersonalNotes.Auth.Tests.Domain
{
    public class PasswordLockStateTests
    {
        private static readonly DateTime Now = new(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);
        private static readonly TimeSpan Lock = TimeSpan.FromMinutes(15);

        [Fact]
        public void LocksAfterMaxAttempts_ForTheConfiguredDuration()
        {
            var state = new PasswordLockState();

            state.RegisterFailedAttempt(3, Lock, Now);
            state.RegisterFailedAttempt(3, Lock, Now);
            Assert.False(state.IsLocked(Now));

            state.RegisterFailedAttempt(3, Lock, Now);
            Assert.True(state.IsLocked(Now.AddMinutes(14)));
            Assert.False(state.IsLocked(Now.AddMinutes(15)));
        }

        [Fact]
        public void AfterLockExpires_CountingStartsOver()
        {
            var state = new PasswordLockState();
            for (var i = 0; i < 3; i++) state.RegisterFailedAttempt(3, Lock, Now);

            state.RegisterFailedAttempt(3, Lock, Now.AddMinutes(20));

            Assert.Equal(1, state.FailedAttempts);
            Assert.False(state.IsLocked(Now.AddMinutes(20)));
        }

        [Fact]
        public void AttemptsWhileLocked_DoNotExtendTheLock()
        {
            var state = new PasswordLockState();
            for (var i = 0; i < 3; i++) state.RegisterFailedAttempt(3, Lock, Now);

            state.RegisterFailedAttempt(3, Lock, Now.AddMinutes(10));

            Assert.Equal(Now.Add(Lock), state.LockedUntil);
        }

        [Fact]
        public void Reset_ClearsAttemptsAndLock()
        {
            var state = new PasswordLockState();
            for (var i = 0; i < 3; i++) state.RegisterFailedAttempt(3, Lock, Now);

            state.Reset();

            Assert.Equal((0, null), (state.FailedAttempts, state.LockedUntil));
        }
    }
}
