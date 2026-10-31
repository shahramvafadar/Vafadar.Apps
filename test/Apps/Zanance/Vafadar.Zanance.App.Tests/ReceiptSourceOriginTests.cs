using System.Globalization;
using System.Resources;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Receipts;
using Vafadar.Zanance.App.Interaction;
namespace Vafadar.Zanance.App.Tests;

/// <summary>Original source choices cannot adopt a page which retired while a native answer was outstanding.</summary>
public sealed class ReceiptSourceOriginTests
{
    private sealed class Port : IAppInteraction
    {
        internal int Choices, Alerts;
        internal Func<Task<string?>> Select = () => Task.FromResult<string?>(null);
        internal Func<Task> Acknowledge = () => Task.CompletedTask;
        public Task<string?> ChooseAsync(string title, string cancel, params string[] actions) { Choices++; return Select(); }
        public Task AlertAsync(string title, string message, string cancel) { Alerts++; return Acknowledge(); }
        public Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) => throw new NotSupportedException();
        public Task<string?> PromptAsync(string title, string message, string accept, string cancel, string? initialValue = null, int maxLength = -1) => throw new NotSupportedException();
        public Task NavigateAsync(string route, IDictionary<string, object>? parameters = null) => throw new NotSupportedException();
        public Task ShowFailureAsync(Exception exception) => throw new NotSupportedException();
    }
    private static Translator Translator()
    {
        var t = new Translator(); t.AddResources(new ResourceManager("Vafadar.Zanance.App.Tests.Resources.AppResources", typeof(FlowFixture).Assembly));
        t.SetCulture(CultureInfo.GetCultureInfo("en")); return t;
    }
    [Theory][InlineData(true)][InlineData(false)]
    public async Task Retired_page_before_selection_never_offers_a_dialog_or_direct_file(bool camera)
    {
        var t = Translator(); var p = new Port { Select = () => Task.FromResult<string?>(t["Receipt_ChooseFile"]) };
        Assert.Equal(ReceiptSourceChoice.Cancelled, await ReceiptSourceChooser.ChooseAsync(t, p, camera, camera, () => false));
        Assert.Equal(0, p.Choices); Assert.Equal(0, p.Alerts);
    }
    [Theory][InlineData(true)][InlineData(false)]
    public async Task Answered_native_source_from_retired_page_cannot_open_a_camera_or_file(bool camera)
    {
        var t = Translator(); var current = true; var p = new Port { Select = () => { current = false; return Task.FromResult<string?>(t[camera ? "Receipt_TakePhoto" : "Receipt_ChooseFile"]); } };
        Assert.Equal(ReceiptSourceChoice.Cancelled, await ReceiptSourceChooser.ChooseAsync(t, p, true, true, () => current));
        Assert.Equal(1, p.Choices); Assert.Equal(0, p.Alerts);
    }
    [Fact]
    public async Task Help_selected_after_retirement_never_opens_late_feedback()
    {
        var t = Translator(); var current = true; var p = new Port();
        p.Select = () => { current = false; return Task.FromResult<string?>(p.Choices == 1 ? t["Receipt_ImageHelp"] : null); };
        Assert.Equal(ReceiptSourceChoice.Cancelled, await ReceiptSourceChooser.ChooseAsync(t, p, true, true, () => current));
        Assert.Equal(0, p.Alerts); Assert.Equal(1, p.Choices);
    }
    [Fact]
    public async Task Page_retired_during_help_never_reopens_the_old_source_sheet()
    {
        var t = Translator(); var current = true; var p = new Port();
        p.Select = () => Task.FromResult<string?>(p.Choices == 1 ? t["Receipt_ImageHelp"] : null);
        p.Acknowledge = () => { current = false; return Task.CompletedTask; };
        Assert.Equal(ReceiptSourceChoice.Cancelled, await ReceiptSourceChooser.ChooseAsync(t, p, true, true, () => current));
        Assert.Equal(1, p.Alerts); Assert.Equal(1, p.Choices);
    }
}
