using Vafadar.Maui.Security;
using Vafadar.Zanance.App.Security;
using Vafadar.Zanance.Core.Security;

namespace Vafadar.Zanance.App.Tests;

/// <summary>A previous modal removal cannot publish access after another cover request arrives.</summary>
public sealed class AppLockCoverRetirementTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Close_completion_preserves_any_newer_cover_request(int scenario)
    {
        using var fixture = new FlowFixture();
        await AppLockTests.EnableDeviceAsync(fixture);
        var host = new CoverHost();
        var access = new AppLockService(new AuthPlatform(), fixture.Store, fixture.Translator,
            fixture.Time, new PinLock(new PinMemory(), fixture.Time), host);
        await access.StartAsync();
        var original = Assert.Single(host.Covers);
        var opened = 0;
        await access.RunWhenUnlockedAsync(() => { opened++; return Task.CompletedTask; });
        if (scenario != 0)
        {
            original.BeforeVisualClose = scenario == 3;
            original.DuringClose = async () =>
            {
                host.Available = scenario != 2;
                await access.SleepAsync();
            };
        }

        await access.UnlockedAsync();

        Assert.Equal(scenario != 0, access.IsLocked);
        Assert.Equal(scenario == 0 ? 1 : 0, opened);
        Assert.Equal(1, original.Closed);
        if (scenario != 0)
        {
            host.Available = true;
            await access.ResumeAsync();
            Assert.True(access.IsLocked);
            Assert.Equal(0, opened);
            Assert.True(host.Covers.Last().IsOnScreen);
            // A repeated resume must not reinterpret the renewed locked state as short-return grace.
            await access.ResumeAsync();
            Assert.True(access.IsLocked);
            Assert.Equal(0, opened);
        }
    }

    private sealed class CoverHost : IAppLockHost
    {
        internal bool Available = true;
        internal readonly List<Cover> Covers = [];
        public void ApplyProtection() { }
        public ILockCover? CreateCover(AppLockService service, bool prompt)
        {
            if (!Available) return null;
            var cover = new Cover(); Covers.Add(cover); return cover;
        }
        public Task<bool> ConfirmPinAsync(AppLockService service, string reason) => Task.FromResult(false);
        public Task<bool> ConfirmRecoveryAsync() => Task.FromResult(false);
    }

    private sealed class Cover : ILockCover
    {
        public bool IsOnScreen { get; private set; }
        public bool WasLockedBeforeSleep { get; set; }
        internal Func<Task>? DuringClose;
        internal bool BeforeVisualClose;
        internal int Closed;
        public Task ShowAsync() { IsOnScreen = true; return Task.CompletedTask; }
        public Task PromptAsync() => Task.CompletedTask;
        public async Task CloseAsync()
        {
            if (!BeforeVisualClose) IsOnScreen = false;
            if (DuringClose is { } callback) await callback();
            IsOnScreen = false;
            Closed++;
        }
    }
}
