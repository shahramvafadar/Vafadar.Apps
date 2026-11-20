"""Check the 16 KiB load alignment of 64-bit native libraries in an Android bundle."""

import struct
import sys
import zipfile

PAGE_SIZE = 16 * 1024
MACHINES = {"arm64-v8a": 183, "x86_64": 62}
PROGRAM_HEADER = struct.Struct("<IIQQQQQQ")


def check_library(bundle, entry, machine):
    """Check the ELF ABI and every load segment without extracting the archive."""
    with bundle.open(entry) as library:
        header = library.read(64)
        if len(header) != 64 or header[:7] != b"\x7fELF\x02\x01\x01":
            raise ValueError("expected a little-endian ELF64 library")
        if struct.unpack_from("<HH", header, 16) != (3, machine):
            raise ValueError("ELF type or ABI differs from the library path")

        offset = struct.unpack_from("<Q", header, 32)[0]
        size, count = struct.unpack_from("<HH", header, 54)
        if offset < len(header) or size != PROGRAM_HEADER.size or offset + size * count > entry.file_size:
            raise ValueError("invalid program header table")
        library.seek(offset)
        seen_load = False
        for _ in range(count):
            kind, _, file_offset, address, _, file_size, memory_size, alignment = PROGRAM_HEADER.unpack(
                library.read(size)
            )
            if kind != 1:  # PT_LOAD
                continue
            seen_load = True
            if file_offset + file_size > entry.file_size or file_size > memory_size:
                raise ValueError("load segment extends beyond the library")
            if alignment < PAGE_SIZE or alignment & (alignment - 1) or file_offset % PAGE_SIZE != address % PAGE_SIZE:
                raise ValueError("load segment is not aligned to 16 KiB")
        if not seen_load:
            raise ValueError("no load segments")


def check_bundle(path):
    """Check each 64-bit Android library under a bundle module's native library directory."""
    checked = 0
    with zipfile.ZipFile(path) as bundle:
        for entry in bundle.infolist():
            parts = entry.filename.split("/")
            if len(parts) != 4 or parts[1] != "lib" or not parts[3].endswith(".so") or parts[2] not in MACHINES:
                continue
            try:
                check_library(bundle, entry, MACHINES[parts[2]])
            except (ValueError, struct.error) as error:
                raise SystemExit(f"{entry.filename}: {error}") from None
            checked += 1
    if checked == 0:
        raise SystemExit("Bundle contains no Android 64-bit native libraries to check.")
    print(f"Verified 16 KiB ELF load alignment for {checked} native libraries.")


if __name__ == "__main__":
    check_bundle(sys.argv[1])
