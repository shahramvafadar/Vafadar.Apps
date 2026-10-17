"""Operate only the isolated Zanance stack; stream private snapshots without exposing their bytes to the host."""
import argparse
import datetime
import ipaddress
import json
import os
import re
import subprocess
import threading
from pathlib import Path

BASE = Path(__file__).resolve().parent
VOLUMES = ('authority', 'database-identity', 'api-identity', 'operator-identity', 'trust', 'private', 'proxy-data')
PROJECT = re.compile(r'zanance-selfhost(?:-[a-z0-9][a-z0-9-]{0,35})?\Z')
SNAPSHOT = re.compile(r'zanance-hosting-snapshot-[a-z0-9][a-z0-9-]{0,50}\Z')
IMAGE = re.compile(r'sha256:[0-9a-f]{64}\Z')


def _run(args, *, data=None, env=None):
    """Capture command output for bounded metadata only, never print command arguments or private payloads."""
    result = subprocess.run(args, input=data, stdout=subprocess.PIPE, stderr=subprocess.PIPE, env=env, check=False)
    if result.returncode:
        raise RuntimeError('Owned operation failed: ' + args[0] + ' exit ' + str(result.returncode))
    return result.stdout


def _metadata(kind, name, expression):
    return json.loads(_run(['docker', kind, 'inspect', name, '--format', expression]).decode())


def _exists(kind, name):
    selector = 'name=^' + name + '$' if kind == 'volume' else 'name=^/' + name + '$'
    return bool(_run(['docker', kind, 'ls', '-q', '--filter', selector]).strip())


def _container(project, service):
    name = project + '-' + service + '-1'
    value = _metadata('container', name, '{"labels":{{json .Config.Labels}},"image":{{json .Image}},"state":{{json .State.Status}}}')
    labels = value['labels']
    if labels.get('com.docker.compose.project') != project or labels.get('com.docker.compose.service') != service or labels.get('pro.vafadar.zanance.hosting') != 'd293':
        raise RuntimeError('Container ownership differs; mutation refused.')
    return name, value


def _volume(project, logical, create=False):
    name = project + '_' + logical
    if create:
        if _exists('volume', name):
            raise RuntimeError('Existing target volume must not be overwritten.')
        _run(['docker', 'volume', 'create', '--label', 'com.docker.compose.project=' + project,
              '--label', 'com.docker.compose.volume=' + logical, '--label', 'pro.vafadar.zanance.hosting=d293', name])
    labels = _metadata('volume', name, '{{json .Labels}}')
    if labels.get('com.docker.compose.project') != project or labels.get('com.docker.compose.volume') != logical:
        raise RuntimeError('Volume ownership differs; mutation refused.')
    return name


def _helper(image, mounts, script, data=None):
    if not IMAGE.fullmatch(image):
        raise RuntimeError('An existing immutable helper image is required.')
    args = ['docker', 'run', '--rm', '-i', '--network', 'none', '--user', '0:0', '--read-only',
            '--cap-drop', 'ALL', '--cap-add', 'DAC_OVERRIDE', '--cap-add', 'CHOWN', '--cap-add', 'FOWNER',
            '--security-opt', 'no-new-privileges:true']
    for volume, target, readonly in mounts:
        args += ['--mount', 'type=volume,source=' + volume + ',target=' + target + (',readonly' if readonly else '')]
    return args + ['--entrypoint', 'sh', image, '-c', script]


def _pipe(left, right):
    """Keep pg_dump/restore private binary bytes in OS pipes, never a PowerShell text pipeline or host file."""
    errors = [bytearray(), bytearray()]
    def drain(stream, target):
        while chunk := stream.read(4096):
            target.extend(chunk[:max(0, 65536 - len(target))])
    with subprocess.Popen(left, stdout=subprocess.PIPE, stderr=subprocess.PIPE) as producer:
        with subprocess.Popen(right, stdin=producer.stdout, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE) as consumer:
            readers = [threading.Thread(target=drain, args=(producer.stderr, errors[0])),
                       threading.Thread(target=drain, args=(consumer.stderr, errors[1]))]
            for reader in readers: reader.start()
            producer.stdout.close()
            consumed = consumer.wait()
            readers[1].join()
        produced = producer.wait()
        readers[0].join()
    if produced or consumed:
        # Classify known operational failures without echoing SQL, values, private bytes or arguments.
        joined = bytes(errors[0] + errors[1]).lower()
        categories = [text for text in ('already exists', 'permission denied', 'certificate', 'not found') if text.encode() in joined]
        raise RuntimeError('Owned private stream failed (producer ' + str(produced) + ', consumer ' + str(consumed)
                           + '; ' + ', '.join(categories) + '); original and partial target preserved.')


def _compose(project, port, subnet, action, images=None, public=False, household=None, gateway="direct"):
    network = ipaddress.ip_network(subnet, strict=True)
    if network.version != 4 or network.prefixlen != 24 or not network.is_private or not str(network).startswith('172.'):
        raise RuntimeError('Explicit private /24 service subnet required.')
    if gateway not in ('direct', 'tunnel') or gateway == 'tunnel' and not public:
        raise RuntimeError('Tunnel origins require explicit public deployment mode.')
    if (public and gateway == 'direct' and port != 443) or ((not public or gateway == 'tunnel') and not 1024 <= port <= 65535):
        raise RuntimeError('Explicit loopback-only proxy port required.')
    env = os.environ.copy()
    if public and household:
        raise RuntimeError('Fictional household qualification cannot activate public hosting.')
    if household is not None:
        env['ZANANCE_HOUSEHOLD_QUALIFICATION'] = 'true' if household else 'false'
    env.update(ZANANCE_PROXY_PORT=str(port), ZANANCE_SERVICE_SUBNET=str(network), ZANANCE_PROXY_ADDRESS=str(network.network_address + 10), ZANANCE_CONNECTOR_ADDRESS=str(network.network_address + 20))
    if images:
        env.update(ZANANCE_SERVICE_IMAGE=images['api'], ZANANCE_PROXY_IMAGE=images['proxy'], ZANANCE_BOOTSTRAP_IMAGE=images['provision'], ZANANCE_DATABASE_IMAGE=images['database'])
    files = ['-f', str(BASE / 'compose.yml')]
    if public: files += ['-f', str(BASE / ('compose.tunnel-origin.yml' if gateway == 'tunnel' else 'compose.public.yml'))]
    return _run(['docker', 'compose', '-p', project, *files, *action], env=env)


def _gateway_mode(project):
    """Inspect only the owned proxy command; never infer routing mode from a caller's port choice."""
    name, _ = _container(project, 'proxy')
    command = _metadata('container', name, '{{json .Config.Cmd}}')
    if command == ['caddy', 'run', '--config', '/etc/caddy/Caddyfile.tunnel', '--adapter', 'caddyfile']: return 'tunnel'
    if command == ['caddy', 'run', '--config', '/etc/caddy/Caddyfile.public', '--adapter', 'caddyfile']: return 'direct'
    if command == ['caddy', 'run', '--config', '/etc/caddy/Caddyfile', '--adapter', 'caddyfile']: return 'local'
    raise RuntimeError('Unknown original gateway configuration; preserve it without mutation.')


def _require_gateway(args):
    """Refuse an implicit public/loopback or direct/tunnel transition before stopping or starting workers."""
    expected = args.gateway if args.public else 'local'
    if _gateway_mode(args.project) != expected:
        raise RuntimeError('Original gateway mode differs; explicit qualified routing migration required.')
    api, _ = _container(args.project, 'api')
    original = _run(['docker', 'inspect', api, '--format',
        '{{range .Config.Env}}{{if eq (index (split . "=") 0) "Service__PublicOrigin"}}{{index (split . "=") 1}}{{end}}{{end}}']).decode().strip()
    authority = 'https://api.zanance.vafadar.pro' if args.public else 'https://localhost:' + str(args.port)
    if original != authority:
        raise RuntimeError('Original API origin differs; preserve existing authority without mutation.')


def _household_mode(project):
    """Read only this nonsecret technical opt-in, never dump the container's credential environment."""
    name, _ = _container(project, 'api')
    value = _run(['docker', 'container', 'inspect', name, '--format',
                  '{{range .Config.Env}}{{if eq (index (split . "=") 0) "Service__Household__LocalQualification"}}{{index (split . "=") 1}}{{end}}{{end}}']).strip().lower()
    if value not in (b'', b'true', b'false'):
        raise RuntimeError('Original household opt-in is not a supported boolean; preserve source configuration.')
    return value == b'true'


def _manifest(snapshot, helper):
    if not SNAPSHOT.fullmatch(snapshot):
        raise RuntimeError('Explicit owned snapshot name required.')
    labels = _metadata('volume', snapshot, '{{json .Labels}}')
    if labels.get('pro.vafadar.zanance.snapshot') != 'd293':
        raise RuntimeError('Snapshot ownership differs.')
    data = _run(_helper(helper, [(snapshot, '/snapshot', True)], 'test -f /snapshot/complete.json && cat /snapshot/complete.json'))
    if len(data) > 65536:
        raise RuntimeError('Snapshot manifest exceeds its bound.')
    value = json.loads(data)
    if value.get('Schema') != 1 or value.get('Quiesced') is not True or not PROJECT.fullmatch(value.get('SourceProject', '')) or set(value.get('Archives', {})) != set(VOLUMES) | {'database'}:
        raise RuntimeError('Complete snapshot manifest is invalid.')
    if value.get('DeploymentFormat') != 'immutable-config-v1':
        raise RuntimeError('Historical snapshot requires its original runtime configuration; current restore cannot silently substitute it.')
    if not isinstance(value.get('HouseholdLocalQualification', False), bool) or value.get('HouseholdLocalQualification', False) and value.get('Public', False):
        raise RuntimeError('Household qualification cannot activate public hosting.')
    if value.get('Gateway', 'direct') not in ('direct', 'tunnel') or value.get('Gateway') == 'tunnel' and not value.get('Public', False):
        raise RuntimeError('Snapshot gateway authority is invalid.')
    if set(value.get('Images', {})) != {'api', 'proxy', 'provision', 'database'}:
        raise RuntimeError('Snapshot image authority is incomplete.')
    for image in value.get('Images', {}).values():
        if not IMAGE.fullmatch(image):
            raise RuntimeError('Snapshot image identity is invalid.')
    for logical, expected in value['Archives'].items():
        if not re.fullmatch('[0-9a-f]{64}', expected):
            raise RuntimeError('Snapshot integrity identity is invalid.')
        filename = 'database.dump' if logical == 'database' else 'volumes/' + logical + '.tar'
        actual = _run(_helper(helper, [(snapshot, '/snapshot', True)], 'sha256sum /snapshot/' + filename)).decode().split()[0]
        if actual != expected:
            raise RuntimeError('Snapshot integrity differs; restore refused.')
    return value


def _receipt(helper, volume, value=None):
    mounts = [(volume, '/operation', value is None)]
    if value is None:
        return json.loads(_run(_helper(helper, mounts, 'cat /operation/state.json')))
    script = 'umask 077; cat >/operation/state.pending; sync -f /operation/state.pending; mv /operation/state.pending /operation/state.json; sync -f /operation'
    _run(_helper(helper, mounts, script), data=json.dumps(value).encode())


def _backup(args, helper):
    if not SNAPSHOT.fullmatch(args.snapshot or '') or _exists('volume', args.snapshot):
        raise RuntimeError('Choose a fresh owned snapshot volume; existing snapshots are never reset.')
    _require_gateway(args)
    database, _ = _container(args.project, 'database')
    household = _household_mode(args.project)
    for service in ('api', 'proxy', 'migrate', 'provision'):
        _container(args.project, service)
    active = _run(['docker', 'ps', '--filter', 'label=com.docker.compose.project=' + args.project,
                   '--format', '{{.Label "com.docker.compose.service"}}']).decode().splitlines()
    if any(service not in ('api', 'proxy', 'database') for service in active):
        raise RuntimeError('An owned migration/qualification is still live; snapshot refused.')
    _compose(args.project, args.port, args.subnet, ['stop', '--timeout', '45', 'api', 'proxy'], public=args.public, gateway=args.gateway)
    _, api = _container(args.project, 'api')
    _, proxy = _container(args.project, 'proxy')
    if api['state'] != 'exited' or proxy['state'] != 'exited':
        raise RuntimeError('Original workers are not drained; snapshot refused.')
    remaining = _run(['docker', 'exec', database, 'psql', '-h', '/tmp', '-U', 'postgres', '-d', 'zanance', '-tAc',
                      "SELECT count(*) FROM pg_stat_activity WHERE usename IN ('zanance_runtime','zanance_owner')"]).strip()
    if remaining != b'0':
        raise RuntimeError('An original database client is still live; source remains stopped.')
    images = {service: _container(args.project, service)[1]['image'] for service in ('api', 'proxy', 'provision', 'database')}
    _run(['docker', 'volume', 'create', '--label', 'pro.vafadar.zanance.snapshot=d293', '--label', 'pro.vafadar.zanance.hosting=d293', args.snapshot])
    target = [(args.snapshot, '/snapshot', False)]
    _run(_helper(helper, target, 'chmod 700 /snapshot; umask 077; mkdir /snapshot/volumes'))
    _pipe(['docker', 'exec', database, 'pg_dump', '-h', '/tmp', '-U', 'postgres', '-Fc', 'zanance'],
          _helper(helper, target, 'umask 077; cat >/snapshot/database.dump; sync -f /snapshot/database.dump'))
    for logical in VOLUMES:
        source = _volume(args.project, logical)
        _run(_helper(helper, target + [(source, '/source', True)],
                     'umask 077; tar -C /source --numeric-owner -cf /snapshot/volumes/' + logical + '.tar .; sync -f /snapshot/volumes/' + logical + '.tar'))
    archives = {}
    for logical in ('database', *VOLUMES):
        filename = 'database.dump' if logical == 'database' else 'volumes/' + logical + '.tar'
        archives[logical] = _run(_helper(helper, target, 'sha256sum /snapshot/' + filename)).decode().split()[0]
    manifest = {'Schema': 1, 'DeploymentFormat': 'immutable-config-v1', 'SourceProject': args.project, 'CreatedUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
                'Quiesced': True, 'Public': args.public, 'Gateway': args.gateway, 'HouseholdLocalQualification': household, 'Images': images, 'Archives': archives}
    _run(_helper(helper, target, 'umask 077; cat >/snapshot/complete.pending; sync -f /snapshot/complete.pending; mv /snapshot/complete.pending /snapshot/complete.json; sync -f /snapshot'), data=json.dumps(manifest).encode())
    _manifest(args.snapshot, helper)
    print('Complete private snapshot: ' + args.snapshot + '; original workers remain stopped.')


def _restore(args, helper):
    manifest = _manifest(args.snapshot, helper)
    if bool(manifest.get('Public', False)) != args.public or manifest.get('Gateway', 'direct') != args.gateway:
        raise RuntimeError('Snapshot deployment mode differs; explicit trust/routing migration required.')
    if args.project == manifest['SourceProject'] or not manifest.get('Quiesced'):
        raise RuntimeError('Only a separate fresh or explicitly continued target is allowed.')
    privacy_source = manifest['SourceProject']
    if not PROJECT.fullmatch(privacy_source) or privacy_source == args.project:
        raise RuntimeError('Explicit distinct original privacy project required.')
    # An old full private-volume snapshot cannot prove that no deletion happened later.
    # Require the actual latest owned source, drained before inspecting/copying markers.
    for service in ('api', 'proxy'):
        if _container(privacy_source, service)[1]['state'] != 'exited':
            raise RuntimeError('Latest original privacy workers must be stopped before restore.')
    active_source = _run(['docker', 'ps', '--filter', 'label=com.docker.compose.project=' + privacy_source,
                         '--format', '{{.Label "com.docker.compose.service"}}']).decode().splitlines()
    if any(service != 'database' for service in active_source):
        raise RuntimeError('Latest original private-storage clients must be drained before restore.')
    current_private = _volume(privacy_source, 'private')
    privacy_image = manifest['Images']['api']
    privacy_labels = _metadata('image', privacy_image, '{{json .Config.Labels}}') or {}
    capable = privacy_labels.get('pro.vafadar.zanance.household-privacy') == 'deletion-v1'
    markers = _run(_helper(helper, [(current_private, '/current', True)],
        'test ! -L /current/household-deletions || exit 23; if test -e /current/household-deletions; then test -d /current/household-deletions || exit 23; find /current/household-deletions -mindepth 1 -maxdepth 1 -name "*.json" -print -quit; fi')).strip()
    if markers and not capable:
        raise RuntimeError('Historical service image cannot enforce current household deletions; explicit upgrade qualification required.')
    state_name = args.project + '_restore-state'
    if _exists('volume', state_name):
        state_name = _volume(args.project, 'restore-state')
        state = _receipt(helper, state_name)
        if state.get('Snapshot') != args.snapshot or state.get('Target') != args.project:
            raise RuntimeError('Existing restore authority differs; no target changes made.')
        if state.get('PrivacySource', privacy_source) != privacy_source:
            raise RuntimeError('Original privacy authority differs; no target changes made.')
    else:
        for logical in (*VOLUMES, 'database-data'):
            if _exists('volume', args.project + '_' + logical):
                raise RuntimeError('Unowned existing target state is preserved; restore refused.')
        if _run(['docker', 'ps', '-aq', '--filter', 'label=com.docker.compose.project=' + args.project]).strip():
            raise RuntimeError('An existing target deployment cannot be overwritten.')
        state_name = _volume(args.project, 'restore-state', create=True)
        state = {'Snapshot': args.snapshot, 'Target': args.project, 'PrivacySource': privacy_source, 'Copied': [], 'DatabaseRestored': False, 'Complete': False}
        _receipt(helper, state_name, state)
    if state.get('Complete'):
        print('Restore already complete; no successful operation repeated.'); return
    for logical in VOLUMES:
        if logical in state['Copied']:
            _volume(args.project, logical); continue
        target = args.project + '_' + logical
        if not _exists('volume', target):
            _volume(args.project, logical, create=True)
        else:
            _volume(args.project, logical)
            empty = _run(_helper(helper, [(target, '/target', True)], 'find /target -mindepth 1 -print -quit')).strip()
            if empty:
                raise RuntimeError('Interrupted nonempty volume copy preserved; inspect its original restore authority before continuing.')
        _run(_helper(helper, [(args.snapshot, '/snapshot', True), (target, '/target', False)],
                     'tar -C /target --numeric-owner -xpf /snapshot/volumes/' + logical + '.tar; sync -f /target'))
        state['Copied'].append(logical); _receipt(helper, state_name, state)
    _compose(args.project, args.port, args.subnet, ['up', '-d', '--wait', 'database'], manifest['Images'], public=args.public, gateway=args.gateway)
    database, restored_database = _container(args.project, 'database')
    if restored_database['image'] != manifest['Images']['database']:
        raise RuntimeError('Database image differs from the snapshot authority; explicit upgrade qualification required.')
    if not state['DatabaseRestored']:
        count = _run(['docker', 'exec', database, 'psql', '-h', '/tmp', '-U', 'postgres', '-d', 'zanance', '-tAc',
                      "SELECT count(*) FROM information_schema.tables WHERE table_schema='public'"]).strip()
        if count != b'0':
            raise RuntimeError('Nonempty target database preserved; automatic overwrite refused.')
        _pipe(_helper(helper, [(args.snapshot, '/snapshot', True)], 'cat /snapshot/database.dump'),
              ['docker', 'exec', '-i', database, 'pg_restore', '-h', '/tmp', '-U', 'postgres', '-d', 'zanance', '--single-transaction', '--exit-on-error'])
        state['DatabaseRestored'] = True; _receipt(helper, state_name, state)
    if capable:
        # Re-run only this idempotent additive merge on interrupted restoration; never
        # replace an original marker or skip newer privacy intent before serving data.
        _run(['docker', 'run', '--rm', '--network', 'none', '--read-only', '--user', '1654:1654',
              '--cap-drop', 'ALL', '--security-opt', 'no-new-privileges:true',
              '--mount', 'type=volume,source=' + current_private + ',target=/current,readonly',
              '--mount', 'type=volume,source=' + _volume(args.project, 'private') + ',target=/restored',
              privacy_image, '--merge-household-deletions', '/current', '/restored'])
    state['PrivacySource'] = privacy_source
    state['PrivacyReconciled'] = True
    _receipt(helper, state_name, state)
    for service in ('api', 'proxy'):
        if _container(privacy_source, service)[1]['state'] != 'exited':
            raise RuntimeError('Original privacy workers restarted during restore; target remains stopped.')
    _compose(args.project, args.port, args.subnet, ['up', '-d', '--wait'], manifest['Images'], public=args.public, gateway=args.gateway,
             household=manifest.get('HouseholdLocalQualification', False))
    state['Complete'] = True; _receipt(helper, state_name, state)
    print('Isolated restore complete; original source and snapshot preserved: ' + args.project)


def main():
    """Require explicit owned targets and never touch another project's containers, network or data."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=('status', 'start', 'stop', 'backup', 'restore', 'renew-transport'))
    parser.add_argument('--project', default='zanance-selfhost')
    parser.add_argument('--port', type=int, default=14543)
    parser.add_argument('--subnet', default='172.29.83.0/24')
    parser.add_argument('--snapshot')
    parser.add_argument('--public', action='store_true', help='Preserve an already approved public deployment.')
    parser.add_argument('--gateway', choices=('direct', 'tunnel'), default='direct', help='Preserve original public ingress; direct mode requires port 443.')
    args = parser.parse_args()
    if not PROJECT.fullmatch(args.project):
        raise RuntimeError('Explicit Zanance project ownership required.')
    helper = _run(['docker', 'image', 'inspect', 'zanance-bootstrap:d293', '--format', '{{.Id}}']).decode().strip()
    if not IMAGE.fullmatch(helper):
        raise RuntimeError('Build the reviewed immutable helper image first.')
    if args.action == 'backup': _backup(args, helper)
    elif args.action == 'restore': _restore(args, helper)
    elif args.action == 'renew-transport':
        # A complete pre-maintenance snapshot is mandatory; never rotate key-ring protection or private keys.
        if not args.snapshot:
            raise RuntimeError('A fresh pre-maintenance snapshot name is required.')
        household = _household_mode(args.project)
        _backup(args, helper)
        _compose(args.project, args.port, args.subnet, ['stop', '--timeout', '45', 'database'], public=args.public, gateway=args.gateway)
        _compose(args.project, args.port, args.subnet,
                 ['run', '--no-deps', '-e', 'ZANANCE_CERTIFICATE_OPERATION=renew-transport', 'provision'], public=args.public, gateway=args.gateway)
        _compose(args.project, args.port, args.subnet, ['up', '-d', '--wait'], public=args.public, gateway=args.gateway, household=household)
        print('Transport renewal complete; retained identities and snapshot were preserved.')
    elif args.action == 'status': print(_compose(args.project, args.port, args.subnet, ['ps', '-a', '--format', '{{.Service}} {{.State}} {{.Health}}'], public=args.public, gateway=args.gateway).decode(), end='')
    else:
        _require_gateway(args)
        for service in ('api', 'proxy', 'database', 'provision', 'migrate'): _container(args.project, service)
        action = ['up', '-d', '--wait'] if args.action == 'start' else ['stop', '--timeout', '45', 'api', 'proxy']
        _compose(args.project, args.port, args.subnet, action, public=args.public, gateway=args.gateway,
                 household=_household_mode(args.project) if args.action == 'start' else None)
        print('Owned operation complete: ' + args.action)


if __name__ == '__main__':
    main()
