using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;

namespace Vafadar.Data;

/// <summary>Associates owned protected native connections with their captured publication check without extending connection lifetime.</summary>
public static class ProtectedSqliteAccess
{
    private static readonly ConditionalWeakTable<SqliteConnection, Access> Connections = new();

    /// <summary>Registers only the actual validated factory connection; no credential material belongs in this registry.</summary>
    internal static void Register(SqliteConnection connection, Action validate) => Connections.Add(connection, new(validate));

    /// <summary>Validates private-material/export access to the exact captured native factory connection; unregistered or retired keyed connections fail closed.</summary>
    public static void Require(SqliteConnection connection)
    {
        if (!Connections.TryGetValue(connection, out var access)) throw new InvalidOperationException("Protected storage requires captured factory access.");
        access.Validate();
    }

    private sealed record Access(Action Validate);
}
