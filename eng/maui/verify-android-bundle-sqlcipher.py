"""Bind Android release archive storage libraries to the qualified SQLCipher package."""

import argparse
import hashlib
import json
import zipfile
from pathlib import Path

PACKAGE_RIDS = {"arm64-v8a": "android-arm64", "x86_64": "android-x64"}


def verify_entry(bundle, name, expected_sha256):
    """Verify the single archive entry's uncompressed bytes against the reviewed digest."""
    matches = [entry for entry in bundle.infolist() if entry.filename == name]
    if len(matches) != 1:
        raise SystemExit(f"Expected exactly one {name} in the archive.")
    with bundle.open(matches[0]) as stream:
        actual = hashlib.file_digest(stream, "sha256").hexdigest()
    if actual != expected_sha256:
        raise SystemExit(f"Archive {name} differs from the qualified SQLCipher package.")


def verify_archive(path, package_contents, *, apk):
    """Verify packaged SQLCipher, provenance and notices for each shipped base ABI."""
    receipt = json.loads(Path(package_contents).read_text(encoding="utf-8"))
    if receipt.get("schemaVersion") != 1 or receipt.get("packageId") != "Vafadar.Native.SqlCipher.Android":
        raise SystemExit("Expected the reviewed Android SQLCipher package contents.")
    entries = receipt["entries"]
    base = "" if apk else "base/"
    with zipfile.ZipFile(path) as bundle:
        names = bundle.namelist()
        abis = {
            parts[1] for name in names if name.startswith(base)
            if (parts := name.removeprefix(base).split("/"))[0] == "lib"
            and len(parts) == 3 and parts[2].endswith(".so")
        }
        if not abis or not abis <= PACKAGE_RIDS.keys():
            raise SystemExit("Archive has no base native ABI or an unsupported ABI for SQLCipher.")
        if any(name.rsplit("/", 1)[-1] in ("libe_sqlite3.so", "libaot-SQLitePCLRaw.batteries_v2.dll.so")
               for name in names):
            raise SystemExit("Archive contains the generic SQLite engine or automatic bundle initializer.")
        for abi in sorted(abis):
            package_path = f"runtimes/{PACKAGE_RIDS[abi]}/native/libsqlcipher.so"
            verify_entry(bundle, f"{base}lib/{abi}/libsqlcipher.so", entries[package_path])
        verify_entry(bundle, f"{base}assets/native/sqlcipher/manifest.json", entries["manifest.json"])
        verify_entry(bundle, f"{base}assets/native/sqlcipher/ThirdPartyNotices.txt", entries["licenses/ThirdPartyNotices.txt"])
    print(f"Verified qualified SQLCipher, manifest and notices for {', '.join(sorted(abis))}.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--apk", action="store_true", help="verify a directly installable APK")
    parser.add_argument("archive")
    parser.add_argument("package_contents")
    args = parser.parse_args()
    verify_archive(args.archive, args.package_contents, apk=args.apk)
