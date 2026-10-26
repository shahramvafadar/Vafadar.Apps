using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Vafadar.Zanance.Performance;

/// <summary>Reads bounded relative signal locations, which never by themselves prove a fatal crash.</summary>
internal static partial class NativeFaultPcRecords
{
    /// <summary>An executable ELF virtual-address interval, obtained from the independently verified installed module.</summary>
    internal sealed record ExecutableRange(ulong Start, ulong Length);

    /// <summary>Trusted external module identity; records cannot provide a replacement name or build identifier.</summary>
    internal sealed record Module(string Name, string BuildId, IReadOnlyList<ExecutableRange> ExecutableRanges);

    /// <summary>A signal's relative code location and process identity, without memory, registers or an absolute address.</summary>
    internal sealed record Location(uint Sequence, uint ThreadId, int SignalCode, string Module, string BuildId,
        ulong RelativePc, ulong MonotonicSeconds, uint Nanoseconds);

    /// <summary>Observed signals only; original runtime handlers may have recovered rather than terminated.</summary>
    internal sealed record Result(uint ProcessId, uint UserId, byte Architecture, IReadOnlyList<Location> Locations)
    {
        /// <summary>Actual process death and event correlation are a separate mandatory acceptance boundary.</summary>
        public bool RequiresIndependentFatalEvidence => true;
    }

    /// <summary>Validates the original process/run and verified module ranges before returning only allowed code metadata.</summary>
    internal static Result Read(Stream stream, uint expectedPid, uint expectedUid, string token, byte architecture,
        IReadOnlyDictionary<byte, Module> modules, bool allowControlledTestOrigin = false)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(modules);
        if (expectedPid == 0 || expectedUid == 0 || architecture is not (1 or 2) || !RunToken().IsMatch(token))
            throw new ArgumentException("Exact original signal observation identity required.");
        foreach (var (id, module) in modules)
        {
            var name = id switch { 1 => "libmonosgen-2.0.so", 2 => "libmonodroid.so", 3 => "libsqlcipher.so",
                4 when allowControlledTestOrigin => "libzanance_fault_origin.so", _ => "" };
            if (name.Length == 0 || module.Name != name || !BuildIdentity().IsMatch(module.BuildId)
                || module.ExecutableRanges.Count is < 1 or > 32
                || module.ExecutableRanges.Any(range => range.Length == 0 || range.Start > ulong.MaxValue - range.Length))
                throw new ArgumentException("Verified original executable module identity required.");
        }
        var expectedToken = Convert.FromHexString(token);
        var packet = new byte[72];
        var locations = new List<Location>();
        var sequences = new HashSet<uint>();
        try
        {
            while (true)
            {
                var first = stream.ReadByte();
                if (first < 0) break;
                if (locations.Count == 32) throw new InvalidDataException("Signal observation capacity exceeded.");
                packet[0] = checked((byte)first);
                try { stream.ReadExactly(packet.AsSpan(1)); }
                catch (EndOfStreamException) { throw new InvalidDataException("Incomplete original signal record."); }
                if (!packet.AsSpan(0, 8).SequenceEqual("ZNCPC001"u8) || packet[8] != 1 || packet[9] != architecture
                    || packet[10] != 11 || U32(packet, 12) != expectedUid || U32(packet, 16) != expectedPid
                    || !CryptographicOperations.FixedTimeEquals(packet.AsSpan(56, 16), expectedToken)
                    || !modules.TryGetValue(packet[11], out var module))
                    throw new InvalidDataException("Original signal protocol or ownership differs.");
                var tid = U32(packet, 20); var code = U32(packet, 24); var sequence = U32(packet, 28);
                var pc = U64(packet, 32); var seconds = U64(packet, 40); var nanos = U64(packet, 48);
                if (tid == 0 || code is 0 or > int.MaxValue || sequence is < 1 or > 32 || !sequences.Add(sequence)
                    || seconds == 0 || nanos >= 1_000_000_000
                    || !module.ExecutableRanges.Any(range => pc >= range.Start && pc - range.Start < range.Length))
                    throw new InvalidDataException("Original signal code interval, sequence or timestamp differs.");
                locations.Add(new(sequence, tid, checked((int)code), module.Name, module.BuildId, pc, seconds, checked((uint)nanos)));
                CryptographicOperations.ZeroMemory(packet);
            }
            if (locations.Count == 0 || !sequences.SetEquals(Enumerable.Range(1, locations.Count).Select(i => (uint)i)))
                throw new InvalidDataException("Original complete signal sequence required.");
            // O_APPEND preserves record boundaries, not allocation order between faulting threads.
            return new(expectedPid, expectedUid, architecture, locations.OrderBy(item => item.Sequence).ToArray());
        }
        finally { CryptographicOperations.ZeroMemory(packet); CryptographicOperations.ZeroMemory(expectedToken); }
    }

    private static uint U32(byte[] packet, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(packet.AsSpan(offset, 4));
    private static ulong U64(byte[] packet, int offset) => BinaryPrimitives.ReadUInt64LittleEndian(packet.AsSpan(offset, 8));
    [GeneratedRegex("^[0-9a-f]{32}$", RegexOptions.CultureInvariant)] private static partial Regex RunToken();
    [GeneratedRegex("^[0-9a-f]{8,128}$", RegexOptions.CultureInvariant)] private static partial Regex BuildIdentity();
}
