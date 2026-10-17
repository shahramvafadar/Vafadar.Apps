"""Exercise the capability boundary used by the actual native Configure invocation."""

import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

SCRIPT = Path(__file__).with_name("read-storage-profile.py")
SPEC = importlib.util.spec_from_file_location("storage_profile", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)
CANONICAL = SCRIPT.parent.parent / "windows.crypto.profile.json"


class StorageProfileBoundaryTests(unittest.TestCase):
    """Unsafe removals and argument injection must fail before producing Configure input."""

    def setUp(self):
        """Give each behavior its own disposable fictitious profile and output directory."""
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.path = Path(self.directory.name) / "profile.json"
        self.profile = json.loads(CANONICAL.read_text(encoding="utf-8-sig"))

    def write(self, value):
        """Persist only the explicitly supplied fictitious capability document."""
        self.path.write_text(json.dumps(value), encoding="utf-8")

    def test_canonical_options_and_original_bytes_are_bound(self):
        """The real input is returned unchanged and its digest binds the precise producer bytes."""
        options, digest = MODULE.read_profile(CANONICAL)
        self.assertEqual(self.profile["opensslConfigureOptions"], options)
        self.assertEqual(hashlib.sha256(CANONICAL.read_bytes()).hexdigest(), digest)

    def test_required_algorithms_memory_threads_and_tests_cannot_be_removed(self):
        """A profile cannot quietly remove storage primitives, secure memory, assembly or verification."""
        for option in ("no-aes", "no-sha1", "no-sha256", "no-sha512", "no-hmac", "no-kdf",
                       "no-rand", "no-threads", "no-secure-memory", "no-asm", "no-tests"):
            with self.subTest(option=option):
                value = copy.deepcopy(self.profile)
                value["opensslConfigureOptions"].append(option)
                self.write(value)
                with self.assertRaises(ValueError):
                    MODULE.read_profile(self.path)

    def test_argument_injection_duplicates_and_non_strings_are_refused(self):
        """No emitted Configure argument may contain arbitrary commands or repeated removals."""
        for option in ("no-dsa; touch unexpected", "no-dsa\nno-aes", "no-dsa", {"no-dsa": True}):
            with self.subTest(option=option):
                value = copy.deepcopy(self.profile)
                value["opensslConfigureOptions"].append(option)
                self.write(value)
                with self.assertRaises(ValueError):
                    MODULE.read_profile(self.path)

    def test_source_fork_or_suppression_and_changed_required_contract_are_refused(self):
        """Required primitive identities and the no-fork/no-suppression commitment cannot be waived."""
        cases = []
        value = copy.deepcopy(self.profile)
        value["sourceForkOrWarningSuppression"] = True
        cases.append(value)
        value = copy.deepcopy(self.profile)
        value["requiredPrimitives"][-1] = "SHA1"
        cases.append(value)
        cases.extend((None, [], {}, {**self.profile, "schemaVersion": 2}))
        for value in cases:
            with self.subTest(value=value):
                self.write(value)
                with self.assertRaises(ValueError):
                    MODULE.read_profile(self.path)

    def test_invalid_cli_input_emits_no_arguments_or_proof(self):
        """The shell receives a nonzero exit before mapfile can consume unsafe or empty arguments."""
        value = copy.deepcopy(self.profile)
        value["opensslConfigureOptions"].append("no-aes")
        self.write(value)
        proof = self.path.with_name("proof.json")
        result = subprocess.run([sys.executable, str(SCRIPT), str(self.path), "--proof", str(proof)],
                                capture_output=True, text=True, check=False)
        self.assertNotEqual(0, result.returncode)
        self.assertEqual("", result.stdout)
        self.assertFalse(proof.exists())

    def test_completed_proof_cannot_be_overwritten(self):
        """A repeated observation cannot silently replace the original capability evidence."""
        self.write(self.profile)
        proof = self.path.with_name("proof.json")
        command = [sys.executable, str(SCRIPT), str(self.path), "--proof", str(proof)]
        first = subprocess.run(command, capture_output=True, text=True, check=False)
        self.assertEqual(0, first.returncode, first.stderr)
        original = proof.read_bytes()
        second = subprocess.run(command, capture_output=True, text=True, check=False)
        self.assertNotEqual(0, second.returncode)
        self.assertEqual("", second.stdout)
        self.assertEqual(original, proof.read_bytes())


if __name__ == "__main__":
    unittest.main()
