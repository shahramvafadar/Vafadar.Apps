using Vafadar.Testing;

namespace Vafadar.Localization.Tests;

/// <summary>
/// The languages of a project's texts are declared per folder tree (ADR 0011). These tests build small repositories in
/// a temporary folder, so they do not depend on the apps that exist today.
/// </summary>
public sealed class ProjectLanguagesTests : IDisposable
{
    private const string SixLanguages = "en;fa;de;es;fr;it";

    private readonly TemporaryDirectory _repository = new();

    public void Dispose() => _repository.Dispose();

    [Fact]
    public void A_project_without_its_own_declaration_has_the_languages_of_the_root()
    {
        Declare(string.Empty, SixLanguages);
        var resource = Write("src/Libraries/Shared/Resources/Texts.resx");

        Assert.Equal(new[] { "en", "fa", "de", "es", "fr", "it" }, ProjectLanguages.Of(resource, _repository.Path));
    }

    [Fact]
    public void An_app_declares_its_own_languages()
    {
        Declare(string.Empty, SixLanguages);
        Declare("src/Apps/Small", "en;fa");
        var resource = Write("src/Apps/Small/Small.Web/Resources/Texts.resx");

        Assert.Equal(new[] { "en", "fa" }, ProjectLanguages.Of(resource, _repository.Path));
    }

    [Fact]
    public void The_declaration_of_one_app_does_not_reach_another()
    {
        Declare(string.Empty, SixLanguages);
        Declare("src/Apps/Small", "en;fa");
        var other = Write("src/Apps/Large/Large.App/Resources/Texts.resx");

        Assert.Equal(6, ProjectLanguages.Of(other, _repository.Path).Count);
    }

    [Fact]
    public void A_folder_file_without_the_property_is_passed_over()
    {
        Declare(string.Empty, SixLanguages);
        Write("src/Directory.Build.props", "<Project><PropertyGroup><IsTestProject>false</IsTestProject></PropertyGroup></Project>");
        var resource = Write("src/Libraries/Shared/Texts.resx");

        Assert.Equal(6, ProjectLanguages.Of(resource, _repository.Path).Count);
    }

    [Fact]
    public void Spaces_and_empty_entries_in_a_declaration_are_ignored()
    {
        Declare(string.Empty, " en ; fa ;; ");

        Assert.Equal(new[] { "en", "fa" }, ProjectLanguages.Of(_repository.Path, _repository.Path));
    }

    [Fact]
    public void A_declaration_must_start_with_english()
    {
        Declare(string.Empty, "fa;en");

        Assert.Throws<InvalidOperationException>(() => ProjectLanguages.Of(_repository.Path, _repository.Path));
    }

    [Fact]
    public void A_missing_declaration_is_an_error()
    {
        var resource = Write("src/Apps/Small/Texts.resx");

        Assert.Throws<InvalidOperationException>(() => ProjectLanguages.Of(resource, _repository.Path));
    }

    [Fact]
    public void A_two_language_app_needs_one_translation_per_resource_file()
    {
        Declare(string.Empty, SixLanguages);
        Declare("src/Apps/Small", "en;fa");
        Write("src/Apps/Small/Small.Web/Resources/Texts.resx");
        Write("src/Apps/Small/Small.Web/Resources/Texts.fa.resx");

        var expected = ResourceCompletenessTests.ExpectedTranslations(_repository.Path);

        Assert.Equal(new[] { (Path.Combine("src", "Apps", "Small", "Small.Web", "Resources", "Texts.resx"), "fa") }, expected);
    }

    [Fact]
    public void A_missing_translation_of_a_declared_language_is_still_expected()
    {
        Declare(string.Empty, SixLanguages);
        Declare("src/Apps/Small", "en;fa");
        Write("src/Apps/Small/Small.Web/Resources/Texts.resx");

        var (neutralFile, culture) = Assert.Single(ResourceCompletenessTests.ExpectedTranslations(_repository.Path));

        // The completeness theory then fails for this pair, because the file it names does not exist.
        Assert.Equal("fa", culture);
        Assert.False(File.Exists(_repository.Combine(Path.ChangeExtension(neutralFile, ".fa.resx"))));
    }

    [Fact]
    public void Apps_with_different_languages_are_checked_side_by_side()
    {
        Declare(string.Empty, SixLanguages);
        Declare("src/Apps/Small", "en;fa");
        Write("src/Apps/Small/Small.Web/Texts.resx");
        Write("src/Apps/Large/Large.App/Texts.resx");

        var cultures = ResourceCompletenessTests.ExpectedTranslations(_repository.Path)
            .ToLookup(expected => expected.NeutralFile.Contains("Small", StringComparison.Ordinal), expected => expected.Culture);

        Assert.Equal(new[] { "fa" }, cultures[true]);
        Assert.Equal(new[] { "fa", "de", "es", "fr", "it" }, cultures[false]);
    }

    private void Declare(string folder, string languages) =>
        Write(
            Path.Combine(folder, "Directory.Build.props"),
            $"<Project><PropertyGroup><VafadarLanguages>{languages}</VafadarLanguages></PropertyGroup></Project>");

    private string Write(string relativePath, string content = "<root />")
    {
        var path = _repository.Combine(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }
}
