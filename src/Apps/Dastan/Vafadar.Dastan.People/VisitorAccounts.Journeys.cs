using System.Collections.Immutable;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Vafadar.Dastan.People;

/// <summary>What an account knows about itself, for its own page.</summary>
/// <param name="Id">The identifier of the account.</param>
/// <param name="Name">The name others see.</param>
/// <param name="Address">The mail address, which nobody else sees.</param>
/// <param name="CreatedAt">When it was made.</param>
/// <param name="HasPassword">Whether it has a password; one that was made through a provider has none until its visitor sets one.</param>
/// <param name="RecoveryCodesLeft">How many recovery codes are not used.</param>
/// <param name="Providers">The providers it can be signed in through.</param>
/// <param name="Notices">Whether the site mails it what somebody else asks of it.</param>
public sealed record AccountInfo(string Id, string Name, string Address, DateTime CreatedAt, bool HasPassword, int RecoveryCodesLeft, ImmutableArray<string> Providers, bool Notices = true);

/// <content>
/// What an account does after it is one (step 19.4, ADR 0016): another password, a forgotten one, another
/// address, signing out everywhere, recovery codes, closing and thinking it over. Whatever changes how an
/// account is entered signs it out everywhere and is told to its address.
/// </content>
public sealed partial class VisitorAccounts
{
    /// <summary>How long the link for a forgotten password can be used.</summary>
    public static readonly TimeSpan ResetValidFor = TimeSpan.FromHours(1);

    private const string ResetToken = "reset-password";
    private const string AddressToken = "change-address";

    /// <summary>Returns what an open account knows about itself, or nothing.</summary>
    public AccountInfo? Info(string id)
    {
        var account = _context.Accounts.AsNoTracking().SingleOrDefault(candidate => candidate.Id == id);
        var visitor = _context.Visitors.AsNoTracking().SingleOrDefault(candidate => candidate.Id == id && candidate.ClosedAt == null);
        return account is null || visitor is null
            ? null
            : new AccountInfo(
                id, visitor.Name, account.Email!, visitor.CreatedAt, account.PasswordHash is not null,
                _context.RecoveryCodes.AsNoTracking().Count(code => code.VisitorId == id && code.UsedAt == null),
                [.. _context.AccountLogins.AsNoTracking().Where(login => login.UserId == id).Select(login => login.LoginProvider).OrderBy(provider => provider)],
                account.Notices);
    }

    /// <summary>
    /// Turns the mail about what somebody else asks of an account – a relationship, a tag, an agreement – on
    /// or off (PRIV-007). Returns whether there is such an open account. What concerns the account itself –
    /// its password, its address, a decision of who runs the site – is mailed either way.
    /// </summary>
    public bool SetNotices(string id, bool on) =>
        _store.Guarded([PeopleStore.OfAccount(id)], () =>
        {
            _context.ChangeTracker.Clear();
            var account = _context.Accounts.SingleOrDefault(candidate => candidate.Id == id);
            if (account is null || !_context.Visitors.AsNoTracking().Any(candidate => candidate.Id == id && candidate.ClosedAt == null))
            {
                return false;
            }

            account.Notices = on;
            _context.SaveChanges();
            _context.ChangeTracker.Clear();
            return true;
        });

    /// <summary>
    /// Gives an account another password, proved by the one it has. Everybody who was signed in is signed out;
    /// the answer carries the session of who changed it, and the address of the account is told.
    /// </summary>
    public AccountResult ChangePassword(string id, string current, string password)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(password);
        return Proved(id, current, (account, visitor) =>
        {
            if (PeopleStore.Weak(password) is { } weak)
            {
                return new([weak]);
            }

            account.PasswordHash = _hasher.HashPassword(account, password);
            Put(AccountMail.PasswordChanged(account.Language, _site, account.Email!, $"password-changed:{id}:{Now.ToUnixTimeSeconds()}"));
            return new([]) { Session = Renew(visitor) };
        });
    }

    /// <summary>
    /// Mails the link for a forgotten password to an account named by its name or its address – at the earliest
    /// <see cref="SendAgainAfter"/> after the one before. An account that has not confirmed its address is
    /// sent that link again instead. Who asks is told the same in every case.
    /// </summary>
    public void ForgotPassword(string nameOrMail)
    {
        ArgumentNullException.ThrowIfNull(nameOrMail);
        if (Find(nameOrMail) is not { } found)
        {
            return;
        }

        if (!found.EmailConfirmed)
        {
            SendConfirmationAgain(nameOrMail);
            return;
        }

        _store.Guarded([PeopleStore.OfAccount(found.Id)], () =>
        {
            _context.ChangeTracker.Clear();
            if (SentLately(found.Id, ResetToken) || !_context.Visitors.AsNoTracking().Any(visitor => visitor.Id == found.Id && visitor.ClosedAt == null))
            {
                return false;
            }

            var token = NewToken(found.Id, ResetToken, ResetValidFor);
            Put(AccountMail.ResetPassword(found.Language, _site, found.Email!, found.Id, token, $"reset:{found.Id}:{Hash(token)[..16]}"));
            _context.SaveChanges();
            _context.ChangeTracker.Clear();
            return true;
        });
    }

    /// <summary>
    /// Sets the password of an account with the token of the link that was mailed for a forgotten one. The
    /// token is used once and for <see cref="ResetValidFor"/>. Everybody is signed out, a pause after wrong
    /// passwords ends, and the address is told that the password changed.
    /// </summary>
    public AccountResult ResetPassword(string id, string token, string password)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(token);
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
            if (account is null || visitor is null || Take(id, ResetToken, token) is null)
            {
                return new AccountResult(["wrong"]);
            }

            (account.PasswordHash, account.AccessFailedCount, account.LockoutEnd) = (_hasher.HashPassword(account, password), 0, null);
            Put(AccountMail.PasswordChanged(account.Language, _site, account.Email!, $"password-changed:{id}:{Now.ToUnixTimeSeconds()}"));
            var session = Renew(visitor);
            _context.SaveChanges();
            _context.ChangeTracker.Clear();
            return new AccountResult([]) { Session = session };
        });
    }

    /// <summary>Signs an account out everywhere. The answer carries the session of who asked, who stays signed in.</summary>
    public VisitorSession? SignOutEverywhere(string id) =>
        _store.Guarded([PeopleStore.OfAccount(id)], () =>
        {
            _context.ChangeTracker.Clear();
            if (_context.Visitors.SingleOrDefault(candidate => candidate.Id == id && candidate.ClosedAt == null) is not { } visitor)
            {
                return null;
            }

            var session = Renew(visitor);
            _context.SaveChanges();
            _context.ChangeTracker.Clear();
            return session;
        });

    /// <summary>
    /// Asks for another mail address, proved by the password: the link goes to the new address, and the old one
    /// is told that a change was asked for. Nothing changes before the link is opened. An address that belongs
    /// to another account is answered like any other – and who has it is told that it was tried.
    /// </summary>
    /// <returns>Nothing when the mail was written; <c>wrong</c> for a wrong password, <c>mail</c> for what is no address, <c>same</c> for the address the account has.</returns>
    public ImmutableArray<string> ChangeAddress(string id, string password, string mail)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(mail);
        var address = mail.Trim();
        return Proved(id, password, (account, _) =>
        {
            if (!Data.MailOutbox.IsAddress(address))
            {
                return new(["mail"]);
            }

            var key = new Keys().NormalizeEmail(address)!;
            if (account.NormalizedEmail == key)
            {
                return new(["same"]);
            }

            if (_context.Accounts.AsNoTracking().SingleOrDefault(candidate => candidate.NormalizedEmail == key) is { } holder)
            {
                Put(AccountMail.AddressInUse(holder.Language, _site, holder.Email!, $"address-in-use:{holder.Id}:{Now.UtcDateTime:yyyyMMddHH}"));
                return new([]);
            }

            var token = NewToken(id, AddressToken, ConfirmationValidFor, address);
            Put(AccountMail.ConfirmNewAddress(account.Language, _site, address, id, token, $"new-address:{id}:{Hash(token)[..16]}"));
            Put(AccountMail.AddressChangeAsked(account.Language, _site, account.Email!, $"address-change-asked:{id}:{Hash(token)[..16]}"));
            return new([]);
        }).Problems;
    }

    /// <summary>
    /// Makes the address of the link the address of the account. The token is used once; the old address is
    /// told that the account has another one now, and everybody is signed out. Returns the session of who
    /// opened the link when the address was changed.
    /// </summary>
    public VisitorSession? ConfirmNewAddress(string id, string token)
    {
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(token);
        try
        {
            return _store.Guarded([PeopleStore.OfAccount(id)], () =>
            {
                _context.ChangeTracker.Clear();
                var account = _context.Accounts.SingleOrDefault(candidate => candidate.Id == id);
                var visitor = _context.Visitors.SingleOrDefault(candidate => candidate.Id == id && candidate.ClosedAt == null);
                if (account is null || visitor is null || Take(id, AddressToken, token) is not { Length: > 0 } address)
                {
                    return null;
                }

                var old = account.Email!;
                (account.Email, account.NormalizedEmail, account.EmailConfirmed, account.AddressProof) = (address, new Keys().NormalizeEmail(address), true, VisitorAccount.ProvenByMail);
                Put(AccountMail.AddressChanged(account.Language, _site, old, $"address-changed:{id}:{Now.ToUnixTimeSeconds()}"));
                var session = Renew(visitor);
                _context.SaveChanges();
                _context.ChangeTracker.Clear();
                return session;
            });
        }
        catch (DbUpdateException)
        {
            // Somebody made an account with that address after the link was mailed: it belongs to one account.
            _context.ChangeTracker.Clear();
            return null;
        }
    }

    /// <summary>
    /// Sets a new password with one of the recovery codes of an account – the second way in, for who cannot
    /// read the mail of the address any more. A code is used once; a wrong one counts like a wrong password.
    /// </summary>
    public AccountResult Recover(string nameOrMail, string code, string password)
    {
        ArgumentNullException.ThrowIfNull(nameOrMail);
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(password);
        if (PeopleStore.Weak(password) is { } weak)
        {
            return new([weak]);
        }

        if (Find(nameOrMail) is not { } found)
        {
            return new(["wrong"]);
        }

        var derived = PeopleStore.DeriveCode(code);
        return _store.Guarded([PeopleStore.OfAccount(found.Id)], () =>
        {
            _context.ChangeTracker.Clear();
            var account = _context.Accounts.Single(candidate => candidate.Id == found.Id);
            var visitor = _context.Visitors.SingleOrDefault(candidate => candidate.Id == found.Id && candidate.ClosedAt == null);
            if (visitor is null || account.LockoutEnd > Now)
            {
                return new AccountResult(["wrong"]);
            }

            var now = Now.UtcDateTime;
            var used = _context.RecoveryCodes
                .Where(candidate => candidate.VisitorId == found.Id && candidate.Code == derived && candidate.UsedAt == null)
                .ExecuteUpdate(change => change.SetProperty(candidate => candidate.UsedAt, (DateTime?)now));
            if (used == 0)
            {
                Miss(account);
                _context.SaveChanges();
                _context.ChangeTracker.Clear();
                return new AccountResult(["wrong"]);
            }

            // Who holds a code of the account has shown to be its visitor. That says nothing about the address:
            // the codes are given when the address is confirmed, and an account that was asked to confirm it
            // anew (step 20.3) stays asked.
            (account.PasswordHash, account.AccessFailedCount, account.LockoutEnd) = (_hasher.HashPassword(account, password), 0, null);
            Put(AccountMail.PasswordChanged(account.Language, _site, account.Email!, $"password-changed:{account.Id}:{Now.ToUnixTimeSeconds()}"));
            var session = Renew(visitor);
            _context.SaveChanges();
            _context.ChangeTracker.Clear();
            return new AccountResult([]) { Session = session };
        });
    }

    /// <summary>Gives an account new recovery codes, proved by its password; the ones before stop working.</summary>
    public AccountResult NewRecoveryCodes(string id, string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        return Proved(id, password, (_, _) =>
        {
            _context.RecoveryCodes.RemoveRange(_context.RecoveryCodes.Where(code => code.VisitorId == id));
            return new([]) { RecoveryCodes = _store.NewCodes(id) };
        });
    }

    /// <summary>
    /// Closes an account, proved by its password: it is shown nowhere and signed out everywhere at once, and
    /// deleted after <see cref="PeopleStore.KeptAfterClosing"/> unless its visitor signs in again before.
    /// </summary>
    public AccountResult Close(string id, string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        return Proved(id, password, (account, visitor) =>
        {
            visitor.ClosedAt = Now.UtcDateTime;
            Renew(visitor);
            Put(AccountMail.Closed(account.Language, _site, account.Email!, $"closed:{id}:{Now.ToUnixTimeSeconds()}"));
            return new([]);
        });
    }

    /// <summary>Opens an account again that its visitor closed, by its name or address and its password, within the time it is kept.</summary>
    public VisitorSession? Reopen(string nameOrMail, string password)
    {
        ArgumentNullException.ThrowIfNull(nameOrMail);
        ArgumentNullException.ThrowIfNull(password);
        if (Find(nameOrMail) is not { PasswordHash: not null } found)
        {
            _hasher.VerifyHashedPassword(Nobody, Nobody.PasswordHash!, password);
            return null;
        }

        var tried = _hasher.VerifyHashedPassword(found, found.PasswordHash, password);
        return _store.Guarded([PeopleStore.OfAccount(found.Id)], () =>
        {
            _context.ChangeTracker.Clear();
            var account = _context.Accounts.Single(candidate => candidate.Id == found.Id);
            var visitor = _context.Visitors.SingleOrDefault(candidate => candidate.Id == found.Id && candidate.ClosedAt != null && !candidate.ClosedByOperator);
            if (visitor is null || account.LockoutEnd > Now)
            {
                return null;
            }

            // A closed account counts wrong passwords like an open one: it is not guessed open.
            if (tried == PasswordVerificationResult.Failed)
            {
                Miss(account);
                _context.SaveChanges();
                _context.ChangeTracker.Clear();
                return null;
            }

            (account.AccessFailedCount, visitor.ClosedAt) = (0, null);
            var session = Renew(visitor);
            _context.SaveChanges();
            _context.ChangeTracker.Clear();
            return session;
        });
    }

    /// <summary>
    /// Returns whether a name or address and a password are those of an account, open or closed – counted like
    /// a sign-in – and the identifier of the account when they are: how somebody proves to be the visitor of an
    /// account that who runs the site closed, to be told why.
    /// </summary>
    public string? Proves(string nameOrMail, string password)
    {
        ArgumentNullException.ThrowIfNull(nameOrMail);
        ArgumentNullException.ThrowIfNull(password);
        if (Find(nameOrMail) is not { PasswordHash: not null } found)
        {
            _hasher.VerifyHashedPassword(Nobody, Nobody.PasswordHash!, password);
            return null;
        }

        var tried = _hasher.VerifyHashedPassword(found, found.PasswordHash, password);
        return _store.Guarded([PeopleStore.OfAccount(found.Id)], () =>
        {
            _context.ChangeTracker.Clear();
            var account = _context.Accounts.Single(candidate => candidate.Id == found.Id);
            if (account.LockoutEnd > Now)
            {
                return null;
            }

            if (tried == PasswordVerificationResult.Failed)
            {
                Miss(account);
            }
            else
            {
                account.AccessFailedCount = 0;
            }

            _context.SaveChanges();
            _context.ChangeTracker.Clear();
            return tried == PasswordVerificationResult.Failed ? null : account.Id;
        });
    }

    /// <summary>Returns whether a password is the one of an open account – counted like a sign-in – before something that needs the proof is done.</summary>
    public bool ProvesItself(string id, string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        return Proved(id, password, (_, _) => new([])).Done;
    }

    // Does something to an open account whose visitor proved to be it with the password, in one step under the
    // lock of the account: the wrong password is counted, the right one lets the act write what it writes.
    private AccountResult Proved(string id, string password, Func<VisitorAccount, StoredVisitor, AccountResult> act)
    {
        var found = _context.Accounts.AsNoTracking().SingleOrDefault(candidate => candidate.Id == id);
        var tried = _hasher.VerifyHashedPassword(found ?? Nobody, found?.PasswordHash ?? Nobody.PasswordHash!, password);
        if (found?.PasswordHash is null)
        {
            return new(["wrong"]);
        }

        return _store.Guarded([PeopleStore.OfAccount(id)], () =>
        {
            _context.ChangeTracker.Clear();
            var account = _context.Accounts.Single(candidate => candidate.Id == id);
            var visitor = _context.Visitors.SingleOrDefault(candidate => candidate.Id == id && candidate.ClosedAt == null);
            if (visitor is null || account.LockoutEnd > Now)
            {
                return new AccountResult(["wrong"]);
            }

            AccountResult result;
            if (tried == PasswordVerificationResult.Failed)
            {
                Miss(account);
                result = new(["wrong"]);
            }
            else
            {
                account.AccessFailedCount = 0;
                result = act(account, visitor);
            }

            _context.SaveChanges();
            _context.ChangeTracker.Clear();
            return result;
        });
    }

    // One more wrong password or code; at the fifth the account takes none for a while.
    private void Miss(VisitorAccount account)
    {
        account.AccessFailedCount++;
        if (account.AccessFailedCount >= PeopleStore.FailuresAllowed)
        {
            (account.AccessFailedCount, account.LockoutEnd) = (0, Now + PeopleStore.LockedFor);
        }
    }

    // A new stamp: every session of the account that carries the one before is over. The session of who asked.
    private static VisitorSession Renew(StoredVisitor visitor)
    {
        visitor.Stamp = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        return new VisitorSession(visitor.Id, visitor.Name, visitor.Stamp);
    }
}
