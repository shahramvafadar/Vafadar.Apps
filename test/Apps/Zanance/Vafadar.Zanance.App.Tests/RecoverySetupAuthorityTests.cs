using Vafadar.Data;
using Vafadar.Zanance.App.Security;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual protected-gate waits retain the original recovery presentation and source, without secret material.</summary>
public sealed class RecoverySetupAuthorityTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Retiring the original presentation cancels its queued request without releasing another actual owner.</summary>
    [Fact]
    public async Task Retired_queued_presentation_never_borrows_or_releases_the_existing_owner()
    {
        var access = new DatabaseAccessGate(); using var original = await access.AcquireExclusiveAsync(Ct);
        using var authority = new RecoverySetupAuthority(() => { }, Ct);
        var pending = authority.AcquireAsync(access); Assert.False(pending.IsCompleted);
        authority.Retire(); await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        original.Validate();
        await Assert.ThrowsAsync<InvalidOperationException>(() => Task.Run(() => access.AcquireExclusive(), Ct));
    }

    /// <summary>Replacing the original source while queued refuses the acquired scope and leaves the gate usable.</summary>
    [Fact]
    public async Task Changed_original_catalogue_after_exclusive_wait_refuses_and_releases_only_its_lease()
    {
        var access = new DatabaseAccessGate(); var first = await access.AcquireExclusiveAsync(Ct); var current = true;
        using var authority = new RecoverySetupAuthority(() => { if (!current) throw new IOException("Original catalogue changed."); }, Ct);
        var pending = authority.AcquireAsync(access); Assert.False(pending.IsCompleted);
        current = false; first.Dispose(); await Assert.ThrowsAsync<IOException>(() => pending);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Ct); timeout.CancelAfter(TimeSpan.FromSeconds(10));
        using var next = await access.AcquireExclusiveAsync(timeout.Token); next.Validate();
    }

    /// <summary>Already retired requests cannot enter protected maintenance or validate a later source as their own.</summary>
    [Fact]
    public async Task Already_retired_authority_refuses_before_actual_gate_acquisition()
    {
        var access = new DatabaseAccessGate(); using var authority = new RecoverySetupAuthority(() => { }, Ct);
        authority.Retire(); await Assert.ThrowsAnyAsync<OperationCanceledException>(() => authority.AcquireAsync(access));
        using var next = await access.AcquireExclusiveAsync(Ct); next.Validate();
    }

    /// <summary>A current original capture can use the actual protected owner until its own lifetime ends.</summary>
    [Fact]
    public async Task Current_original_capture_acquires_actual_owner_and_retirement_invalidates_later_work()
    {
        var access = new DatabaseAccessGate(); using var authority = new RecoverySetupAuthority(() => { }, Ct);
        using var original = await authority.AcquireAsync(access); original.Validate(); authority.RequireCurrent();
        authority.Retire(); Assert.Throws<OperationCanceledException>(() => authority.RequireCurrent());
        original.Validate();
    }

    /// <summary>Retirement during original catalogue validation cannot return current authority to the next protected step.</summary>
    [Fact]
    public async Task Retirement_during_source_validation_refuses_before_returning_current_authority()
    {
        var access = new DatabaseAccessGate(); var retireDuringValidation = false; RecoverySetupAuthority? original = null;
        using var authority = original = new RecoverySetupAuthority(() => { if (retireDuringValidation) original!.Retire(); }, Ct);
        using var owner = await authority.AcquireAsync(access); owner.Validate();
        retireDuringValidation = true;
        Assert.Throws<OperationCanceledException>(() => authority.RequireCurrent()); owner.Validate();
    }
}
