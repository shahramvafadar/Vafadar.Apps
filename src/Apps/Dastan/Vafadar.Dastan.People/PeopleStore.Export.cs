using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Vafadar.Core.Security;
using Vafadar.Dastan.Core.Personal;

namespace Vafadar.Dastan.People;

/// <content>
/// A copy of what the store holds about an account, for its visitor to take away (step 16.7, D-055). It holds
/// what the account may see anyway, in words: no password and nothing derived from one, no recovery code, and
/// other accounts by their names only.
/// </content>
public sealed partial class PeopleStore
{
    /// <summary>The version of the form of a copy. It grows when the form changes.</summary>
    public const int ExportVersion = 1;

    /// <summary>
    /// Returns everything the store holds about an open account, proved by its password, or
    /// <see langword="null"/> when the password is wrong.
    /// </summary>
    public JsonObject? Export(string id, string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        var visitor = _context.Visitors.AsNoTracking().SingleOrDefault(candidate => candidate.Id == id && candidate.ClosedAt == null);
        if (visitor is null || !PasswordHash.Matches(password, visitor.Password))
        {
            return null;
        }

        var belongings = Belongings(id);
        var circles = Circles(id);
        var trees = Trees(id);
        return new JsonObject
        {
            ["format"] = "dastan-account",
            ["version"] = ExportVersion,
            ["madeAt"] = Stamp(Now),
            ["account"] = new JsonObject
            {
                ["name"] = visitor.Name,
                ["createdAt"] = Stamp(visitor.CreatedAt),
                ["recoveryCodesLeft"] = _context.RecoveryCodes.AsNoTracking().Count(code => code.VisitorId == id && code.UsedAt == null),
                ["signInThrough"] = new JsonArray([.. _context.ExternalLogins.AsNoTracking().Where(login => login.VisitorId == id).Select(login => login.Provider).OrderBy(provider => provider).AsEnumerable().Select(provider => (JsonNode)provider)]),
            },
            ["readingPositions"] = new JsonArray([.. belongings.Positions.OrderBy(position => position.Key, StringComparer.Ordinal)
                .Select(position => (JsonNode)new JsonObject { ["collection"] = position.Key, ["stage"] = position.Value })]),
            ["list"] = new JsonArray([.. belongings.Kept
                .Select(page => (JsonNode)new JsonObject { ["collection"] = page.Collection, ["kind"] = page.Kind, ["id"] = page.Id })]),
            ["invitations"] = new JsonArray([.. Invitations(id)
                .Select(invitation => (JsonNode)new JsonObject
                {
                    ["code"] = invitation.Code,
                    ["lastDay"] = invitation.LastDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    ["state"] = invitation.RunOut ? "run out" : invitation.State.ToString().ToLowerInvariant(),
                })]),
            ["relationships"] = new JsonArray([.. Relationships(id)
                .Select(relationship => (JsonNode)new JsonObject
                {
                    ["with"] = relationship.Other.Name,
                    ["kind"] = relationship.Kind,
                    ["state"] = relationship.State.ToString().ToLowerInvariant(),
                    ["askedByMe"] = relationship.AskedByMe,
                    ["shownTo"] = relationship.Shown switch
                    {
                        ScopeKind.Public => "everybody",
                        ScopeKind.Circle => circles.FirstOrDefault(circle => circle.Number == relationship.Circle)?.Name ?? "nobody else",
                        _ => "nobody else",
                    },
                })]),
            ["circles"] = new JsonArray([.. circles
                .Select(circle => (JsonNode)new JsonObject
                {
                    ["name"] = circle.Name,
                    ["members"] = new JsonArray([.. circle.Members.Select(member => (JsonNode)member.Name)]),
                })]),
            ["photographs"] = new JsonArray([.. Photos(id)
                .Select(photo => (JsonNode)new JsonObject
                {
                    ["caption"] = photo.Caption,
                    ["taken"] = photo.Taken?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    ["broughtInAt"] = Stamp(photo.SharedAt),
                    ["shownTo"] = photo.Shown switch
                    {
                        ScopeKind.Public => "everybody",
                        ScopeKind.Circle => circles.FirstOrDefault(circle => circle.Number == photo.Circle)?.Name ?? "nobody else",
                        _ => "nobody else",
                    },
                    ["file"] = photo.File.Name,
                    ["shows"] = new JsonArray([.. photo.Tags.Select(tag => (JsonNode)new JsonObject { ["account"] = tag.Person.Name, ["state"] = tag.State.ToString().ToLowerInvariant() })]),
                })]),
            ["trees"] = new JsonArray([.. trees.Where(tree => tree.Role == TreeRole.Owner).Select(tree => (JsonNode)Written(Tree(id, tree.Id)!))]),
            ["treesSharedWithMe"] = new JsonArray([.. trees.Where(tree => tree.Role != TreeRole.Owner)
                .Select(tree =>
                {
                    // A tree of somebody else is theirs: the copy says that it is shared and which record is this account.
                    var shared = Tree(id, tree.Id)!;
                    return (JsonNode)new JsonObject
                    {
                        ["name"] = tree.Name,
                        ["owner"] = tree.Owner.Name,
                        ["role"] = tree.Role.ToString().ToLowerInvariant(),
                        ["recordThatIsMe"] = shared.People.FirstOrDefault(person => person.Account?.Id == id)?.Name,
                        ["recordIClaim"] = shared.Claims.Where(claim => claim.Mine).Select(claim => shared.Person(claim.Person).Name).FirstOrDefault(),
                        ["iAgreedToBeNamedInPublic"] = shared.Publication.Consented,
                    };
                })]),
        };
    }

    private static JsonObject Written(TreeView tree) => new()
    {
        ["name"] = tree.Tree.Name,
        ["public"] = tree.Publication.IsPublic,
        ["people"] = new JsonArray([.. tree.People
            .Select(person => (JsonNode)new JsonObject
            {
                ["name"] = person.Name,
                ["born"] = person.Born,
                ["died"] = person.Died,
                ["deceased"] = person.Deceased,
                ["account"] = person.Account?.Name,
            })]),
        ["links"] = new JsonArray([.. tree.Links
            .Select(link => (JsonNode)new JsonObject { ["kind"] = link.Kind, ["from"] = tree.Person(link.From).Name, ["to"] = tree.Person(link.To).Name })]),
        ["sharedWith"] = new JsonArray([.. tree.Members
            .Select(member => (JsonNode)new JsonObject { ["account"] = member.Account.Name, ["role"] = member.Role.ToString().ToLowerInvariant() })]),
        ["history"] = new JsonArray([.. tree.History
            .Select(change => (JsonNode)new JsonObject { ["at"] = Stamp(change.At), ["by"] = change.By, ["what"] = change.What, ["subject"] = change.Subject })]),
    };

    private static string Stamp(DateTime at) => at.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
}
