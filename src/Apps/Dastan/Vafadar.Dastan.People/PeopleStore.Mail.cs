using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Vafadar.Dastan.Data;

namespace Vafadar.Dastan.People;

/// <content>
/// What the store tells an account by mail about what others did (step 19.4, ADR 0016): written in the step
/// of what it tells of, so that the one exists exactly when the other does.
/// </content>
public sealed partial class PeopleStore
{
    /// <summary>
    /// Makes an invitation and mails it to an address, in the language of the page it was asked on. Returns
    /// <c>sent</c>, or why not: <c>mail</c> (no address), <c>invitations</c> (the account has as many open
    /// invitations as it may). The address is kept for nothing else: it stands in the message until that is
    /// sent, and the invitation does not know it.
    /// </summary>
    public string InviteByMail(string id, string mail, string language)
    {
        ArgumentNullException.ThrowIfNull(mail);
        ArgumentNullException.ThrowIfNull(language);
        var to = mail.Trim();
        if (!MailOutbox.IsAddress(to))
        {
            return "mail";
        }

        return Guarded([OfAccount(id)], () =>
        {
            // The address of somebody who blocked this account gets nothing from it, and the account is not
            // told (PRIV-009): the answer is the one of a message that was sent.
            var owner = _context.Accounts.AsNoTracking().Where(account => account.NormalizedEmail == to.ToUpperInvariant()).Select(account => account.Id).SingleOrDefault();
            if (owner is not null && Blocks(owner, id))
            {
                return "sent";
            }

            if (InviteUnguarded(id) is not { } code)
            {
                return "invitations";
            }

            var invitation = _context.Invitations.AsNoTracking().Single(stored => stored.Id == code);
            var from = _context.Visitors.AsNoTracking().Where(visitor => visitor.Id == id).Select(visitor => visitor.Name).Single();
            Put(NoticeMail.Invitation(language, _site, to, from, code, invitation.LastDay, $"invitation:{code}"));
            return "sent";
        });
    }

    // Tells an open account something, in the language of the account: always, or – a message about what
    // somebody else asked – only while the account takes such messages. An account whose address is not
    // confirmed is no account yet and is told nothing.
    private void Tell(string account, bool always, Func<VisitorAccount, OutgoingMail> write)
    {
        var to = _context.Accounts.AsNoTracking().SingleOrDefault(candidate => candidate.Id == account);
        if (to is { EmailConfirmed: true, Email.Length: > 0 } && (always || to.Notices))
        {
            Put(write(to));
        }
    }

    // The name others see of an account.
    private string NameOf(string account) =>
        _context.Visitors.AsNoTracking().Where(visitor => visitor.Id == account).Select(visitor => visitor.Name).SingleOrDefault() ?? string.Empty;

    // Writes a message into the outbox in the step that is under way.
    private void Put(OutgoingMail mail) =>
        MailOutbox.Put(
            _context.Database.GetDbConnection(),
            (_context.Database.CurrentTransaction ?? throw new InvalidOperationException("A mail is written in the step of what it tells of.")).GetDbTransaction(),
            mail,
            Now);
}
