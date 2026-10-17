using System.Buffers.Text;
using System.Data.Common;
using System.Runtime.CompilerServices;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace Vafadar.Zanance.Data;

/// <summary>Reads a complete current SQLite row without repeating its native row/type checks for every getter.</summary>
internal readonly struct CompleteHomeLedgerRow
{
    private readonly DbDataReader _reader;
    private readonly sqlite3_stmt? _statement;
    private readonly int[] _types;

    /// <summary>Captures storage types only after the original reader advanced successfully; wrappers retain their own getters.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal CompleteHomeLedgerRow(DbDataReader reader, int[] types)
    {
        _reader = reader; _types = types;
        // An interceptor may replace or derive the reader. Never bypass its values or behavior.
        _statement = reader.GetType() == typeof(SqliteDataReader) ? ((SqliteDataReader)reader).Handle : null;
        if (_statement is not null)
            for (var ordinal = 0; ordinal < types.Length; ordinal++) types[ordinal] = raw.sqlite3_column_type(_statement, ordinal);
    }

    /// <summary>Uses the captured row's null type, preserving provider behavior for intercepted readers.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool IsDBNull(int ordinal) => _statement is null ? _reader.IsDBNull(ordinal) : _types[ordinal] == raw.SQLITE_NULL;

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
            if (_types[ordinal] == raw.SQLITE_BLOB)
            {
                var bytes = raw.sqlite3_column_blob(_statement, ordinal);
                if (bytes.Length == 16) return new Guid(bytes);
            }
            else if (_types[ordinal] == raw.SQLITE_TEXT)
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
