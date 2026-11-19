using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Vafadar.Dastan.Store;

/// <summary>
/// An editor the store knows (step 11.7), as ASP.NET Core Identity keeps an account (step 19.4, ADR 0016): the
/// name as the editor writes it – it is what an act is kept with – and as it is compared, what is kept of the
/// password, the stamp every sign-in carries, and the count of wrong passwords. The accounts of the editors
/// have tables of their own in the schema <c>identity</c>, apart from those of the visitors: an editor is
/// made by the operator and is never a visitor's account under another name. An editor has no mail address.
/// </summary>
public sealed class EditorAccount : IdentityUser
{
    /// <summary>Gets or sets when the account was made.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets when the account was closed; nothing for an account that is open.</summary>
    public DateTime? ClosedAt { get; set; }

    /// <summary>Gets or sets what the editor may do, as the names of the roles with commas between them.</summary>
    public string Roles { get; set; } = EditorialStore.RolesText(EditorRoles.All);
}

/// <summary>What an editor may do in the tool (step 13.3). An account has any of them.</summary>
[Flags]
public enum EditorRoles
{
    /// <summary>Nothing but reading.</summary>
    None = 0,

    /// <summary>Changing, adding and taking out records, texts, stages and comparisons.</summary>
    Writer = 1,

    /// <summary>Recording the review of a statement: reviewed, disputed, withdrawn.</summary>
    Reviewer = 2,

    /// <summary>Publishing a revision.</summary>
    Publisher = 4,

    /// <summary>Everything.</summary>
    All = Writer | Reviewer | Publisher,
}

/// <summary>One time somebody tried the password of an account (step 13.4). Where from is not kept.</summary>
public sealed class EditorSignIn
{
    /// <summary>Gets or sets the number of the entry in the store.</summary>
    public long Number { get; set; }

    /// <summary>Gets or sets the account, as names are compared.</summary>
    public required string EditorId { get; set; }

    /// <summary>Gets or sets when.</summary>
    public DateTime At { get; set; }

    /// <summary>Gets or sets whether the password was right and the account took it.</summary>
    public bool Succeeded { get; set; }
}

/// <summary>An editor of the store, without what proves the name.</summary>
/// <param name="Name">The name.</param>
/// <param name="CreatedAt">When the account was made.</param>
/// <param name="Closed">Whether the account is closed.</param>
/// <param name="LockedUntil">Until when the account takes no password; nothing when it does.</param>
public sealed record EditorInfo(string Name, DateTime CreatedAt, bool Closed, DateTime? LockedUntil)
{
    /// <summary>Gets what the editor may do.</summary>
    public EditorRoles Roles { get; init; } = EditorRoles.All;
}

/// <summary>An editor who has signed in.</summary>
/// <param name="Name">The name, as the account writes it.</param>
/// <param name="Stamp">The value the sign-in holds as long as the password and the account stay as they are.</param>
public sealed record EditorSession(string Name, string Stamp);

// The accounts of the editors. They are not content: an account has no history in the store, and nothing of an
// account is written out with a collection.
public sealed partial class EditorialStore
{
    /// <summary>The fewest characters a password has.</summary>
    public const int PasswordLength = 12;

    /// <summary>How many wrong passwords in a row an account takes before it takes none for a while.</summary>
    public const int FailuresAllowed = 5;

    /// <summary>How long an account takes no password after too many wrong ones.</summary>
    public static readonly TimeSpan LockedFor = TimeSpan.FromMinutes(15);

    /// <summary>
    /// How many rounds what is kept of a password is derived with (PBKDF2 with HMAC-SHA-512). The number is
    /// written into the value, so that it can be raised later without asking anybody for a new password.
    /// </summary>
    public const int PasswordRounds = 210_000;

    private static readonly PasswordHasher<EditorAccount> Hasher = new(Microsoft.Extensions.Options.Options.Create(new PasswordHasherOptions { IterationCount = PasswordRounds }));

    // What a password is compared with when the name is unknown, so that an unknown name takes as long as a known one.
    private static readonly EditorAccount Nobody = new() { PasswordHash = Hasher.HashPassword(new EditorAccount(), Guid.NewGuid().ToString("N")) };

    private UserManager<EditorAccount>? _editors;

    // Identity's own keeper of accounts, on the context of the store. A name may be written in any script; the
    // password has a length and nothing else is asked of it; an editor has no mail address.
    private UserManager<EditorAccount> Accounts => _editors ??= new UserManager<EditorAccount>(
        new UserOnlyStore<EditorAccount, StoreContext>(_context),
        Microsoft.Extensions.Options.Options.Create(new IdentityOptions
        {
            User = { AllowedUserNameCharacters = string.Empty, RequireUniqueEmail = false },
            Password = { RequiredLength = PasswordLength, RequireDigit = false, RequireLowercase = false, RequireUppercase = false, RequireNonAlphanumeric = false, RequiredUniqueChars = 1 },
        }),
        Hasher,
        [new UserValidator<EditorAccount>()],
        [new PasswordValidator<EditorAccount>()],
        new Names(),
        new IdentityErrorDescriber(),
        null!,
        NullLogger<UserManager<EditorAccount>>.Instance);

    // A name is compared as the store always compared it.
    private sealed class Names : ILookupNormalizer
    {
        public string? NormalizeName(string? name) => name is null ? null : Key(name.Trim());

        public string? NormalizeEmail(string? email) => email?.Trim().Normalize().ToUpperInvariant();
    }

    /// <summary>Returns the editors the store knows, by their name.</summary>
    public ImmutableArray<EditorInfo> Editors()
    {
        var now = _time.GetUtcNow();
        return
        [
            .. _context.Editors.AsNoTracking().OrderBy(editor => editor.NormalizedUserName).AsEnumerable()
                .Select(editor => new EditorInfo(editor.UserName!, editor.CreatedAt, editor.ClosedAt is not null, editor.LockoutEnd > now ? editor.LockoutEnd.Value.UtcDateTime : null) { Roles = ReadRoles(editor.Roles) ?? EditorRoles.None }),
        ];
    }

    /// <summary>How many sign-ins of an account the store remembers.</summary>
    public const int SignInsKept = 50;

    /// <summary>Returns the roles as an account keeps them: their names with commas between them.</summary>
    public static string RolesText(EditorRoles roles) =>
        string.Join(',', new[] { EditorRoles.Writer, EditorRoles.Reviewer, EditorRoles.Publisher }.Where(role => roles.HasFlag(role)).Select(role => role.ToString().ToLowerInvariant()));

    /// <summary>Reads roles as they are written – names with commas between them – or nothing when a name is no role.</summary>
    public static EditorRoles? ReadRoles(string? written)
    {
        var roles = EditorRoles.None;
        foreach (var name in (written ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (name.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                roles = EditorRoles.All;
            }
            else if (name.All(char.IsLetter) && Enum.TryParse<EditorRoles>(name, ignoreCase: true, out var role) && role is EditorRoles.Writer or EditorRoles.Reviewer or EditorRoles.Publisher)
            {
                roles |= role;
            }
            else
            {
                return null;
            }
        }

        return roles;
    }

    /// <summary>Returns what the editor of that name may do; nothing for a name the store does not know or an account that is closed.</summary>
    public EditorRoles RolesOf(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var key = Key(name.Trim());
        var editor = _context.Editors.AsNoTracking().SingleOrDefault(account => account.NormalizedUserName == key && account.ClosedAt == null);
        return editor is null ? EditorRoles.None : ReadRoles(editor.Roles) ?? EditorRoles.None;
    }

    /// <summary>Says what an editor may do from now on.</summary>
    public ActResult SetRoles(string name, EditorRoles roles)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (Found(name) is not { } editor)
        {
            return Refused($"{name.Trim()}: the store knows no editor of that name.");
        }

        var written = RolesText(roles);
        if (editor.Roles == written)
        {
            _context.ChangeTracker.Clear();
            return new ActResult([]);
        }

        editor.Roles = written;
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
        return new ActResult([], 1);
    }

    /// <summary>
    /// Gives an editor another password when the editor proves the present one. A wrong present password counts
    /// like a wrong one at a sign-in. Whoever is signed in with the old password is signed in no longer.
    /// </summary>
    public ActResult ChangePassword(string name, string current, string password)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(password);
        if (Weak(password) is { } weak)
        {
            return Refused(weak);
        }

        if (password == current)
        {
            return Refused("the new password is the present one.");
        }

        return SignIn(name, current) is null ? Refused("the present password is not right.") : SetPassword(name, password);
    }

    /// <summary>Returns the last times the password of an account was tried, the newest first.</summary>
    public ImmutableArray<EditorSignIn> SignIns(string name, int take = 20)
    {
        ArgumentNullException.ThrowIfNull(name);
        var key = Key(name.Trim());
        return [.. _context.SignIns.AsNoTracking().Where(entry => entry.EditorId == key).OrderByDescending(entry => entry.Number).Take(take)];
    }

    // Remembers one try of a password, and forgets the oldest ones of the account beyond what is kept.
    private void Tried(EditorAccount editor, DateTime at, bool succeeded)
    {
        var key = editor.NormalizedUserName!;
        _context.SignIns.Add(new EditorSignIn { EditorId = key, At = at, Succeeded = succeeded });
        var old = _context.SignIns.Where(entry => entry.EditorId == key).OrderByDescending(entry => entry.Number).Skip(SignInsKept - 1).ToList();
        _context.SignIns.RemoveRange(old);
    }

    /// <summary>Makes the account of an editor, who may do everything unless roles are given.</summary>
    public ActResult AddEditor(string name, string password, EditorRoles roles = EditorRoles.All)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(password);
        var written = name.Trim();
        if (written.Length is 0 or > 200 || written.Any(char.IsControl))
        {
            return Refused("an editor has a name of 1 to 200 characters in one line.");
        }

        if (Found(written) is not null)
        {
            return Refused($"{written}: the store already knows an editor of that name.");
        }

        if (Weak(password) is { } weak)
        {
            return Refused(weak);
        }

        // Identity makes the account: it checks the name once more, derives what is kept of the password and
        // gives the account its stamp. Two who make the same name at the same moment: the database keeps one.
        _context.ChangeTracker.Clear();
        var account = new EditorAccount { Id = Guid.NewGuid().ToString("N"), UserName = written, Roles = RolesText(roles), CreatedAt = _time.GetUtcNow().UtcDateTime, LockoutEnabled = true };
        try
        {
            var made = Accounts.CreateAsync(account, password).GetAwaiter().GetResult();
            return made.Succeeded
                ? new ActResult([], 1)
                : Refused(made.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.DuplicateUserName))
                    ? $"{written}: the store already knows an editor of that name."
                    : string.Join(" ", made.Errors.Select(error => error.Description)));
        }
        catch (DbUpdateException)
        {
            return Refused($"{written}: the store already knows an editor of that name.");
        }
        finally
        {
            _context.ChangeTracker.Clear();
        }
    }

    /// <summary>Gives an editor another password. Whoever signed in with the old one is signed in no longer.</summary>
    public ActResult SetPassword(string name, string password)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(password);
        if (Found(name) is not { } editor)
        {
            return Refused($"{name.Trim()}: the store knows no editor of that name.");
        }

        if (Weak(password) is { } weak)
        {
            return Refused(weak);
        }

        (editor.PasswordHash, editor.SecurityStamp, editor.AccessFailedCount, editor.LockoutEnd) = (Hasher.HashPassword(editor, password), NewStamp(), 0, null);
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
        return new ActResult([], 1);
    }

    /// <summary>
    /// Closes the account of an editor, or opens it again. A closed account cannot sign in and is signed in
    /// nowhere; what the editor did stays in the history under their name.
    /// </summary>
    public ActResult CloseEditor(string name, bool closed = true)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (Found(name) is not { } editor)
        {
            return Refused($"{name.Trim()}: the store knows no editor of that name.");
        }

        if (editor.ClosedAt is not null == closed)
        {
            return new ActResult([]);
        }

        (editor.ClosedAt, editor.SecurityStamp, editor.AccessFailedCount, editor.LockoutEnd) = (closed ? _time.GetUtcNow().UtcDateTime : null, NewStamp(), 0, null);
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
        return new ActResult([], 1);
    }

    /// <summary>
    /// Signs an editor in: the name and the password of an open account. A wrong password is counted; after
    /// <see cref="FailuresAllowed"/> in a row the account takes none for <see cref="LockedFor"/>. Why a sign-in
    /// was refused is not said – a wrong name, a wrong password and a closed account answer alike.
    /// </summary>
    public EditorSession? SignIn(string name, string password)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(password);
        var now = _time.GetUtcNow();
        var editor = Found(name);
        var right = Hasher.VerifyHashedPassword(editor ?? Nobody, (editor ?? Nobody).PasswordHash ?? Nobody.PasswordHash!, password) != PasswordVerificationResult.Failed;
        if (editor is null)
        {
            return null;
        }

        try
        {
            // The password was tried – that takes its time – before this step, which reads the account again
            // and writes the count: tries made at the same moment are all counted.
            using var step = _context.Database.BeginTransaction();
            Lock("editors");
            _context.Entry(editor).Reload();
            if (editor.ClosedAt is not null || editor.LockoutEnd > now)
            {
                // A locked account does not count further and does not open for the right password either.
                return null;
            }

            // Counted by the clock of the store, in the columns Identity has for it: a pause can be tried in a test.
            Tried(editor, now.UtcDateTime, right);
            if (!right)
            {
                editor.AccessFailedCount++;
                if (editor.AccessFailedCount >= FailuresAllowed)
                {
                    (editor.AccessFailedCount, editor.LockoutEnd) = (0, now + LockedFor);
                }

                _context.SaveChanges();
                step.Commit();
                return null;
            }

            (editor.AccessFailedCount, editor.LockoutEnd) = (0, null);
            _context.SaveChanges();
            step.Commit();
            return new EditorSession(editor.UserName!, editor.SecurityStamp!);
        }
        finally
        {
            _context.ChangeTracker.Clear();
        }
    }

    /// <summary>Returns whether a sign-in still holds: the account is open and has the password it was signed in with.</summary>
    public bool Holds(EditorSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var key = Key(session.Name);
        return _context.Editors.AsNoTracking().Any(editor => editor.NormalizedUserName == key && editor.SecurityStamp == session.Stamp && editor.ClosedAt == null);
    }

    private EditorAccount? Found(string name)
    {
        var key = Key(name.Trim());
        return _context.Editors.SingleOrDefault(editor => editor.NormalizedUserName == key);
    }

    private static string Key(string name) => name.Normalize().ToUpperInvariant();

    private static string NewStamp() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

    private static string? Weak(string password) =>
        password.Length < PasswordLength ? $"a password has at least {PasswordLength} characters."
        : password.Length > 1000 ? "a password has at most 1000 characters."
        : null;
}
