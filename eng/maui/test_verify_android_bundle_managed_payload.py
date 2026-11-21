"""Exercise the release payload guard with small in-memory archive fixtures."""

import contextlib
import io
from pathlib import Path
import runpy
import unittest
import warnings
import zipfile


verify_archive = runpy.run_path(
    Path(__file__).with_name("verify-android-bundle-managed-payload.py")
)["verify_archive"]

DEX = "base/dex/classes.dex"
ARM = "base/lib/arm64-v8a/libassembly-store.so"
X64 = "base/lib/x86_64/libassembly-store.so"
X64_LIBRARY = "base/lib/x86_64/libmono.so"
DUAL = "android-arm64;android-x64"


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
    def test_rejects_an_additional_app_bundle_module(self):
        archive = bundle_fixture(
            (DEX, b"dex"),
            (ARM, b"arm"),
            ("base/manifest/AndroidManifest.xml", b"base manifest"),
            ("optional_feature/manifest/AndroidManifest.xml", b"additional manifest"),
        )
        with self.assertRaisesRegex(SystemExit, "Only the base app module"):
            verify_archive(archive, "android-arm64")

    def test_complete_base_payload(self):
        for rids, entries in (
            ("android-arm64", [(DEX, b"dex"), (ARM, b"arm")]),
            (DUAL, [(DEX, b"dex"), (ARM, b"arm"), (X64, b"x64"), (X64_LIBRARY, b"library")]),
        ):
            with self.subTest(entries=entries), contextlib.redirect_stdout(io.StringIO()):
                verify_archive(bundle_fixture(*entries), rids)

    def test_requires_one_nonempty_primary_base_dex(self):
        for dex_entries in ([], [(DEX, b"")], [(DEX, b"one"), (DEX, b"two")],
                            [("base/dex/classes2.dex", b"secondary")]):
            with self.subTest(dex_entries=dex_entries), self.assertRaisesRegex(SystemExit, DEX):
                verify_archive(bundle_fixture((ARM, b"arm"), *dex_entries), "android-arm64")

    def test_requires_one_nonempty_store_per_base_abi(self):
        for stores in ([], [(X64, b"")], [(X64, b"one"), (X64, b"two")],
                       [(X64.replace("base/", "feature/", 1), b"x64")]):
            with self.subTest(stores=stores), self.assertRaisesRegex(SystemExit, X64):
                verify_archive(bundle_fixture((DEX, b"dex"), (ARM, b"arm"), (X64_LIBRARY, b"lib"), *stores), DUAL)

    def test_requires_a_native_base_abi(self):
        with self.assertRaisesRegex(SystemExit, "found none"):
            verify_archive(bundle_fixture((DEX, b"dex"), (ARM.replace("base/", "feature/", 1), b"arm")), "android-arm64")

    def test_refuses_missing_or_unexpected_architectures(self):
        for apk in (False, True):
            arm = ARM.removeprefix("base/") if apk else ARM
            x64 = X64.removeprefix("base/") if apk else X64
            with self.subTest(apk=apk, mismatch="missing"), self.assertRaisesRegex(SystemExit, "found x86_64"):
                verify_archive(bundle_fixture((x64, b"x64")), DUAL, apk=apk)
            with self.subTest(apk=apk, mismatch="unexpected"), self.assertRaisesRegex(SystemExit, "found arm64-v8a, x86_64"):
                verify_archive(bundle_fixture((arm, b"arm"), (x64, b"x64")), "android-arm64", apk=apk)

    def test_apk_accepts_an_explicit_single_architecture(self):
        with contextlib.redirect_stdout(io.StringIO()):
            verify_archive(bundle_fixture((X64_LIBRARY.removeprefix("base/"), b"lib")), "android-x64", apk=True)

    def test_unknown_or_missing_build_architecture_fails(self):
        for rids in ("", "android-arm", "android-arm64;"):
            with self.subTest(rids=rids), self.assertRaisesRegex(SystemExit, "Expected supported"):
                verify_archive(bundle_fixture((DEX, b"dex"), (ARM, b"arm")), rids)


if __name__ == "__main__":
    unittest.main()
