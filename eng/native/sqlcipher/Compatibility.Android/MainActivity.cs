using Android.App;
using Android.OS;
using Android.Widget;
using Vafadar.SqlCipher.Compatibility;

namespace Vafadar.SqlCipher.Compatibility.Android;

/// <summary>Independent original-schema probe with no access to Zanance storage or credentials.</summary>
[Activity(Label = "Native schema compatibility", MainLauncher = true, Exported = true)]
public sealed class MainActivity : Activity
{
    /// <inheritdoc />
    protected override async void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        var label = new TextView(this) { Text = "Independent fictitious schema check running" };
        SetContentView(label);
        var schemaRunText = Intent?.GetStringExtra("schema_run");
        var resultName = "result.json";
        var errorName = "error.txt";
        var owned = Path.Combine(FilesDir!.AbsolutePath, "native-compatibility");
        var evidence = Path.Combine(owned, "run-" + Guid.NewGuid().ToString("N"));
        try
        {
            if (schemaRunText is not null)
            {
                if (!Guid.TryParseExact(schemaRunText, "N", out var schemaRun) || schemaRun == Guid.Empty)
                    throw new ArgumentException("Schema qualification requires a fresh run identity.");
                resultName = "schema-result-" + schemaRun.ToString("N") + ".json";
                errorName = "schema-error-" + schemaRun.ToString("N") + ".txt";
                evidence = Path.Combine(owned, "run-" + schemaRun.ToString("N"));
                if (File.Exists(Path.Combine(FilesDir.AbsolutePath, resultName)))
                    throw new InvalidOperationException("Preserve the original qualification report.");
            }
            else
            {
                // Existing explicit lifecycle callers retain their original fixed report contract.
                foreach (var name in new[] { resultName, errorName })
                    File.Delete(Path.Combine(FilesDir.AbsolutePath, name));
            }
            var keyPhase = Intent?.GetStringExtra("key_phase");
            var journalPhase = Intent?.GetStringExtra("journal_phase");
            var migrationPhase = Intent?.GetStringExtra("migration_phase");
            if (migrationPhase is not null && (keyPhase is not null || journalPhase is not null || migrationPhase is not ("validated-restore-create" or "validated-restore-reopen" or "retained-preselect-create" or "retained-preselect-reopen" or "retained-preferences-create" or "retained-preferences-reopen" or "recovery-chain-repair" or "recovery-chain-create" or "recovery-chain-reopen" or "recovery-pending-reference" or "alias-loss-future" or "alias-loss-create" or "alias-loss-reopen" or "all-profile-recovery-create" or "all-profile-recovery-reopen" or "recovered-retirement-create" or "recovered-retirement-reopen" or "recovery-retirement-create" or "recovery-retirement-reopen" or "recovery-import" or "recovery-native" or "recovery-create" or "recovery-reopen" or "profile-creation-pending" or "profile-creation-resume" or "profile-retirement-create" or "profile-retirement-reopen" or "retirement-discovery-resume" or "retirement-discovery" or "retirement-negatives" or "retirement-create" or "retirement-reopen" or "restore-session" or "restore-session-reopen" or "fresh-startup-create" or "fresh-startup-reopen" or "fresh-startup-notification" or "startup-create" or "startup-reopen" or "startup-deletion-guard" or "safety-create" or "safety-reopen" or "migration-create" or "migration-key-prepare" or "migration-export-kill" or "migration-check-interruption" or "migration-selected-kill" or "migration-missing-key" or "migration-resume" or "migration-factory" or "restore-staging" or "restore-select" or "restore-selected-resume" or "package-restore" or "package-resume" or "profile-maintenance" or "identity-create" or "identity-reopen" or "bootstrap-intent" or "bootstrap-create" or "bootstrap-reopen" or "package-applied-reopen" or "profile-create-flow" or "profile-create-cold" or "new-profile-create" or "new-profile-resume" or "new-profile-reopen" or "all-profiles-create" or "all-profiles-reopen" or "metadata-journal" or "metadata-history" or "metadata-reopen" or "settings-create" or "settings-reopen")))
                throw new ArgumentException("Unknown or combined independent migration phase.");
            if (journalPhase is not null && (keyPhase is not null || journalPhase is not ("journal-create" or "journal-uncommitted" or "journal-reopen-rollback" or "journal-committed" or "journal-reopen-selected")))
                throw new ArgumentException("Unknown or combined independent journal phase.");
            if (keyPhase is not null && keyPhase is not ("keys-create" or "keys-reopen" or "keys-negative" or "keys-store-create" or "keys-store-reopen" or "keys-store-negative"))
                throw new ArgumentException("Unknown independent key phase.");
            var keyRoot = owned;
            if (keyPhase is not null)
            {
                if (!Guid.TryParseExact(Intent?.GetStringExtra("key_run"), "N", out var keyRun) || keyRun == Guid.Empty)
                    throw new ArgumentException("Independent key check requires a fresh run identity.");
                keyRoot = Path.Combine(owned, "key-proofs", keyRun.ToString("N"));
            }
            var journalRoot = owned;
            if (journalPhase is not null)
            {
                if (!Guid.TryParseExact(Intent?.GetStringExtra("journal_run"), "N", out var journalRun) || journalRun == Guid.Empty)
                    throw new ArgumentException("Independent journal requires fresh run identity.");
                journalRoot = Path.Combine(owned, "journal-proofs", journalRun.ToString("N"));
            }
            var migrationRoot = owned;
            if (migrationPhase is not null)
            {
                if (!Guid.TryParseExact(Intent?.GetStringExtra("migration_run"), "N", out var migrationRun) || migrationRun == Guid.Empty)
                    throw new ArgumentException("Independent migration requires fresh run identity.");
                migrationRoot = Path.Combine(owned, "migration-proofs", migrationRun.ToString("N"));
            }
            var result = migrationPhase is not null
                ? await Task.Run(() => GenerationMigrationCompatibility.RunAsync(migrationRoot, migrationPhase))
                : journalPhase is not null
                ? await Task.Run(() => JournalCompatibility.RunAsync(journalRoot, journalPhase))
                : keyPhase is null
                ? await Task.Run(() => SchemaCompatibility.RunAsync(evidence, owned))
                : await Task.Run(() => AndroidKeyCompatibility.RunAsync(keyRoot, keyPhase));
            await PublishResultAsync(resultName, result, schemaRunText is not null);
            label.Text = "Independent schema check passed";
        }
        catch (Exception error)
        {
            // Report type/stack without a message that might include a library's key-bearing command.
            Directory.CreateDirectory(owned);
            await File.WriteAllTextAsync(Path.Combine(FilesDir.AbsolutePath, errorName), error.GetType().FullName + "\n" + error.StackTrace);
            var result = "{\"Failed\":true}";
            if (!File.Exists(Path.Combine(FilesDir.AbsolutePath, resultName)))
                await PublishResultAsync(resultName, result, schemaRunText is not null);
            label.Text = "Independent schema check failed";
        }
    }

    /// <summary>Publishes a complete identity-bound observation without replacing an earlier result.</summary>
    private async Task PublishResultAsync(string name, string result, bool identityBound)
    {
        var path = Path.Combine(FilesDir!.AbsolutePath, name);
        if (!identityBound)
        {
            await File.WriteAllTextAsync(path, result);
            return;
        }
        var staging = path + ".tmp";
        await using (var output = new FileStream(staging, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            await output.WriteAsync(System.Text.Encoding.UTF8.GetBytes(result));
        File.Move(staging, path, overwrite: false);
    }
}
