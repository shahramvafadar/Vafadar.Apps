using Vafadar.Zanance.App.Features.Reports;

namespace Vafadar.Zanance.App.Tests;

/// <summary>D-647: original KPI query, pending close and immutable displayed explanation boundaries.</summary>
public sealed class KpiOriginalQueryBoundaryTests
{
    /// <summary>A pending original close cannot invoke another sheet's newly installed callback.</summary>
    [Fact]
    public async Task Pending_original_close_cannot_invoke_a_replacement_query_callback()
    {
        using var f = new FlowFixture(); var close = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); var first = 0; var replacement = 0;
        Shell.Current = new() { Navigate = _ => close.Task }; var vm = new KpiSheetViewModel(f.Translator);
        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["sheet"] = new KpiExplanation("K01", "0 EUR", "D647 original scope", [], null, () => { first++; return Task.CompletedTask; }) });
        var opening = vm.ShowEntriesCommand.ExecuteAsync(null);
        vm.ApplyQueryAttributes(new Dictionary<string, object> { ["sheet"] = new KpiExplanation("K03", "0 USD", "D647 replacement scope", [], null, () => { replacement++; return Task.CompletedTask; }) });
        close.SetResult(); await opening;
        Assert.Equal(0, first); Assert.Equal(0, replacement);
    }
    /// <summary>The displayed explanation cannot follow a later mutation of its caller-owned line list.</summary>
    [Fact]
    public void Original_explanation_lines_do_not_follow_later_caller_mutation()
    {
        using var f = new FlowFixture(); var original = new AmountLine("D647 original line", "0 EUR", false); var lines = new List<AmountLine> { original };
        var vm = new KpiSheetViewModel(f.Translator); vm.ApplyQueryAttributes(new Dictionary<string, object> { ["sheet"] = new KpiExplanation("K01", "0 EUR", "D647 original scope", lines, null, null) });
        lines[0] = new AmountLine("D647 changed caller line", "1 USD", false);
        Assert.Equal(original, Assert.Single(vm.Lines)); Assert.Equal("0 EUR", vm.Value); Assert.Equal("D647 original scope", vm.Scope);
    }
    private sealed class NativeClosePort : IKpiSheetInteraction
    {
        internal Func<Task> Close = () => Task.CompletedTask;
        internal int Calls;
        public Task CloseAsync() { Calls++; return Close(); }
    }
    private static void Query(KpiSheetViewModel vm, Func<Task> show, Func<Task>? validate = null, Func<bool>? source = null, Func<bool>? handoff = null)
        => vm.ApplyQueryAttributes(new Dictionary<string, object> { ["sheet"] = new KpiExplanation("K01", "0 EUR", "D647 original scope", [], null, show, validate, source, handoff) });
    /// <summary>The legitimate owned close preserves its callback despite the normal native page disappearance.</summary>
    [Fact]
    public async Task Own_native_close_hands_off_the_original_target_after_two_source_validations()
    {
        using var f = new FlowFixture(); var port = new NativeClosePort(); var vm = new KpiSheetViewModel(f.Translator, port); var calls = 0; var validations = 0;
        port.Close = () => { vm.SetActive(false); return Task.CompletedTask; };
        Query(vm, () => { calls++; return Task.CompletedTask; }, () => { validations++; return Task.CompletedTask; }, () => true, () => true);
        await vm.ShowEntriesCommand.ExecuteAsync(null); Assert.Equal(1, port.Calls); Assert.Equal(1, calls); Assert.Equal(2, validations); Assert.False(vm.CanShowEntries);
    }
    /// <summary>Leaving before pending source validation finishes must not close a replacement native page.</summary>
    [Fact]
    public async Task Departed_sheet_during_source_validation_cannot_close_or_navigate()
    {
        using var f = new FlowFixture(); var port = new NativeClosePort(); var vm = new KpiSheetViewModel(f.Translator, port); var calls = 0;
        var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Query(vm, () => { calls++; return Task.CompletedTask; }, () => held.Task);
        var pending = vm.ShowEntriesCommand.ExecuteAsync(null); vm.SetActive(false); held.SetResult(); await pending;
        Assert.Equal(0, port.Calls); Assert.Equal(0, calls);
    }
    /// <summary>Actual root replacement or a foreign parent after close cannot receive the original child callback.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Retired_root_or_foreign_native_parent_refuses_the_close_handoff(bool retire)
    {
        using var f = new FlowFixture(); var port = new NativeClosePort(); var vm = new KpiSheetViewModel(f.Translator, port); var calls = 0;
        port.Close = () => { vm.SetActive(false); if (retire) vm.RetireRoot(); return Task.CompletedTask; };
        Query(vm, () => { calls++; return Task.CompletedTask; }, handoff: () => retire);
        await vm.ShowEntriesCommand.ExecuteAsync(null); Assert.Equal(1, port.Calls); Assert.Equal(0, calls);
    }
    /// <summary>A source error keeps the actual explanation open and names the stale original report.</summary>
    [Fact]
    public async Task Failed_source_validation_keeps_sheet_open_with_feedback()
    {
        using var f = new FlowFixture(); var port = new NativeClosePort(); var vm = new KpiSheetViewModel(f.Translator, port); var calls = 0;
        Query(vm, () => { calls++; return Task.CompletedTask; }, () => Task.FromException(new InvalidOperationException("D647 refused original source")));
        await vm.ShowEntriesCommand.ExecuteAsync(null); Assert.Equal(0, port.Calls); Assert.Equal(0, calls); Assert.Equal(f.Translator["Report_ExplanationStale"], vm.Feedback); vm.SetActive(false);
    }
    /// <summary>A malformed replacement query clears the previous content and captured native target.</summary>
    [Fact]
    public async Task Missing_sheet_query_clears_previous_target_and_lines()
    {
        using var f = new FlowFixture(); var port = new NativeClosePort(); var vm = new KpiSheetViewModel(f.Translator, port); var calls = 0;
        Query(vm, () => { calls++; return Task.CompletedTask; }); vm.ApplyQueryAttributes(new Dictionary<string, object>());
        await vm.ShowEntriesCommand.ExecuteAsync(null); Assert.False(vm.HasEntries); Assert.Empty(vm.Lines); Assert.Null(vm.Value); Assert.Equal(0, port.Calls); Assert.Equal(0, calls); vm.SetActive(false);
    }
    /// <summary>Two invocations during one actual pending native close do not duplicate navigation.</summary>
    [Fact]
    public async Task Pending_native_close_serializes_repeated_entries_invocations()
    {
        using var f = new FlowFixture(); var port = new NativeClosePort(); var vm = new KpiSheetViewModel(f.Translator, port); var calls = 0;
        var held = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); port.Close = () => held.Task;
        Query(vm, () => { calls++; return Task.CompletedTask; }); var pending = vm.ShowEntriesCommand.ExecuteAsync(null);
        await vm.ShowEntriesCommand.ExecuteAsync(null); Assert.Equal(1, port.Calls); held.SetResult(); await pending; Assert.Equal(1, calls); vm.SetActive(false);
    }

}
