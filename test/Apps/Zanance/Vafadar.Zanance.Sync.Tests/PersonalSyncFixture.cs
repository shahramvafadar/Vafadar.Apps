using Microsoft.EntityFrameworkCore;
using Npgsql;
using Vafadar.Zanance.Api;

namespace Vafadar.Zanance.Sync.Tests;

public sealed partial class PersonalSyncAcceptanceTests
{
    /// <summary>Prepares an explicitly owned standalone fixture without resetting retained finance or service history.</summary>
    private static async Task PrepareStandaloneFixtureAsync(string connection, string root)
    {
        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.Matches("^zanance_identity_sync_[a-z0-9_]+$", settings.Database ?? "");
        Assert.Contains(settings.Host, new[] { "127.0.0.1", "localhost" });
        Assert.True(Path.IsPathFullyQualified(root));
        const string marker = "Zanance fictional personal sync acceptance v1";
        if (Directory.Exists(root))
            Assert.Equal(marker, await File.ReadAllTextAsync(Path.Combine(root, "fixture.marker"), Ct));
        else
        {
            Directory.CreateDirectory(root);
            await File.WriteAllTextAsync(Path.Combine(root, "fixture.marker"), marker, Ct);
        }
        await using var schema = new ServiceDbContext(new DbContextOptionsBuilder<ServiceDbContext>().UseNpgsql(connection).Options);
        await schema.Database.MigrateAsync(Ct);
        Assert.False(schema.Database.HasPendingModelChanges());
        // This exact isolated database uses an existing non-superuser runtime role, like the original fixture.
        await schema.Database.ExecuteSqlRawAsync("GRANT USAGE ON SCHEMA public TO zanance_runtime; GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO zanance_runtime; GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO zanance_runtime;", Ct);
    }
}
