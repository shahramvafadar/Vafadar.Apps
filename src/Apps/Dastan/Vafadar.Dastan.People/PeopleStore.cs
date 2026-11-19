using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Vafadar.Dastan.People;

/// <summary>A sign-in of a visitor: the account and the stamp its password had when the visitor signed in.</summary>
/// <param name="Id">The identifier of the account.</param>
/// <param name="Name">The name of the account.</param>
/// <param name="Stamp">The stamp of the password.</param>
public sealed record VisitorSession(string Id, string Name, string Stamp);

/// <summary>What became of an act on an account.</summary>
/// <param name="Problems">Why it was not done; empty when it was.</param>
public sealed record AccountResult(ImmutableArray<string> Problems)
{
    /// <summary>Gets a value indicating whether the act was done.</summary>
    public bool Done => Problems.IsEmpty;

    /// <summary>Gets the sign-in the act ends with, when it ends with one.</summary>
    public VisitorSession? Session { get; init; }

    /// <summary>Gets the recovery codes the act made, to be shown once. They are kept nowhere as they are.</summary>
    public ImmutableArray<string> RecoveryCodes { get; init; } = [];
}

/// <summary>
/// The store of the visitors (D-050): what belongs to their accounts in Dastan – trees, photographs, circles,
/// lists, consent – apart from the store of the editors. The accounts themselves, with their mail address and
/// what is kept of a password, are kept by ASP.NET Core Identity and reached through <see cref="Accounts"/>
/// (ADR 0016). An account is made by anybody (D-049) and closed by its visitor; a closed account is deleted
/// for good after <see cref="KeptAfterClosing"/> (D-055).
/// </summary>
public sealed partial class PeopleStore : IDisposable
{
    /// <summary>The least length of a password.</summary>
    public const int PasswordLength = 12;

    /// <summary>The least and the greatest length of a name.</summary>
    public const int ShortestName = 3;

    /// <inheritdoc cref="ShortestName" />
    public const int LongestName = 40;

    /// <summary>How many wrong passwords in a row an account takes.</summary>
    public const int FailuresAllowed = 5;

    /// <summary>How many recovery codes an account gets.</summary>
    public const int RecoveryCodeCount = 8;

    /// <summary>How long an account takes no password after too many wrong ones.</summary>
    public static readonly TimeSpan LockedFor = TimeSpan.FromMinutes(15);

    /// <summary>How long a closed account is kept before it is deleted for good: the time to think again.</summary>
    public static readonly TimeSpan KeptAfterClosing = TimeSpan.FromDays(30);

    // The letters of a recovery code: no 0, O, 1, I or L, which are read for each other.
    private const string CodeLetters = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    private readonly PeopleContext _context;
    private readonly TimeProvider _time;

    private PeopleStore(PeopleContext context, TimeProvider time, string site)
    {
        (_context, _time) = (context, time);
        Accounts = new VisitorAccounts(this, context, time, site);
    }

    /// <summary>Gets the accounts of the visitors as ASP.NET Core Identity keeps them: with a mail address that is confirmed (ADR 0016).</summary>
    public VisitorAccounts Accounts { get; }

    /// <summary>The schema of the database the store lives in (ADR 0015): what belongs to accounts.</summary>
    public const string Schema = "personal";

    /// <summary>
    /// Opens the store in a database of PostgreSQL. The database has the form this program knows already:
    /// bringing it there is an act of its own (<see cref="Migrate"/>), done by who runs the site and not by a
    /// site that starts.
    /// </summary>
    /// <param name="connection">The connection of the database, as Npgsql reads it.</param>
    /// <param name="time">The clock; the system's when nothing is given.</param>
    /// <param name="site">The address of the site, as a link in a mail begins; a development machine's when nothing is given.</param>
    public static PeopleStore Open(string connection, TimeProvider? time = null, string? site = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connection);
        return new PeopleStore(new PeopleContext(Options(connection)), time ?? TimeProvider.System, string.IsNullOrWhiteSpace(site) ? "http://localhost" : site);
    }

    /// <summary>Makes the schema of the store in a database, or brings it to the form this program knows.</summary>
    public static void Migrate(string connection)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connection);
        using var context = new PeopleContext(Options(connection));
        context.Database.Migrate();
    }

    /// <summary>Returns the changes of form a database still lacks; none for one this program can work on.</summary>
    public static IReadOnlyList<string> Pending(string connection)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connection);
        using var context = new PeopleContext(Options(connection));
        return [.. context.Database.GetPendingMigrations()];
    }

    // The history of the changes of form lies in the schema of the store, so that each part of the database
    // keeps its own (ADR 0015).
    internal static DbContextOptions<PeopleContext> Options(string connection) =>
        new DbContextOptionsBuilder<PeopleContext>().UseNpgsql(connection, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", Schema)).Options;

    /// <inheritdoc />
    public void Dispose() => _context.Dispose();

    /// <summary>Returns whether a sign-in still holds: the account is open and nothing changed how it is entered since.</summary>
    public bool Holds(VisitorSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return _context.Visitors.AsNoTracking().Any(visitor => visitor.Id == session.Id && visitor.Stamp == session.Stamp && visitor.ClosedAt == null);
    }

    /// <summary>
    /// Deletes for good the accounts that were closed longer ago than <see cref="KeptAfterClosing"/>, with
    /// everything that belongs to them, and returns how many. The name of a deleted account is free again.
    /// </summary>
    public int Purge()
    {
        var before = _time.GetUtcNow().UtcDateTime - KeptAfterClosing;
        var gone = _context.Visitors.Where(visitor => visitor.ClosedAt != null && visitor.ClosedAt < before).ToList();

        // The name goes with the account (D-055) – out of the histories of the trees of others too, where a
        // line says with whom a tree was shared. The line stays; whom it named is not said any more.
        foreach (var name in gone.Select(visitor => visitor.Name))
        {
            _context.TreeChanges
                .Where(change => change.What.StartsWith("member-") && change.Subject == name)
                .ExecuteUpdate(change => change.SetProperty(line => line.Subject, string.Empty));
        }

        // The account goes with what belonged to it: Identity's row, and with it its tokens and its providers.
        var ids = gone.Select(visitor => visitor.Id).ToList();
        _context.Accounts.RemoveRange(_context.Accounts.Where(account => ids.Contains(account.Id)));
        _context.Visitors.RemoveRange(gone);
        ForgetDeclined();
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
        return gone.Count;
    }

    internal ImmutableArray<string> NewCodes(string visitorId)
    {
        var codes = Enumerable.Range(0, RecoveryCodeCount).Select(_ => NewCode()).ToImmutableArray();
        foreach (var code in codes)
        {
            _context.RecoveryCodes.Add(new StoredRecoveryCode { VisitorId = visitorId, Code = DeriveCode(code) });
        }

        return codes;
    }

    // Sixteen letters of thirty-one: about seventy-nine bits, written in four groups to be copied by hand.
    private static string NewCode()
    {
        var letters = Enumerable.Range(0, 16).Select(_ => CodeLetters[RandomNumberGenerator.GetInt32(CodeLetters.Length)]).ToArray();
        return string.Join('-', letters.Chunk(4).Select(group => new string(group)));
    }

    // A code is long and random, so one round of a hash keeps it safely; what is typed is read without its
    // hyphens and spaces and in capital letters.
    internal static string DeriveCode(string code)
    {
        var typed = new string([.. code.Where(char.IsAsciiLetterOrDigit)]).ToUpperInvariant();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(typed)));
    }

    internal static string Key(string name) => name.Normalize().ToUpperInvariant();

    private static string NewStamp() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

    internal static string? NameProblem(string name) =>
        name.Length < ShortestName || name.Length > LongestName ? "name-length"
        : name.Any(character => !(char.IsLetterOrDigit(character) || character is ' ' or '.' or '_' or '-')) || name.Contains("  ", StringComparison.Ordinal) ? "name-letters"
        : null;

    internal static string? Weak(string password) =>
        password.Length < PasswordLength ? "password-short"
        : password.Length > 1000 ? "password-long"
        : null;
}
