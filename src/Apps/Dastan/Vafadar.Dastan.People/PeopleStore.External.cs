using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Vafadar.Core.Security;

namespace Vafadar.Dastan.People;

/// <content>
/// Signing in through somebody else (step 16.8, D-048): Google or Microsoft says "this is the same person as last
/// time", and nothing else is taken from them – no mail address, no name, no picture. What is kept is which
/// provider it was and a hash of the identifier the provider gave; the identifier itself is not kept.
/// </content>
public sealed partial class PeopleStore
{
    /// <summary>Signs in the account a sign-in of a provider belongs to, or returns <see langword="null"/> when there is none or it is closed.</summary>
    public VisitorSession? SignInWith(string provider, string subject)
    {
        var key = LoginKey(provider, subject);
        var login = _context.ExternalLogins.AsNoTracking().SingleOrDefault(candidate => candidate.Provider == provider && candidate.Key == key);
        var visitor = login is null ? null : _context.Visitors.AsNoTracking().SingleOrDefault(candidate => candidate.Id == login.VisitorId && candidate.ClosedAt == null);
        return visitor is null ? null : new VisitorSession(visitor.Id, visitor.Name, visitor.Stamp);
    }

    /// <summary>
    /// Makes an account for somebody who signed in through a provider. They choose its name themselves – none is
    /// taken from the provider. The account has no password until its visitor sets one, and gets recovery codes
    /// like every account.
    /// </summary>
    public AccountResult RegisterWith(string name, string provider, string subject, string? invitation = null, bool invitationNeeded = false)
    {
        ArgumentNullException.ThrowIfNull(name);
        var written = name.Trim();
        if (NameProblem(written) is { } wrong)
        {
            return new([wrong]);
        }

        var key = LoginKey(provider, subject);
        if (_context.ExternalLogins.AsNoTracking().Any(candidate => candidate.Provider == provider && candidate.Key == key))
        {
            // The sign-in belongs to an account already – perhaps a closed one. It makes no second account.
            return new(["login-taken"]);
        }

        return Create(written, string.Empty, invitation, new StoredExternalLogin { Provider = provider, Key = key, VisitorId = string.Empty }, invitationNeeded);
    }

    /// <summary>
    /// Lets an account be signed in through a provider from now on. Refused when that sign-in belongs to another
    /// account, or the account has one of that provider already.
    /// </summary>
    public bool Link(string id, string provider, string subject)
    {
        var key = LoginKey(provider, subject);
        if (!IsOpen(id) || _context.ExternalLogins.AsNoTracking().Any(candidate => candidate.Provider == provider && (candidate.Key == key || candidate.VisitorId == id)))
        {
            return false;
        }

        _context.ExternalLogins.Add(new StoredExternalLogin { Provider = provider, Key = key, VisitorId = id });
        try
        {
            Save();
        }
        catch (DbUpdateException)
        {
            // The same was asked twice at the same moment: the store keeps one sign-in of a provider for an
            // account, and one account for a sign-in.
            _context.ChangeTracker.Clear();
            return false;
        }

        return true;
    }

    /// <summary>
    /// Takes a provider away from an account. Refused when it is the only way into the account: an account keeps
    /// a password or another provider.
    /// </summary>
    public bool Unlink(string id, string provider)
    {
        var logins = _context.ExternalLogins.Where(candidate => candidate.VisitorId == id).ToList();
        var login = logins.FirstOrDefault(candidate => candidate.Provider == provider);
        var visitor = _context.Visitors.AsNoTracking().SingleOrDefault(candidate => candidate.Id == id && candidate.ClosedAt == null);
        if (login is null || visitor is null || (visitor.Password.Length == 0 && logins.Count == 1))
        {
            _context.ChangeTracker.Clear();
            return false;
        }

        _context.ExternalLogins.Remove(login);
        Save();
        return true;
    }

    /// <summary>
    /// Gives a password to an account that has none – one that was made through a provider. An account that has
    /// a password changes it with <see cref="ChangePassword"/>, which asks for the present one.
    /// </summary>
    public AccountResult SetPassword(string id, string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        var visitor = _context.Visitors.SingleOrDefault(candidate => candidate.Id == id && candidate.ClosedAt == null);
        try
        {
            if (visitor is null || visitor.Password.Length > 0)
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

    // The provider's identifier of a person is not kept: a hash of it finds the account again and tells nobody
    // who reads the store which account of the provider it is.
    private static string LoginKey(string provider, string subject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{provider}\n{subject}")));
    }
}
