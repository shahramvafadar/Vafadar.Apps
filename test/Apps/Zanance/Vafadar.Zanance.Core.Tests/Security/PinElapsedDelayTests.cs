using Vafadar.Zanance.Core.Security;

namespace Vafadar.Zanance.Core.Tests.Security;

/// <summary>Original restart-persistent minute must not be consumed merely by a forward UTC correction.</summary>
public sealed class PinElapsedDelayTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Same_boot_clock_forward_does_not_consume_or_bypass_the_existing_retry_minute(bool matchingPIN)
    {
        var clock = new IndependentClock(); var storage = new MemoryStorage();
        var gate = new PinLock(storage, clock, clock);
        await gate.SetAsync("", "0137");
        for (var index = 0; index < 5; index++) await gate.VerifyAsync("9999");
        Assert.Equal(5, storage.State!.Failures); var writes = storage.Writes;
        clock.Now = clock.Now.AddHours(1); clock.ElapsedMilliseconds += 1000;
        var restarted = new PinLock(storage, clock, clock);
        var result = await restarted.VerifyAsync(matchingPIN ? "0137" : "9999");
        Assert.Equal(5, storage.State.Failures); Assert.Equal(writes, storage.Writes);
        Assert.Equal(PinOutcome.Delayed, result.Outcome); Assert.Equal(TimeSpan.FromSeconds(59), result.Wait);
    }

    [Fact]
    public async Task Clock_rollback_does_not_extend_a_known_same_boot_penalty()
    {
        var (storage, clock, _) = await PenaltyAsync(); var writes = storage.Writes;
        clock.Now = clock.Now.AddHours(-1); clock.ElapsedMilliseconds += 1000;
        var result = await new PinLock(storage, clock, clock).VerifyAsync("0137");
        Assert.Equal(PinOutcome.Delayed, result.Outcome); Assert.Equal(TimeSpan.FromSeconds(59), result.Wait);
        Assert.Equal(writes, storage.Writes); Assert.Equal(5, storage.State!.Failures);
    }

    [Fact]
    public async Task Actual_elapsed_expiry_allows_correct_PIN_and_clears_both_deadlines_despite_UTC_rollback()
    {
        var (storage, clock, _) = await PenaltyAsync();
        clock.Now = clock.Now.AddHours(-1); clock.ElapsedMilliseconds += 60000;
        Assert.Equal(PinOutcome.Success, (await new PinLock(storage, clock, clock).VerifyAsync("0137")).Outcome);
        Assert.Equal(0, storage.State!.Failures); Assert.Null(storage.State.RetryAt); Assert.Null(storage.State.RetryDeadline);
    }

    [Fact]
    public async Task Distinct_expired_penalties_keep_original_doubling_and_fifteen_minute_cap()
    {
        var (storage, clock, gate) = await PenaltyAsync(); var previous = TimeSpan.FromMinutes(1);
        foreach (var minutes in new[] { 2, 4, 8, 15, 15 })
        {
            clock.ElapsedMilliseconds += (long)previous.TotalMilliseconds;
            var result = await gate.VerifyAsync("9999");
            Assert.Equal(PinOutcome.Delayed, result.Outcome); Assert.Equal(TimeSpan.FromMinutes(minutes), result.Wait);
            Assert.Equal(clock.ElapsedMilliseconds + (long)result.Wait.TotalMilliseconds, storage.State!.RetryDeadline!.TargetMilliseconds);
            previous = result.Wait;
        }
    }

    [Fact]
    public async Task Proven_different_boot_uses_original_UTC_deadline_without_a_new_elapsed_origin()
    {
        var (storage, clock, _) = await PenaltyAsync(); var writes = storage.Writes; var original = storage.State!.RetryDeadline;
        clock.BootGeneration++; clock.ElapsedMilliseconds = 1000; clock.Now = clock.Now.AddSeconds(30);
        var gate = new PinLock(storage, clock, clock); var result = await gate.VerifyAsync("0137");
        Assert.Equal(TimeSpan.FromSeconds(30), result.Wait); Assert.Equal(writes, storage.Writes); Assert.Equal(original, storage.State.RetryDeadline);
        clock.Now = clock.Now.AddSeconds(31);
        Assert.Equal(PinOutcome.Success, (await gate.VerifyAsync("0137")).Outcome);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Missing_required_or_invalid_current_clock_refuses_access_and_changes_no_state(int kind)
    {
        var (storage, clock, _) = await PenaltyAsync(); var writes = storage.Writes; clock.Now = clock.Now.AddHours(1);
        if (kind == 0) clock.Available = false;
        else if (kind == 1) clock.BootGeneration = -1;
        else clock.ElapsedMilliseconds = -1;
        await Assert.ThrowsAsync<InvalidOperationException>(() => new PinLock(storage, clock, clock).VerifyAsync("0137"));
        Assert.Equal(writes, storage.Writes); Assert.Equal(5, storage.State!.Failures);
    }

    [Fact]
    public async Task Impossible_same_boot_future_deadline_refuses_instead_of_falling_through_advanced_UTC()
    {
        var (storage, clock, _) = await PenaltyAsync(); var writes = storage.Writes;
        clock.Now = clock.Now.AddHours(1); clock.ElapsedMilliseconds = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => new PinLock(storage, clock, clock).VerifyAsync("0137"));
        Assert.Equal(writes, storage.Writes);
    }

    [Fact]
    public async Task Legacy_six_field_JSON_retains_UTC_semantics_without_retrofitting_an_unknown_anchor()
    {
        var storage = new MemoryStorage(); var clock = new IndependentClock(); var legacy = new PinLock(storage, clock);
        await legacy.SetAsync("", "0137"); for (var index = 0; index < 5; index++) await legacy.VerifyAsync("9999");
        var json = System.Text.Json.JsonSerializer.Serialize(storage.State);
        Assert.False(json.Contains("RetryDeadline", StringComparison.Ordinal));
        storage.State = System.Text.Json.JsonSerializer.Deserialize<PinState>(json); var writes = storage.Writes;
        clock.Now = clock.Now.AddSeconds(30); clock.ElapsedMilliseconds += 1000;
        var result = await new PinLock(storage, clock, clock).VerifyAsync("0137");
        Assert.Equal(TimeSpan.FromSeconds(30), result.Wait); Assert.Null(storage.State!.RetryDeadline); Assert.Equal(writes, storage.Writes);
    }

    [Fact]
    public async Task New_optional_deadline_JSON_survives_restart_without_exposing_verifier_material()
    {
        var (storage, clock, _) = await PenaltyAsync(); var original = storage.State!;
        var json = System.Text.Json.JsonSerializer.Serialize(original); storage.State = System.Text.Json.JsonSerializer.Deserialize<PinState>(json);
        Assert.Equal(original.RetryDeadline, storage.State!.RetryDeadline);
        Assert.True(original.Hash.AsSpan().SequenceEqual(storage.State.Hash)); Assert.True(original.Salt.AsSpan().SequenceEqual(storage.State.Salt));
        clock.Now = clock.Now.AddHours(1); clock.ElapsedMilliseconds += 1000;
        Assert.Equal(TimeSpan.FromSeconds(59), (await new PinLock(storage, clock, clock).VerifyAsync("0137")).Wait);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Structurally_invalid_protected_deadline_does_not_disable_or_rewrite_the_verifier(int kind)
    {
        var (storage, clock, _) = await PenaltyAsync(); var state = storage.State!; var writes = storage.Writes;
        storage.State = kind switch
        {
            0 => state with { RetryDeadline = new(-1, 160000) },
            1 => state with { RetryDeadline = new(7, -1) },
            2 => state with { Failures = 4 },
            _ => state with { RetryAt = null },
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => new PinLock(storage, clock, clock).LoadAsync());
        Assert.Equal(writes, storage.Writes);
    }

    /// <summary>Creates only this distinct test's required failed-attempt threshold, never a financial fixture.</summary>
    private static async Task<(MemoryStorage Storage, IndependentClock Clock, PinLock Gate)> PenaltyAsync()
    {
        var storage = new MemoryStorage(); var clock = new IndependentClock(); var gate = new PinLock(storage, clock, clock);
        await gate.SetAsync("", "0137"); for (var index = 0; index < 5; index++) await gate.VerifyAsync("9999");
        return (storage, clock, gate);
    }

    /// <summary>Independent fictitious clocks never change system time or carry owner identifiers.</summary>
    private sealed class IndependentClock : TimeProvider, IPinAttemptClock
    {
        internal DateTimeOffset Now = new(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);
        internal long ElapsedMilliseconds = 100000;
        internal int BootGeneration = 7;
        internal bool Available = true;
        public PinAttemptClockReading? Read() => Available ? new(BootGeneration, ElapsedMilliseconds) : null;
        public override DateTimeOffset GetUtcNow() => Now;
        public override long GetTimestamp() => ElapsedMilliseconds;
        public override long TimestampFrequency => 1000;
    }

    /// <summary>Only fresh fictitious verifier state; no platform key, password or protected owner data is read.</summary>
    private sealed class MemoryStorage : IPinStorage
    {
        internal PinState? State; internal int Writes;
        public Task<PinState?> ReadAsync() => Task.FromResult(State);
        public Task WriteAsync(PinState? state) { State = state; Writes++; return Task.CompletedTask; }
    }
}
