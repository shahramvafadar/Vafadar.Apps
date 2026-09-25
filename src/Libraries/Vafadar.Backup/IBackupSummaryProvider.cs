namespace Vafadar.Backup;

/// <summary>
/// Adds a short, non-sensitive summary to backups, e.g. how many accounts and entries they contain, so that a restore
/// can show what the file holds before anything is replaced (BAK-09). Never include names, amounts or notes.
/// </summary>
public interface IBackupSummaryProvider
{
    /// <summary>Returns summary values, e.g. <c>accounts = 3</c>. Keys are stable identifiers, values plain text.</summary>
    Task<IReadOnlyDictionary<string, string>> GetSummaryAsync(CancellationToken cancellationToken);
}
