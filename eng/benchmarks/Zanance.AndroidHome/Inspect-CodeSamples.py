"""Validate a known, owned CPU-clock/frame-pointer recording without decoding memory or code addresses.

The perf header/sample field layout is the Linux perf_event UAPI. Refuse other event formats,
raw/user-stack/register sample fields, unknown record payload types, clocks or process identities.
This does not symbolize samples or establish that a whole application is fast or stable.
"""

import argparse
import collections
import hashlib
import json
from pathlib import Path
import struct


def inspect(path, expected_sha256, expected_pid):
    """Return safe header metadata and timestamps for an exact owned recording."""
    path = Path(path)
    if path.stat().st_size < 104 or path.stat().st_size > 16 * 1024 * 1024:
        raise ValueError("Recording size is outside the bounded code-sampling format")
    data = path.read_bytes()
    if hashlib.sha256(data).hexdigest() != expected_sha256 or data[:8] != b"PERFILE2":
        raise ValueError("Owned recording digest or perf header differs")
    entry_size = struct.unpack_from("<Q", data, 16)[0]
    attr_offset, attr_bytes = struct.unpack_from("<QQ", data, 24)
    data_offset, data_bytes = struct.unpack_from("<QQ", data, 40)
    if attr_bytes != entry_size or entry_size < 112 or attr_offset + entry_size > len(data):
        raise ValueError("Exactly one bounded CPU event is required")
    if data_offset + data_bytes > len(data):
        raise ValueError("Incomplete perf data section")
    event_type, attr_size = struct.unpack_from("<II", data, attr_offset)
    config = struct.unpack_from("<Q", data, attr_offset + 8)[0]
    sample_type = struct.unpack_from("<Q", data, attr_offset + 24)[0]
    flags = struct.unpack_from("<Q", data, attr_offset + 40)[0]
    clock_id = struct.unpack_from("<i", data, attr_offset + 92)[0]
    # 487 = IP/TID/TIME/CALLCHAIN/ID/CPU/PERIOD. In particular, no RAW, ADDR,
    # READ, user stack, user registers, interrupt registers or AUX sample payload.
    if (event_type, config, sample_type) != (1, 0, 487) or attr_size < 96 or attr_size + 16 > entry_size:
        raise ValueError("Not the reviewed code-only CPU-clock sample format")
    if clock_id != 1 or not flags & (1 << 25):
        raise ValueError("Explicit CLOCK_MONOTONIC recording is required")
    safe_records = {1, 2, 3, 4, 7, 9, 10, 13}
    counts = collections.Counter()
    timestamps = []
    offset = data_offset
    while offset < data_offset + data_bytes:
        record_type, miscellaneous, size = struct.unpack_from("<IHH", data, offset)
        if size < 8 or offset + size > data_offset + data_bytes or record_type not in safe_records:
            raise ValueError("Unknown or incomplete record payload; do not interpret it as code-only")
        counts[record_type] += 1
        if record_type == 9:
            # For this exact sample_type the timestamp follows IP and pid/tid.
            if size < 64 or miscellaneous & 7 != 2:
                raise ValueError("A complete userspace sample is required")
            pid = struct.unpack_from("<I", data, offset + 16)[0]
            if pid != expected_pid:
                raise ValueError("Sample belongs to another process")
            timestamps.append(struct.unpack_from("<Q", data, offset + 24)[0])
        offset += size
    if offset != data_offset + data_bytes or not timestamps:
        raise ValueError("No complete owned-process samples")
    return ({"Sha256": expected_sha256, "Bytes": len(data), "ExpectedPid": expected_pid,
             "EventType": event_type, "EventConfig": config, "SampleType": sample_type,
             "ClockId": clock_id, "UseClockIdFlag": True, "SampleCount": len(timestamps),
             "RecordTypes": dict(counts), "MinimumSampleNs": min(timestamps),
             "MaximumSampleNs": max(timestamps), "NoRawStackRegistersOrUnknownRecords": True}, timestamps)


def main():
    """Write bounded metadata only; retain the original sample file unchanged."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("path", type=Path)
    parser.add_argument("--sha256", required=True)
    parser.add_argument("--pid", required=True, type=int)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    if args.output.exists() or args.pid < 1:
        raise ValueError("Fresh output and explicit owned process required")
    metadata, _ = inspect(args.path, args.sha256, args.pid)
    args.output.write_text(json.dumps(metadata, indent=2), encoding="utf-8")
    print(json.dumps(metadata))


if __name__ == "__main__":
    main()
