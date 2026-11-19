using Vafadar.Dastan.Store;

namespace Vafadar.Dastan.Content;

/// <summary>
/// The commands of the accounts of the editors (step 11.7). An account is made where the store lies, by whoever
/// runs the tool of the editors: the tool itself has no page that makes one. A password is typed when the command
/// asks for it and is never one of its words – the words of a command stay in the history of a shell.
/// </summary>
internal static class EditorCommands
{
    public const string Usage = """

          store editors --store <database>
              Lists the editors the store knows.

          store editor add <name> --store <database> [--roles <roles>]
          store editor password <name> --store <database>
              Makes the account of an editor, or gives the editor another password. The password is asked
              for and typed twice; it has at least 12 characters. Another password signs the editor out
              everywhere.

          store editor roles <name> <roles> --store <database>
              Says what an editor may do: writer (changes records, texts, stages, comparisons), reviewer
              (records reviews), publisher (publishes a revision) – with commas between them – or all.
              An account that is made without roles may do everything.

          store editor close <name> --store <database>
          store editor open <name> --store <database>
              Closes an account – it signs in nowhere, what the editor did stays in the history – or opens
              it again.

          Wherever a command asks for a store, it is the connection of the PostgreSQL database; the word
          env stands for the one in the variable DASTAN_DATABASE. A copy of a database is made with
          pg_dump, and brought back with pg_restore (the operations guide says how).
        """;

    /// <summary>Runs a command of the accounts; <see langword="null"/> when it was not understood.</summary>
    /// <param name="words">The words after <c>store</c>.</param>
    /// <param name="output">Where the answer is written.</param>
    /// <param name="time">The clock of the store.</param>
    /// <param name="secret">Asks for a text that is not shown while it is typed; nothing when nobody can be asked.</param>
    public static int? Run(IReadOnlyList<string> words, TextWriter output, TimeProvider time, Func<string, string?>? secret)
    {
        switch (words)
        {
            case ["editors", "--store", var database]:
                using (var store = EditorialStore.Open(OperatorCommands.Connection(database), time))
                {
                    var editors = store.Editors();
                    output.WriteLine($"{editors.Length} {(editors.Length == 1 ? "editor" : "editors")}.");
                    foreach (var editor in editors)
                    {
                        var state = editor.Closed ? "  closed" : editor.LockedUntil is { } until ? $"  takes no password until {until:yyyy-MM-dd HH:mm} UTC" : null;
                        output.WriteLine($"  {editor.Name}  since {editor.CreatedAt:yyyy-MM-dd}  {(editor.Roles == EditorRoles.None ? "reads only" : EditorialStore.RolesText(editor.Roles))}{state}");
                    }

                    return ContentCommands.Done;
                }

            case ["editor", "roles", var name, var written, "--store", var database]:
                if (EditorialStore.ReadRoles(written) is not { } given)
                {
                    output.WriteLine($"{written}: roles are writer, reviewer and publisher with commas between them, or all.");
                    return ContentCommands.NotUsable;
                }

                return Said(output, database, time, $"{name}: {(given == EditorRoles.None ? "reads only" : EditorialStore.RolesText(given))}.", store => store.SetRoles(name, given));

            case ["editor", "add", var name, "--roles", var written, "--store", var database]:
                return Run(["editor", "add", name, "--store", database, "--roles", written], output, time, secret);

            case ["editor", "add", var name, "--store", var database, "--roles", var written]:
                if (EditorialStore.ReadRoles(written) is not { } roles)
                {
                    output.WriteLine($"{written}: roles are writer, reviewer and publisher with commas between them, or all.");
                    return ContentCommands.NotUsable;
                }

                return Add(name, database, roles, output, time, secret);

            case ["editor", "add" or "password", var name, "--store", var database]:
                if (words[1] == "add")
                {
                    return Add(name, database, EditorRoles.All, output, time, secret);
                }

                if (secret is null)
                {
                    output.WriteLine("A password is typed where the command runs: run it in a terminal.");
                    return ContentCommands.NotUsable;
                }

                var password = secret($"Password for {name}: ");
                if (password is null || password != secret("The password again: "))
                {
                    output.WriteLine("The two passwords differ. Nothing was changed.");
                    return ContentCommands.NotUsable;
                }

                return Said(output, database, time, $"{name}: has another password and is signed out everywhere.", store => store.SetPassword(name, password));


            case ["editor", "close" or "open", var name, "--store", var database]:
                var closing = words[1] == "close";
                return Said(output, database, time, closing ? $"{name}: the account is closed." : $"{name}: the account is open.", store => store.CloseEditor(name, closing));

            default:
                return null;
        }
    }

    private static int Add(string name, string database, EditorRoles roles, TextWriter output, TimeProvider time, Func<string, string?>? secret)
    {
        if (secret is null)
        {
            output.WriteLine("A password is typed where the command runs: run it in a terminal.");
            return ContentCommands.NotUsable;
        }

        var password = secret($"Password for {name}: ");
        if (password is null || password != secret("The password again: "))
        {
            output.WriteLine("The two passwords differ. Nothing was changed.");
            return ContentCommands.NotUsable;
        }

        return Said(output, database, time, $"{name}: the account was made.", store => store.AddEditor(name, password, roles));
    }

    private static int Said(TextWriter output, string database, TimeProvider time, string done, Func<EditorialStore, ActResult> act)
    {
        using var store = EditorialStore.Open(OperatorCommands.Connection(database), time);
        var result = act(store);
        if (!result.Done)
        {
            foreach (var problem in result.Problems)
            {
                output.WriteLine(problem);
            }

            output.WriteLine("Nothing was changed.");
            return ContentCommands.NotUsable;
        }

        output.WriteLine(result.Changed == 0 ? "Nothing had to be changed." : done);
        return ContentCommands.Done;
    }
}
