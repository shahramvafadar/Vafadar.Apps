using System.Globalization;
using System.Resources;
using Vafadar.Localization;
using Vafadar.Zanance.App.Features.Receipts;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Original source-selection flow through explicit native interaction ports, without a camera or financial writer.</summary>
[Trait("AT", "AT-171")]
public sealed class ReceiptSourceChooserTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Ordinary_source_choices_do_not_show_help_or_an_extra_confirmation(bool photo)
    {
        var f = Context();
        f.Platform.Choices.Enqueue(f.Translator[photo ? "Receipt_TakePhoto" : "Receipt_ChooseFile"]);
        var actual = await ReceiptSourceChooser.ChooseAsync(f.Translator, f.Platform, true, true);
        Assert.Equal(photo ? ReceiptSourceChoice.Photo : ReceiptSourceChoice.File, actual);
        Assert.Single(f.Platform.OfferedActions);
        Assert.Empty(f.Platform.Alerts);
        Assert.Contains(f.Translator["Receipt_ImageHelp"], f.Platform.OfferedActions[0]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Reading_help_returns_to_the_same_available_sources(bool photo)
    {
        var f = Context();
        f.Platform.Choices.Enqueue(f.Translator["Receipt_ImageHelp"]);
        f.Platform.Choices.Enqueue(f.Translator[photo ? "Receipt_TakePhoto" : "Receipt_ChooseFile"]);
        Assert.Equal(photo ? ReceiptSourceChoice.Photo : ReceiptSourceChoice.File,
            await ReceiptSourceChooser.ChooseAsync(f.Translator, f.Platform, true, true));
        Assert.Equal(f.Platform.OfferedActions[0], f.Platform.OfferedActions[1]);
        Assert.Equal(f.Translator["Receipt_ImageHelpText"], Assert.Single(f.Platform.Alerts).Message);
    }

    [Fact]
    public async Task Cancelling_after_help_does_not_select_a_source()
    {
        var f = Context();
        f.Platform.Choices.Enqueue(f.Translator["Receipt_ImageHelp"]);
        f.Platform.Choices.Enqueue(null);
        Assert.Equal(ReceiptSourceChoice.Cancelled,
            await ReceiptSourceChooser.ChooseAsync(f.Translator, f.Platform, true, true));
        Assert.Single(f.Platform.Alerts);
        Assert.Equal(2, f.Platform.OfferedActions.Count);
    }

    [Fact]
    public async Task Android_help_remains_available_without_a_camera()
    {
        var f = Context();
        f.Platform.Choices.Enqueue(f.Translator["Receipt_ImageHelp"]);
        f.Platform.Choices.Enqueue(f.Translator["Receipt_ChooseFile"]);
        Assert.Equal(ReceiptSourceChoice.File,
            await ReceiptSourceChooser.ChooseAsync(f.Translator, f.Platform, false, true));
        Assert.All(f.Platform.OfferedActions, a => Assert.DoesNotContain(f.Translator["Receipt_TakePhoto"], a));
    }

    [Fact]
    public async Task Non_Android_file_only_route_keeps_its_direct_picker()
    {
        var f = Context();
        Assert.Equal(ReceiptSourceChoice.File,
            await ReceiptSourceChooser.ChooseAsync(f.Translator, f.Platform, false, false));
        Assert.Empty(f.Platform.OfferedActions);
        Assert.Empty(f.Platform.Alerts);
    }

    [Fact]
    public async Task Other_platform_photo_choice_does_not_offer_Android_specific_help()
    {
        var f = Context();
        f.Platform.Choices.Enqueue(f.Translator["Receipt_TakePhoto"]);
        Assert.Equal(ReceiptSourceChoice.Photo,
            await ReceiptSourceChooser.ChooseAsync(f.Translator, f.Platform, true, false));
        Assert.DoesNotContain(f.Translator["Receipt_ImageHelp"], Assert.Single(f.Platform.OfferedActions));
    }

    [Fact]
    public async Task Source_selection_waits_for_help_acknowledgement()
    {
        var f = Context();
        var acknowledged = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Platform.PendingAlert = () => acknowledged.Task;
        f.Platform.Choices.Enqueue(f.Translator["Receipt_ImageHelp"]);
        f.Platform.Choices.Enqueue(f.Translator["Receipt_ChooseFile"]);
        var selection = ReceiptSourceChooser.ChooseAsync(f.Translator, f.Platform, true, true);
        Assert.False(selection.IsCompleted);
        Assert.Single(f.Platform.OfferedActions);
        acknowledged.SetResult();
        Assert.Equal(ReceiptSourceChoice.File, await selection);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("fr")]
    [InlineData("it")]
    public void Every_product_language_has_distinct_complete_reading_help(string language)
    {
        var f = Context(language);
        Assert.NotEqual("Receipt_ImageHelp", f.Translator["Receipt_ImageHelp"]);
        Assert.NotEqual("Receipt_ImageHelpText", f.Translator["Receipt_ImageHelpText"]);
        Assert.Contains("Android", f.Translator["Receipt_ImageHelpText"]);
        Assert.Contains("PDF", f.Translator["Receipt_ImageHelpText"]);
        if (language != "en") Assert.NotEqual(Context().Translator["Receipt_ImageHelpText"], f.Translator["Receipt_ImageHelpText"]);
    }

    private static (Translator Translator, FlowPlatform Platform) Context(string language = "en")
    {
        // These source-choice cases need real resources and native-effect ports, but no database,
        // process-wide culture, digit settings or other mutable financial fixture state.
        var translator = new Translator();
        translator.AddResources(new ResourceManager("Vafadar.Zanance.App.Tests.Resources.AppResources", typeof(FlowFixture).Assembly));
        translator.SetCulture(CultureInfo.GetCultureInfo(language));
        return (translator, new FlowPlatform());
    }
}
