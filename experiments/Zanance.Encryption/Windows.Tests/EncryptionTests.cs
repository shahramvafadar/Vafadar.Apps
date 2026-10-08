using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Zanance.Encryption.Probe;

namespace Zanance.Encryption.Windows.Tests;

/// <summary>Windows-host feasibility against an isolated generated directory, never the app database.</summary>
public sealed class EncryptionTests
{
    /// <summary>Checks encrypted files, sidecars and transformations against a detectable plaintext control.</summary>
    [Fact]
    [Trait("AT", "AT-75")]
    public void EncryptedFilesRejectWrongKeysAndRetainRowsAcrossExportAndRotation()
    {
        var root = Path.Combine(Path.GetTempPath(), "zanance-encryption-proof", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var result = EncryptionProbe.Run(root);
        Assert.True(result.WrongKeyRejected && result.NoKeyRejected && result.CorruptionRejected);
        Assert.True(result.DatabaseMarkerAbsent && result.WalMarkerAbsent && result.ShmMarkerAbsent && result.PlainControlFound);
        Assert.True(result.RotationPassed && result.ExportPassed);
        var output = Environment.GetEnvironmentVariable("ZANANCE_PROOF_RESULT");
        if (!string.IsNullOrEmpty(output)) File.WriteAllText(output, result.ToJson());
    }

    /// <summary>Checks the EF relational provider using a fictitious encrypted entity database.</summary>
    [Fact]
    [Trait("AT", "AT-75")]
    public void EfCoreProviderReadsAndWritesEncryptedSqliteWithoutDefaultBundle()
    {
        SQLitePCL.Batteries_V2.Init();
        var folder = Path.Combine(Path.GetTempPath(), "zanance-encryption-proof", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "orm.sqlite");
        var key = RandomNumberGenerator.GetBytes(32);
        try
        {
            using var connection = EncryptionProbe.Open(path, key);
            var options = new DbContextOptionsBuilder<ProofContext>().UseSqlite(connection).Options;
            using (var context = new ProofContext(options))
            {
                context.Database.EnsureCreated();
                context.Rows.Add(new ProofRow { Amount = 12345, Note = EncryptionProbe.Marker });
                context.SaveChanges();
            }
            using var reopenedConnection = EncryptionProbe.Open(path, key);
            using var reopened = new ProofContext(new DbContextOptionsBuilder<ProofContext>().UseSqlite(reopenedConnection).Options);
            Assert.Equal(12345, reopened.Rows.Single().Amount);
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    private sealed class ProofContext(DbContextOptions<ProofContext> options) : DbContext(options)
    {
        public DbSet<ProofRow> Rows => Set<ProofRow>();
    }
    private sealed class ProofRow
    {
        public int Id { get; set; }
        public long Amount { get; set; }
        public string Note { get; set; } = string.Empty;
    }
}
