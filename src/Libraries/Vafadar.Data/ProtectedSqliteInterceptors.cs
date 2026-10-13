using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Vafadar.Data;

/// <summary>Refuses missing/plaintext files and unreviewed providers before protected EF work.</summary>
internal sealed class ProtectedSqliteConnectionInterceptor(string path, string cipherVersion, Action validate) : DbConnectionInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result)
    { RequireFile(); return result; }
    /// <inheritdoc />
    public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection, ConnectionEventData eventData,
        InterceptionResult result, CancellationToken cancellationToken = default)
    { cancellationToken.ThrowIfCancellationRequested(); RequireFile(); return ValueTask.FromResult(result); }
    /// <inheritdoc />
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        validate(); using var command = connection.CreateCommand(); command.CommandText = "PRAGMA cipher_version";
        RequireVersion(command.ExecuteScalar());
    }
    /// <inheritdoc />
    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        validate(); using var command = connection.CreateCommand(); command.CommandText = "PRAGMA cipher_version";
        RequireVersion(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)); validate();
    }
    private void RequireFile()
    {
        validate();
        if (!File.Exists(path) || (File.GetAttributes(path) & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0)
            throw new IOException("Captured encrypted file is missing or redirected.");
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var header = new byte[16]; file.ReadExactly(header);
        if (header.AsSpan().SequenceEqual("SQLite format 3\0"u8)) throw new InvalidDataException("Protected access cannot open plaintext.");
    }
    private void RequireVersion(object? value)
    {
        if ((Convert.ToString(value) ?? "").Split(' ')[0] != cipherVersion)
            throw new InvalidDataException("Protected access requires the reviewed cipher provider.");
        validate();
    }
}

/// <summary>Retires already-open protected contexts on every EF command, including same-path key republishing.</summary>
internal sealed class ProtectedSqliteCommandInterceptor(Action validate) : DbCommandInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> NonQueryExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<int> result)
    { validate(); return result; }
    /// <inheritdoc />
    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    { validate(); return result; }
    /// <inheritdoc />
    public override InterceptionResult<object> ScalarExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<object> result)
    { validate(); return result; }
    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    { cancellationToken.ThrowIfCancellationRequested(); validate(); return ValueTask.FromResult(result); }
    /// <inheritdoc />
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
        InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
    { cancellationToken.ThrowIfCancellationRequested(); validate(); return ValueTask.FromResult(result); }
    /// <inheritdoc />
    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData,
        InterceptionResult<object> result, CancellationToken cancellationToken = default)
    { cancellationToken.ThrowIfCancellationRequested(); validate(); return ValueTask.FromResult(result); }
}
