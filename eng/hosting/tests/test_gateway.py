"""Focused routing/ownership regressions; no actual credential bytes or unrelated app tests."""
import argparse
import json
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import operate
import tunnel


class GatewayTests(unittest.TestCase):
    """Protect original ingress across operations and refuse credential-content access."""

    def test_tunnel_origin_uses_no_direct_overlay_and_scopes_connector_address(self):
        """Compose preserves the tunnel overlay and exact private connector rather than opening 443."""
        with patch.object(operate, '_run', return_value=b'') as run:
            operate._compose('zanance-selfhost-test', 14543, '172.29.90.0/24', ['up', '-d'], public=True, gateway='tunnel')
        args, = run.call_args.args
        self.assertIn(str(operate.BASE / 'compose.tunnel-origin.yml'), args)
        self.assertNotIn(str(operate.BASE / 'compose.public.yml'), args)
        self.assertEqual('172.29.90.20', run.call_args.kwargs['env']['ZANANCE_CONNECTOR_ADDRESS'])

    def test_direct_public_requires_443_and_tunnel_cannot_be_implicit_local(self):
        """Invalid mixed routing is refused before invoking any operation."""
        with patch.object(operate, '_run') as run:
            with self.assertRaises(RuntimeError): operate._compose('zanance-selfhost-test', 14543, '172.29.90.0/24', ['up'], public=True)
            with self.assertRaises(RuntimeError): operate._compose('zanance-selfhost-test', 14543, '172.29.90.0/24', ['up'], gateway='tunnel')
            run.assert_not_called()

    def test_changed_original_gateway_is_refused_before_worker_mutation(self):
        """A tunnel cannot silently become a direct-public deployment, including during maintenance."""
        args = argparse.Namespace(project='zanance-selfhost-test', public=True, gateway='direct')
        with patch.object(operate, '_gateway_mode', return_value='tunnel'), patch.object(operate, '_run') as run:
            with self.assertRaises(RuntimeError): operate._require_gateway(args)
            run.assert_not_called()

    def test_matching_gateway_cannot_promote_a_fictional_api_origin(self):
        """A matching Caddy command alone is not proof of production API authority."""
        args = argparse.Namespace(project='zanance-selfhost-test', public=True, gateway='tunnel')
        with patch.object(operate, '_gateway_mode', return_value='tunnel'), patch.object(operate, '_container', return_value=('owned-api', {})), patch.object(operate, '_run', return_value=b'https://localhost:14543') as run:
            with self.assertRaises(RuntimeError): operate._require_gateway(args)
            self.assertEqual(1, run.call_count)
            self.assertEqual(['docker', 'inspect', 'owned-api'], run.call_args.args[0][:3])

    def test_snapshot_gateway_mismatch_is_refused_before_target_creation(self):
        """An authentic manifest still cannot change ingress mode as a side effect of restore."""
        args = argparse.Namespace(snapshot='zanance-hosting-snapshot-test', public=True, gateway='tunnel')
        with patch.object(operate, '_manifest', return_value={'Public': True, 'Gateway': 'direct'}), patch.object(operate, '_run') as run:
            with self.assertRaises(RuntimeError): operate._restore(args, 'sha256:' + '1' * 64)
            run.assert_not_called()

    def test_owner_credential_validation_reads_metadata_only(self):
        """The external file is mounted later by the container, never parsed or copied by this operator."""
        with tempfile.TemporaryDirectory(prefix='zanance-tunnel-test-') as directory:
            path = Path(directory) / 'fictitious.json'
            path.write_text('fictitious fixture, not a credential', encoding='utf-8')
            with patch.object(Path, 'read_bytes', side_effect=AssertionError('Credential content access refused')):
                self.assertEqual(str(path.absolute()), tunnel._file(str(path), credential=True))

    def test_missing_credential_path_cannot_create_a_host_directory(self):
        """A missing bind source fails through metadata validation before any Compose invocation."""
        with tempfile.TemporaryDirectory(prefix='zanance-tunnel-test-') as directory:
            path = Path(directory) / 'missing.json'
            with self.assertRaises(RuntimeError): tunnel._file(str(path), credential=True)
            self.assertFalse(path.exists())


if __name__ == '__main__': unittest.main()
