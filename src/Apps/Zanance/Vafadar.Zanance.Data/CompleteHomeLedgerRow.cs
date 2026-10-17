using System.Buffers.Text;
using System.Data.Common;
using System.Runtime.CompilerServices;
using SQLitePCL;

namespace Vafadar.Zanance.Data;

/// <summary>Reads a complete current SQLite row using the exact prepared query's null bitmap.</summary>
internal readonly struct CompleteHomeLedgerRow
{
    private readonly DbDataReader _reader;
    private readonly sqlite3_stmt? _statement;
    private readonly uint _nulls;

    /// <summary>Captures one bounded bitmap after original advancement; rewritten or replaced readers keep their getters.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal CompleteHomeLedgerRow(DbDataReader reader, sqlite3_stmt? statement, int nullOrdinal)
    {
        _reader = reader; _statement = statement;
        _nulls = statement is null ? 0 : checked((uint)reader.GetInt64(nullOrdinal));
    }

    /// <summary>Uses the captured row's null type, preserving provider behavior for intercepted readers.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsDBNull(int ordinal) => _statement is null ? _reader.IsDBNull(ordinal) : (_nulls & (1u << ordinal)) != 0;

    /// <summary>Retains SQLite integer coercion and original provider refusal of a required null.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal long GetInt64(int ordinal) => _statement is not null && !IsDBNull(ordinal)
        ? raw.sqlite3_column_int64(_statement, ordinal) : _reader.GetInt64(ordinal);

    /// <summary>Retains the provider's checked narrowing instead of silently wrapping an enum value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal int GetInt32(int ordinal) => _statement is null ? _reader.GetInt32(ordinal) : checked((int)GetInt64(ordinal));

    /// <summary>Retains the provider's nonzero integer boolean semantics.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool GetBoolean(int ordinal) => _statement is null ? _reader.GetBoolean(ordinal) : GetInt64(ordinal) != 0;

    /// <summary>Uses the same SQLite text conversion and UTF-8 decoder as the original provider, including embedded nulls.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal string GetString(int ordinal) => _statement is not null && !IsDBNull(ordinal)
        ? raw.sqlite3_column_text(_statement, ordinal).utf8_to_string()! : _reader.GetString(ordinal);

    /// <summary>Avoids temporary GUID text for canonical storage; every alternative representation retains provider parsing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Guid GetGuid(int ordinal)
    {
        if (_statement is not null)
        {
            var type = raw.sqlite3_column_type(_statement, ordinal);
            if (type == raw.SQLITE_BLOB)
            {
                var bytes = raw.sqlite3_column_blob(_statement, ordinal);
                if (bytes.Length == 16) return new Guid(bytes);
            }
            else if (type == raw.SQLITE_TEXT)
            {
                var text = raw.sqlite3_column_blob(_statement, ordinal);
                if (text.Length == 36 && Utf8Parser.TryParse(text, out Guid value, out var consumed) && consumed == text.Length)
                    return value;
            }
        }
        return _reader.GetGuid(ordinal);
    }

    /// <summary>Keeps original date parsing, including legacy text and Julian storage, in the provider.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T GetFieldValue<T>(int ordinal) => _reader.GetFieldValue<T>(ordinal);
}
