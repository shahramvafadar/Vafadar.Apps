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

    def test_old_service_image_cannot_restore_after_an_account_deletion(self):
        """An opaque current marker requires the corresponding capability before creating target state."""
        args = argparse.Namespace(snapshot='zanance-hosting-snapshot-test', project='zanance-selfhost-target',
                                  public=False, gateway='direct')
        manifest = {'SourceProject': 'zanance-selfhost-source', 'Quiesced': True, 'Images': {'api': 'sha256:' + '2' * 64}}
        with (patch.object(operate, '_manifest', return_value=manifest),
              patch.object(operate, '_container', return_value=('stopped', {'state': 'exited'})),
              patch.object(operate, '_volume', return_value='original-private') as volume,
              patch.object(operate, '_metadata', return_value={'pro.vafadar.zanance.household-privacy': 'deletion-v1'}),
              patch.object(operate, '_run', side_effect=[b'database\n', b'', b'account ' + b'1' * 64 + b'\n']),
              patch.object(operate, '_exists') as exists):
            with self.assertRaisesRegex(RuntimeError, 'cannot enforce current account deletions'):
                operate._restore(args, 'sha256:' + '1' * 64)
            volume.assert_called_once_with('zanance-selfhost-source', 'private')
            exists.assert_not_called()

    def test_analysis_retirement_marker_alone_refuses_an_older_receipt_fence(self):
        args = argparse.Namespace(snapshot='zanance-hosting-snapshot-test', project='zanance-selfhost-target',
                                  public=False, gateway='direct')
        manifest = {'SourceProject': 'zanance-selfhost-source', 'Quiesced': True, 'Images': {'api': 'sha256:' + '2' * 64}}
        with (patch.object(operate, '_manifest', return_value=manifest),
              patch.object(operate, '_container', return_value=('stopped', {'state': 'exited'})),
              patch.object(operate, '_volume', return_value='original-private'),
              patch.object(operate, '_metadata', return_value={'pro.vafadar.zanance.account-privacy': 'receipt-fence-v2'}),
              patch.object(operate, '_run', side_effect=[b'database\n', b'', b'analysis ' + b'1' * 64 + b'\n']) as run,
              patch.object(operate, '_exists') as exists,
              patch.object(operate, '_compose') as compose):
            with self.assertRaisesRegex(RuntimeError, 'cannot enforce current analysis retirements'):
                operate._restore(args, 'sha256:' + '1' * 64)
            self.assertIn('*.analysis-retired', run.call_args.args[0][-1])
            exists.assert_not_called()
            compose.assert_not_called()

    def test_pending_account_request_refuses_v3_before_creating_restore_state(self):
        args = argparse.Namespace(snapshot='zanance-hosting-snapshot-test', project='zanance-selfhost-target',
                                  public=False, gateway='direct')
        manifest = {'SourceProject': 'zanance-selfhost-source', 'Quiesced': True, 'Images': {'api': 'sha256:' + '2' * 64}}
        with (patch.object(operate, '_manifest', return_value=manifest),
              patch.object(operate, '_container', return_value=('stopped', {'state': 'exited'})),
              patch.object(operate, '_volume', return_value='original-private'),
              patch.object(operate, '_metadata', return_value={'pro.vafadar.zanance.account-privacy': 'receipt-fence-v3'}),
              patch.object(operate, '_run', side_effect=[b'database\n', b'', b'request ' + b'1' * 64 + b'\n']) as run,
              patch.object(operate, '_exists') as exists,
              patch.object(operate, '_compose') as compose):
            with self.assertRaisesRegex(RuntimeError, 'cannot enforce current pending account deletions'):
                operate._restore(args, 'sha256:' + '1' * 64)
            self.assertIn('*.request', run.call_args.args[0][-1])
            exists.assert_not_called()
            compose.assert_not_called()

    def test_restore_runs_confirmed_account_reconciliation_before_starting_api(self):
        """An unfinished retirement aborts before worker startup and preserves restore continuation."""
        args = argparse.Namespace(snapshot='zanance-hosting-snapshot-test', project='zanance-selfhost-target',
                                  public=False, gateway='direct', port=14543, subnet='172.29.90.0/24',
                                  reconcile_account_deletions=True)
        source = 'zanance-selfhost-source'
        images = {'api': 'sha256:' + '2' * 64, 'database': 'sha256:' + '3' * 64}
        manifest = {'SourceProject': source, 'Quiesced': True, 'Images': images}
        labels = {'pro.vafadar.zanance.household-privacy': 'deletion-v1',
                  'pro.vafadar.zanance.account-privacy': 'receipt-fence-v3'}
        for unfinished in (False, True):
            with self.subTest(unfinished=unfinished):
                state = {'Snapshot': args.snapshot, 'Target': args.project, 'PrivacySource': source,
                         'Copied': list(operate.VOLUMES), 'DatabaseRestored': True, 'Complete': False}
                actions = []

                def compose(project, port, subnet, action, *inputs, **options):
                    self.assertEqual(args.project, project)
                    actions.append(action)
                    if action[0] == 'run' and unfinished:
                        raise RuntimeError('Outstanding restored account deletions')

                def run_command(command, **_):
                    if command[:2] == ['docker', 'ps'] and '--format' in command:
                        return b'database\n'
                    if '*.analysis-retired' in command[-1]:
                        return b'analysis ' + b'1' * 64 + b'\n'
                    return b''

                with (patch.object(operate, '_manifest', return_value=manifest),
                      patch.object(operate, '_container', return_value=('stopped', {'state': 'exited', 'image': images['database']})),
                      patch.object(operate, '_volume', side_effect=lambda project, name: project + '_' + name),
                      patch.object(operate, '_metadata', return_value=labels),
                      patch.object(operate, '_exists', return_value=True),
                      patch.object(operate, '_receipt', return_value=state),
                      patch.object(operate, '_compose', side_effect=compose),
                      patch.object(operate, '_run', side_effect=run_command) as run):
                    if unfinished:
                        with self.assertRaisesRegex(RuntimeError, 'Outstanding'):
                            operate._restore(args, 'sha256:' + '1' * 64)
                    else:
                        operate._restore(args, 'sha256:' + '1' * 64)
                    merge = [command.args[0] for command in run.call_args_list if '--merge-account-deletions' in command.args[0]]
                    self.assertEqual([['--merge-account-deletions', '/current', '/restored']], [args[-3:] for args in merge])
                self.assertEqual(['up', '-d', '--wait', 'database'], actions[0])
                self.assertEqual(['run', '--rm', '--no-deps', 'api', '--reconcile-restored-account-deletions',
                                  '--confirm-isolated-restored-database'], actions[1])
                self.assertEqual(not unfinished, state['Complete'])
                if unfinished:
                    self.assertEqual(2, len(actions))
                else:
                    self.assertEqual(['up', '-d', '--wait'], actions[2])

    def test_restore_refuses_a_live_client_of_the_original_private_volume_before_target_startup(self):
        """A running mount matters even when all original Compose clients report stopped."""
        args = argparse.Namespace(snapshot='zanance-hosting-snapshot-test', project='zanance-selfhost-target',
                                  public=False, gateway='direct', port=14543, subnet='172.29.90.0/24')
        source = 'zanance-selfhost-source'
        images = {'api': 'sha256:' + '2' * 64, 'database': 'sha256:' + '3' * 64}
        manifest = {'SourceProject': source, 'Quiesced': True, 'Images': images}
        original_private = source + '_private'
        state = {'Snapshot': args.snapshot, 'Target': args.project, 'PrivacySource': source,
                 'Copied': list(operate.VOLUMES), 'DatabaseRestored': True, 'Complete': False}
        actions = []
        source_volume_checks = 0

        def run(command, **_):
            nonlocal source_volume_checks
            if 'volume=' + original_private in command:
                source_volume_checks += 1
                if source_volume_checks == 3:
                    return b'fictional-running-source-client\n'
            if '--format' in command and 'label=com.docker.compose.project=' + source in command:
                return b'database\n'
            return b''

        with (patch.object(operate, '_manifest', return_value=manifest),
              patch.object(operate, '_container', return_value=('stopped', {'state': 'exited', 'image': images['database']})),
              patch.object(operate, '_volume', side_effect=lambda project, name: project + '_' + name),
              patch.object(operate, '_metadata', return_value={}),
              patch.object(operate, '_exists', return_value=True),
              patch.object(operate, '_receipt', return_value=state),
              patch.object(operate, '_compose', side_effect=lambda project, port, subnet, action, *inputs, **options: actions.append(action)),
              patch.object(operate, '_run', side_effect=run)):
            with self.assertRaisesRegex(RuntimeError, 'Private volume clients'):
                operate._restore(args, 'sha256:' + '1' * 64)
        self.assertEqual([['up', '-d', '--wait', 'database']], actions)
        self.assertEqual(3, source_volume_checks)
        self.assertFalse(state['Complete'])
        self.assertFalse(state.get('PrivacyReconciled', False))

    def test_restore_refuses_an_added_marker_even_after_its_writer_stops(self):
        """Adding a receipt of an already admitted type still invalidates the restore."""
        args = argparse.Namespace(snapshot='zanance-hosting-snapshot-test', project='zanance-selfhost-target',
                                  public=False, gateway='direct', port=14543, subnet='172.29.90.0/24')
        source = 'zanance-selfhost-source'
        images = {'api': 'sha256:' + '2' * 64, 'database': 'sha256:' + '3' * 64}
        manifest = {'SourceProject': source, 'Quiesced': True, 'Images': images}
        state = {'Snapshot': args.snapshot, 'Target': args.project, 'PrivacySource': source,
                 'Copied': list(operate.VOLUMES), 'DatabaseRestored': True, 'Complete': False}
        actions = []
        marker_copies = []

        def run(command, **_):
            if '--merge-account-deletions' in command:
                marker_copies.append(command)
            if '*.request' in command[-1]:
                return b'request ' + (b'2' if marker_copies else b'1') * 64 + b'\n'
            return b''

        with (patch.object(operate, '_manifest', return_value=manifest),
              patch.object(operate, '_container', return_value=('stopped', {'state': 'exited', 'image': images['database']})),
              patch.object(operate, '_volume', side_effect=lambda project, name: project + '_' + name),
              patch.object(operate, '_metadata', return_value={'pro.vafadar.zanance.account-privacy': 'receipt-fence-v4'}),
              patch.object(operate, '_exists', return_value=True),
              patch.object(operate, '_receipt', return_value=state),
              patch.object(operate, '_compose', side_effect=lambda project, port, subnet, action, *inputs, **options: actions.append(action)),
              patch.object(operate, '_run', side_effect=run)):
            with self.assertRaisesRegex(RuntimeError, 'Original deletion marker inventory changed'):
                operate._restore(args, 'sha256:' + '1' * 64)
        self.assertEqual(1, len(marker_copies))
        self.assertEqual([['up', '-d', '--wait', 'database']], actions)
        self.assertFalse(state['Complete'])
        self.assertFalse(state.get('PrivacyReconciled', False))

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
