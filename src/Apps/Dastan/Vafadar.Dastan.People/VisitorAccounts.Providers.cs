using System.Collections.Immutable;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Vafadar.Dastan.People;

/// <content>
/// Signing in through Google or Microsoft (steps 19.4 and 20.3, ADR 0016). A provider is asked who somebody is
/// and for a mail address, nothing else. Who somebody is at the provider finds their account; an address never
/// does. A first sign-in makes an account: with an address the provider says it verified, at once; with an
/// address the provider only names, or one the visitor wrote, when the visitor has confirmed it by a mail of
/// the site – until then the account is none, whichever way somebody comes. An address that an account has
/// makes no second account and signs nobody in. Linking is always an act of who is signed in.
/// </content>
public sealed partial class VisitorAccounts
{
    // The provider's identifier of a person is not kept: a hash of it finds the account again and tells nobody
    // who reads the database which account of the provider it is.
    internal static string LoginKey(string provider, string subject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{provider}\n{subject}")));
    }

    /// <summary>Signs in the open account a sign-in of a provider belongs to; nothing when it belongs to none.</summary>
    public VisitorSession? SignInWith(string provider, string subject)
    {
        var key = LoginKey(provider, subject);
        var login = _context.AccountLogins.AsNoTracking().SingleOrDefault(candidate => candidate.LoginProvider == provider && candidate.ProviderKey == key);
        var visitor = login is null ? null : _context.Visitors.AsNoTracking().SingleOrDefault(candidate => candidate.Id == login.UserId && candidate.ClosedAt == null);

        // Who the provider says somebody is, is one thing; that the address of the account is theirs is
        // another. An account whose address is not confirmed is none yet, whichever way somebody comes.
        return visitor is null || !_context.Accounts.AsNoTracking().Any(account => account.Id == visitor.Id && account.EmailConfirmed)
            ? null
            : new VisitorSession(visitor.Id, visitor.Name, visitor.Stamp);
    }

    /// <summary>
    /// Returns whether a sign-in of a provider belongs to an open account that still waits for its address to
    /// be confirmed. Who comes with that sign-in made the account, so telling them tells nobody else anything.
    /// </summary>
    public bool Waits(string provider, string subject) => Waiting(provider, subject) is not null;

    // The account a sign-in of a provider made that has not confirmed its address.
    private VisitorAccount? Waiting(string provider, string subject)
    {
        var key = LoginKey(provider, subject);
        var id = _context.AccountLogins.AsNoTracking().Where(candidate => candidate.LoginProvider == provider && candidate.ProviderKey == key).Select(candidate => candidate.UserId).SingleOrDefault();
        return id is null || !_context.Visitors.AsNoTracking().Any(visitor => visitor.Id == id && visitor.ClosedAt == null)
            ? null
            : _context.Accounts.AsNoTracking().SingleOrDefault(account => account.Id == id && !account.EmailConfirmed);
    }

    /// <summary>
    /// Mails the link that confirms the address once more to an account a sign-in of a provider made and that
    /// still waits – to the address it has or, when <paramref name="mail"/> is given, to that one instead: who
    /// mistyped the address can correct it, and nobody else can, because only who signs in at the provider
    /// comes here. At the earliest <see cref="SendAgainAfter"/> after the mail before. An address that has an
    /// account is not taken; its holder is told that somebody tried, and who asked is told what everybody is
    /// told. Returns <c>sent</c>, or <c>mail</c> for something that is no address and <c>none</c> when no
    /// account waits.
    /// </summary>
    public string ConfirmWaiting(string provider, string subject, string? mail = null)
    {
        var address = mail?.Trim();
        if (address is { Length: > 0 } && !Data.MailOutbox.IsAddress(address))
        {
            return "mail";
        }

        if (Waiting(provider, subject) is not { } found)
        {
            return "none";
        }

        return _store.Guarded([PeopleStore.OfAccount(found.Id)], () =>
        {
            _context.ChangeTracker.Clear();
            var account = _context.Accounts.SingleOrDefault(candidate => candidate.Id == found.Id && !candidate.EmailConfirmed);
            if (account is null || SentLately(account.Id, ConfirmToken))
            {
                return "sent";
            }

            if (address is { Length: > 0 } && !string.Equals(address, account.Email, StringComparison.OrdinalIgnoreCase))
            {
                var key = new Keys().NormalizeEmail(address)!;
                if (_context.Accounts.AsNoTracking().SingleOrDefault(candidate => candidate.NormalizedEmail == key) is { } holder)
                {
                    Put(AccountMail.AddressInUse(holder.Language, _site, holder.Email!, $"address-in-use:{holder.Id}:{Now.UtcDateTime:yyyyMMddHH}"));
                    _context.ChangeTracker.Clear();
                    return "sent";
                }

                (account.Email, account.NormalizedEmail) = (address, key);
            }

            var name = _context.Visitors.AsNoTracking().Where(visitor => visitor.Id == account.Id).Select(visitor => visitor.Name).Single();
            var token = NewToken(account.Id, ConfirmToken, ConfirmationValidFor);
            Put(AccountMail.Confirm(account.Language, _site, account.Email!, name, account.Id, token, $"confirm:{account.Id}:{Hash(token)[..16]}"));
            _context.SaveChanges();
            _context.ChangeTracker.Clear();
            return "sent";
        });
    }

    /// <summary>
    /// Makes an account for somebody who signed in through a provider for the first time and chose a name. It
    /// has no password. With <paramref name="proven"/> – the provider said it had verified the address – the
    /// account has that address and gets its recovery codes at once. Without, the address is only what somebody
    /// wrote: the account is made, the link that confirms the address is mailed there, and the answer carries
    /// no sign-in and no codes; an address that has an account makes none, its holder is told that somebody
    /// tried, and the answer is the same.
    /// Problems by their names: <c>name-length</c>, <c>name-letters</c>, <c>taken</c>, <c>mail</c>,
    /// <c>login-taken</c> (the sign-in belongs to an account, perhaps a closed one),
    /// <c>address-has-account</c> (sign in to that account and link the provider there),
    /// <c>invitation-needed</c>.
    /// </summary>
    /// <param name="name">The name others see.</param>
    /// <param name="provider">The provider.</param>
    /// <param name="subject">Who the provider says this is.</param>
    /// <param name="mail">The mail address: the one the provider verified, or the one the visitor wrote.</param>
    /// <param name="language">The language of the page.</param>
    /// <param name="invitation">The invitation the account is made through, if any.</param>
    /// <param name="invitationNeeded">Whether no account is made without an invitation that can be used.</param>
    /// <param name="terms">The version of the terms and the privacy notice the visitor said they read.</param>
    /// <param name="proven">Whether the provider said it had verified that the address is the visitor's. A field of an answer that only names an address is no such word.</param>
    public AccountResult RegisterWith(string name, string provider, string subject, string mail, string language, string? invitation = null, bool invitationNeeded = false, string? terms = null, bool proven = false)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(mail);
        var written = name.Trim();
        var address = mail.Trim();
        if (PeopleStore.NameProblem(written) is { } wrong)
        {
            return new([wrong]);
        }

        if (!Data.MailOutbox.IsAddress(address))
        {
            return new(["mail"]);
        }

        var login = LoginKey(provider, subject);
        var key = PeopleStore.Key(written);
        var mailKey = new Keys().NormalizeEmail(address)!;
        using var step = _context.Database.BeginTransaction();
        _context.ChangeTracker.Clear();
        if (_context.AccountLogins.AsNoTracking().Any(candidate => candidate.LoginProvider == provider && candidate.ProviderKey == login))
        {
            return new(["login-taken"]);
        }

        if (_context.Accounts.AsNoTracking().SingleOrDefault(candidate => candidate.NormalizedEmail == mailKey) is { } holder)
        {
            // Who comes with an address the provider verified has that address: telling them that it has an
            // account tells them nothing about anybody else. Who only wrote an address is told what everybody
            // is told, and the holder of the address that somebody tried – once an hour at most.
            if (proven)
            {
                return new(["address-has-account"]);
            }

            Put(step, AccountMail.AddressInUse(holder.Language, _site, holder.Email!, $"address-in-use:{holder.Id}:{Now.UtcDateTime:yyyyMMddHH}"));
            step.Commit();
            return new([]);
        }

        if (_context.Visitors.AsNoTracking().Any(visitor => visitor.NameKey == key) || _context.Accounts.AsNoTracking().Any(candidate => candidate.NormalizedUserName == key))
        {
            return new(["taken"]);
        }

        var invited = _store.Usable(invitation)?.Id;
        _context.ChangeTracker.Clear();
        if (invitationNeeded && invited is null)
        {
            return new(["invitation-needed"]);
        }

        var account = new VisitorAccount
        {
            Id = Guid.NewGuid().ToString("N"), UserName = written, Email = address, EmailConfirmed = proven, LockoutEnabled = true,
            AddressProof = proven ? $"provider:{provider}" : null,
            Language = language is { Length: > 0 and <= 20 } ? language : "en",
        };
        StoredVisitor visitor;
        System.Collections.Immutable.ImmutableArray<string> codes;
        try
        {
            var made = Manager.CreateAsync(account).GetAwaiter().GetResult();
            if (!made.Succeeded)
            {
                // An address somebody took in this very moment is answered like one that was taken before.
                return made.Errors.Any(error => error.Code is "DuplicateUserName") ? new(["taken"])
                    : made.Errors.Any(error => error.Code is "DuplicateEmail") ? new(proven ? ["address-has-account"] : [])
                    : new(["mail"]);
            }

            var now = Now.UtcDateTime;
            var kept = terms is { Length: > 0 and <= 20 };
            visitor = new StoredVisitor
            {
                Id = account.Id, Name = written, NameKey = key, Stamp = account.SecurityStamp!, CreatedAt = now,
                TermsVersion = kept ? terms : null, TermsAcceptedAt = kept ? now : null,
            };
            _context.Visitors.Add(visitor);
            _context.AccountLogins.Add(new IdentityUserLogin<string> { LoginProvider = provider, ProviderKey = login, ProviderDisplayName = provider, UserId = account.Id });
            if (proven)
            {
                codes = _store.NewCodes(account.Id);
            }
            else
            {
                // The codes come with the confirmation, like for an account with a password.
                codes = [];
                var token = NewToken(account.Id, ConfirmToken, ConfirmationValidFor);
                Put(step, AccountMail.Confirm(account.Language, _site, address, written, account.Id, token, $"confirm:{account.Id}:{Hash(token)[..16]}"));
            }

            _context.SaveChanges();
        }
        catch (DbUpdateException)
        {
            // Somebody took the name, the address or the sign-in between the question and the answer.
            step.Rollback();
            _context.ChangeTracker.Clear();
            return _context.AccountLogins.AsNoTracking().Any(candidate => candidate.LoginProvider == provider && candidate.ProviderKey == login) ? new(["login-taken"])
                : _context.Accounts.AsNoTracking().Any(candidate => candidate.NormalizedEmail == mailKey) ? new(proven ? ["address-has-account"] : [])
                : new(["taken"]);
        }

        _context.ChangeTracker.Clear();
        if (invited is not null)
        {
            var today = _store.Today;
            var used = _context.Invitations
                .Where(candidate => candidate.Id == invited && candidate.UsedOn == null && candidate.RevokedAt == null)
                .ExecuteUpdate(change => change.SetProperty(candidate => candidate.UsedBy, account.Id).SetProperty(candidate => candidate.UsedOn, today));
            if (used == 0 && invitationNeeded)
            {
                step.Rollback();
                return new(["invitation-needed"]);
            }
        }

        step.Commit();
        return proven ? new([]) { Session = new VisitorSession(visitor.Id, visitor.Name, visitor.Stamp), RecoveryCodes = codes } : new([]);
    }

    /// <summary>
    /// Links a sign-in of a provider to an open account – the act of who is signed in to it. An account has one
    /// sign-in of a provider, and a sign-in belongs to one account. Returns whether it was linked.
    /// </summary>
    public bool Link(string id, string provider, string subject)
    {
        var key = LoginKey(provider, subject);
        _context.ChangeTracker.Clear();
        if (!_context.Visitors.AsNoTracking().Any(visitor => visitor.Id == id && visitor.ClosedAt == null) || !_context.Accounts.AsNoTracking().Any(account => account.Id == id)
            || _context.AccountLogins.AsNoTracking().Any(candidate => candidate.LoginProvider == provider && (candidate.ProviderKey == key || candidate.UserId == id)))
        {
            return false;
        }

        _context.AccountLogins.Add(new IdentityUserLogin<string> { LoginProvider = provider, ProviderKey = key, ProviderDisplayName = provider, UserId = id });
        try
        {
            _context.SaveChanges();
            _context.ChangeTracker.Clear();
            return true;
        }
        catch (DbUpdateException)
        {
            // The same was asked twice at the same moment: the database keeps one of them.
            _context.ChangeTracker.Clear();
            return false;
        }
    }

    /// <summary>
    /// Takes the sign-in of a provider from an account. Not the last way in: an account without a password
    /// keeps its only provider until it has a password.
    /// </summary>
    public bool Unlink(string id, string provider) =>
        _store.Guarded([PeopleStore.OfAccount(id)], () =>
        {
            _context.ChangeTracker.Clear();
            var logins = _context.AccountLogins.Where(candidate => candidate.UserId == id).ToList();
            var account = _context.Accounts.AsNoTracking().SingleOrDefault(candidate => candidate.Id == id);
            if (logins.FirstOrDefault(candidate => candidate.LoginProvider == provider) is not { } login || account is null
                || !_context.Visitors.AsNoTracking().Any(visitor => visitor.Id == id && visitor.ClosedAt == null) || (account.PasswordHash is null && logins.Count == 1))
            {
                _context.ChangeTracker.Clear();
                return false;
            }

            _context.AccountLogins.Remove(login);
            _context.SaveChanges();
            _context.ChangeTracker.Clear();
            return true;
        });

    /// <summary>
    /// Gives a password to an account that has none – one that was made through a provider. An account that
    /// has a password changes it with the one it has. Everybody is signed out, and the address is told.
    /// </summary>
    public AccountResult SetPassword(string id, string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        if (PeopleStore.Weak(password) is { } weak)
        {
            return new([weak]);
        }

        return _store.Guarded([PeopleStore.OfAccount(id)], () =>
        {
            _context.ChangeTracker.Clear();
            var account = _context.Accounts.SingleOrDefault(candidate => candidate.Id == id);
            var visitor = _context.Visitors.SingleOrDefault(candidate => candidate.Id == id && candidate.ClosedAt == null);
            if (account is null || visitor is null || account.PasswordHash is not null)
            {
                return new AccountResult(["wrong"]);
            }

            account.PasswordHash = _hasher.HashPassword(account, password);
            Put(AccountMail.PasswordChanged(account.Language, _site, account.Email!, $"password-changed:{id}:{Now.ToUnixTimeSeconds()}"));
            var session = Renew(visitor);
            _context.SaveChanges();
            _context.ChangeTracker.Clear();
            return new AccountResult([]) { Session = session };
        });
    }

    /// <summary>
    /// Returns the open accounts whose address counts as confirmed and of which nothing says how it was
    /// confirmed, each with the providers it can be signed in through – what was made before the site wrote that
    /// down (step 20.3). An account that never had a provider is not among them: it confirmed by mail.
    /// </summary>
    public IReadOnlyList<(string Name, System.Collections.Immutable.ImmutableArray<string> Providers)> WithoutProof()
    {
        var accounts = _context.Accounts.AsNoTracking().Where(account => account.EmailConfirmed && account.AddressProof == null).Select(account => account.Id).ToList();
        var names = _context.Visitors.AsNoTracking().Where(visitor => accounts.Contains(visitor.Id) && visitor.ClosedAt == null).ToDictionary(visitor => visitor.Id, visitor => visitor.Name, StringComparer.Ordinal);
        var logins = _context.AccountLogins.AsNoTracking().Where(login => accounts.Contains(login.UserId)).Select(login => new { login.UserId, login.LoginProvider }).ToList();
        return [.. names.OrderBy(name => name.Value, StringComparer.OrdinalIgnoreCase)
            .Select(name => (name.Value, logins.Where(login => login.UserId == name.Key).Select(login => login.LoginProvider).Order(StringComparer.Ordinal).ToImmutableArray()))];
    }

    /// <summary>
    /// Asks the open accounts that can be signed in through a provider, and of whose address nothing says how
    /// it was confirmed, to confirm it anew: the address counts as not confirmed, every sign-in of the account
    /// is over, and the link is mailed to the address. The account, what belongs to it and its ways in stay as
    /// they are; with the link it is an account again. An account that is asked once is not among them a second
    /// time. Returns how many were asked.
    /// </summary>
    public int AskForProof(string provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        var asked = 0;
        var ids = _context.Accounts.AsNoTracking()
            .Where(account => account.EmailConfirmed && account.AddressProof == null && _context.AccountLogins.Any(login => login.UserId == account.Id && login.LoginProvider == provider))
            .Select(account => account.Id).ToList();
        foreach (var id in ids)
        {
            asked += _store.Guarded([PeopleStore.OfAccount(id)], () =>
            {
                _context.ChangeTracker.Clear();
                var account = _context.Accounts.SingleOrDefault(candidate => candidate.Id == id && candidate.EmailConfirmed && candidate.AddressProof == null);
                var visitor = _context.Visitors.SingleOrDefault(candidate => candidate.Id == id && candidate.ClosedAt == null);
                if (account is null || visitor is null || account.Email is not { Length: > 0 } address)
                {
                    return 0;
                }

                account.EmailConfirmed = false;
                Renew(visitor);
                var token = NewToken(id, ConfirmToken, ConfirmationValidFor);
                Put(AccountMail.Confirm(account.Language, _site, address, visitor.Name, id, token, $"confirm:{id}:{Hash(token)[..16]}"));
                _context.SaveChanges();
                _context.ChangeTracker.Clear();
                return 1;
            });
        }

        return asked;
    }
}
