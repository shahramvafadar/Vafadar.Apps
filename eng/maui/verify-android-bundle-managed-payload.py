"""Require the base DEX and managed assembly store for each bundled native ABI."""

import argparse
import zipfile


def verify_bundle(path):
    """Check the current .NET Android base-module payload without extracting it."""
    with zipfile.ZipFile(path) as bundle:
        entries = bundle.infolist()
        abis = {
            parts[2]
            for entry in entries
            if (parts := entry.filename.split("/"))[:2] == ["base", "lib"]
            and len(parts) == 4
            and parts[3].endswith(".so")
        }
        if not abis:
            raise SystemExit("Bundle contains no base native ABI.")

        required = ["base/dex/classes.dex"]
        required.extend(f"base/lib/{abi}/libassembly-store.so" for abi in sorted(abis))
        for name in required:
            matches = [entry for entry in entries if entry.filename == name]
            if len(matches) != 1 or matches[0].file_size == 0:
                raise SystemExit(f"Bundle must contain exactly one nonempty {name}.")

    print(f"Verified primary base DEX and managed assembly stores for {', '.join(sorted(abis))}.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("bundle")
    verify_bundle(parser.parse_args().bundle)
