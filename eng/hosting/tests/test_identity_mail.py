"""Original production sender continuity without credential access, Docker effects or prior operation replay."""
import argparse
import os
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import operate


class IdentityMailTests(unittest.TestCase):
    """Preserve the separate sender mode across controlled restart, snapshot and recovery."""

    def test_explicit_brevo_overlay_is_only_added_to_production_start(self):
        with patch.object(operate, '_run', return_value=b'') as run:
            operate._compose('zanance-selfhost-test', 14543, '172.29.90.0/24', ['up', '-d'], public=True, gateway='tunnel', mail='brevo')
            self.assertIn(str(operate.BASE / 'compose.brevo.yml'), run.call_args.args[0])
        with patch.object(operate, '_run') as run:
            with self.assertRaises(RuntimeError):
                operate._compose('zanance-selfhost-test', 14543, '172.29.90.0/24', ['up'], mail='brevo')
            run.assert_not_called()

    def test_stop_does_not_require_a_missing_credential_mount(self):
        with patch.object(operate, '_run', return_value=b'') as run:
            operate._compose('zanance-selfhost-test', 14543, '172.29.90.0/24', ['stop', 'api'], public=True, gateway='tunnel', mail='brevo')
            self.assertNotIn(str(operate.BASE / 'compose.brevo.yml'), run.call_args.args[0])

    def test_existing_sender_is_preserved_and_replacement_or_missing_mode_refuses(self):
        original = {'Provider': 'brevo', 'SenderEmail': 'sender@example.invalid'}
        args = argparse.Namespace(public=True, mail_provider='brevo', action='start')
        with (patch.dict(os.environ, {'ZANANCE_BREVO_SENDER_EMAIL': 'sender@example.invalid'}),
              patch.object(operate, '_require_brevo_inputs') as require_inputs):
            operate._require_mail(args, original)
            require_inputs.assert_called_once_with()
            with self.assertRaises(RuntimeError): operate._require_mail(args, None)
        with patch.dict(os.environ, {'ZANANCE_BREVO_SENDER_EMAIL': 'other@example.invalid'}):
            with self.assertRaises(RuntimeError): operate._require_mail(args, original)
        args.mail_provider = None
        with self.assertRaises(RuntimeError): operate._require_mail(args, original)

    def test_sender_metadata_reader_never_inspects_the_credential_environment(self):
        with patch.object(operate, '_container', return_value=('owned-api', {})), patch.object(operate, '_run', side_effect=[b'brevo', b'sender@example.invalid']) as run:
            self.assertEqual({'Provider': 'brevo', 'SenderEmail': 'sender@example.invalid'}, operate._mail_mode('zanance-selfhost-test'))
            for call in run.call_args_list:
                self.assertNotIn('ApiKey', call.args[0][-1])
                self.assertNotIn('json .Config.Env', call.args[0][-1])

    def test_stop_remains_possible_without_fresh_owner_configuration(self):
        args = argparse.Namespace(public=True, mail_provider='brevo', action='stop')
        with patch.dict(os.environ, {}, clear=True):
            operate._require_mail(args, {'Provider': 'brevo', 'SenderEmail': 'sender@example.invalid'})

    def test_brevo_configuration_check_reads_metadata_without_docker(self):
        with tempfile.TemporaryDirectory(prefix='zanance-mail-input-test-') as folder:
            path = Path(folder) / 'fictional-key.txt'
            path.write_text('fictional fixture only', encoding='utf-8')
            with (
                patch.object(sys, 'argv', ['operate.py', 'check-brevo-config']),
                patch.dict(os.environ, {'ZANANCE_BREVO_SENDER_EMAIL': 'sender@example.invalid',
                                        'ZANANCE_BREVO_API_KEY_FILE': str(path)}, clear=True),
                patch.object(Path, 'open', side_effect=AssertionError('Credential content access refused')),
                patch.object(operate, '_run', side_effect=AssertionError('Docker effects refused')),
            ):
                operate.main()

    def test_brevo_maintenance_refuses_unusable_key_paths_before_docker(self):
        original = {'Provider': 'brevo', 'SenderEmail': 'sender@example.invalid'}
        args = argparse.Namespace(public=True, mail_provider='brevo', action='renew-transport')
        with tempfile.TemporaryDirectory(prefix='zanance-mail-input-test-') as folder:
            root = Path(folder)
            checkout = root / 'checkout'
            checkout.mkdir()
            internal = checkout / 'fictional-key.txt'
            internal.write_text('fictional fixture only', encoding='utf-8')
            oversized = root / 'oversized.txt'
            oversized.write_bytes(b'x' * 513)
            for candidate in ('', 'relative.txt', checkout / '..' / 'checkout' / internal.name,
                              root, root / 'missing.txt', oversized):
                with (
                    self.subTest(candidate=candidate),
                    patch.object(operate, 'BASE', checkout / 'eng' / 'hosting'),
                    patch.dict(os.environ, {'ZANANCE_BREVO_SENDER_EMAIL': original['SenderEmail'],
                                            'ZANANCE_BREVO_API_KEY_FILE': str(candidate)}, clear=True),
                    patch.object(operate, '_run', side_effect=AssertionError('Docker effects refused')),
                ):
                    with self.assertRaises(RuntimeError):
                        operate._require_mail(args, original)


if __name__ == '__main__': unittest.main()
