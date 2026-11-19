using Microsoft.EntityFrameworkCore;

namespace Vafadar.Dastan.People;

/// <content>
/// Which terms an account said it read. The site notes the version and the moment with a new account; when
/// the terms change, the account page asks again, and a visitor who agrees is noted with the new version.
/// Nothing is agreed to by silence: the version of an account changes only by an act of its visitor.
/// </content>
public sealed partial class PeopleStore
{
    /// <summary>Returns the version of the terms an open account said it read, or nothing when it never said so.</summary>
    public string? TermsOf(string id) =>
        _context.Visitors.AsNoTracking().Where(visitor => visitor.Id == id && visitor.ClosedAt == null).Select(visitor => visitor.TermsVersion).SingleOrDefault();

    /// <summary>
    /// Ends the time a closed account is kept: for somebody who closed the account and asks for it to be
    /// deleted at once (Article 17 of Regulation (EU) 2016/679). The site deletes it, with its files, the next
    /// time it deletes what was closed long enough ago. Returns whether there is such a closed account.
    /// </summary>
    public bool ForgetSooner(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var key = Key(name.Trim());
        var visitor = _context.Visitors.SingleOrDefault(candidate => candidate.NameKey == key && candidate.ClosedAt != null);
        if (visitor is null)
        {
            return false;
        }

        visitor.ClosedAt = Now - KeptAfterClosing - TimeSpan.FromSeconds(1);
        Save();
        return true;
    }

    /// <summary>Notes that the visitor of an open account read a version of the terms. Returns whether there is such an account.</summary>
    public bool AcceptTerms(string id, string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        var visitor = _context.Visitors.SingleOrDefault(candidate => candidate.Id == id && candidate.ClosedAt == null);
        if (visitor is null || version.Length > 20)
        {
            return false;
        }

        if (visitor.TermsVersion != version)
        {
            (visitor.TermsVersion, visitor.TermsAcceptedAt) = (version, Now);
            Save();
        }

        return true;
    }
}
