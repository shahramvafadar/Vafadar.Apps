using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Vafadar.Dastan.People;

/// <content>
/// Signing in through Google or Microsoft (step 19.4, ADR 0016). A provider is asked who somebody is and for
/// the mail address it confirmed, nothing else. A first sign-in makes an account with that address – confirmed
/// already, by the provider. An address that an account has makes no second account and signs nobody in: the
/// visitor signs in to that account and links the provider there. Linking is always an act of who is signed in.
/// </content>
public sealed partial class VisitorAccounts
{
    /// <summary>Signs in the open account a sign-in of a provider belongs to; nothing when it belongs to none.</summary>
    public VisitorSession? SignInWith(string provider, string subject)
    {
        var key = PeopleStore.LoginKey(provider, subject);
        var login = _context.AccountLogins.AsNoTracking().SingleOrDefault(candidate => candidate.LoginProvider == provider && candidate.ProviderKey == key);
        var visitor = login is null ? null : _context.Visitors.AsNoTracking().SingleOrDefault(candidate => candidate.Id == login.UserId && candidate.ClosedAt == null);
        return visitor is null ? null : new VisitorSession(visitor.Id, visitor.Name, visitor.Stamp);
    }

    /// <summary>
    /// Makes an account for somebody who signed in through a provider for the first time and chose a name. The
    /// account has the address the provider confirmed and no password; it gets its recovery codes at once.
    /// Problems by their names: <c>name-length</c>, <c>name-letters</c>, <c>taken</c>, <c>mail</c>,
    /// <c>login-taken</c> (the sign-in belongs to an account, perhaps a closed one),
    /// <c>address-has-account</c> (sign in to that account and link the provider there),
    /// <c>invitation-needed</c>.
    /// </summary>
    /// <param name="name">The name others see.</param>
    /// <param name="provider">The provider.</param>
    /// <param name="subject">Who the provider says this is.</param>
    /// <param name="mail">The mail address the provider confirmed.</param>
    /// <param name="language">The language of the page.</param>
    /// <param name="invitation">The invitation the account is made through, if any.</param>
    /// <param name="invitationNeeded">Whether no account is made without an invitation that can be used.</param>
    /// <param name="terms">The version of the terms and the privacy notice the visitor said they read.</param>
    public AccountResult RegisterWith(string name, string provider, string subject, string mail, string language, string? invitation = null, bool invitationNeeded = false, string? terms = null)
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

        var login = PeopleStore.LoginKey(provider, subject);
        var key = PeopleStore.Key(written);
        var mailKey = new Keys().NormalizeEmail(address)!;
        using var step = _context.Database.BeginTransaction();
        _context.ChangeTracker.Clear();
        if (_context.AccountLogins.AsNoTracking().Any(candidate => candidate.LoginProvider == provider && candidate.ProviderKey == login))
        {
            return new(["login-taken"]);
        }

        // Who comes with an address the provider confirmed has that address: telling them that it has an
        // account tells them nothing about anybody else.
        if (_context.Accounts.AsNoTracking().Any(candidate => candidate.NormalizedEmail == mailKey))
        {
            return new(["address-has-account"]);
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
            Id = Guid.NewGuid().ToString("N"), UserName = written, Email = address, EmailConfirmed = true, LockoutEnabled = true,
            Language = language is { Length: > 0 and <= 20 } ? language : "en",
        };
        StoredVisitor visitor;
        System.Collections.Immutable.ImmutableArray<string> codes;
        try
        {
            var made = Manager.CreateAsync(account).GetAwaiter().GetResult();
            if (!made.Succeeded)
            {
                return new([made.Errors.Any(error => error.Code is "DuplicateUserName") ? "taken" : made.Errors.Any(error => error.Code is "DuplicateEmail") ? "address-has-account" : "mail"]);
            }

            var now = Now.UtcDateTime;
            var kept = terms is { Length: > 0 and <= 20 };
            visitor = new StoredVisitor
            {
                Id = account.Id, Name = written, NameKey = key, Password = string.Empty, Stamp = account.SecurityStamp!, CreatedAt = now,
                TermsVersion = kept ? terms : null, TermsAcceptedAt = kept ? now : null,
            };
            _context.Visitors.Add(visitor);
            _context.AccountLogins.Add(new IdentityUserLogin<string> { LoginProvider = provider, ProviderKey = login, ProviderDisplayName = provider, UserId = account.Id });
            codes = _store.NewCodes(account.Id);
            _context.SaveChanges();
        }
        catch (DbUpdateException)
        {
            // Somebody took the name, the address or the sign-in between the question and the answer.
            step.Rollback();
            _context.ChangeTracker.Clear();
            return new([_context.AccountLogins.AsNoTracking().Any(candidate => candidate.LoginProvider == provider && candidate.ProviderKey == login) ? "login-taken"
                : _context.Accounts.AsNoTracking().Any(candidate => candidate.NormalizedEmail == mailKey) ? "address-has-account"
                : "taken"]);
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
        return new([]) { Session = new VisitorSession(visitor.Id, visitor.Name, visitor.Stamp), RecoveryCodes = codes };
    }

    /// <summary>
    /// Links a sign-in of a provider to an open account – the act of who is signed in to it. An account has one
    /// sign-in of a provider, and a sign-in belongs to one account. Returns whether it was linked.
    /// </summary>
    public bool Link(string id, string provider, string subject)
    {
        var key = PeopleStore.LoginKey(provider, subject);
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
}
