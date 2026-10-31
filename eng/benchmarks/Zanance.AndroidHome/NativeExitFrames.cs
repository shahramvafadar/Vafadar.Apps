using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Vafadar.Zanance.Performance;

/// <summary>Reads only an owned Android tombstone's crashed-thread code frames; never retains its memory or registers.</summary>
internal static partial class NativeExitFrames
{
    /// <summary>A relative code location and original frame ordinal, with no absolute address or application data.</summary>
    internal sealed record Frame(ulong RelativePc, string Module, string BuildId, int OriginalFrameIndex = 0);

    /// <summary>Only the requested crashed thread's allowed library locations and an omitted-frame count are returned.</summary>
    internal sealed record Result(uint ProcessId, uint UserId, uint ThreadId, IReadOnlyList<Frame> Frames,
        int DiscardedNonModuleFrames = 0);

    /// <summary>Keeps a selected thread's allowed locations separate from discarded non-library frames.</summary>
    private sealed record ThreadFrames(IReadOnlyList<Frame> Frames, int DiscardedNonModuleFrames);

    /// <summary>Consumes the AOSP tombstone protobuf through a bounded, non-seeking allowlist reader.</summary>
    internal static Result Read(Stream stream, uint expectedPid, uint expectedUid)
    {
        var reader = new Reader(stream, new Budget(), int.MaxValue, root: true);
        uint pid = 0, uid = 0, tid = 0;
        var identities = new HashSet<int>();
        var threads = new Dictionary<uint, ThreadFrames>();
        while (reader.Tag(out var field, out var wire))
        {
            if (field is 5 or 6 or 7)
            {
                if (wire != 0 || !identities.Add(field)) throw new InvalidDataException("Ambiguous native exit identity.");
                var value = checked((uint)reader.Varint());
                if (field == 5) pid = value;
                else if (field == 6) tid = value;
                else uid = value;
            }
            else if (field == 16 && wire == 2)
            {
                if (tid == 0) throw new InvalidDataException("Crashed thread identity must precede native thread entries.");
                if (threads.Count >= 256) throw new InvalidDataException("Native thread bound exceeded.");
                var entry = reader.Child();
                uint key = 0;
                ThreadFrames? frames = null;
                var seen = new HashSet<int>();
                while (entry.Tag(out var childField, out var childWire))
                {
                    if (childField is 1 or 2 && !seen.Add(childField)) throw new InvalidDataException("Duplicate native thread entry.");
                    if (childField == 1 && childWire == 0) key = checked((uint)entry.Varint());
                    else if (childField == 2 && childWire == 2)
                    {
                        if (key == 0) throw new InvalidDataException("Native thread key must precede its value.");
                        if (key == tid) frames = ReadThread(entry.Child());
                        else { entry.Skip(childWire); frames = new([], 0); }
                    }
                    else entry.Skip(childWire);
                }
                if (key == 0 || frames is null || !threads.TryAdd(key, frames)) throw new InvalidDataException("Invalid native thread identity.");
            }
            else reader.Skip(wire);
        }
        if (pid != expectedPid || uid != expectedUid || tid == 0 || !threads.TryGetValue(tid, out var selected) || selected.Frames.Count == 0)
            throw new InvalidDataException("Requested owned native exit frames are unavailable.");
        return new Result(pid, uid, tid, selected.Frames, selected.DiscardedNonModuleFrames);
    }

    private static ThreadFrames ReadThread(Reader reader)
    {
        var frames = new List<Frame>();
        var seenFrames = 0;
        var discarded = 0;
        while (reader.Tag(out var field, out var wire))
        {
            // Thread registers, names, memory, notes and all extension fields are discarded.
            if (field != 4 || wire != 2) { reader.Skip(wire); continue; }
            // D-676: ignored pseudo/mapped frames still consume the original bound. They must
            // neither hide later valid library frames nor become an unlimited discarded prefix.
            if (++seenFrames > 128) throw new InvalidDataException("Native frame bound exceeded.");
            var frame = reader.Child();
            ulong pc = 0;
            string module = "", build = "";
            var seen = new HashSet<int>();
            while (frame.Tag(out var f, out var w))
            {
                if (f is 1 or 6 or 8 && !seen.Add(f)) throw new InvalidDataException("Ambiguous native code frame.");
                if (f == 1 && w == 0) pc = frame.Varint();
                else if (f == 6 && w == 2)
                {
                    var name = frame.Text();
                    module = name[(name.LastIndexOf('/') + 1)..];
                }
                else if (f == 8 && w == 2) build = frame.Text();
                else frame.Skip(w); // Absolute PC/SP, function strings and memory never enter the result.
            }
            if (!ModuleName().IsMatch(module))
            {
                // Export no rejected name, path or build text; only its count survives.
                discarded++;
                continue;
            }
            if (!BuildId().IsMatch(build))
                throw new InvalidDataException("Native module identity is not a bounded code location.");
            // Keep its original ordinal: a filtered caller frame must not become a guessed fault frame.
            frames.Add(new Frame(pc, module, build, seenFrames - 1));
        }
        return new(frames, discarded);
    }

    [GeneratedRegex("^lib[A-Za-z0-9_.-]{1,160}\\.so$", RegexOptions.CultureInvariant)]
    private static partial Regex ModuleName();

    [GeneratedRegex("^[a-fA-F0-9]{0,128}$", RegexOptions.CultureInvariant)]
    private static partial Regex BuildId();

    private sealed class Budget
    {
        internal int Left = 8 * 1024 * 1024;
    }

    private sealed class Reader(Stream stream, Budget budget, int remaining, bool root = false)
    {
        private int left = remaining;

        internal bool Tag(out int field, out int wire)
        {
            field = wire = 0;
            if (left == 0) return false;
            var first = Byte(allowEnd: root);
            if (first < 0) return false;
            var tag = Varint(first);
            field = checked((int)(tag >> 3)); wire = (int)(tag & 7);
            if (field == 0 || wire is not (0 or 1 or 2 or 5)) throw new InvalidDataException("Unsupported native protobuf field.");
            return true;
        }

        internal ulong Varint(int first = -1)
        {
            ulong value = 0;
            for (var n = 0; n < 10; n++)
            {
                var b = n == 0 && first >= 0 ? first : Byte();
                if (n == 9 && b > 1) throw new InvalidDataException("Native protobuf integer overflow.");
                value |= (ulong)(b & 127) << (n * 7);
                if ((b & 128) == 0) return value;
            }
            throw new InvalidDataException("Invalid native protobuf integer.");
        }

        internal Reader Child()
        {
            var length = Length();
            left -= length;
            return new Reader(stream, budget, length);
        }

        internal string Text()
        {
            var length = Length();
            if (length > 1024) throw new InvalidDataException("Native code identity string bound exceeded.");
            var bytes = new byte[length];
            try
            {
                for (var i = 0; i < length; i++) bytes[i] = checked((byte)Byte());
                return new UTF8Encoding(false, true).GetString(bytes);
            }
            finally { CryptographicOperations.ZeroMemory(bytes); }
        }

        internal void Skip(int wire)
        {
            if (wire == 0) { Varint(); return; }
            var length = wire switch { 1 => 8, 2 => Length(), 5 => 4, _ => throw new InvalidDataException("Unsupported native wire type.") };
            if (length > left || length > budget.Left) throw new InvalidDataException("Native payload exceeds remaining bound.");
            // Unselected payload is discarded in small, immediately cleared buffers, never copied or logged.
            var buffer = new byte[Math.Min(length, 4096)];
            try
            {
                while (length > 0)
                {
                    var read = stream.Read(buffer, 0, Math.Min(length, buffer.Length));
                    if (read == 0) throw new EndOfStreamException();
                    length -= read; left -= read; budget.Left -= read;
                    CryptographicOperations.ZeroMemory(buffer);
                }
            }
            finally { CryptographicOperations.ZeroMemory(buffer); }
        }

        private int Length()
        {
            var length = checked((int)Varint());
            if (length > left || length > budget.Left) throw new InvalidDataException("Native message exceeds remaining bound.");
            return length;
        }

        private int Byte(bool allowEnd = false)
        {
            if (left == 0 || budget.Left == 0) throw new InvalidDataException("Native trace bound exhausted.");
            var value = stream.ReadByte();
            if (value < 0)
            {
                if (allowEnd) return -1;
                throw new EndOfStreamException();
            }
            left--; budget.Left--; return value;
        }
    }
}
