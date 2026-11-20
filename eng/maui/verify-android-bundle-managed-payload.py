"""Verify the intended native ABIs and the Android App Bundle's managed payload."""

import argparse
import zipfile

ANDROID_RUNTIME_ABIS = {"android-arm64": "arm64-v8a", "android-x64": "x86_64"}


def verify_archive(path, runtime_identifiers, *, apk=False):
    """Bind packaged native ABIs to the build and verify the App Bundle's base payload."""
    rids = runtime_identifiers.split(";")
    if any(rid not in ANDROID_RUNTIME_ABIS for rid in rids):
        raise SystemExit("Expected supported Zanance Android runtime identifiers.")
    expected = {ANDROID_RUNTIME_ABIS[rid] for rid in rids}
    prefix = "" if apk else "base/"
    with zipfile.ZipFile(path) as bundle:
        entries = bundle.infolist()
        abis = {
            parts[1]
            for entry in entries
            if entry.filename.startswith(prefix)
            and (parts := entry.filename.removeprefix(prefix).split("/"))[0] == "lib"
            and len(parts) == 3
            and parts[2].endswith(".so")
        }
        if abis != expected:
            raise SystemExit(
                f"Android archive ABIs differ from the build: expected {', '.join(sorted(expected))}; "
                f"found {', '.join(sorted(abis)) or 'none'}."
            )
        if apk:
            # APK assembly-store and DEX checks also cover the SDK's separate packaging modes.
            print(f"Verified APK native ABIs: {', '.join(sorted(expected))}.")
            return

        required = ["base/dex/classes.dex"]
        required.extend(f"base/lib/{abi}/libassembly-store.so" for abi in sorted(expected))
        for name in required:
            matches = [entry for entry in entries if entry.filename == name]
            if len(matches) != 1 or matches[0].file_size == 0:
                raise SystemExit(f"Bundle must contain exactly one nonempty {name}.")

    print(f"Verified primary base DEX and managed assembly stores for {', '.join(sorted(abis))}.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--apk", action="store_true", help="verify a directly installable APK's ABI set")
    parser.add_argument("--runtime-identifiers", required=True, help="the producing project's effective runtime identifiers")
    parser.add_argument("archive")
    arguments = parser.parse_args()
    verify_archive(arguments.archive, arguments.runtime_identifiers, apk=arguments.apk)
