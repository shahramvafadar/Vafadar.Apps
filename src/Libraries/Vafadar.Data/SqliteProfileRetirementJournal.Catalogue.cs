using Microsoft.Data.Sqlite;

namespace Vafadar.Data;

public sealed partial class SqliteProfileRetirementJournal
{
    private const string CatalogueSchema = "CREATE TABLE retirement_catalogue (id INTEGER PRIMARY KEY CHECK(id=1), original TEXT NOT NULL, removed TEXT NOT NULL)";

    /// <summary>Durably captures immutable catalogue digests while this exact reviewed request remains Authorized.</summary>
    public async Task CaptureCatalogueAsync(ProfileRetirementCatalogueDigest digest, IDatabaseGenerationFileDurability durability,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(digest); ArgumentNullException.ThrowIfNull(durability);
        await using (var connection = await OpenAsync(false, ct).ConfigureAwait(false))
        {
            using var transaction = connection.BeginTransaction(deferred: false);
            if ((await ReadCoreAsync(connection, transaction, ct).ConfigureAwait(false)).Phase != ProfileRetirementPhase.Authorized)
                throw new InvalidOperationException("Catalogue capture must precede removal.");
            using var command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = "SELECT count(*) FROM sqlite_schema WHERE name='retirement_catalogue'";
            if (Convert.ToInt64(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) == 0)
            {
                command.CommandText = CatalogueSchema + "; INSERT INTO retirement_catalogue VALUES(1,$original,$removed)";
                command.Parameters.AddWithValue("$original", digest.Original); command.Parameters.AddWithValue("$removed", digest.Removed);
                await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
            if (await ReadCatalogueCoreAsync(connection, transaction, ct).ConfigureAwait(false) != digest)
                throw new IOException("Retirement catalogue capture differs and cannot be replaced.");
            Check(ct); transaction.Commit();
        }
        await durability.SynchronizeFileAsync(path, ct).ConfigureAwait(false);
        await durability.SynchronizeDirectoryAsync(ct).ConfigureAwait(false); Check(ct);
        if (await ReadCatalogueAsync(ct).ConfigureAwait(false) != digest) throw new IOException("Retirement catalogue readback differs.");
    }

    /// <summary>Reads existing exact catalogue authority; absence never authorizes a preference mutation.</summary>
    public async Task<ProfileRetirementCatalogueDigest> ReadCatalogueAsync(CancellationToken ct = default)
    {
        await using var connection = await OpenAsync(false, ct).ConfigureAwait(false);
        using var transaction = connection.BeginTransaction(deferred: true);
        return await ReadCatalogueCoreAsync(connection, transaction, ct).ConfigureAwait(false);
    }
    private async Task<ProfileRetirementCatalogueDigest> ReadCatalogueCoreAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken ct)
    {
        Check(ct); using var command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = "SELECT sql FROM sqlite_schema WHERE name='retirement_catalogue'";
        if (Convert.ToString(await command.ExecuteScalarAsync(ct).ConfigureAwait(false)) != CatalogueSchema)
            throw new IOException("Retirement catalogue authority is absent or modified.");
        command.CommandText = "SELECT id,original,removed FROM retirement_catalogue";
        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (!await reader.ReadAsync(ct).ConfigureAwait(false) || reader.GetFieldType(0) != typeof(long) || reader.GetInt64(0) != 1
            || reader.GetFieldType(1) != typeof(string) || reader.GetFieldType(2) != typeof(string))
            throw new IOException("Retirement catalogue row is absent or incorrectly typed.");
        ProfileRetirementCatalogueDigest result;
        try { result = new(reader.GetString(1), reader.GetString(2)); }
        catch (ArgumentException error) { throw new IOException("Retirement catalogue fingerprints differ.", error); }
        if (await reader.ReadAsync(ct).ConfigureAwait(false)) throw new IOException("Retirement catalogue row is not unique.");
        Check(ct); return result;
    }
}
