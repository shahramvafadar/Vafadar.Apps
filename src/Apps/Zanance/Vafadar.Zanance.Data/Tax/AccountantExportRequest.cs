namespace Vafadar.Zanance.Data.Tax;

/// <summary>Explicit disclosure choices for Germany's data-only accountant packet (D-296).</summary>
public sealed class AccountantExportRequest
{
    /// <summary>Retains a distinct immutable selection; display region never selects tax jurisdiction.</summary>
    public AccountantExportRequest(int year, IEnumerable<Guid> accountIds, bool includeNotes, bool includeDocuments)
    {
        if (year is < 1900 or > 9998) throw new ArgumentOutOfRangeException(nameof(year));
        Year = year;
        AccountIds = Array.AsReadOnly(accountIds.Distinct().Order().ToArray());
        if (AccountIds.Count == 0) throw new ArgumentException("Select at least one account.", nameof(accountIds));
        IncludeNotes = includeNotes; IncludeDocuments = includeDocuments;
    }
    /// <summary>Gets the Gregorian calendar year of stored ledger dates, not a tax recognition rule.</summary>
    public int Year { get; }
    /// <summary>Gets the explicitly selected financial accounts.</summary>
    public IReadOnlyList<Guid> AccountIds { get; }
    /// <summary>Gets whether optional payees, notes and reimbursement names may leave the device.</summary>
    public bool IncludeNotes { get; }
    /// <summary>Gets whether exact stored attachment copies may leave the device.</summary>
    public bool IncludeDocuments { get; }
}

/// <summary>Minimal account identity available for explicit selection; no balances or private service rows.</summary>
public sealed record AccountantAccount(Guid Id, string Name, string Currency, bool Archived);

/// <summary>A reviewed immutable source digest and disclosure counts; never retains attachment blobs.</summary>
public sealed class AccountantExportReview
{
    /// <summary>Captures source identity and immutable disclosure evidence without retaining private blobs.</summary>
    internal AccountantExportReview(AccountantExportRequest request, string path, Commerce.CommercialFileAccess access,
        string digest, int entries, int documents, long documentBytes, int linkedAccounts, DateOnly day)
    { Request = request; Path = path; Access = access; Digest = digest; Entries = entries; Documents = documents;
        DocumentBytes = documentBytes; LinkedAccounts = linkedAccounts; Day = day; }
    /// <summary>Gets the actual opened file for profile-change validation, never portable output.</summary>
    internal string Path { get; }
    /// <summary>Gets the original file-bound cached permission snapshot.</summary>
    internal Commerce.CommercialFileAccess Access { get; }
    /// <summary>Gets the digest of exactly disclosed output members for stale-source refusal.</summary>
    internal string Digest { get; }
    /// <summary>Gets the reviewed local day, independently of the selected historical year.</summary>
    internal DateOnly Day { get; }
    /// <summary>Gets the immutable original disclosure selection.</summary>
    public AccountantExportRequest Request { get; }
    /// <summary>Gets the number of actual ledger rows, counting a transfer once.</summary>
    public int Entries { get; }
    /// <summary>Gets the number of included stored document copies.</summary>
    public int Documents { get; }
    /// <summary>Gets their exact uncompressed byte count.</summary>
    public long DocumentBytes { get; }
    /// <summary>Gets additional counterparty account identities disclosed by complete transfer rows.</summary>
    public int LinkedAccounts { get; }
}
