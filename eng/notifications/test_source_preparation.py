"""Execute the actual pinned-source preparation against disposable owned fixtures."""

import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import unittest
from unittest.mock import patch


HERE = Path(__file__).resolve().parent
REPO = HERE.parent.parent
SPEC = importlib.util.spec_from_file_location("notification_source", HERE / "Prepare-NotificationCore.py")
SOURCE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(SOURCE)


class SourcePreparationTests(unittest.TestCase):
    """Changed archives, replacements and cached inputs must remain refused and retained."""

    @classmethod
    def setUpClass(cls):
        SOURCE.prepare()
        cls.parent = REPO / "artifacts" / "notification-source-guard-tests"
        cls.parent.mkdir(exist_ok=True)

    def setUp(self):
        self.fixture = tempfile.TemporaryDirectory(prefix="owned-", dir=self.parent)
        self.root = Path(self.fixture.name).resolve()
        assert self.root.is_relative_to(self.parent.resolve())
        self.here = self.root / "eng" / "notifications"
        self.here.mkdir(parents=True)
        for name in ["notification-core.inputs.json", "AndroidUtils.cs", "LICENSE.upstream"]:
            shutil.copyfile(HERE / name, self.here / name)
        self.cache = self.root / "artifacts" / "notification-core"
        self.cache.mkdir(parents=True)
        shutil.copyfile(REPO / "artifacts" / "notification-core" / "upstream.zip", self.cache / "upstream.zip")
        self.network = patch.object(SOURCE.urllib.request, "urlopen", side_effect=AssertionError("A retained archive must not be downloaded again."))
        self.network.start()

    def tearDown(self):
        self.network.stop()
        # Only the newly created fixture under the explicitly owned test directory is removed.
        assert self.root.is_relative_to(self.parent.resolve()) and self.root.name.startswith("owned-")
        self.fixture.cleanup()

    def test_exact_original_archive_and_reviewed_replacement_prepare(self):
        SOURCE.prepare(self.here)
        self.assertEqual((self.cache / "source/Platforms/Android/AndroidUtils.cs").read_bytes(), (HERE / "AndroidUtils.cs").read_bytes())
        manifest = json.loads((self.here / "notification-core.inputs.json").read_text())
        self.assertEqual(len(list((self.cache / "source").rglob("*.cs"))), len(manifest["Files"]))

    def test_changed_archive_refuses_before_compilation_source(self):
        archive = self.cache / "upstream.zip"
        changed = archive.read_bytes()[:-1]
        archive.write_bytes(changed)
        with self.assertRaises(AssertionError):
            SOURCE.prepare(self.here)
        self.assertFalse((self.cache / "source").exists())
        self.assertEqual(archive.read_bytes(), changed)

    def test_unreviewed_replacement_refuses_before_compilation_source(self):
        replacement = self.here / "AndroidUtils.cs"
        replacement.write_bytes(replacement.read_bytes() + b"\n// Unreviewed fixture change.\n")
        with self.assertRaises(AssertionError):
            SOURCE.prepare(self.here)
        self.assertFalse((self.cache / "source").exists())

    def test_changed_cached_source_is_refused_and_preserved(self):
        SOURCE.prepare(self.here)
        target = self.cache / "source/GeofenceHandlerRegistry.cs"
        changed = target.read_bytes() + b"\n// Changed retained source fixture.\n"
        target.write_bytes(changed)
        with self.assertRaises(AssertionError):
            SOURCE.prepare(self.here)
        self.assertEqual(target.read_bytes(), changed)

    def test_extra_cached_compilation_input_is_refused_and_preserved(self):
        SOURCE.prepare(self.here)
        extra = self.cache / "source/Unexpected.cs"
        extra.write_bytes(b"// Unexpected owned fixture input.\n")
        with self.assertRaises(AssertionError):
            SOURCE.prepare(self.here)
        self.assertTrue(extra.exists())


if __name__ == "__main__":
    unittest.main()
