"""Bind Android release bundle storage libraries to the qualified SQLCipher package."""

import hashlib
import json
import sys
import zipfile
from pathlib import Path

PACKAGE_RIDS = {"arm64-v8a": "android-arm64", "x86_64": "android-x64"}


def verify_entry(bundle, name, expected_sha256):
    """Verify the single archive entry's uncompressed bytes against the reviewed digest."""
    matches = [entry for entry in bundle.infolist() if entry.filename == name]
    if len(matches) != 1:
        raise SystemExit(f"Expected exactly one {name} in the bundle.")
    with bundle.open(matches[0]) as stream:
        actual = hashlib.file_digest(stream, "sha256").hexdigest()
    if actual != expected_sha256:
        raise SystemExit(f"Bundle {name} differs from the qualified SQLCipher package.")


def verify_bundle(path, package_contents):
    """Verify packaged SQLCipher, provenance and notices for each shipped base ABI."""
    receipt = json.loads(Path(package_contents).read_text(encoding="utf-8"))
    if receipt.get("schemaVersion") != 1 or receipt.get("packageId") != "Vafadar.Native.SqlCipher.Android":
        raise SystemExit("Expected the reviewed Android SQLCipher package contents.")
    entries = receipt["entries"]
    with zipfile.ZipFile(path) as bundle:
        names = bundle.namelist()
        abis = {
            parts[2] for name in names
            if (parts := name.split("/"))[:2] == ["base", "lib"] and len(parts) == 4 and parts[3].endswith(".so")
        }
        if not abis or not abis <= PACKAGE_RIDS.keys():
            raise SystemExit("Bundle has no base native ABI or an unsupported ABI for SQLCipher.")
        if any(name.rsplit("/", 1)[-1] in ("libe_sqlite3.so", "libaot-SQLitePCLRaw.batteries_v2.dll.so")
               for name in names):
            raise SystemExit("Bundle contains the generic SQLite engine or automatic bundle initializer.")
        for abi in sorted(abis):
            package_path = f"runtimes/{PACKAGE_RIDS[abi]}/native/libsqlcipher.so"
            verify_entry(bundle, f"base/lib/{abi}/libsqlcipher.so", entries[package_path])
        verify_entry(bundle, "base/assets/native/sqlcipher/manifest.json", entries["manifest.json"])
        verify_entry(bundle, "base/assets/native/sqlcipher/ThirdPartyNotices.txt", entries["licenses/ThirdPartyNotices.txt"])
    print(f"Verified qualified SQLCipher, manifest and notices for {', '.join(sorted(abis))}.")


if __name__ == "__main__":
    verify_bundle(*sys.argv[1:])
