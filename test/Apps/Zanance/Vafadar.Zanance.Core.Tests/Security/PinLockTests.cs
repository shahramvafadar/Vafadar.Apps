using Vafadar.Zanance.Core.Security;

namespace Vafadar.Zanance.Core.Tests.Security;

public sealed class PinLockTests
{
    [Theory]
    [InlineData("0123", true)]
    [InlineData("0000", true)]
    [InlineData("9876", true)]
    [InlineData("123", false)]
    [InlineData("12345", false)]
    [InlineData("12a4", false)]
    [InlineData("۱۲۳۴", false)]
    [InlineData(" 123", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void PIN_contract_accepts_exactly_four_ASCII_digits(string? pin, bool valid) => Assert.Equal(valid, PinLock.IsValid(pin));

    [Fact]
    [Trait("AT", "AT-69")]
    public async Task Fresh_start_reads_the_device_verifier_and_leading_zeroes_are_significant()
    {
        var storage = new MemoryStorage();
        var gate = new PinLock(storage, new Clock());
        await gate.LoadAsync();
        Assert.False(gate.IsEnabled);
        Assert.Equal(PinOutcome.NotConfigured, (await gate.VerifyAsync("0123")).Outcome);
        Assert.Equal(PinOutcome.Success, (await gate.SetAsync("", "0123")).Outcome);
        var restarted = new PinLock(storage, new Clock());
        await restarted.LoadAsync();
        Assert.True(restarted.IsEnabled);
        Assert.Equal(PinOutcome.Incorrect, (await restarted.VerifyAsync("1230")).Outcome);
        Assert.Equal(PinOutcome.Success, (await restarted.VerifyAsync("0123")).Outcome);
        Assert.Equal(0, storage.State!.Failures);
        Assert.Null(storage.State.RetryAt);
    }

    [Fact]
    [Trait("AT", "AT-69")]
    public async Task Changing_and_removing_require_the_current_PIN_and_keep_it_after_failed_attempts()
    {
        var storage = new MemoryStorage();
        var gate = new PinLock(storage, new Clock());
        await gate.SetAsync("", "0123");
        Assert.Equal(PinOutcome.Incorrect, (await gate.SetAsync("9999", "4567")).Outcome);
        Assert.Equal(PinOutcome.Incorrect, (await gate.RemoveAsync("9999")).Outcome);
        Assert.True(gate.IsEnabled);
        Assert.Equal(PinOutcome.Success, (await gate.SetAsync("0123", "4567")).Outcome);
        Assert.Equal(PinOutcome.Incorrect, (await gate.VerifyAsync("0123")).Outcome);
        Assert.Equal(PinOutcome.Success, (await gate.VerifyAsync("4567")).Outcome);
        Assert.Equal(PinOutcome.Success, (await gate.RemoveAsync("4567")).Outcome);
        Assert.False(gate.IsEnabled);
        Assert.Null(storage.State);
    }

    [Fact]
    [Trait("AT", "AT-69")]
    public async Task Five_failures_delay_every_operation_and_restart_does_not_reset_the_deadline()
    {
        var storage = new MemoryStorage();
        var clock = new Clock();
        var gate = new PinLock(storage, clock);
        await gate.SetAsync("", "0123");
        for (var i = 0; i < 4; i++) { Assert.Equal(PinOutcome.Incorrect, (await gate.VerifyAsync("9999")).Outcome); }
        Assert.Equal(TimeSpan.FromMinutes(1), (await gate.VerifyAsync("9999")).Wait);
        gate = new PinLock(storage, clock);
        await gate.LoadAsync();
        Assert.Equal(PinOutcome.Delayed, (await gate.VerifyAsync("0123")).Outcome);
        Assert.Equal(PinOutcome.Delayed, (await gate.SetAsync("0123", "4567")).Outcome);
        Assert.Equal(PinOutcome.Delayed, (await gate.RemoveAsync("0123")).Outcome);
        clock.Now = clock.Now.AddMinutes(1);
        Assert.Equal(TimeSpan.FromMinutes(2), (await gate.VerifyAsync("9999")).Wait);
        clock.Now = clock.Now.AddMinutes(2);
        Assert.Equal(PinOutcome.Success, (await gate.VerifyAsync("0123")).Outcome);
        Assert.Equal(0, storage.State!.Failures);
    }

    [Fact]
    public async Task Increasing_delay_caps_at_fifteen_minutes_and_clock_rollback_never_grants_access()
    {
        var storage = new MemoryStorage();
        var clock = new Clock();
        var gate = new PinLock(storage, clock);
        await gate.SetAsync("", "0123");
        storage.State = storage.State! with { Failures = 9 };
        Assert.Equal(TimeSpan.FromMinutes(15), (await gate.VerifyAsync("9999")).Wait);
        clock.Now = clock.Now.AddHours(-1);
        Assert.Equal(PinOutcome.Delayed, (await gate.VerifyAsync("0123")).Outcome);
    }

    [Fact]
    public async Task Every_configuration_uses_a_new_salt_and_invalid_new_PIN_changes_nothing()
    {
        var storage = new MemoryStorage();
        var gate = new PinLock(storage, new Clock());
        await gate.SetAsync("", "0123");
        var first = storage.State!;
        await gate.SetAsync("0123", "0123");
        Assert.False(first.Salt.SequenceEqual(storage.State!.Salt));
        Assert.False(first.Hash.SequenceEqual(storage.State.Hash));
        Assert.Equal(16, first.Salt.Length);
        Assert.Equal(32, first.Hash.Length);
        var state = storage.State;
        Assert.Equal(PinOutcome.Incorrect, (await gate.SetAsync("0123", "12")).Outcome);
        Assert.Same(state, storage.State);
    }

    [Fact]
    public async Task Protected_storage_failure_never_counts_as_a_successful_unlock_or_disabled_PIN()
    {
        var storage = new MemoryStorage();
        var gate = new PinLock(storage, new Clock());
        await gate.SetAsync("", "0123");
        storage.FailWrites = true;
        await Assert.ThrowsAsync<IOException>(() => gate.VerifyAsync("0123"));
        await Assert.ThrowsAsync<IOException>(() => gate.RemoveAsync("0123"));
        Assert.True(gate.IsEnabled);
        Assert.NotNull(storage.State);
        storage.FailReads = true;
        await Assert.ThrowsAsync<IOException>(gate.LoadAsync);
        Assert.True(gate.IsEnabled);
    }

    [Theory]
    [InlineData(2, 600000)]
    [InlineData(1, 1)]
    public async Task Unsupported_verifier_parameters_fail_closed(int version, int iterations)
    {
        var storage = new MemoryStorage { State = new(version, iterations, new byte[16], new byte[32], 0, null) };
        var gate = new PinLock(storage, new Clock());
        await Assert.ThrowsAsync<InvalidOperationException>(gate.LoadAsync);
        await Assert.ThrowsAsync<InvalidOperationException>(() => gate.VerifyAsync("0123"));
    }

    [Fact]
    [Trait("AT", "AT-69")]
    public async Task Recovery_cancellation_and_unavailable_device_authentication_leave_PIN_intact()
    {
        var storage = new MemoryStorage();
        var gate = new PinLock(storage, new Clock());
        await gate.SetAsync("", "0123");
        Assert.False(await gate.RecoverAsync(() => Task.FromResult(false)));
        Assert.True(gate.IsEnabled);
        Assert.NotNull(storage.State);
        storage.FailReads = true;
        Assert.True(await gate.RecoverAsync(() => Task.FromResult(true)));
        Assert.False(gate.IsEnabled);
        Assert.Null(storage.State);
    }

    [Fact]
    public async Task Concurrent_wrong_attempts_are_counted_once_each_without_bypassing_the_delay()
    {
        var storage = new MemoryStorage();
        var gate = new PinLock(storage, new Clock());
        await gate.SetAsync("", "0123");
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => gate.VerifyAsync("9999")));
        Assert.Equal(5, storage.State!.Failures);
        Assert.Equal(4, results.Count(r => r.Outcome == PinOutcome.Delayed));
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class MemoryStorage : IPinStorage
    {
        public PinState? State { get; set; }
        public bool FailReads { get; set; }
        public bool FailWrites { get; set; }
        public Task<PinState?> ReadAsync() => FailReads ? throw new IOException("Protected read failed.") : Task.FromResult(State);
        public Task WriteAsync(PinState? state)
        {
            if (FailWrites) { throw new IOException("Protected write failed."); }
            State = state;
            return Task.CompletedTask;
        }
    }
}
