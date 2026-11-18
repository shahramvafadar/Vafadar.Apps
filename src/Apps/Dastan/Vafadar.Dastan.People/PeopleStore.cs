using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Vafadar.Core.Security;

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

/// <summary>What an account is, for the page of its visitor.</summary>
/// <param name="Id">The identifier.</param>
/// <param name="Name">The name.</param>
/// <param name="CreatedAt">When it was made.</param>
/// <param name="RecoveryCodesLeft">How many recovery codes are not used yet.</param>
public sealed record VisitorInfo(string Id, string Name, DateTime CreatedAt, int RecoveryCodesLeft);

/// <summary>
/// The store of the visitors (D-050): a database of its own beside the site, apart from the store of the
/// editors. This part holds the accounts (D-048): a name and what is kept of a password – nothing else about the
/// person, no mail address. An account is made by anybody (D-049) and closed by its visitor; a closed account is
/// deleted for good after <see cref="KeptAfterClosing"/> (D-055).
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

    // Compared against when no account has the name, so that a wrong name takes as long as a wrong password.
    private static readonly string Nobody = PasswordHash.Derive(Guid.NewGuid().ToString("N"));

    // The letters of a recovery code: no 0, O, 1, I or L, which are read for each other.
    private const string CodeLetters = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    private readonly PeopleContext _context;
    private readonly TimeProvider _time;

    private PeopleStore(PeopleContext context, TimeProvider time) => (_context, _time) = (context, time);

    /// <summary>Opens the store in a file, making or bringing it to the form this program knows.</summary>
    public static PeopleStore Open(string file, TimeProvider? time = null, bool migrate = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(file);
        var path = Path.GetFullPath(file);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Pooling is off: the file of a store is released when the store is closed, not when the process ends.
        var context = new PeopleContext(new DbContextOptionsBuilder<PeopleContext>().UseSqlite($"Data Source={path};Pooling=False").Options);
        if (migrate)
        {
            context.Database.Migrate();
        }

        return new PeopleStore(context, time ?? TimeProvider.System);
    }

    /// <inheritdoc />
    public void Dispose() => _context.Dispose();

    /// <summary>
    /// Makes an account. The name is free to choose and belongs to one account; the password has at least
    /// <see cref="PasswordLength"/> characters. The account gets recovery codes, which the answer carries once.
    /// Made through an invitation, the account uses the invitation up – and nothing else follows from it.
    /// </summary>
    public AccountResult Register(string name, string password, string? invitation = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(password);
        var written = name.Trim();
        var problems = new List<string>();
        if (NameProblem(written) is { } wrong)
        {
            problems.Add(wrong);
        }

        if (Weak(password) is { } weak)
        {
            problems.Add(weak);
        }

        var key = Key(written);
        if (problems.Count == 0 && _context.Visitors.AsNoTracking().Any(visitor => visitor.NameKey == key))
        {
            problems.Add("taken");
        }

        if (problems.Count > 0)
        {
            return new([.. problems]);
        }

        var visitor = new StoredVisitor
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = written,
            NameKey = key,
            Password = PasswordHash.Derive(password),
            Stamp = NewStamp(),
            CreatedAt = _time.GetUtcNow().UtcDateTime,
        };
        _context.Visitors.Add(visitor);
        var codes = NewCodes(visitor.Id);

        // An invitation that can be used is used up by the account it led to; one that cannot stops nobody
        // from making an account, since anybody may (D-049).
        if (Usable(invitation) is { } invited)
        {
            Use(invited, visitor.Id);
        }

        try
        {
            _context.SaveChanges();
        }
        catch (DbUpdateException)
        {
            // Somebody took the name between the question and the answer.
            _context.ChangeTracker.Clear();
            return new(["taken"]);
        }

        _context.ChangeTracker.Clear();
        return new([]) { Session = new VisitorSession(visitor.Id, visitor.Name, visitor.Stamp), RecoveryCodes = codes };
    }

    /// <summary>
    /// Signs a visitor in: the name and the password of an open account. A wrong password is counted; after
    /// <see cref="FailuresAllowed"/> in a row the account takes none for <see cref="LockedFor"/>. Why a sign-in
    /// was refused is not said – a wrong name, a wrong password, a locked and a closed account answer alike.
    /// </summary>
    public VisitorSession? SignIn(string name, string password)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(password);
        var now = _time.GetUtcNow().UtcDateTime;
        var key = Key(name.Trim());
        var visitor = _context.Visitors.SingleOrDefault(candidate => candidate.NameKey == key);
        var right = PasswordHash.Matches(password, visitor?.Password ?? Nobody);
        try
        {
            if (visitor is null || visitor.ClosedAt is not null || visitor.LockedUntil > now)
            {
                // A locked account does not count further and does not open for the right password either.
                return null;
            }

            if (!right)
            {
                visitor.Failures++;
                if (visitor.Failures >= FailuresAllowed)
                {
                    (visitor.Failures, visitor.LockedUntil) = (0, now + LockedFor);
                }

                _context.SaveChanges();
                return null;
            }

            (visitor.Failures, visitor.LockedUntil) = (0, null);
            _context.SaveChanges();
            return new VisitorSession(visitor.Id, visitor.Name, visitor.Stamp);
        }
        finally
        {
            _context.ChangeTracker.Clear();
        }
    }

    /// <summary>Returns whether a sign-in still holds: the account is open and has the password it was signed in with.</summary>
    public bool Holds(VisitorSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return _context.Visitors.AsNoTracking().Any(visitor => visitor.Id == session.Id && visitor.Stamp == session.Stamp && visitor.ClosedAt == null);
    }

    /// <summary>Returns what an open account is, or <see langword="null"/>.</summary>
    public VisitorInfo? Info(string id)
    {
        var visitor = _context.Visitors.AsNoTracking().SingleOrDefault(candidate => candidate.Id == id && candidate.ClosedAt == null);
        return visitor is null
            ? null
            : new VisitorInfo(visitor.Id, visitor.Name, visitor.CreatedAt, _context.RecoveryCodes.AsNoTracking().Count(code => code.VisitorId == id && code.UsedAt == null));
    }

    /// <summary>
    /// Gives an account another password, proved by the present one. Every sign-in made before ends; the answer
    /// carries the one that holds from now on.
    /// </summary>
    public AccountResult ChangePassword(string id, string current, string password)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(password);
        var visitor = _context.Visitors.SingleOrDefault(candidate => candidate.Id == id && candidate.ClosedAt == null);
        try
        {
            if (visitor is null || !PasswordHash.Matches(current, visitor.Password))
            {
                return new(["wrong"]);
            }

            if (Weak(password) is { } weak)
            {
                return new([weak]);
            }

            (visitor.Password, visitor.Stamp) = (PasswordHash.Derive(password), NewStamp());
            _context.SaveChanges();
            return new([]) { Session = new VisitorSession(visitor.Id, visitor.Name, visitor.Stamp) };
        }
        finally
        {
            _context.ChangeTracker.Clear();
        }
    }

    /// <summary>
    /// Gets into an account whose password is lost: the name, one recovery code and a new password. The code is
    /// used up; every sign-in made before ends. Why it was refused is not said.
    /// </summary>
    public AccountResult Recover(string name, string code, string password)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(password);
        if (Weak(password) is { } weak)
        {
            return new([weak]);
        }

        var now = _time.GetUtcNow().UtcDateTime;
        var key = Key(name.Trim());
        var visitor = _context.Visitors.SingleOrDefault(candidate => candidate.NameKey == key && candidate.ClosedAt == null);
        var derived = DeriveCode(code);
        try
        {
            if (visitor is null || visitor.LockedUntil > now)
            {
                return new(["wrong"]);
            }

            var stored = _context.RecoveryCodes.SingleOrDefault(candidate => candidate.VisitorId == visitor.Id && candidate.Code == derived && candidate.UsedAt == null);
            if (stored is null)
            {
                // A wrong code counts like a wrong password: codes cannot be tried without end.
                visitor.Failures++;
                if (visitor.Failures >= FailuresAllowed)
                {
                    (visitor.Failures, visitor.LockedUntil) = (0, now + LockedFor);
                }

                _context.SaveChanges();
                return new(["wrong"]);
            }

            stored.UsedAt = now;
            (visitor.Password, visitor.Stamp, visitor.Failures, visitor.LockedUntil) = (PasswordHash.Derive(password), NewStamp(), 0, null);
            _context.SaveChanges();
            return new([]) { Session = new VisitorSession(visitor.Id, visitor.Name, visitor.Stamp) };
        }
        finally
        {
            _context.ChangeTracker.Clear();
        }
    }

    /// <summary>Gives an account new recovery codes, proved by its password. The codes it had are worth nothing any more.</summary>
    public AccountResult NewRecoveryCodes(string id, string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        var visitor = _context.Visitors.AsNoTracking().SingleOrDefault(candidate => candidate.Id == id && candidate.ClosedAt == null);
        if (visitor is null || !PasswordHash.Matches(password, visitor.Password))
        {
            return new(["wrong"]);
        }

        _context.RecoveryCodes.RemoveRange(_context.RecoveryCodes.Where(code => code.VisitorId == id));
        var codes = NewCodes(id);
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
        return new([]) { RecoveryCodes = codes };
    }

    /// <summary>
    /// Closes an account, proved by its password. From now on nobody signs in with it and every sign-in ends;
    /// after <see cref="KeptAfterClosing"/> it is deleted for good (<see cref="Purge"/>).
    /// </summary>
    public AccountResult Close(string id, string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        var visitor = _context.Visitors.SingleOrDefault(candidate => candidate.Id == id && candidate.ClosedAt == null);
        try
        {
            if (visitor is null || !PasswordHash.Matches(password, visitor.Password))
            {
                return new(["wrong"]);
            }

            (visitor.ClosedAt, visitor.Stamp) = (_time.GetUtcNow().UtcDateTime, NewStamp());
            _context.SaveChanges();
            return new([]);
        }
        finally
        {
            _context.ChangeTracker.Clear();
        }
    }

    /// <summary>
    /// Opens a closed account again, with its name and password, while it is still kept. Returns the sign-in, or
    /// <see langword="null"/> when there is no such closed account or the password is wrong.
    /// </summary>
    public VisitorSession? Reopen(string name, string password)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(password);
        var key = Key(name.Trim());
        var visitor = _context.Visitors.SingleOrDefault(candidate => candidate.NameKey == key && candidate.ClosedAt != null);
        try
        {
            if (!PasswordHash.Matches(password, visitor?.Password ?? Nobody) || visitor is null)
            {
                return null;
            }

            (visitor.ClosedAt, visitor.Stamp, visitor.Failures, visitor.LockedUntil) = (null, NewStamp(), 0, null);
            _context.SaveChanges();
            return new VisitorSession(visitor.Id, visitor.Name, visitor.Stamp);
        }
        finally
        {
            _context.ChangeTracker.Clear();
        }
    }

    /// <summary>
    /// Deletes for good the accounts that were closed longer ago than <see cref="KeptAfterClosing"/>, with
    /// everything that belongs to them, and returns how many. The name of a deleted account is free again.
    /// </summary>
    public int Purge()
    {
        var before = _time.GetUtcNow().UtcDateTime - KeptAfterClosing;
        var gone = _context.Visitors.Where(visitor => visitor.ClosedAt != null && visitor.ClosedAt < before).ToList();
        _context.Visitors.RemoveRange(gone);
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
        return gone.Count;
    }

    private ImmutableArray<string> NewCodes(string visitorId)
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
    private static string DeriveCode(string code)
    {
        var typed = new string([.. code.Where(char.IsAsciiLetterOrDigit)]).ToUpperInvariant();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(typed)));
    }

    private static string Key(string name) => name.Normalize().ToUpperInvariant();

    private static string NewStamp() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

    private static string? NameProblem(string name) =>
        name.Length < ShortestName || name.Length > LongestName ? "name-length"
        : name.Any(character => !(char.IsLetterOrDigit(character) || character is ' ' or '.' or '_' or '-')) || name.Contains("  ", StringComparison.Ordinal) ? "name-letters"
        : null;

    private static string? Weak(string password) =>
        password.Length < PasswordLength ? "password-short"
        : password.Length > 1000 ? "password-long"
        : null;
}
