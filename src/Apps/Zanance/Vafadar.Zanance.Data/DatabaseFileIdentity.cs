using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Vafadar.Zanance.Data;

/// <summary>Retains the original configured SQLite target independently of native connection reporting aliases.</summary>
internal static class DatabaseFileIdentity
{
    /// <summary>Reads the actual connection's configured file without opening it, changing aliases or guessing another profile.</summary>
    internal static string ConfiguredPath(ZananceDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        // D-160: Android SQLite reports /data/data while open but MAUI configures /data/user/0.
        // Those native reporting aliases must not retire the same reviewed target. The configuration remains
        // bound to this actual context; another factory/file still fails the original exact-file guards.
        var source = new SqliteConnectionStringBuilder(db.Database.GetDbConnection().ConnectionString).DataSource;
        return Path.GetFullPath(source);
    }
}
