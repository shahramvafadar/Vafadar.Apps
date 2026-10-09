using Vafadar.Zanance.App.Presentation;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Commands must enforce the undo window themselves, including delayed taps and clock rollback.</summary>
public sealed class UndoTests
{
    [Theory, Trait("AT", "AT-80")]
    [InlineData(7, 1)]
    [InlineData(8, 0)]
    [InlineData(-1, 0)]
    public async Task Undo_only_executes_once_within_the_nonnegative_offer_window(int seconds, int expectedCalls)
    {
        using var f = new FlowFixture(); var undo = new UndoService(f.Store, f.Time); var calls = 0;
        undo.Offer(() => { calls++; return Task.CompletedTask; }); f.Time.Now = f.Time.Now.AddSeconds(seconds);
        Assert.Equal(expectedCalls == 1, undo.CanUndo); Assert.InRange(undo.Remaining, TimeSpan.Zero, TimeSpan.FromSeconds(8));
        await undo.UndoAsync(); await undo.UndoAsync(); Assert.Equal(expectedCalls, calls); Assert.False(undo.CanUndo);
    }

    [Fact, Trait("AT", "AT-80")]
    public async Task Replacing_or_dismissing_an_offer_never_executes_its_previous_action()
    {
        using var f = new FlowFixture(); var undo = new UndoService(f.Store, f.Time); var old = 0; var current = 0;
        undo.Offer(() => { old++; return Task.CompletedTask; }); undo.Offer(() => { current++; return Task.CompletedTask; });
        await undo.UndoAsync(); Assert.Equal(0, old); Assert.Equal(1, current);
        undo.Offer(() => { old++; return Task.CompletedTask; }); undo.Dismiss(); await undo.UndoAsync(); Assert.Equal(0, old);
    }
}
