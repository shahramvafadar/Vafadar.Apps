using System.Collections.Concurrent;
using Npgsql;

namespace Vafadar.Dastan.Tests;

/// <summary>
/// A database of its own for a test, on the PostgreSQL the tests are told (step 19.3, ADR 0015): the one of
/// <c>src/Apps/Dastan/compose.yaml</c> on a developer's machine, a service of the job in CI. A test names its
/// database by a key – the path of the folder it would once have kept a file in – and gets the connection of a
/// fresh database with the schema of the product, copied from a template that is made once per process.
/// Without a server the tests that need one are skipped; where <c>DASTAN_POSTGRES_REQUIRED</c> is set – in CI –
/// they fail instead, so that a missing server cannot pass for a green run.
/// </summary>
internal static partial class TestDatabase
{
    private static readonly ConcurrentDictionary<string, string> Made = new(StringComparer.Ordinal);
    private static readonly Lock Gate = new();
    private static readonly string Run = Guid.NewGuid().ToString("N")[..12];
    private static string? _template;
    private static int _next;

    // The tests of a project run side by side and wait for the database on the threads of the pool; some let a
    // dozen requests wait for one lock on purpose. The driver finishes the sending of a large command on a
    // thread of the pool too. A pool that grows by one thread a second then leaves a command half sent for
    // longer than the database waits, and a test fails for a reason that is none. So the pool starts wide.
    [System.Runtime.CompilerServices.ModuleInitializer]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2255:The 'ModuleInitializer' attribute should not be used in libraries", Justification = "A test project, not a library.")]
    internal static void WidePool()
    {
        ThreadPool.GetMinThreads(out var workers, out var ports);
        ThreadPool.SetMinThreads(Math.Max(workers, 128), Math.Max(ports, 128));
    }

    /// <summary>Gets the connection of the server, without a database.</summary>
    public static string Server =>
        Environment.GetEnvironmentVariable("DASTAN_POSTGRES") is { Length: > 0 } told ? told : "Host=127.0.0.1;Port=53432;Username=postgres;Timeout=5";

    /// <summary>Returns the connection of the database of a key, made when it is first asked for.</summary>
    public static string For(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        return Made.GetOrAdd(key, _ =>
        {
            lock (Gate)
            {
                // The key is a path inside the folder of its test. The folder is made here and is the sign that
                // the test still runs: a test takes its folder away when it ends, and the database of a folder
                // that is gone is dropped before the next one is made. The server keeps its data in memory, and
                // thousands of databases that nobody uses any more would not fit.
                var template = Template();
                Sweep();
                if (Path.GetDirectoryName(key) is { Length: > 0 } folder)
                {
                    Directory.CreateDirectory(folder);
                }

                var name = $"dastan_test_{Run}_{Interlocked.Increment(ref _next)}";
                Execute($"CREATE DATABASE \"{name}\" TEMPLATE \"{template}\"");
                return Connection(name);
            }
        });
    }

    private static void Sweep()
    {
        foreach (var (key, connection) in Made)
        {
            if (Path.GetDirectoryName(key) is { Length: > 0 } folder && !Directory.Exists(folder) && Made.TryRemove(key, out _))
            {
                Execute($"DROP DATABASE IF EXISTS \"{new NpgsqlConnectionStringBuilder(connection).Database}\" WITH (FORCE)");
            }
        }
    }

    /// <summary>
    /// Writes a plain question to the store as PostgreSQL reads it: the names of tables and columns are kept as
    /// the program writes them, so they stand in quotes, and a table lies in the schema of the store.
    /// </summary>
    public static string Sql(string plain, string schema = "personal") =>
        System.Text.RegularExpressions.Regex.Replace(
            System.Text.RegularExpressions.Regex.Replace(plain, @"\b[A-Z][A-Za-z]+\b", name => $"\"{name.Value}\""),
            "from (\"[A-Za-z]+\")",
            $"from {schema}.$1");

    /// <summary>Returns the connection of a fresh database that has no table yet, as one is before it is first brought to form.</summary>
    public static string Empty()
    {
        lock (Gate)
        {
            Template();
            var name = $"dastan_test_{Run}_{Interlocked.Increment(ref _next)}";
            Execute($"CREATE DATABASE \"{name}\" TEMPLATE template0 ENCODING 'UTF8' LOCALE 'C'");
            return Connection(name);
        }
    }

    /// <summary>Returns the connection of a database that does not exist.</summary>
    public static string Missing() => Connection($"dastan_test_{Run}_missing");

    // The template: an empty database brought to the form the product knows. Made once; nothing stays
    // connected to it, or it could not be copied.
    private static string Template()
    {
        if (_template is null)
        {
            var name = $"dastan_test_{Run}_template";
            try
            {
                Execute($"CREATE DATABASE \"{name}\" TEMPLATE template0 ENCODING 'UTF8' LOCALE 'C'");
            }
            catch (Exception failed) when (failed is (NpgsqlException and not PostgresException) or TimeoutException or System.Net.Sockets.SocketException)
            {
                var message = $"No PostgreSQL at \"{Server}\": start it with \"docker compose -f src/Apps/Dastan/compose.yaml up -d postgres\", or name another in DASTAN_POSTGRES. ({failed.Message})";
                if (Environment.GetEnvironmentVariable("DASTAN_POSTGRES_REQUIRED") is { Length: > 0 })
                {
                    throw new InvalidOperationException(message, failed);
                }

                Assert.Skip(message);
            }

            Migrate(Connection(name));
            NpgsqlConnection.ClearAllPools();
            AppDomain.CurrentDomain.ProcessExit += (_, _) => Clean();
            _template = name;
        }

        return _template;
    }

    // Connections are kept and used again: a test opens the store hundreds of times, and a machine has only so many
    // ports to open a new connection from in a minute. A database that is dropped ends the ones it had.
    private static string Connection(string database) => new NpgsqlConnectionStringBuilder(Server) { Database = database, MaxPoolSize = 20, ConnectionIdleLifetime = 5, ConnectionPruningInterval = 5 }.ConnectionString;

    private static void Execute(string command)
    {
        using var connection = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(Server) { Database = "postgres" }.ConnectionString);
        connection.Open();
        using var statement = new NpgsqlCommand(command, connection);
        statement.ExecuteNonQuery();
    }

    // What this process made is taken away again; a server that is thrown away with its container needs none of it.
    private static void Clean()
    {
        try
        {
            using var connection = new NpgsqlConnection(new NpgsqlConnectionStringBuilder(Server) { Database = "postgres" }.ConnectionString);
            connection.Open();
            var names = new List<string>();
            using (var list = new NpgsqlCommand($"SELECT datname FROM pg_database WHERE datname LIKE 'dastan_test_{Run}_%'", connection))
            using (var reader = list.ExecuteReader())
            {
                while (reader.Read())
                {
                    names.Add(reader.GetString(0));
                }
            }

            foreach (var name in names)
            {
                using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)", connection);
                drop.ExecuteNonQuery();
            }
        }
        catch (Exception failed) when (failed is NpgsqlException or TimeoutException or InvalidOperationException)
        {
            // Left behind: the next start of the container begins empty.
        }
    }
}
