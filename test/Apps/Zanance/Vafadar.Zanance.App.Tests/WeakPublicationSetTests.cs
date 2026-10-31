using System.Runtime.CompilerServices;
using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Checks page lifetime and live retirement independently of financial commands or native GC heuristics.</summary>
public sealed class WeakPublicationSetTests
{
    [Fact]
    public void Completed_owner_is_collectible_while_original_strong_registration_retains_it()
    {
        var weak = new WeakPublicationSet<object>();
        var strong = new HashSet<object>();
        var completed = RegisterCompleted(weak, null);
        var retained = RegisterCompleted(null, strong);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Assert.False(completed.IsAlive);
        Assert.True(retained.IsAlive);
        Assert.Empty(weak);
        GC.KeepAlive(strong);
    }

    [Fact]
    public void Repeated_appearance_retires_each_original_reference_once_even_with_equal_values()
    {
        var owners = new WeakPublicationSet<Owner>();
        var first = new Owner(1); var second = new Owner(1);
        owners.Add(first); owners.Add(first); owners.Add(second);
        foreach (var owner in owners) owner.Retire();
        Assert.Equal(1, first.Retirements); Assert.Equal(1, second.Retirements);
    }

    [Fact]
    public void Retirement_snapshot_survives_registration_clear_during_an_original_callback()
    {
        var owners = new WeakPublicationSet<Owner>();
        var first = new Owner(1); var second = new Owner(2);
        owners.Add(first); owners.Add(second);
        foreach (var owner in owners) { owner.Retire(); owners.Clear(); }
        Assert.Equal(1, first.Retirements); Assert.Equal(1, second.Retirements);
        Assert.Empty(owners);
    }

    [Fact]
    public void Collected_predecessor_does_not_prevent_a_new_live_owner_from_retiring()
    {
        var owners = new WeakPublicationSet<object>();
        var completed = RegisterCompleted(owners, null);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Assert.False(completed.IsAlive);
        var current = new object(); owners.Add(current);
        Assert.Same(current, Assert.Single(owners));
        owners.Clear(); Assert.Empty(owners);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference RegisterCompleted(WeakPublicationSet<object>? weak, HashSet<object>? strong)
    {
        var owner = new object(); weak?.Add(owner); strong?.Add(owner);
        return new WeakReference(owner);
    }

    private sealed record Owner(int Identity)
    {
        internal int Retirements { get; private set; }
        internal void Retire() => Retirements++;
    }
}
