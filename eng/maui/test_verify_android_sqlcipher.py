"""Exercise the release storage gate with synthetic APK and AAB entries."""

import hashlib
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
import zipfile


VERIFIER = Path(__file__).with_name("verify-android-bundle-sqlcipher.py")
FILES = {
    "lib/arm64-v8a/libsqlcipher.so": b"synthetic arm64 library",
    "lib/x86_64/libsqlcipher.so": b"synthetic x64 library",
    "assets/native/sqlcipher/manifest.json": b"synthetic manifest",
    "assets/native/sqlcipher/ThirdPartyNotices.txt": b"synthetic notices",
}
RECEIPT_PATHS = (
    "runtimes/android-arm64/native/libsqlcipher.so",
    "runtimes/android-x64/native/libsqlcipher.so",
    "manifest.json",
    "licenses/ThirdPartyNotices.txt",
)


class AndroidSqlCipherTests(unittest.TestCase):
    def verify(self, files, *, apk):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            receipt = root / "receipt.json"
            receipt.write_text(json.dumps({
                "schemaVersion": 1,
                "packageId": "Vafadar.Native.SqlCipher.Android",
                "entries": {
                    name: hashlib.sha256(data).hexdigest()
                    for name, data in zip(RECEIPT_PATHS, FILES.values(), strict=True)
                },
            }), encoding="utf-8")
            package = root / ("fixture.apk" if apk else "fixture.aab")
            prefix = "" if apk else "base/"
            with zipfile.ZipFile(package, "w") as archive:
                for name, data in files.items():
                    archive.writestr(prefix + name, data)
            flags = ["--apk"] if apk else []
            return subprocess.run(
                [sys.executable, str(VERIFIER), *flags, str(package), str(receipt)],
                capture_output=True, text=True, check=False,
            )

    def test_both_archive_layouts_match_the_same_receipt(self):
        for apk in (False, True):
            with self.subTest(apk=apk):
                result = self.verify(FILES, apk=apk)
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertIn("arm64-v8a, x86_64", result.stdout)

    def test_apk_refuses_unqualified_library_and_notices(self):
        for path in ("lib/arm64-v8a/libsqlcipher.so", "assets/native/sqlcipher/ThirdPartyNotices.txt"):
            with self.subTest(path=path):
                result = self.verify({**FILES, path: b"different bytes"}, apk=True)
                self.assertEqual(result.returncode, 1)
                self.assertIn(f"Archive {path} differs from the qualified SQLCipher package", result.stderr)

    def test_apk_requires_sqlcipher_in_every_present_abi(self):
        files = dict(FILES)
        del files["lib/x86_64/libsqlcipher.so"]
        files["lib/x86_64/libfixture.so"] = b"another library"
        result = self.verify(files, apk=True)
        self.assertEqual(result.returncode, 1)
        self.assertIn("Expected exactly one lib/x86_64/libsqlcipher.so", result.stderr)

    def test_apk_refuses_the_generic_sqlite_engine(self):
        result = self.verify({**FILES, "lib/x86_64/libe_sqlite3.so": b"generic"}, apk=True)
        self.assertEqual(result.returncode, 1)
        self.assertIn("contains the generic SQLite engine", result.stderr)


if __name__ == "__main__":
    unittest.main()
