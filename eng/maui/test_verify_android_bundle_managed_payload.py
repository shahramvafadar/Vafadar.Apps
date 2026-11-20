"""Exercise the release payload guard with small in-memory archive fixtures."""

import contextlib
import io
from pathlib import Path
import runpy
import unittest
import warnings
import zipfile


verify_bundle = runpy.run_path(
    Path(__file__).with_name("verify-android-bundle-managed-payload.py")
)["verify_bundle"]

DEX = "base/dex/classes.dex"
ARM = "base/lib/arm64-v8a/libassembly-store.so"
X64 = "base/lib/x86_64/libassembly-store.so"
X64_LIBRARY = "base/lib/x86_64/libmono.so"


def bundle_fixture(*entries):
    """Use small synthetic bytes because the guard checks membership and size."""
    stream = io.BytesIO()
    with zipfile.ZipFile(stream, "w") as bundle, warnings.catch_warnings():
        warnings.filterwarnings("ignore", message="Duplicate name:")
        for name, payload in entries:
            bundle.writestr(name, payload)
    stream.seek(0)
    return stream


class ManagedPayloadTests(unittest.TestCase):
    def test_complete_base_payload(self):
        for entries in (
            [(DEX, b"dex"), (ARM, b"arm")],
            [(DEX, b"dex"), (ARM, b"arm"), (X64, b"x64"), (X64_LIBRARY, b"library")],
        ):
            with self.subTest(entries=entries), contextlib.redirect_stdout(io.StringIO()):
                verify_bundle(bundle_fixture(*entries))

    def test_requires_one_nonempty_primary_base_dex(self):
        for dex_entries in ([], [(DEX, b"")], [(DEX, b"one"), (DEX, b"two")],
                            [("base/dex/classes2.dex", b"secondary")]):
            with self.subTest(dex_entries=dex_entries), self.assertRaisesRegex(SystemExit, DEX):
                verify_bundle(bundle_fixture((ARM, b"arm"), *dex_entries))

    def test_requires_one_nonempty_store_per_base_abi(self):
        for stores in ([], [(X64, b"")], [(X64, b"one"), (X64, b"two")],
                       [(X64.replace("base/", "feature/", 1), b"x64")]):
            with self.subTest(stores=stores), self.assertRaisesRegex(SystemExit, X64):
                verify_bundle(bundle_fixture((DEX, b"dex"), (ARM, b"arm"), (X64_LIBRARY, b"lib"), *stores))

    def test_requires_a_native_base_abi(self):
        with self.assertRaisesRegex(SystemExit, "no base native ABI"):
            verify_bundle(bundle_fixture((DEX, b"dex"), (ARM.replace("base/", "feature/", 1), b"arm")))


if __name__ == "__main__":
    unittest.main()
