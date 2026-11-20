"""Exercise the shared Android ELF alignment gate with synthetic archive entries."""

from pathlib import Path
import struct
import subprocess
import sys
import tempfile
import unittest
import zipfile


VERIFIER = Path(__file__).with_name("verify-android-bundle-elf.py")
ARM64 = 183
X64 = 62


def library(machine, alignment=16384):
    """Supply one ELF64 header and one PT_LOAD entry without executable contents."""
    identification = b"\x7fELF\x02\x01\x01".ljust(16, b"\0")
    header = struct.pack(
        "<16sHHIQQQIHHHHHH",
        identification, 3, machine, 1, 0, 64, 0, 0, 64, 56, 1, 0, 0, 0,
    )
    segment = struct.pack("<IIQQQQQQ", 1, 4, 0, 0, 0, 120, 120, alignment)
    return header + segment


class AndroidElfTests(unittest.TestCase):
    def verify(self, members, *, apk):
        with tempfile.TemporaryDirectory() as source:
            path = Path(source) / ("fixture.apk" if apk else "fixture.aab")
            with zipfile.ZipFile(path, "w", compression=zipfile.ZIP_DEFLATED) as archive:
                for name, data in members.items():
                    archive.writestr(name, data)
            arguments = ["--apk"] if apk else []
            return subprocess.run(
                [sys.executable, str(VERIFIER), *arguments, str(path)],
                capture_output=True,
                text=True,
                check=False,
            )

    def test_apk_checks_both_compressed_abis(self):
        result = self.verify({
            "lib/arm64-v8a/libfixture.so": library(ARM64),
            "lib/x86_64/libfixture.so": library(X64),
        }, apk=True)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("for 2 native libraries", result.stdout)

    def test_apk_refuses_small_load_alignment_even_with_valid_peer(self):
        result = self.verify({
            "lib/x86_64/libfixture.so": library(X64),
            "lib/arm64-v8a/libbad.so": library(ARM64, alignment=4096),
        }, apk=True)
        self.assertEqual(result.returncode, 1)
        self.assertIn("lib/arm64-v8a/libbad.so: load segment is not aligned to 16 KiB", result.stderr)

    def test_bundle_checks_native_libraries_in_each_module(self):
        members = {
            "base/lib/arm64-v8a/libfixture.so": library(ARM64),
            "feature/lib/x86_64/libfeature.so": library(X64),
        }
        result = self.verify(members, apk=False)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("for 2 native libraries", result.stdout)
        members["feature/lib/x86_64/libfeature.so"] = library(X64, alignment=4096)
        result = self.verify(members, apk=False)
        self.assertEqual(result.returncode, 1)
        self.assertIn("feature/lib/x86_64/libfeature.so: load segment is not aligned", result.stderr)

    def test_native_entries_from_the_other_archive_layout_do_not_count(self):
        for apk, entry in (
            (True, "base/lib/arm64-v8a/libfixture.so"),
            (False, "lib/arm64-v8a/libfixture.so"),
        ):
            with self.subTest(apk=apk):
                result = self.verify({entry: library(ARM64)}, apk=apk)
                self.assertEqual(result.returncode, 1)
                self.assertIn("Archive contains no Android 64-bit native libraries", result.stderr)


if __name__ == "__main__":
    unittest.main()
