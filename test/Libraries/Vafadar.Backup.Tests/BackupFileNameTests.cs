namespace Vafadar.Backup.Tests;

public sealed class BackupFileNameTests
{
    [Fact]
    public void Creates_a_sortable_utc_file_name()
    {
        var name = BackupFileName.Create("pro.vafadar.finance", new DateTimeOffset(2026, 9, 25, 16, 30, 5, TimeSpan.FromHours(2)));

        Assert.Equal("pro.vafadar.finance_20260925T143005Z.vbak", name);
    }

    [Fact]
    public void Parses_names_it_created()
    {
        var createdAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

        Assert.True(BackupFileName.TryParse(BackupFileName.Create("pro.vafadar.finance", createdAt), out var appId, out var parsed));
        Assert.Equal("pro.vafadar.finance", appId);
        Assert.Equal(createdAt, parsed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("notes.txt")]
    [InlineData("pro.vafadar.finance.vbak")]
    [InlineData("pro.vafadar.finance_yesterday.vbak")]
    [InlineData("_20260925T143005Z.vbak")]
    public void Rejects_other_file_names(string? fileName)
    {
        Assert.False(BackupFileName.TryParse(fileName, out _, out _));
    }

    [Fact]
    public void App_ids_with_separators_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => BackupFileName.Create("my_app", DateTimeOffset.UtcNow));
    }
}
