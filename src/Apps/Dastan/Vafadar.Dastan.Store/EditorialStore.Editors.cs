using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;

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
}

/// <summary>An editor of the store, without what proves the name.</summary>
/// <param name="Name">The name.</param>
/// <param name="CreatedAt">When the account was made.</param>
/// <param name="Closed">Whether the account is closed.</param>
/// <param name="LockedUntil">Until when the account takes no password; nothing when it does.</param>
public sealed record EditorInfo(string Name, DateTime CreatedAt, bool Closed, DateTime? LockedUntil);

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

    // PBKDF2 with SHA-256, as the platform derives it; the work is written into the value, so that it can be
    // raised later without asking anybody for a new password before their next sign-in.
    private const int Work = 600_000;
    private const int SaltLength = 16;
    private const int KeyLength = 32;

    // What a password is compared with when the name is unknown, so that an unknown name takes as long as a known one.
    private static readonly string Nobody = Derive(Guid.NewGuid().ToString("N"));

    /// <summary>Returns the editors the store knows, by their name.</summary>
    public ImmutableArray<EditorInfo> Editors()
    {
        var now = _time.GetUtcNow().UtcDateTime;
        return
        [
            .. _context.Editors.AsNoTracking().OrderBy(editor => editor.Id).AsEnumerable()
                .Select(editor => new EditorInfo(editor.Name, editor.CreatedAt, editor.ClosedAt is not null, editor.LockedUntil > now ? editor.LockedUntil : null)),
        ];
    }

    /// <summary>Makes the account of an editor.</summary>
    public ActResult AddEditor(string name, string password)
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

        _context.Editors.Add(new StoredEditor { Id = Key(written), Name = written, Password = Derive(password), Stamp = NewStamp(), CreatedAt = _time.GetUtcNow().UtcDateTime });
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
            if (editor.ClosedAt is not null || editor.LockedUntil > now)
            {
                // A locked account does not count further and does not open for the right password either.
                return null;
            }

            if (!right)
            {
                editor.Failures++;
                if (editor.Failures >= FailuresAllowed)
                {
                    (editor.Failures, editor.LockedUntil) = (0, now + LockedFor);
                }

                _context.SaveChanges();
                return null;
            }

            if (editor.Failures != 0 || editor.LockedUntil is not null)
            {
                (editor.Failures, editor.LockedUntil) = (0, null);
                _context.SaveChanges();
            }

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

    private static string Derive(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Work, HashAlgorithmName.SHA256, KeyLength);
        return string.Create(CultureInfo.InvariantCulture, $"pbkdf2-sha256${Work}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}");
    }

    private static bool Matches(string password, string derived)
    {
        var parts = derived.Split('$');
        if (parts is not ["pbkdf2-sha256", var work, var salt, var key] || !int.TryParse(work, NumberStyles.None, CultureInfo.InvariantCulture, out var rounds))
        {
            return false;
        }

        var expected = Convert.FromBase64String(key);
        var given = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(salt), rounds, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(given, expected);
    }
}
