using System.Text;
using Vafadar.Zanance.App.Features.DataFiles;

namespace Vafadar.Zanance.App.Tests;

/// <summary>Actual private filesystem CSV bytes and the original native unoffered/offered boundary.</summary>
public sealed class CsvSharePreparationTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    [Fact]
    public async Task Complete_original_UTF8_BOM_bytes_and_same_minute_offered_copy_are_preserved()
    {
        var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Zanance-D653-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            const string content = "id,title,note\r\n1,هزینه,\"first\nsecond\"\r\n";
            var first = await CsvSharePreparation.CreateAsync(root, "zanance-export-20261030-1200.csv", content, () => { }, Ct);
            first.MarkOffered(); var second = await CsvSharePreparation.CreateAsync(root, "zanance-export-20261030-1200.csv", "different", () => { }, Ct);
            Assert.NotEqual(first.Path, second.Path);
            Assert.Equal(new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(content)), await File.ReadAllBytesAsync(first.Path, Ct));
            second.DiscardUnoffered(); first.DiscardUnoffered(); Assert.True(File.Exists(first.Path)); Assert.False(File.Exists(second.Path));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
    [Fact]
    public async Task Retirement_after_actual_flush_removes_only_the_fresh_unoffered_copy()
    {
        var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Zanance-D653-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var earlier = await CsvSharePreparation.CreateAsync(root, "zanance-export-earlier.csv", "earlier", () => { }, Ct); earlier.MarkOffered();
            var calls = 0;
            await Assert.ThrowsAsync<OperationCanceledException>(() => CsvSharePreparation.CreateAsync(root, "zanance-export-late.csv", "new", () =>
            { if (++calls == 2) throw new OperationCanceledException("Fictitious page retirement after the actual write."); }, Ct));
            Assert.Equal(2, calls); Assert.Equal(new[] { earlier.Path }, Directory.GetFiles(root, "*.csv", SearchOption.AllDirectories));
            Assert.Equal("earlier", await File.ReadAllTextAsync(earlier.Path, Ct));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
    [Theory]
    [InlineData("../zanance-export.csv")][InlineData("zanance-export.csv:other")][InlineData("other.csv")]
    public async Task Nonneutral_or_redirected_name_is_refused_before_any_file_is_created(string name)
    {
        var root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Zanance-D653-test-" + Guid.NewGuid().ToString("N"));
        await Assert.ThrowsAsync<ArgumentException>(() => CsvSharePreparation.CreateAsync(root, name, "text", () => { }, Ct));
        Assert.False(Directory.Exists(root));
    }
}
