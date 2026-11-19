using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.People;

/// <summary>
/// The account of a visitor as ASP.NET Core Identity keeps it (ADR 0016), in the schema <c>identity</c>: the
/// name, the mail address and whether it is confirmed, what is kept of the password, the stamp that every
/// sign-in carries, and the count of wrong passwords. What belongs to the visitor in Dastan – trees,
/// photographs, circles, consent – lies in the schema <c>personal</c> under the same identifier.
/// </summary>
public sealed class VisitorAccount : IdentityUser
{
    /// <summary>Gets or sets the language the site writes to the visitor in, by its tag: the one of the page the account was made on.</summary>
    public string Language { get; set; } = "en";
}

/// <summary>What came of a try to sign in.</summary>
public enum SignInOutcome
{
    /// <summary>Not signed in. Why is not said: a wrong name, a wrong password, a locked and a closed account answer alike.</summary>
    Refused,

    /// <summary>The name and the password are right, and the mail address is not confirmed yet: nothing of the site treats this as an account until it is.</summary>
    Unconfirmed,

    /// <summary>Signed in.</summary>
    SignedIn,
}

/// <summary>What came of a try to sign in, with the session when there is one.</summary>
/// <param name="Outcome">What came of it.</param>
/// <param name="Session">The session of who signed in.</param>
public sealed record SignInAnswer(SignInOutcome Outcome, VisitorSession? Session = null);

/// <summary>What came of opening the link that confirms a mail address.</summary>
/// <param name="Done">Whether the address is confirmed now.</param>
/// <param name="RecoveryCodes">The recovery codes of the account, which are shown this once.</param>
/// <param name="Session">The session of who opened the link: the codes they are shown open the account anyway.</param>
public sealed record ConfirmAnswer(bool Done, ImmutableArray<string> RecoveryCodes, VisitorSession? Session = null);

/// <summary>
/// The accounts of the visitors on ASP.NET Core Identity (step 19.4, ADR 0016): an account has a name others
/// see, a mail address nobody else sees, and a password. The address is required, belongs to one account, and
/// is confirmed before the account is one. Identity keeps the accounts, checks and derives the passwords and
/// stamps the sessions; the tokens of a link and the pause after wrong passwords are counted here by the clock
/// of the store, so that they can be tried in a test, and are kept in Identity's tables.
/// </summary>
public sealed partial class VisitorAccounts
{
    /// <summary>How long the link that confirms an address can be used.</summary>
    public static readonly TimeSpan ConfirmationValidFor = TimeSpan.FromHours(24);

    /// <summary>How long after a mail with such a link the next one is sent at the earliest.</summary>
    public static readonly TimeSpan SendAgainAfter = TimeSpan.FromMinutes(2);

    private const string TokenProvider = "dastan";
    private const string ConfirmToken = "confirm-address";

    // What is kept of a password nobody has, so that a name nobody has takes as long as a wrong password.
    private static readonly VisitorAccount Nobody = new() { PasswordHash = new PasswordHasher<VisitorAccount>().HashPassword(new VisitorAccount(), Guid.NewGuid().ToString("N")) };

    private readonly PeopleStore _store;
    private readonly PeopleContext _context;
    private readonly TimeProvider _time;
    private readonly string _site;
    private readonly PasswordHasher<VisitorAccount> _hasher = new();
    private UserManager<VisitorAccount>? _manager;

    internal VisitorAccounts(PeopleStore store, PeopleContext context, TimeProvider time, string site) =>
        (_store, _context, _time, _site) = (store, context, time, site.TrimEnd('/'));

    private DateTimeOffset Now => _time.GetUtcNow();

    // Identity's own keeper of accounts, on the context of the store: what it writes is part of the step the
    // store is in. A name may be written in any script; the password has a length and nothing else is asked.
    private UserManager<VisitorAccount> Manager => _manager ??= new UserManager<VisitorAccount>(
        new UserOnlyStore<VisitorAccount, PeopleContext>(_context),
        Options.Create(new IdentityOptions
        {
            User = { AllowedUserNameCharacters = string.Empty, RequireUniqueEmail = true },
            Password = { RequiredLength = PeopleStore.PasswordLength, RequireDigit = false, RequireLowercase = false, RequireUppercase = false, RequireNonAlphanumeric = false, RequiredUniqueChars = 1 },
        }),
        _hasher,
        [new UserValidator<VisitorAccount>()],
        [new PasswordValidator<VisitorAccount>()],
        new Keys(),
        new IdentityErrorDescriber(),
        null!,
        NullLogger<UserManager<VisitorAccount>>.Instance);

    // A name is compared as the store always compared it; an address without regard to capital letters.
    private sealed class Keys : ILookupNormalizer
    {
        public string? NormalizeName(string? name) => name is null ? null : PeopleStore.Key(name.Trim());

        public string? NormalizeEmail(string? email) => email?.Trim().Normalize().ToUpperInvariant();
    }

    /// <summary>
    /// Makes an account that waits for its address to be confirmed, and writes the mail with the link – in the
    /// same step, so that the one exists exactly when the other does. Returns what is wrong with what was
    /// written, by the names of the problems (<c>name-length</c>, <c>name-letters</c>, <c>taken</c>,
    /// <c>mail</c>, <c>password-short</c>, <c>password-long</c>, <c>invitation-needed</c>); nothing when the
    /// visitor is told to look at their mail. That an address already has an account is not a problem that is
    /// said: the answer is the same, and the mail goes to who has the address and says what was tried.
    /// </summary>
    /// <param name="name">The name others see.</param>
    /// <param name="mail">The mail address.</param>
    /// <param name="password">The password.</param>
    /// <param name="language">The language of the page, in which the mail is written.</param>
    /// <param name="invitation">The invitation the account is made through, if any.</param>
    /// <param name="invitationNeeded">Whether no account is made without an invitation that can be used.</param>
    /// <param name="terms">The version of the terms and the privacy notice the visitor said they read.</param>
    public ImmutableArray<string> Register(string name, string mail, string password, string language, string? invitation = null, bool invitationNeeded = false, string? terms = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(mail);
        ArgumentNullException.ThrowIfNull(password);
        var written = name.Trim();
        var address = mail.Trim();
        var problems = new List<string>();
        if (PeopleStore.NameProblem(written) is { } wrong)
        {
            problems.Add(wrong);
        }

        if (!MailOutbox.IsAddress(address))
        {
            problems.Add("mail");
        }

        if (PeopleStore.Weak(password) is { } weak)
        {
            problems.Add(weak);
        }

        if (problems.Count > 0)
        {
            return [.. problems];
        }

        var key = PeopleStore.Key(written);
        var tongue = language is { Length: > 0 and <= 20 } ? language : "en";

        // One step for the account, what belongs to it, the invitation it uses up and the mail that tells of it.
        using var step = _context.Database.BeginTransaction();
        _context.ChangeTracker.Clear();
        if (_context.Visitors.AsNoTracking().Any(visitor => visitor.NameKey == key) || _context.Accounts.AsNoTracking().Any(account => account.NormalizedUserName == key))
        {
            return ["taken"];
        }

        var mailKey = new Keys().NormalizeEmail(address)!;
        if (_context.Accounts.AsNoTracking().SingleOrDefault(account => account.NormalizedEmail == mailKey) is { } holder)
        {
            // Who has the address is told that somebody tried – once an hour at most – and who tried is told
            // what everybody is told. Whether an address has an account is nobody else's to learn.
            Put(step, AccountMail.AddressInUse(holder.Language, _site, holder.Email!, $"address-in-use:{holder.Id}:{Now.UtcDateTime:yyyyMMddHH}"));
            step.Commit();
            return [];
        }

        var invited = _store.Usable(invitation)?.Id;
        _context.ChangeTracker.Clear();
        if (invitationNeeded && invited is null)
        {
            return ["invitation-needed"];
        }

        var account = new VisitorAccount { Id = Guid.NewGuid().ToString("N"), UserName = written, Email = address, Language = tongue, LockoutEnabled = true };
        try
        {
            var made = Manager.CreateAsync(account, password).GetAwaiter().GetResult();
            if (!made.Succeeded)
            {
                // Identity looks at the name and the address once more, and at the password by its own rule.
                // An address somebody took in this very moment is answered like one that was taken before.
                return made.Errors.Any(error => error.Code is "DuplicateUserName") ? ["taken"]
                    : made.Errors.Any(error => error.Code is "DuplicateEmail") ? []
                    : [made.Errors.Any(error => error.Code.StartsWith("Password", StringComparison.Ordinal)) ? "password-short" : "mail"];
            }

            var now = Now.UtcDateTime;
            var kept = terms is { Length: > 0 and <= 20 };
            _context.Visitors.Add(new StoredVisitor
            {
                Id = account.Id, Name = written, NameKey = key, Stamp = account.SecurityStamp!, CreatedAt = now,
                TermsVersion = kept ? terms : null, TermsAcceptedAt = kept ? now : null,
            });
            var token = NewToken(account.Id, ConfirmToken, ConfirmationValidFor);
            Put(step, AccountMail.Confirm(tongue, _site, address, written, account.Id, token, $"confirm:{account.Id}:{Hash(token)[..16]}"));
            _context.SaveChanges();
        }
        catch (DbUpdateException)
        {
            // Somebody took the name or the address between the question and the answer: the database keeps
            // both to one account. The name is said; the address is not.
            step.Rollback();
            _context.ChangeTracker.Clear();
            return _context.Visitors.AsNoTracking().Any(visitor => visitor.NameKey == key) || _context.Accounts.AsNoTracking().Any(candidate => candidate.NormalizedUserName == key) ? ["taken"] : [];
        }

        _context.ChangeTracker.Clear();
        if (invited is not null)
        {
            // Used up in one sentence, so that of two forms with the same invitation one makes an account.
            var today = _store.Today;
            var used = _context.Invitations
                .Where(candidate => candidate.Id == invited && candidate.UsedOn == null && candidate.RevokedAt == null)
                .ExecuteUpdate(change => change.SetProperty(candidate => candidate.UsedBy, account.Id).SetProperty(candidate => candidate.UsedOn, today));
            if (used == 0 && invitationNeeded)
            {
                step.Rollback();
                return ["invitation-needed"];
            }
        }

        step.Commit();
        return [];
    }

    /// <summary>
    /// Confirms the address of an account with the token of the link that was sent there. A token is used once
    /// and for <see cref="ConfirmationValidFor"/>. With the address confirmed the account is one, and gets its
    /// recovery codes, which the answer carries this once.
    /// </summary>
    public ConfirmAnswer Confirm(string id, string token)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(token);
        return _store.Guarded([PeopleStore.OfAccount(id)], () =>
        {
            _context.ChangeTracker.Clear();
            var account = _context.Accounts.SingleOrDefault(candidate => candidate.Id == id);
            var visitor = _context.Visitors.AsNoTracking().SingleOrDefault(candidate => candidate.Id == id && candidate.ClosedAt == null);
            if (account is null || visitor is null || account.EmailConfirmed || Take(id, ConfirmToken, token) is null)
            {
                return new ConfirmAnswer(false, []);
            }

            account.EmailConfirmed = true;
            var codes = _store.NewCodes(id);
            _context.SaveChanges();
            _context.ChangeTracker.Clear();
            return new ConfirmAnswer(true, codes, new VisitorSession(visitor.Id, visitor.Name, visitor.Stamp));
        });
    }

    /// <summary>
    /// Sends the mail with the link once more to an account that has not confirmed its address, named by its
    /// name or its address – at the earliest <see cref="SendAgainAfter"/> after the one before. It says
    /// nothing about whether there is such an account: who asks is told the same in every case.
    /// </summary>
    public void SendConfirmationAgain(string nameOrMail)
    {
        ArgumentNullException.ThrowIfNull(nameOrMail);
        if (Find(nameOrMail) is not { EmailConfirmed: false } found)
        {
            return;
        }

        _store.Guarded([PeopleStore.OfAccount(found.Id)], () =>
        {
            _context.ChangeTracker.Clear();
            if (SentLately(found.Id, ConfirmToken))
            {
                return false;
            }

            var name = _context.Visitors.AsNoTracking().Where(visitor => visitor.Id == found.Id).Select(visitor => visitor.Name).Single();
            var token = NewToken(found.Id, ConfirmToken, ConfirmationValidFor);
            Put(_context.Database.CurrentTransaction!, AccountMail.Confirm(found.Language, _site, found.Email!, name, found.Id, token, $"confirm:{found.Id}:{Hash(token)[..16]}"));
            _context.SaveChanges();
            _context.ChangeTracker.Clear();
            return true;
        });
    }

    /// <summary>
    /// Signs a visitor in by the name or the mail address of an open account and its password. A wrong
    /// password is counted; after <see cref="PeopleStore.FailuresAllowed"/> in a row the account takes none
    /// for <see cref="PeopleStore.LockedFor"/>. An account whose address is not confirmed is told so – after
    /// the right password, and to nobody else.
    /// </summary>
    public SignInAnswer SignIn(string nameOrMail, string password)
    {
        ArgumentNullException.ThrowIfNull(nameOrMail);
        ArgumentNullException.ThrowIfNull(password);
        var found = Find(nameOrMail);

        // The password is tried – that takes its time – before the step that counts, and also for nobody.
        var tried = _hasher.VerifyHashedPassword(found ?? Nobody, (found ?? Nobody).PasswordHash ?? Nobody.PasswordHash!, password);
        if (found is null || found.PasswordHash is null)
        {
            return new SignInAnswer(SignInOutcome.Refused);
        }

        return _store.Guarded([PeopleStore.OfAccount(found.Id)], () =>
        {
            _context.ChangeTracker.Clear();
            var account = _context.Accounts.Single(candidate => candidate.Id == found.Id);
            var visitor = _context.Visitors.AsNoTracking().SingleOrDefault(candidate => candidate.Id == found.Id);
            if (visitor is null || visitor.ClosedAt is not null || account.LockoutEnd > Now)
            {
                // A locked account does not count further and does not open for the right password either.
                return new SignInAnswer(SignInOutcome.Refused);
            }

            if (tried == PasswordVerificationResult.Failed)
            {
                account.AccessFailedCount++;
                if (account.AccessFailedCount >= PeopleStore.FailuresAllowed)
                {
                    (account.AccessFailedCount, account.LockoutEnd) = (0, Now + PeopleStore.LockedFor);
                }

                _context.SaveChanges();
                _context.ChangeTracker.Clear();
                return new SignInAnswer(SignInOutcome.Refused);
            }

            account.AccessFailedCount = 0;
            account.LockoutEnd = null;
            if (tried == PasswordVerificationResult.SuccessRehashNeeded)
            {
                // What is kept of a password follows the rule of the day, without the visitor doing anything.
                account.PasswordHash = _hasher.HashPassword(account, password);
            }

            _context.SaveChanges();
            _context.ChangeTracker.Clear();
            return account.EmailConfirmed
                ? new SignInAnswer(SignInOutcome.SignedIn, new VisitorSession(account.Id, visitor.Name, visitor.Stamp))
                : new SignInAnswer(SignInOutcome.Unconfirmed);
        });
    }

    /// <summary>Returns the mail address of an account and whether it is confirmed, for the account itself; nothing for an account Identity does not keep.</summary>
    public (string Address, bool Confirmed)? Address(string id) =>
        _context.Accounts.AsNoTracking().Where(account => account.Id == id).Select(account => new { account.Email, account.EmailConfirmed }).SingleOrDefault() is { Email: { } address } held
            ? (address, held.EmailConfirmed)
            : null;

    // The account of a name, or of an address when what was written has the sign of one.
    private VisitorAccount? Find(string nameOrMail)
    {
        var keys = new Keys();
        var written = nameOrMail.Trim();
        if (written.Contains('@', StringComparison.Ordinal))
        {
            var mail = keys.NormalizeEmail(written);
            return _context.Accounts.AsNoTracking().SingleOrDefault(account => account.NormalizedEmail == mail);
        }

        var name = keys.NormalizeName(written);
        return _context.Accounts.AsNoTracking().SingleOrDefault(account => account.NormalizedUserName == name);
    }

    // A token of a link: long and random, told once – in the mail – and kept as its hash with the moment it
    // ends and the moment it was sent. A new one takes the place of the one before.
    private string NewToken(string id, string purpose, TimeSpan validFor, string carries = "")
    {
        var token = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        var value = string.Create(CultureInfo.InvariantCulture, $"{Hash(token)}|{(Now + validFor).ToUnixTimeSeconds()}|{Now.ToUnixTimeSeconds()}|{Convert.ToBase64String(Encoding.UTF8.GetBytes(carries))}");
        if (_context.AccountTokens.SingleOrDefault(candidate => candidate.UserId == id && candidate.LoginProvider == TokenProvider && candidate.Name == purpose) is { } held)
        {
            held.Value = value;
        }
        else
        {
            _context.AccountTokens.Add(new IdentityUserToken<string> { UserId = id, LoginProvider = TokenProvider, Name = purpose, Value = value });
        }

        return token;
    }

    // Uses a token up: returns what it carries when it is the one that was told, for this purpose, and has not
    // ended – and nothing otherwise. The row goes with the step it is used in.
    private string? Take(string id, string purpose, string token)
    {
        var held = _context.AccountTokens.SingleOrDefault(candidate => candidate.UserId == id && candidate.LoginProvider == TokenProvider && candidate.Name == purpose);
        if (held?.Value is not { } value || Read(value) is not var (hash, expires, _, carries)
            || expires < Now || !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(hash), Encoding.ASCII.GetBytes(Hash(token))))
        {
            return null;
        }

        _context.AccountTokens.Remove(held);
        return carries;
    }

    // Whether a mail with a token of this purpose was sent so lately that the next one waits.
    private bool SentLately(string id, string purpose) =>
        _context.AccountTokens.AsNoTracking().SingleOrDefault(candidate => candidate.UserId == id && candidate.LoginProvider == TokenProvider && candidate.Name == purpose)?.Value is { } value
        && Read(value) is var (_, _, sent, _) && sent + SendAgainAfter > Now;

    private static (string Hash, DateTimeOffset Expires, DateTimeOffset Sent, string Carries)? Read(string value) =>
        value.Split('|') is [var hash, var expires, var sent, var carries]
        && long.TryParse(expires, NumberStyles.None, CultureInfo.InvariantCulture, out var until) && long.TryParse(sent, NumberStyles.None, CultureInfo.InvariantCulture, out var at)
            ? (hash, DateTimeOffset.FromUnixTimeSeconds(until), DateTimeOffset.FromUnixTimeSeconds(at), Encoding.UTF8.GetString(Convert.FromBase64String(carries)))
            : null;

    private static string Hash(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    // The mail is written in the step of what it tells of: kept with it, or not at all.
    private void Put(OutgoingMail mail) => Put(_context.Database.CurrentTransaction!, mail);

    private void Put(IDbContextTransaction step, OutgoingMail mail) =>
        MailOutbox.Put(_context.Database.GetDbConnection(), step.GetDbTransaction(), mail, Now.UtcDateTime);
}
