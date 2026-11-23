using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.Data.Tests;

/// <summary>
/// The library of the repository – the collections written to be published (delivery plan, 25.1): every file
/// of it is read exactly as the site reads it, so that a file the site cannot use, or one a template left
/// unfinished, fails the build and not a visitor.
/// </summary>
public sealed class LibraryTests
{
    private static string Library => Path.Combine(SampleFiles.App, "library");

    [Fact]
    public void TheLibraryHasTheFoldersOfAContentRoot()
    {
        Assert.True(ContentCheck.IsContentRoot(Library), "library/ holds samples, comparisons and media, as content/ does.");
        Assert.True(ContentCheck.IsContentRoot(SampleFiles.Content));
    }

    [Fact]
    public void EveryFileOfTheLibraryIsOneTheSiteCanUseAndIsFinished()
    {
        var reports = ContentCheck.FilesOf(Library).Select(ContentCheck.File).ToList();

        var problems = reports.SelectMany(report => report.Problems).ToList();
        Assert.True(problems.Count == 0, "The library holds files the site cannot use:\n" + string.Join('\n', problems));

        // A notice is a place a template left to be filled: such a collection is not finished.
        var notices = reports.SelectMany(report => report.Notices).ToList();
        Assert.True(notices.Count == 0, "The library holds files that are not finished:\n" + string.Join('\n', notices));
    }

    [Fact]
    public void NothingOfTheLibraryIsReleasedByWhoWroteIt()
    {
        // Reviewing and releasing are the editors' (ADR 0009): a file of the library states neither.
        foreach (var file in ContentCheck.FilesOf(Path.Combine(Library, "samples")))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("\"publicReleaseApproved\": true", text, StringComparison.Ordinal);
            Assert.DoesNotContain("\"publicReleaseApproved\":true", text, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("Dockerfile.dockerignore")]
    [InlineData("Dockerfile.studio.dockerignore")]
    public void TheLibraryIsInNoImage(string file)
    {
        var lines = File.ReadAllLines(Path.Combine(SampleFiles.App, file)).Select(line => line.Trim()).ToList();

        Assert.Contains("src/Apps/Dastan/library", lines);
        Assert.Contains("src/Apps/Dastan/content", lines);
    }

    [Fact]
    public void TheCommandCallsAnEmptyContentRootInOrderAndAnEmptyFolderNot()
    {
        var folder = Path.Combine(Path.GetTempPath(), "dastan-empty-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            using var refused = new StringWriter();
            Assert.Equal(1, Vafadar.Dastan.Content.ContentCommands.Run(["check", folder], refused, new DateOnly(2026, 11, 23)));
            Assert.Contains("holds no content file", refused.ToString(), StringComparison.Ordinal);

            foreach (var name in new[] { "samples", "comparisons", "media" })
            {
                Directory.CreateDirectory(Path.Combine(folder, name));
            }

            using var output = new StringWriter();
            Assert.Equal(0, Vafadar.Dastan.Content.ContentCommands.Run(["check", folder], output, new DateOnly(2026, 11, 23)));
            Assert.Contains("In order, and empty", output.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void AFolderWithoutTheThreeFoldersIsNoContentRoot()
    {
        var folder = Path.Combine(Path.GetTempPath(), "dastan-root-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(folder, "samples"));
        try
        {
            Assert.False(ContentCheck.IsContentRoot(folder));
            Directory.CreateDirectory(Path.Combine(folder, "comparisons"));
            Directory.CreateDirectory(Path.Combine(folder, "media"));
            Assert.True(ContentCheck.IsContentRoot(folder));
            Assert.Empty(ContentCheck.FilesOf(folder));
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
