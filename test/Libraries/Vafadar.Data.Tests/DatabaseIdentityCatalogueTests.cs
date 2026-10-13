using Microsoft.Data.Sqlite;
using Vafadar.Testing;

namespace Vafadar.Data.Tests;

/// <summary>Checks stable committed bootstrap identity and failure without replacement.</summary>
public sealed class DatabaseIdentityCatalogueTests : IDisposable
{
    private readonly TemporaryDirectory directory = new();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private string PathOfCatalogue => directory.Combine("identities.sqlite");
    private SqliteDatabaseIdentityCatalogue Catalogue() => new(PathOfCatalogue, () => { });
    public void Dispose() => directory.Dispose();

    [Fact, Trait("AT", "AT-215")]
    public async Task Interrupted_publication_reuses_staged_identities_instead_of_regenerating_them()
    {
        var other = Guid.NewGuid().ToString("N"); var durability = new InterruptedPublication();
        await Assert.ThrowsAsync<IOException>(() => Catalogue().CreateNewAsync(["", other], durability, Ct));
        Assert.False(File.Exists(PathOfCatalogue)); Assert.True(File.Exists(PathOfCatalogue + ".creating"));
        var staged = await new SqliteDatabaseIdentityCatalogue(PathOfCatalogue + ".creating", () => { }).ReadAsync(Ct);
        await Catalogue().CreateNewAsync(["", other], Ct);
        Assert.Equal(staged, await Catalogue().ReadAsync(Ct)); Assert.False(File.Exists(PathOfCatalogue + ".creating"));
    }

    [Fact, Trait("AT", "AT-215")]
    public async Task Another_profile_set_cannot_reinterpret_or_replace_staged_bootstrap()
    {
        await Assert.ThrowsAsync<IOException>(() => Catalogue().CreateNewAsync(["", Guid.NewGuid().ToString("N")], new InterruptedPublication(), Ct));
        var before = await File.ReadAllBytesAsync(PathOfCatalogue + ".creating", Ct);
        await Assert.ThrowsAsync<IOException>(() => Catalogue().CreateNewAsync([""], Ct));
        Assert.Equal(before, await File.ReadAllBytesAsync(PathOfCatalogue + ".creating", Ct)); Assert.False(File.Exists(PathOfCatalogue));
    }

    private sealed class InterruptedPublication : IDatabaseGenerationFileDurability
    {
        public long AvailableBytes => long.MaxValue;
        public Task SynchronizeFileAsync(string path, CancellationToken ct) => throw new IOException("Fictitious interruption before publication.");
        public Task SynchronizeDirectoryAsync(CancellationToken ct) => throw new IOException("Unexpected publication.");
    }

    [Fact, Trait("AT", "AT-205")]
    public async Task Main_and_existing_profile_bindings_survive_reopen_without_empty_or_shared_identities()
    {
        var other = Guid.NewGuid().ToString("N"); await Catalogue().CreateNewAsync(["", other], Ct);
        var first = await Catalogue().ReadAsync(Ct); var second = await Catalogue().ReadAsync(Ct);
        Assert.Equal(first, second); Assert.Equal(2, first.Count); Assert.Single(first.Select(row => row.InstallationId).Distinct());
        Assert.All(first, row => { Assert.NotEqual(Guid.Empty, row.ProfileId); Assert.NotEqual(row.OriginalGeneration, row.CandidateGeneration);
            Assert.Equal(row.ProfileId, row.InitialCandidateKey.ProfileId); Assert.Equal(row.CandidateGeneration, row.InitialMigration.CandidateGeneration); });
        Assert.Equal(2, first.Select(row => row.ProfileId).Distinct().Count());
        Assert.Equal(first.Single(row => row.LegacyProfileId == other), await Catalogue().RegisterAsync(other, Ct));
    }

    [Fact, Trait("AT", "AT-205")]
    public async Task Explicit_new_profile_registration_retains_original_installation_and_main_binding()
    {
        await Catalogue().CreateNewAsync([""], Ct); var main = Assert.Single(await Catalogue().ReadAsync(Ct));
        var other = Guid.NewGuid().ToString("N"); var registered = await Catalogue().RegisterAsync(other, Ct);
        Assert.Equal(main.InstallationId, registered.InstallationId); Assert.NotEqual(main.ProfileId, registered.ProfileId);
        Assert.Equal(main, (await Catalogue().ReadAsync(Ct)).Single(row => row.LegacyProfileId == ""));
        Assert.Equal(registered, await Catalogue().RegisterAsync(other, Ct));
    }

    [Fact, Trait("AT", "AT-205")]
    public async Task Missing_or_interrupted_catalogue_is_not_recreated_by_read_or_registration()
    {
        await Assert.ThrowsAsync<IOException>(() => Catalogue().ReadAsync(Ct)); Assert.False(File.Exists(PathOfCatalogue));
        await Assert.ThrowsAsync<IOException>(() => Catalogue().RegisterAsync(Guid.NewGuid().ToString("N"), Ct));
        await File.WriteAllBytesAsync(PathOfCatalogue, [], Ct);
        await Assert.ThrowsAsync<IOException>(() => Catalogue().CreateNewAsync([""], Ct)); Assert.Equal(0, new FileInfo(PathOfCatalogue).Length);
    }

    [Fact, Trait("AT", "AT-205")]
    public async Task Corrupt_identity_is_refused_without_regenerating_or_changing_bytes()
    {
        await Catalogue().CreateNewAsync([""], Ct);
        await using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = PathOfCatalogue, Pooling = false }.ToString()))
        {
            await connection.OpenAsync(Ct); using var command = connection.CreateCommand();
            command.CommandText = "UPDATE installation_identity SET installation='00000000000000000000000000000000'"; await command.ExecuteNonQueryAsync(Ct);
        }
        var before = await File.ReadAllBytesAsync(PathOfCatalogue, Ct);
        await Assert.ThrowsAsync<IOException>(() => Catalogue().ReadAsync(Ct));
        Assert.Equal(before, await File.ReadAllBytesAsync(PathOfCatalogue, Ct));
    }

    [Fact, Trait("AT", "AT-205")]
    public async Task Retired_access_or_invalid_legacy_ids_fail_before_bootstrap_reservation()
    {
        var retired = new SqliteDatabaseIdentityCatalogue(PathOfCatalogue, () => throw new InvalidOperationException("Retired fictitious lease."));
        await Assert.ThrowsAsync<InvalidOperationException>(() => retired.CreateNewAsync([""], Ct));
        await Assert.ThrowsAsync<IOException>(() => Catalogue().CreateNewAsync(["", "../foreign"], Ct));
        await Assert.ThrowsAsync<ArgumentException>(() => Catalogue().CreateNewAsync([], Ct));
        await Assert.ThrowsAsync<ArgumentException>(() => Catalogue().CreateNewAsync(["", ""], Ct)); Assert.False(File.Exists(PathOfCatalogue));
    }

    [Fact, Trait("AT", "AT-205")]
    public async Task Blob_identity_cannot_be_reinterpreted_as_canonical_text()
    {
        await Catalogue().CreateNewAsync([""], Ct);
        await using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = PathOfCatalogue, Pooling = false }.ToString()))
        {
            await connection.OpenAsync(Ct); using var command = connection.CreateCommand();
            command.CommandText = "UPDATE installation_identity SET installation=CAST(installation AS BLOB)";
            await command.ExecuteNonQueryAsync(Ct);
        }
        await Assert.ThrowsAsync<IOException>(() => Catalogue().ReadAsync(Ct));
    }
}
