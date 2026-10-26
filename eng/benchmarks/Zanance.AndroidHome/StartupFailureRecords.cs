using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace Vafadar.Zanance.Performance;

/// <summary>Produces bounded diagnostic code identities without exception messages, source paths or arbitrary data.</summary>
internal static partial class StartupFailureRecords
{
    /// <summary>Only original exception type/code and sanitized method names, never an exception object or message.</summary>
    internal sealed record Failure(string Type, int HResult, int? SqliteCode, int? SqliteExtendedCode,
        IReadOnlyList<string> Methods);

    /// <summary>Copies at most four type/code packets and eight bounded method names from each original failure.</summary>
    internal static IReadOnlyList<Failure> Capture(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        var result = new List<Failure>();
        for (Exception? current = error; current is not null && result.Count < 4; current = current.InnerException)
        {
            var type = current.GetType().FullName ?? current.GetType().Name;
            var stack = current.StackTrace ?? string.Empty;
            var methods = new List<string>();
            foreach (var line in stack[..Math.Min(stack.Length, 8192)].Split('\n'))
            {
                var match = MethodName().Match(line);
                if (!match.Success) continue;
                var name = match.Groups[1].Value;
                methods.Add(name[..Math.Min(name.Length, 256)]);
                if (methods.Count == 8) break;
            }
            result.Add(new(type[..Math.Min(type.Length, 256)], current.HResult,
                (current as SqliteException)?.SqliteErrorCode, (current as SqliteException)?.SqliteExtendedErrorCode, methods));
        }
        return result;
    }

    [GeneratedRegex(@"^\s*at\s+([A-Za-z_][A-Za-z0-9_+.`<>]*)\(", RegexOptions.CultureInvariant)]
    private static partial Regex MethodName();
}
