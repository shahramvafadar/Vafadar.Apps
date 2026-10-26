using System.Text.Json;
using Microsoft.Data.Sqlite;
using Vafadar.Zanance.Performance;

namespace Vafadar.Zanance.Data.Tests;

/// <summary>Checks content exclusion and fixed diagnostic bounds on the actual native caller's formatter.</summary>
public sealed class StartupFailureRecordsTests
{
    [Fact]
    public void Messages_and_source_paths_never_enter_original_failure_records()
    {
        var error = new FixedStackError("PRIVATE_MESSAGE_SENTINEL",
            "   at Public.Code.Prepare(System.String value) in C:\\PRIVATE_SOURCE_SENTINEL.cs:line 12");
        var result = StartupFailureRecords.Capture(error);
        var record = Assert.Single(result);
        Assert.Equal("Public.Code.Prepare", Assert.Single(record.Methods));
        var json = JsonSerializer.Serialize(result);
        Assert.DoesNotContain("PRIVATE_MESSAGE_SENTINEL", json);
        Assert.DoesNotContain("PRIVATE_SOURCE_SENTINEL", json);
        Assert.DoesNotContain("System.String value", json);
    }

    [Fact]
    public void Nested_failure_and_method_counts_remain_bounded()
    {
        Exception error = new IOException("PRIVATE_INNER_SENTINEL");
        for (var index = 0; index < 10; index++)
            error = new FixedStackError("PRIVATE_SENTINEL", string.Concat(Enumerable.Repeat("   at Public.Code.Read()\n", 100)), error);
        var result = StartupFailureRecords.Capture(error);
        Assert.Equal(4, result.Count);
        Assert.All(result, record => Assert.Equal(8, record.Methods.Count));
        Assert.DoesNotContain("PRIVATE_SENTINEL", JsonSerializer.Serialize(result));
    }

    [Fact]
    public void Original_sqlite_codes_are_retained_without_the_native_error_message()
    {
        var error = new SqliteException("PRIVATE_SQL_SENTINEL", 19, 787);
        var record = Assert.Single(StartupFailureRecords.Capture(error));
        Assert.Equal(19, record.SqliteCode); Assert.Equal(787, record.SqliteExtendedCode);
        Assert.Equal(error.HResult, record.HResult);
        Assert.DoesNotContain("PRIVATE_SQL_SENTINEL", JsonSerializer.Serialize(record));
    }

    [Fact]
    public void Missing_original_error_is_rejected() => Assert.Throws<ArgumentNullException>(() => StartupFailureRecords.Capture(null!));

    private sealed class FixedStackError(string message, string stack, Exception? inner = null) : Exception(message, inner)
    {
        public override string StackTrace => stack;
    }
}
