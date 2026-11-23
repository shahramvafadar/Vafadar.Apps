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

    /// <summary>
    /// How many days the record of a message that was sent or given up is kept when nothing else is said: it
    /// names an address, and is needed only to answer whether a message left.
    /// </summary>
    public const int MailKeptDays = 30;

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

          mail <database>
              Lists the mail that waits to be sent and the mail sending was given up for – its kind, its
              address and what the last try failed with; never what a message says.

          mail again <key> <database>
              Lets a message that was given up be tried again, as often as a new one.

          mail forget <database> [--kept-days <days>]
              Removes the record of what was sent or given up more than a number of days ago (30 when none
              is named). A message that waits is never removed.

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

          people restrict photo <photo> --people <database> --site <address> --reason <text> --ground <text> [--noticed]
          people restrict tree <tree> --people <database> --site <address> --reason <text> --ground <text> [--noticed]
          people restrict account <name> --people <database> --site <address> --reason <text> --ground <text> [--noticed]
              Takes a photograph out of everybody's sight but its owner's, takes a tree back from the
              public, or closes an account. The reason (the facts) and the ground (the law or the rule of
              the terms) are shown to the person concerned as the statement of reasons, on the page of the
              account and in a mail to its address; --noticed says the decision followed a notice. --site
              is the address of the site, such as https://dastan.online: the link in that mail begins
              with it.

          people lift <number> --people <database> --site <address>
              Lifts a restriction; an account that was closed by it is opened again. The account is told
              by mail.

          people forget <name> --people <database>
              Ends the 30 days a closed account is kept, for somebody who closed the account and asks
              for it to be deleted at once. The site deletes it the next time somebody opens a page of
              an account.

          people addresses --people <database>
              Lists the open accounts whose mail address counts as confirmed while nothing says
              how it was confirmed, with the providers they can be signed in through.

          people addresses ask <provider> --people <database> --site <address>
              Asks those of them that can be signed in through the provider to confirm the address
              anew: the link is mailed, every sign-in of the account ends, and it is an account
              again when the link was opened. Nothing of the account is removed.

          people repair --people <database> [--change]
              Counts who is in a circle or a tree of an account they show no accepted relationship
              with, and names the trees in which somebody is their own ancestor. With --change the
              places in circles and trees are taken away; a tree with a loop is left as it is.

          people persons <tree> --people <database>
              Lists the records of the persons in a tree with their identifiers.

          people person remove <tree> <person> --people <database>
              Takes the record of a person out of a tree, with its links – for a person who objects to
              being in it and whom the owner of the tree did not take out.

          people import-tree <account name> --from <file> --people <database>
              Writes a family tree from a file (dastan-tree-0.1) into the account of that name, as a new,
              private tree of the account – for an account that asked for it. A file that cannot be
              written whole writes nothing, and nothing is added to a tree that is there. The file
              says a name, the years and the family of a person and nothing else, and is kept nowhere.
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

            case ["mail", var database]:
            {
                if (Delivered(database, output) is not { } connection)
                {
                    return ContentCommands.NotUsable;
                }

                var outbox = new MailOutbox(connection);
                var (waiting, givenUp) = (outbox.Waiting(), outbox.GivenUp());
                output.WriteLine($"{waiting.Count} {(waiting.Count == 1 ? "message waits" : "messages wait")} to be sent; {givenUp.Count} {(givenUp.Count == 1 ? "was" : "were")} given up.");
                foreach (var mail in waiting)
                {
                    output.WriteLine($"  waits     {mail.Key}  {mail.Kind}  to {mail.To}  tried {mail.Attempts}×{(mail.LastError is null ? null : $", last: {mail.LastError}")}  next {mail.NextTry:yyyy-MM-dd HH:mm} UTC");
                }

                foreach (var mail in givenUp)
                {
                    output.WriteLine($"  given up  {mail.Key}  {mail.Kind}  to {mail.To}  tried {mail.Attempts}×, last: {mail.LastError}  on {mail.GaveUpAt:yyyy-MM-dd HH:mm} UTC");
                }

                return ContentCommands.Done;
            }

            case ["mail", "again", var key, var database]:
            {
                if (Delivered(database, output) is not { } connection)
                {
                    return ContentCommands.NotUsable;
                }

                if (!new MailOutbox(connection).TryAgain(key, time.GetUtcNow().UtcDateTime))
                {
                    output.WriteLine($"{key}: no message of this key was given up.");
                    return ContentCommands.NotUsable;
                }

                output.WriteLine($"{key}: it is tried again.");
                return ContentCommands.Done;
            }

            case ["mail", "forget", _]:
            case ["mail", "forget", _, "--kept-days", _]:
            {
                var kept = MailKeptDays;
                if (words.Count == 5 && (!int.TryParse(words[4], NumberStyles.None, CultureInfo.InvariantCulture, out kept) || kept > 36500))
                {
                    return null;
                }

                if (Delivered(words[2], output) is not { } connection)
                {
                    return ContentCommands.NotUsable;
                }

                var forgotten = new MailOutbox(connection).Forget(time.GetUtcNow().UtcDateTime.AddDays(-kept));
                output.WriteLine($"{forgotten} {(forgotten == 1 ? "message" : "messages")} that left or were given up more than {kept} days ago removed.");
                return ContentCommands.Done;
            }

            case ["database", "migrate", "--database", var database]:
                try
                {
                    Delivery.Migrate(Connection(database));
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

            case ["people", "restrict", "photo" or "tree" or "account", _, "--people", _, "--site", _, "--reason", _, "--ground", _]:
            case ["people", "restrict", "photo" or "tree" or "account", _, "--people", _, "--site", _, "--reason", _, "--ground", _, "--noticed"]:
                var (target, store0, reason, ground) = (words[3], words[5], words[9], words[11]);
                var kind = words[2] switch { "photo" => RestrictionKind.Photo, "tree" => RestrictionKind.Tree, _ => RestrictionKind.Account };
                return People(store0, output, time, site: words[7], act: store =>
                {
                    if (store.Restrict(kind, target, reason, ground, noticed: words.Count == 13) is not { } number)
                    {
                        output.WriteLine($"{target}: there is no such {words[2]}, or the reason or the ground is missing or longer than {PeopleStore.LongestReason} characters.");
                        return ContentCommands.NotUsable;
                    }

                    output.WriteLine($"{target}: restricted, decision no. {number}. The person concerned reads the statement of reasons on the page of the account{(kind == RestrictionKind.Account ? ", after giving the name and the password" : null)}, and it is mailed to the address of the account.");
                    return ContentCommands.Done;
                });

            case ["people", "lift", var written, "--people", var database, "--site", var address]:
                if (!long.TryParse(written, NumberStyles.None, CultureInfo.InvariantCulture, out var lifted))
                {
                    return null;
                }

                return People(database, output, time, site: address, act: store =>
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

            case ["people", "addresses", "--people", var store1]:
                return People(store1, output, time, store =>
                {
                    // Accounts whose address counts as confirmed and of which nothing says how (ADR 0016, step 20.3).
                    var found = store.Accounts.WithoutProof();
                    output.WriteLine($"{found.Count} {(found.Count == 1 ? "account" : "accounts")} without a record of how the address was confirmed.");
                    foreach (var (name, providers) in found)
                    {
                        output.WriteLine($"  {name}  {string.Join(", ", providers)}");
                    }

                    return ContentCommands.Done;
                });

            case ["people", "addresses", "ask", var provider, "--people", var store2, "--site", var address2]:
                return People(store2, output, time, site: address2, act: store =>
                {
                    var asked = store.Accounts.AskForProof(provider);
                    output.WriteLine($"{asked} {(asked == 1 ? "account was" : "accounts were")} asked to confirm the address anew: the link is mailed, and until it is opened nobody is signed in to them.");
                    return ContentCommands.Done;
                });

            case ["people", "repair", "--people", _]:
            case ["people", "repair", "--people", _, "--change"]:
                var change = words.Count == 5;
                return People(words[3], output, time, store =>
                {
                    // What two requests at the same moment could leave behind before every act took its locks
                    // (ADR 0022). Without --change nothing is written.
                    var found = store.RepairMemberships(change);
                    var verb = change ? "taken out" : "to take out";
                    output.WriteLine($"{found.CircleMembers} {(found.CircleMembers == 1 ? "place" : "places")} in a circle {verb}: no accepted relationship with the owner.");
                    output.WriteLine($"{found.TreeMembers} {(found.TreeMembers == 1 ? "place" : "places")} in a tree {verb}: no accepted relationship with the owner.");
                    output.WriteLine($"{found.TreesWithALoop.Length} {(found.TreesWithALoop.Length == 1 ? "tree" : "trees")} in which somebody is their own ancestor; nothing is changed there, the owner decides which link is wrong.");
                    foreach (var tree in found.TreesWithALoop)
                    {
                        output.WriteLine($"  {tree}");
                    }

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

            case ["people", "import-tree", var account, "--from", var file, "--people", var database]:
                if (!File.Exists(file))
                {
                    output.WriteLine($"{file}: there is no such file.");
                    return ContentCommands.NotUsable;
                }

                var (read, problems) = TreeFile.Read(File.ReadAllText(file));
                if (read is null)
                {
                    output.WriteLine($"{Path.GetFileName(file)}: the file cannot be read as a tree; nothing was written. {problems.Length} {(problems.Length == 1 ? "problem" : "problems")}.");
                    for (var index = 0; index < problems.Length; index++)
                    {
                        output.WriteLine($"  {index + 1}. {problems[index]}");
                    }

                    return ContentCommands.NotUsable;
                }

                return People(database, output, time, store =>
                {
                    var came = store.ImportTree(account, read);
                    if (came.Tree is null)
                    {
                        output.WriteLine($"Nothing was written. {came.Problem}");
                        return ContentCommands.NotUsable;
                    }

                    // What was written is said in numbers: the output of a command names nobody of a family.
                    output.WriteLine($"{account}: a tree was written – {came.People} {(came.People == 1 ? "person" : "persons")}, {came.Links} {(came.Links == 1 ? "link" : "links")}. Its identifier is {came.Tree}.");
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

    private static int People(string database, TextWriter output, TimeProvider time, Func<PeopleStore, int> act, string? site = null)
    {
        // What a command mails links to the site: an address that is none would mail a link that leads nowhere.
        if (site is not null && !(Uri.TryCreate(site, UriKind.Absolute, out var where) && where.Scheme is "https" or "http" && where.PathAndQuery == "/"))
        {
            output.WriteLine($"{site}: this is not the address of a site, such as https://dastan.online.");
            return ContentCommands.NotUsable;
        }

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

            using var store = PeopleStore.Open(database, time, site);
            return act(store);
        }
        catch (Exception failed) when (failed is System.Data.Common.DbException or ArgumentException or System.Net.Sockets.SocketException or TimeoutException)
        {
            output.WriteLine($"The database cannot be reached: {failed.Message}");
            return ContentCommands.NotUsable;
        }
    }
}
