using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Vafadar.Core.Security;

namespace Vafadar.Dastan.Store;

/// <summary>
/// An editor the store knows (step 11.7): a name and what proves the name. The password itself is not kept –
/// only a value derived from it, from which it cannot be read back.
/// </summary>
public sealed class StoredEditor
{
    /// <summary>Gets or sets the name as it is compared: without case.</summary>
    public required string Id { get; set; }

    /// <summary>Gets or sets the name as the editor writes it; it is what an act is kept with.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the value derived from the password, with how it was derived.</summary>
    public required string Password { get; set; }

    /// <summary>
    /// Gets or sets a value that changes with the password and with the account being closed: whoever signed in
    /// before is signed in no longer.
    /// </summary>
    public required string Stamp { get; set; }

    /// <summary>Gets or sets when the account was made.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets how often in a row a wrong password was given.</summary>
    public int Failures { get; set; }

    /// <summary>Gets or sets until when the account takes no password, after too many wrong ones.</summary>
    public DateTime? LockedUntil { get; set; }

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

    // What a password is compared with when the name is unknown, so that an unknown name takes as long as a known one.
    private static readonly string Nobody = Derive(Guid.NewGuid().ToString("N"));

    /// <summary>Returns the editors the store knows, by their name.</summary>
    public ImmutableArray<EditorInfo> Editors()
    {
        var now = _time.GetUtcNow().UtcDateTime;
        return
        [
            .. _context.Editors.AsNoTracking().OrderBy(editor => editor.Id).AsEnumerable()
                .Select(editor => new EditorInfo(editor.Name, editor.CreatedAt, editor.ClosedAt is not null, editor.LockedUntil > now ? editor.LockedUntil : null) { Roles = ReadRoles(editor.Roles) ?? EditorRoles.None }),
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
        var editor = _context.Editors.AsNoTracking().SingleOrDefault(account => account.Id == key && account.ClosedAt == null);
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
    private void Tried(StoredEditor editor, DateTime at, bool succeeded)
    {
        _context.SignIns.Add(new EditorSignIn { EditorId = editor.Id, At = at, Succeeded = succeeded });
        var old = _context.SignIns.Where(entry => entry.EditorId == editor.Id).OrderByDescending(entry => entry.Number).Skip(SignInsKept - 1).ToList();
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

        _context.Editors.Add(new StoredEditor { Id = Key(written), Name = written, Password = Derive(password), Stamp = NewStamp(), Roles = RolesText(roles), CreatedAt = _time.GetUtcNow().UtcDateTime });
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
        return new ActResult([], 1);
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

        (editor.Password, editor.Stamp, editor.Failures, editor.LockedUntil) = (Derive(password), NewStamp(), 0, null);
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

        (editor.ClosedAt, editor.Stamp, editor.Failures, editor.LockedUntil) = (closed ? _time.GetUtcNow().UtcDateTime : null, NewStamp(), 0, null);
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
        var now = _time.GetUtcNow().UtcDateTime;
        var editor = Found(name);
        var right = Matches(password, editor?.Password ?? Nobody);
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
            if (editor.ClosedAt is not null || editor.LockedUntil > now)
            {
                // A locked account does not count further and does not open for the right password either.
                return null;
            }

            Tried(editor, now, right);
            if (!right)
            {
                editor.Failures++;
                if (editor.Failures >= FailuresAllowed)
                {
                    (editor.Failures, editor.LockedUntil) = (0, now + LockedFor);
                }

                _context.SaveChanges();
                step.Commit();
                return null;
            }

            (editor.Failures, editor.LockedUntil) = (0, null);
            _context.SaveChanges();
            step.Commit();
            return new EditorSession(editor.Name, editor.Stamp);
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
        return _context.Editors.AsNoTracking().Any(editor => editor.Id == key && editor.Stamp == session.Stamp && editor.ClosedAt == null);
    }

    private StoredEditor? Found(string name)
    {
        var key = Key(name.Trim());
        return _context.Editors.SingleOrDefault(editor => editor.Id == key);
    }

    private static string Key(string name) => name.Normalize().ToUpperInvariant();

    private static string NewStamp() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

    private static string? Weak(string password) =>
        password.Length < PasswordLength ? $"a password has at least {PasswordLength} characters."
        : password.Length > 1000 ? "a password has at most 1000 characters."
        : null;

    // What is kept of a password is derived as everywhere in these apps (PasswordHash of Vafadar.Core): the work
    // is written into the value, so that it can be raised later without asking anybody for a new password.
    private static string Derive(string password) => PasswordHash.Derive(password);

    private static bool Matches(string password, string derived) => PasswordHash.Matches(password, derived);
}
