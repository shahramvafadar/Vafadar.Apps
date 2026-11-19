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
public sealed record VisitorInfo(string Id, string Name, DateTime CreatedAt, int RecoveryCodesLeft)
{
    /// <summary>Gets a value indicating whether the account has a password. One that was made through a provider has none until its visitor sets one.</summary>
    public bool HasPassword { get; init; } = true;

    /// <summary>Gets the providers the account can be signed in through.</summary>
    public ImmutableArray<string> Providers { get; init; } = [];
}

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

    /// <summary>The schema of the database the store lives in (ADR 0015): what belongs to accounts.</summary>
    public const string Schema = "personal";

    /// <summary>
    /// Opens the store in a database of PostgreSQL. The database has the form this program knows already:
    /// bringing it there is an act of its own (<see cref="Migrate"/>), done by who runs the site and not by a
    /// site that starts.
    /// </summary>
    /// <param name="connection">The connection of the database, as Npgsql reads it.</param>
    /// <param name="time">The clock; the system's when nothing is given.</param>
    public static PeopleStore Open(string connection, TimeProvider? time = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connection);
        return new PeopleStore(new PeopleContext(Options(connection)), time ?? TimeProvider.System);
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

    /// <summary>
    /// Makes an account. The name is free to choose and belongs to one account; the password has at least
    /// <see cref="PasswordLength"/> characters. The account gets recovery codes, which the answer carries once.
    /// Made through an invitation, the account uses the invitation up – and nothing else follows from it.
    /// </summary>
    /// <param name="name">The name of the account.</param>
    /// <param name="password">Its password.</param>
    /// <param name="invitation">The invitation it is made through, if any.</param>
    /// <param name="invitationNeeded">Whether no account is made without an invitation that can be used (a closed trial, D-068).</param>
    /// <param name="terms">The version of the terms and the privacy notice the visitor said they read, when the site asks.</param>
    public AccountResult Register(string name, string password, string? invitation = null, bool invitationNeeded = false, string? terms = null)
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

        return Create(written, PasswordHash.Derive(password), invitation, null, invitationNeeded, terms);
    }

    // Makes the account of a name that was looked at: with what is kept of a password, or without a password
    // and with the sign-in of a provider instead.
    private AccountResult Create(string written, string password, string? invitation, StoredExternalLogin? login, bool invitationNeeded, string? terms)
    {
        var key = Key(written);
        if (_context.Visitors.AsNoTracking().Any(candidate => candidate.NameKey == key))
        {
            return new(["taken"]);
        }

        var invited = Usable(invitation)?.Id;
        _context.ChangeTracker.Clear();
        if (invitationNeeded && invited is null)
        {
            return new(["invitation-needed"]);
        }

        // The account and the invitation it uses up are one step: of two forms that carry the same
        // invitation at the same moment, one makes an account.
        using var step = _context.Database.BeginTransaction();

        var visitor = new StoredVisitor
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = written,
            NameKey = key,
            Password = password,
            Stamp = NewStamp(),
            CreatedAt = _time.GetUtcNow().UtcDateTime,
            TermsVersion = terms is { Length: > 0 and <= 20 } ? terms : null,
            TermsAcceptedAt = terms is { Length: > 0 and <= 20 } ? _time.GetUtcNow().UtcDateTime : null,
        };
        _context.Visitors.Add(visitor);
        if (login is not null)
        {
            login.VisitorId = visitor.Id;
            _context.ExternalLogins.Add(login);
        }

        var codes = NewCodes(visitor.Id);
        try
        {
            _context.SaveChanges();
        }
        catch (DbUpdateException)
        {
            // Somebody took the name – or the sign-in of the provider – between the question and the answer.
            _context.ChangeTracker.Clear();
            return new([login is not null && _context.ExternalLogins.AsNoTracking().Any(candidate => candidate.Provider == login.Provider && candidate.Key == login.Key) ? "login-taken" : "taken"]);
        }

        _context.ChangeTracker.Clear();

        // An invitation that can be used is used up by the account it led to – asked of the store in one
        // sentence, so that it is used once. One that cannot be used stops nobody from making an account
        // where anybody may (D-049); where an invitation is needed, no account is made.
        if (invited is not null)
        {
            var today = Today;
            var used = _context.Invitations
                .Where(candidate => candidate.Id == invited && candidate.UsedOn == null && candidate.RevokedAt == null)
                .ExecuteUpdate(change => change.SetProperty(candidate => candidate.UsedBy, visitor.Id).SetProperty(candidate => candidate.UsedOn, today));
            if (used == 0 && invitationNeeded)
            {
                step.Rollback();
                return new(["invitation-needed"]);
            }
        }

        step.Commit();
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
        var visitor = _context.Visitors.AsNoTracking().SingleOrDefault(candidate => candidate.NameKey == key && candidate.ClosedAt == null);
        if (!Proves(visitor, password, now))
        {
            return null;
        }

        return new VisitorSession(visitor!.Id, visitor.Name, visitor.Stamp);
    }

    /// <summary>
    /// Returns whether a password is the password of an account – the one proof every act asks for that needs
    /// the password: signing in, another password, new recovery codes, a copy of the data, closing, opening
    /// again. A wrong password is counted wherever it is tried, and after <see cref="FailuresAllowed"/> in a
    /// row the account takes none for <see cref="LockedFor"/>: a locked account does not count further and
    /// does not open for the right password either. The count is kept by the store in one sentence, so that
    /// tries made at the same moment are all counted. Nobody, and an account without a password, take as long
    /// to refuse as a wrong password.
    /// </summary>
    private bool Proves(StoredVisitor? visitor, string password, DateTime now)
    {
        var has = visitor is { Password.Length: > 0 };
        var right = PasswordHash.Matches(password, has ? visitor!.Password : Nobody) && has;
        if (visitor is null || visitor.LockedUntil > now)
        {
            return false;
        }

        if (!right)
        {
            if (has)
            {
                Miss(visitor.Id, now);
            }

            return false;
        }

        _context.Visitors
            .Where(candidate => candidate.Id == visitor.Id && (candidate.Failures != 0 || candidate.LockedUntil != null))
            .ExecuteUpdate(change => change.SetProperty(candidate => candidate.Failures, 0).SetProperty(candidate => candidate.LockedUntil, (DateTime?)null));
        return true;
    }

    // Counts a wrong password or a wrong recovery code, and locks the account at the last one that is allowed.
    private void Miss(string id, DateTime now)
    {
        var until = now + LockedFor;
        _context.Visitors.Where(candidate => candidate.Id == id).ExecuteUpdate(change => change.SetProperty(candidate => candidate.Failures, candidate => candidate.Failures + 1));
        _context.Visitors
            .Where(candidate => candidate.Id == id && candidate.Failures >= FailuresAllowed)
            .ExecuteUpdate(change => change.SetProperty(candidate => candidate.Failures, 0).SetProperty(candidate => candidate.LockedUntil, (DateTime?)until));
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
            : new VisitorInfo(visitor.Id, visitor.Name, visitor.CreatedAt, _context.RecoveryCodes.AsNoTracking().Count(code => code.VisitorId == id && code.UsedAt == null))
            {
                HasPassword = visitor.Password.Length > 0,
                Providers = [.. _context.ExternalLogins.AsNoTracking().Where(login => login.VisitorId == id).Select(login => login.Provider).OrderBy(provider => provider)],
            };
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
            if (!Proves(visitor, current, _time.GetUtcNow().UtcDateTime))
            {
                return new(["wrong"]);
            }

            if (Weak(password) is { } weak)
            {
                return new([weak]);
            }

            (visitor!.Password, visitor.Stamp) = (PasswordHash.Derive(password), NewStamp());
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

            // The code is used up in one sentence of the store: of two forms with the same code, one gets in.
            var used = _context.RecoveryCodes
                .Where(candidate => candidate.VisitorId == visitor.Id && candidate.Code == derived && candidate.UsedAt == null)
                .ExecuteUpdate(change => change.SetProperty(candidate => candidate.UsedAt, (DateTime?)now));
            if (used == 0)
            {
                // A wrong code counts like a wrong password: codes cannot be tried without end.
                Miss(visitor.Id, now);
                return new(["wrong"]);
            }

            (visitor.Password, visitor.Stamp) = (PasswordHash.Derive(password), NewStamp());
            _context.SaveChanges();
            _context.Visitors.Where(candidate => candidate.Id == visitor.Id)
                .ExecuteUpdate(change => change.SetProperty(candidate => candidate.Failures, 0).SetProperty(candidate => candidate.LockedUntil, (DateTime?)null));
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
        if (!Proves(visitor, password, _time.GetUtcNow().UtcDateTime))
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
            if (!Proves(visitor, password, _time.GetUtcNow().UtcDateTime))
            {
                return new(["wrong"]);
            }

            (visitor!.ClosedAt, visitor.Stamp) = (_time.GetUtcNow().UtcDateTime, NewStamp());
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
        var visitor = _context.Visitors.SingleOrDefault(candidate => candidate.NameKey == key && candidate.ClosedAt != null && !candidate.ClosedByOperator);
        try
        {
            // A closed account counts wrong passwords like an open one: it is not guessed open.
            if (!Proves(visitor, password, _time.GetUtcNow().UtcDateTime))
            {
                return null;
            }

            (visitor!.ClosedAt, visitor.Stamp) = (null, NewStamp());
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

        // The name goes with the account (D-055) – out of the histories of the trees of others too, where a
        // line says with whom a tree was shared. The line stays; whom it named is not said any more.
        foreach (var name in gone.Select(visitor => visitor.Name))
        {
            _context.TreeChanges
                .Where(change => change.What.StartsWith("member-") && change.Subject == name)
                .ExecuteUpdate(change => change.SetProperty(line => line.Subject, string.Empty));
        }

        _context.Visitors.RemoveRange(gone);
        ForgetDeclined();
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
