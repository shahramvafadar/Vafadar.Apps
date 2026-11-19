using System.Globalization;
using Vafadar.Dastan.Data;
using Vafadar.Dastan.People;

namespace Vafadar.Dastan.Content;

/// <summary>
/// The commands of who runs the site for what the law asks of a place that keeps what others wrote
/// (Regulation (EU) 2022/2065, Articles 16 and 17, and Regulation (EU) 2016/679, Articles 17 and 21): reading
/// the notices about content, deciding on them, restricting a photograph, a tree or an account with a statement
/// of reasons, lifting a restriction, and taking the record of a person out of a tree when that person objects.
/// The site does none of this by itself; every act here is one of a person.
/// </summary>
internal static class OperatorCommands
{
    /// <summary>How many days a notice is kept after it was settled, when no other number is named.</summary>
    public const int NoticesKeptDays = 365;

    public const string Usage = """

          notices <database>
              Lists the notices about content that wait for a decision, the oldest first. The database is
              named by its connection, or by the word env for the one in the variable DASTAN_DATABASE.

          notices show <reference> <database>
              Shows one notice in full: where the content is, what is said to be wrong with it, and who
              gave it when they said so.

          notices settle <reference> <database> --decision <text>
              Notes what was decided on a notice and takes it out of the waiting ones. The decision is told
              to who gave the notice by mail, when they left an address: the command sends nothing.

          notices forget <database> [--kept-days <days>]
              Removes the settled notices that were decided more than a number of days ago (365 when none
              is named).

          database migrate --database env
              Makes the tables of the site in its PostgreSQL database, or brings them to the form this
              program knows. Run it before a new version of the site is started; the site itself changes
              no table. Wherever a command asks for a database, the word env stands for the connection in
              the variable DASTAN_DATABASE – a connection holds a password and is never typed as a word.

          database grant site|tool <role> --database env
              Gives a role of the database what the site or the tool of the editors needs, and takes from
              it everything else: the site reads what was released and never the work of the editors; the
              tool never reaches what belongs to visitors. The role is made beforehand by who runs the
              database, with a password this program never sees; the connection here is the one of the
              role that made the tables. Run it after "database migrate".

          people restrictions --people <database>
              Lists the restrictions that stand.

          people restrict photo <photo> --people <database> --reason <text> --ground <text> [--noticed]
          people restrict tree <tree> --people <database> --reason <text> --ground <text> [--noticed]
          people restrict account <name> --people <database> --reason <text> --ground <text> [--noticed]
              Takes a photograph out of everybody's sight but its owner's, takes a tree back from the
              public, or closes an account. The reason (the facts) and the ground (the law or the rule of
              the terms) are shown to the person concerned as the statement of reasons; --noticed says the
              decision followed a notice. Stop the site first, or run the command where the site's store
              lies while few people are on it: both write into the same file.

          people lift <number> --people <database>
              Lifts a restriction; an account that was closed by it is opened again.

          people forget <name> --people <database>
              Ends the 30 days a closed account is kept, for somebody who closed the account and asks
              for it to be deleted at once. The site deletes it the next time somebody opens a page of
              an account.

          people persons <tree> --people <database>
              Lists the records of the persons in a tree with their identifiers.

          people person remove <tree> <person> --people <database>
              Takes the record of a person out of a tree, with its links – for a person who objects to
              being in it and whom the owner of the tree did not take out.
        """;

    /// <summary>Runs a command of the operator; <see langword="null"/> when it was not understood.</summary>
    public static int? Run(IReadOnlyList<string> words, TextWriter output, TimeProvider time)
    {
        switch (words)
        {
            case ["notices", var folder]:
                return Folder(folder, output) is { } box ? List(box, output) : ContentCommands.NotUsable;

            case ["notices", "show", var reference, var folder]:
                return Folder(folder, output) is { } shown ? Show(shown, reference, output) : ContentCommands.NotUsable;

            case ["notices", "settle", var reference, var folder, "--decision", var decision]:
                if (Folder(folder, output) is not { } settled)
                {
                    return ContentCommands.NotUsable;
                }

                if (string.IsNullOrWhiteSpace(decision) || !settled.Settle(reference, decision, time.GetUtcNow().UtcDateTime))
                {
                    output.WriteLine($"{reference}: no notice of this reference waits, or no decision was written.");
                    return ContentCommands.NotUsable;
                }

                output.WriteLine($"{reference}: settled.");
                return ContentCommands.Done;

            case ["notices", "forget", var folder]:
                return Forget(folder, NoticesKeptDays, output, time);

            case ["notices", "forget", var folder, "--kept-days", var written]:
                return int.TryParse(written, NumberStyles.None, CultureInfo.InvariantCulture, out var days) && days <= 36500 ? Forget(folder, days, output, time) : null;

            case ["database", "migrate", "--database", var database]:
                try
                {
                    PeopleStore.Migrate(Connection(database));
                    Vafadar.Dastan.Store.EditorialStore.Migrate(Connection(database));
                }
                catch (Exception failed) when (failed is System.Data.Common.DbException or ArgumentException or System.Net.Sockets.SocketException or TimeoutException)
                {
                    output.WriteLine($"The database cannot be reached or changed: {failed.Message}");
                    return ContentCommands.NotUsable;
                }

                output.WriteLine("The database has the form this program knows.");
                return ContentCommands.Done;

            case ["database", "grant", var program, var role, "--database", var database]:
                if (!DatabaseRoles.Programs.Contains(program) || !DatabaseRoles.IsName(role))
                {
                    return null;
                }

                if (string.IsNullOrWhiteSpace(Connection(database)))
                {
                    output.WriteLine("No database: set DASTAN_DATABASE to its connection and name it with the word env.");
                    return ContentCommands.NotUsable;
                }

                try
                {
                    DatabaseRoles.Grant(Connection(database), program, role);
                }
                catch (Exception failed) when (failed is System.Data.Common.DbException or ArgumentException or System.Net.Sockets.SocketException or TimeoutException)
                {
                    output.WriteLine($"The role was given nothing: {failed.Message}");
                    return ContentCommands.NotUsable;
                }

                output.WriteLine($"{role} may do in the database what the {program} does, and no more.");
                return ContentCommands.Done;

            case ["people", "restrictions", "--people", var database]:
                return People(database, output, time, store =>
                {
                    var standing = store.AllRestrictions();
                    output.WriteLine($"{standing.Count} {(standing.Count == 1 ? "restriction stands" : "restrictions stand")}.");
                    foreach (var (restriction, account) in standing)
                    {
                        output.WriteLine($"  {restriction.Number}  {restriction.At:yyyy-MM-dd}  {Kind(restriction.Kind)}  {restriction.Target ?? "-"}  of {account}{(restriction.Noticed ? "  (after a notice)" : null)}");
                        output.WriteLine($"      reason: {restriction.Reason}");
                        output.WriteLine($"      ground: {restriction.Ground}");
                    }

                    return ContentCommands.Done;
                });

            case ["people", "restrict", "photo" or "tree" or "account", _, "--people", _, "--reason", _, "--ground", _]:
            case ["people", "restrict", "photo" or "tree" or "account", _, "--people", _, "--reason", _, "--ground", _, "--noticed"]:
                var (target, store0, reason, ground) = (words[3], words[5], words[7], words[9]);
                var kind = words[2] switch { "photo" => RestrictionKind.Photo, "tree" => RestrictionKind.Tree, _ => RestrictionKind.Account };
                return People(store0, output, time, store =>
                {
                    if (store.Restrict(kind, target, reason, ground, noticed: words.Count == 11) is not { } number)
                    {
                        output.WriteLine($"{target}: there is no such {words[2]}, or the reason or the ground is missing or longer than {PeopleStore.LongestReason} characters.");
                        return ContentCommands.NotUsable;
                    }

                    output.WriteLine($"{target}: restricted, decision no. {number}. The person concerned reads the statement of reasons on the page of the account{(kind == RestrictionKind.Account ? ", after giving the name and the password" : null)}.");
                    return ContentCommands.Done;
                });

            case ["people", "lift", var written, "--people", var database]:
                if (!long.TryParse(written, NumberStyles.None, CultureInfo.InvariantCulture, out var lifted))
                {
                    return null;
                }

                return People(database, output, time, store =>
                {
                    if (!store.Lift(lifted))
                    {
                        output.WriteLine($"{lifted}: no restriction of this number stands.");
                        return ContentCommands.NotUsable;
                    }

                    output.WriteLine($"{lifted}: lifted.");
                    return ContentCommands.Done;
                });

            case ["people", "forget", var name, "--people", var database]:
                return People(database, output, time, store =>
                {
                    if (!store.ForgetSooner(name))
                    {
                        output.WriteLine($"{name}: there is no closed account of this name. An account is closed by its visitor, or with people restrict account.");
                        return ContentCommands.NotUsable;
                    }

                    output.WriteLine($"{name}: the account is deleted the next time the site deletes what was closed.");
                    return ContentCommands.Done;
                });

            case ["people", "persons", var tree, "--people", var database]:
                return People(database, output, time, store =>
                {
                    var persons = store.PersonsOfTree(tree);
                    output.WriteLine($"{persons.Count} {(persons.Count == 1 ? "person" : "persons")}.");
                    foreach (var (id, name) in persons)
                    {
                        output.WriteLine($"  {id}  {name}");
                    }

                    return ContentCommands.Done;
                });

            case ["people", "person", "remove", var tree, var person, "--people", var database]:
                return People(database, output, time, store =>
                {
                    if (!store.RemovePersonByOperator(tree, person))
                    {
                        output.WriteLine($"{person}: the tree {tree} has no such person.");
                        return ContentCommands.NotUsable;
                    }

                    output.WriteLine($"{person}: taken out of the tree {tree}.");
                    return ContentCommands.Done;
                });

            default:
                return null;
        }
    }

    private static string Kind(RestrictionKind kind) => kind switch { RestrictionKind.Photo => "photo", RestrictionKind.Tree => "tree", _ => "account" };

    private static NoticeBox? Folder(string database, TextWriter output) =>
        Delivered(database, output) is { } connection ? new NoticeBox(connection) : null;

    /// <summary>
    /// Returns the connection of a database that has the tables of reports and notices, as a command was told
    /// it (<see cref="Connection"/>); nothing, with the reason written, for one that is not named, cannot be
    /// reached or lacks the tables.
    /// </summary>
    internal static string? Delivered(string database, TextWriter output)
    {
        database = Connection(database);
        if (string.IsNullOrWhiteSpace(database))
        {
            output.WriteLine("No database: set DASTAN_DATABASE to its connection and name it with the word env.");
            return null;
        }

        try
        {
            if (Delivery.Missing(database) is not { } missing)
            {
                return database;
            }

            output.WriteLine(missing);
        }
        catch (Exception failed) when (failed is System.Data.Common.DbException or ArgumentException or System.Net.Sockets.SocketException or TimeoutException)
        {
            output.WriteLine($"The database cannot be reached: {failed.Message}");
        }

        return null;
    }

    private static int List(NoticeBox box, TextWriter output)
    {
        var waiting = box.Waiting().OrderBy(notice => notice.ReceivedAt).ToList();
        output.WriteLine($"{waiting.Count} {(waiting.Count == 1 ? "notice waits" : "notices wait")}.");
        foreach (var notice in waiting)
        {
            output.WriteLine($"  {notice.Id}  {notice.ReceivedAt:yyyy-MM-dd HH:mm} UTC  {notice.Kind}  {notice.Address}{(notice.Mail is { Length: > 0 } ? "  (left an address)" : null)}");
        }

        return ContentCommands.Done;
    }

    private static int Show(NoticeBox box, string reference, TextWriter output)
    {
        var notice = box.Waiting().Concat(box.AllSettled()).FirstOrDefault(candidate => candidate.Id == reference);
        if (notice is null)
        {
            output.WriteLine($"{reference}: there is no notice of this reference.");
            return ContentCommands.NotUsable;
        }

        output.WriteLine($"reference:   {notice.Id}");
        output.WriteLine($"received:    {notice.ReceivedAt:yyyy-MM-dd HH:mm} UTC, in {notice.Language}");
        output.WriteLine($"where:       {notice.Address}");
        output.WriteLine($"what:        {notice.Kind}");
        output.WriteLine($"from:        {(notice.Name is { Length: > 0 } name ? name : "(no name)")}, {(notice.Mail is { Length: > 0 } mail ? mail : "(no address)")}");
        output.WriteLine($"good faith:  {(notice.GoodFaith ? "declared" : "not declared")}");
        output.WriteLine("explanation:");
        output.WriteLine(notice.Explanation);
        if (notice.SettledAt is { } at)
        {
            output.WriteLine($"settled:     {at:yyyy-MM-dd HH:mm} UTC");
            output.WriteLine($"decision:    {notice.Decision}");
        }

        return ContentCommands.Done;
    }

    private static int? Forget(string folder, int days, TextWriter output, TimeProvider time)
    {
        if (Folder(folder, output) is not { } box)
        {
            return ContentCommands.NotUsable;
        }

        var forgotten = box.Forget(time.GetUtcNow().UtcDateTime.AddDays(-days));
        output.WriteLine($"{forgotten} settled {(forgotten == 1 ? "notice" : "notices")} removed.");
        return ContentCommands.Done;
    }

    /// <summary>
    /// Returns the connection a command works on. The word <c>env</c> stands for the variable
    /// <c>DASTAN_DATABASE</c>: a connection holds a password, and a password is never a word of a command –
    /// the words of a command stay in the history of a shell.
    /// </summary>
    internal static string Connection(string written) =>
        written == "env" ? Environment.GetEnvironmentVariable("DASTAN_DATABASE") ?? string.Empty : written;

    private static int People(string database, TextWriter output, TimeProvider time, Func<PeopleStore, int> act)
    {
        database = Connection(database);
        if (string.IsNullOrWhiteSpace(database))
        {
            output.WriteLine("No database: set DASTAN_DATABASE to its connection and name it with the word env.");
            return ContentCommands.NotUsable;
        }

        // A store is never made by a slip of the hand: the command works on the one that is there.
        try
        {
            if (PeopleStore.Pending(database).Count > 0)
            {
                output.WriteLine("The database does not have the form this program knows: run \"database migrate\" first.");
                return ContentCommands.NotUsable;
            }

            using var store = PeopleStore.Open(database, time);
            return act(store);
        }
        catch (Exception failed) when (failed is System.Data.Common.DbException or ArgumentException or System.Net.Sockets.SocketException or TimeoutException)
        {
            output.WriteLine($"The database cannot be reached: {failed.Message}");
            return ContentCommands.NotUsable;
        }
    }
}
