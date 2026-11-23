using Microsoft.EntityFrameworkCore;
using Vafadar.Temporal;

namespace Vafadar.Dastan.People;

/// <summary>What came of writing a tree from a file.</summary>
/// <param name="Tree">The identifier of the tree that was written; nothing when nothing was written.</param>
/// <param name="People">How many people were written.</param>
/// <param name="Links">How many links were written.</param>
/// <param name="Problem">Why nothing was written; nothing when the tree was.</param>
public sealed record TreeImport(string? Tree, int People, int Links, string? Problem);

/// <content>
/// A family tree written from a file (delivery plan, 25.2): what who runs the site does for an account that
/// asked for it. The tree is written through the acts of the pages – so every rule of a tree holds – and in
/// one piece: a file that cannot be written whole writes nothing. It is the account's own, private tree; it is
/// shared with nobody, and no record of it is said to be an account (a claim is reviewed by somebody else,
/// PRIV-004).
/// </content>
public sealed partial class PeopleStore
{
    /// <summary>Writes a tree file into the account of a name as a new tree of that account.</summary>
    /// <param name="account">The name of the account, as it is shown.</param>
    /// <param name="file">The tree.</param>
    public TreeImport ImportTree(string account, TreeFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        var key = Key((account ?? string.Empty).Trim());
        var id = _context.Visitors.AsNoTracking().Where(visitor => visitor.NameKey == key && visitor.ClosedAt == null).Select(visitor => visitor.Id).SingleOrDefault();
        if (id is null)
        {
            return new TreeImport(null, 0, 0, $"{account}: there is no open account of this name. An account is made by its visitor on the site.");
        }

        try
        {
            return Guarded([OfAccount(id)], () =>
            {
                if (Named(file.Name) is not { } name)
                {
                    throw new TreeImportRefused($"{file.Name}: this is not a name a tree can have.");
                }

                if (_context.Trees.AsNoTracking().Any(tree => tree.Owner == id && tree.Name == name))
                {
                    throw new TreeImportRefused($"{name}: the account has a tree of this name already. Nothing is added to a tree that is there; change it on the site, or give the tree of the file another name.");
                }

                var tree = AddTree(id, name) ?? throw new TreeImportRefused("The account owns as many trees as an account may.");
                var written = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var person in file.People)
                {
                    written[person.Key] = AddPerson(id, tree, person.Name, Year(person.Born), Year(person.Died), person.Deceased)
                        ?? throw new TreeImportRefused($"{person.Key}: the record cannot be written – its name is not a name, or its years are not years a person lived.");
                }

                foreach (var link in file.Links)
                {
                    var came = Link(id, tree, link.Kind, written[link.From], written[link.To]);
                    if (came != "done")
                    {
                        throw new TreeImportRefused($"{link.From} – {link.To}: the link cannot be written ({came}).");
                    }
                }

                return new TreeImport(tree, file.People.Length, file.Links.Length, null);
            });
        }
        catch (TreeImportRefused refused)
        {
            return new TreeImport(null, 0, 0, refused.Message);
        }

        static TemporalExpression? Year(int? year) =>
            year is { } stated ? new Occurrence(CalendarPosition.OfYear(CalendarSystem.Gregorian, Era.CommonEra, stated)) : null;
    }

    // Leaves the transaction of an import, so that nothing of it is written.
    private sealed class TreeImportRefused(string message) : Exception(message);
}
