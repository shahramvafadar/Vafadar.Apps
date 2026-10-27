using System.Globalization;
using System.Resources;
using Vafadar.Localization;
using Vafadar.Zanance.PhysicalReaderGestures;

namespace Vafadar.Zanance.App.Tests;

/// <summary>AT-579: the actual reader whitelist admits only original nonfinancial category captions.</summary>
[Trait("AT", "AT-579")]
public sealed class PhysicalReaderCategoryCaptionTests
{
    private static readonly ResourceManager App = new("Vafadar.Zanance.App.Tests.Resources.AppResources", typeof(PhysicalReaderCategoryCaptionTests).Assembly);
    private static readonly ResourceManager Shared = new("Vafadar.Localization.Resources.SharedStrings", typeof(Translator).Assembly);

    /// <summary>Uses real bundled translations without changing global culture or creating native controls.</summary>
    [Theory]
    [InlineData("en")]
    [InlineData("fa")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("fr")]
    [InlineData("it")]
    public void Original_category_states_are_allowed_but_financial_save_is_not(string language)
    {
        var culture = CultureInfo.GetCultureInfo(language);
        string Read(string key) => App.GetString(key, culture) ?? Shared.GetString(key, culture)
            ?? throw new InvalidOperationException("Original resource missing.");
        var captions = PhysicalReaderCaptions.FromOriginalResources(Read);
        var food = Read("Category_Food");
        var housing = Read("Category_Housing");
        Assert.Contains(food, captions);
        Assert.Contains(housing, captions);
        Assert.Contains(string.Format(culture, Read("Common_ChipSelected"), food), captions);
        Assert.Contains(string.Format(culture, Read("Common_ChipSelected"), housing), captions);
        var cancel = Read("Common_Cancel");
        if (new[] { "Common_Save", "Common_Delete", "Common_Undo" }.Any(key => Read(key) == cancel))
            Assert.DoesNotContain(cancel, captions);
        else Assert.Contains(cancel, captions);
        Assert.DoesNotContain(Read("Common_Save"), captions);
        Assert.DoesNotContain(Read("Common_Delete"), captions);
        Assert.DoesNotContain(Read("Common_Undo"), captions);
        Assert.DoesNotContain("arbitrary account or command", captions);
    }

    /// <summary>A damaged format must refuse rather than silently broadening the gesture whitelist.</summary>
    [Theory]
    [InlineData("{0}")]
    [InlineData("{0} {0}")]
    [InlineData("{1}, selected")]
    public void Missing_distinct_or_ambiguous_selected_state_is_refused(string format)
    {
        string Read(string key) => key == "Common_ChipSelected" ? format : "original caption";
        Assert.Throws<InvalidOperationException>(() => PhysicalReaderCaptions.FromOriginalResources(Read));
    }

    /// <summary>Updating the finite cohort cannot inherit the old candidate's installed-file admission.</summary>
    [Fact]
    public void Current_installed_category_candidate_is_required()
    {
        var device = new PhysicalReaderDevice("samsung", "SM-A176B", 36, "arm64-v8a", 450);
        var install = new ReaderOwnerInstall(10380, false, "/data/app/fictitious/pro.vafadar.zanance/base.apk", 100);
        Assert.True(PhysicalReaderPolicy.Admits(device, install,
            "f7488d1da0bbed5b9910a7ace960ef5f883da008f31dd24e173e9421996ae9c7"));
        Assert.False(PhysicalReaderPolicy.Admits(device, install,
            "510a3197cb6d77bf69df03d10f3d4079286726a82fecce557f8a4bebeef3b879"));
        Assert.False(PhysicalReaderPolicy.Admits(device, install,
            "4a6cb5be5effa30cf02ce74600774b84d777e71de29463ea6db4fac52117d38c"));
    }
}
