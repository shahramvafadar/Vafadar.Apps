using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace Vafadar.Localization.Tests;

public sealed class TranslatorTests
{
    private static readonly ResourceManager TestStrings =
        new("Vafadar.Localization.Tests.Resources.TestStrings", typeof(TranslatorTests).Assembly);

    [Theory]
    [InlineData("en", "Settings")]
    [InlineData("fa", "تنظیمات")]
    [InlineData("de", "Einstellungen")]
    public void Returns_shared_strings_in_the_current_culture(string culture, string expected)
    {
        var translator = new Translator();

        translator.SetCulture(CultureInfo.GetCultureInfo(culture));

        Assert.Equal(expected, translator["Settings_Title"]);
    }

    [Fact]
    public void App_resources_override_shared_strings()
    {
        var translator = new Translator();
        translator.AddResources(TestStrings);
        translator.SetCulture(CultureInfo.GetCultureInfo("en"));

        Assert.Equal("Okay (app override)", translator["Common_Ok"]);
        Assert.Equal("Cancel", translator["Common_Cancel"]);
    }

    [Fact]
    public void Falls_back_to_the_neutral_resource_when_a_translation_is_missing()
    {
        var translator = new Translator();
        translator.AddResources(TestStrings);
        translator.SetCulture(CultureInfo.GetCultureInfo("fa"));

        Assert.Equal("سلام", translator["Greeting"]);
        Assert.Equal("Neutral only", translator["NeutralOnly"]);
    }

    [Fact]
    public void Missing_keys_return_the_key()
    {
        var translator = new Translator();

        Assert.Equal("Does_Not_Exist", translator["Does_Not_Exist"]);
    }

    [Fact]
    public void Format_uses_the_current_culture()
    {
        var translator = new Translator();
        translator.SetCulture(CultureInfo.GetCultureInfo("de"));

        Assert.Equal("Version 1,5", translator.Format("Settings_Version", 1.5m));
    }

    [Fact]
    public void Changing_the_culture_notifies_bindings()
    {
        var translator = new Translator();
        PropertyChangedEventArgs? args = null;
        translator.PropertyChanged += (_, e) => args = e;

        translator.SetCulture(CultureInfo.GetCultureInfo("fa"));

        Assert.NotNull(args);
        Assert.True(string.IsNullOrEmpty(args.PropertyName), "An empty property name refreshes all bindings.");
    }
}
