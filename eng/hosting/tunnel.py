"""Operate the approved outbound connector without reading or printing owner credential bytes."""
import argparse
import hashlib
import ipaddress
import json
import os
import re
import subprocess
import uuid
from pathlib import Path
import sys
# Operator imports must not leave ignored source-cache files in the checkout.
sys.dont_write_bytecode = True
import operate

BASE = Path(__file__).resolve().parent
REPOSITORY = BASE.parent.parent


def _configuration(origin):
    """Bind one connector to a validated production proxy and its exact private network."""
    if not operate.PROJECT.fullmatch(origin): raise RuntimeError('Explicit owned origin required.')
    if operate._gateway_mode(origin) != 'tunnel': raise RuntimeError('Origin is not the qualified tunnel gateway.')
    api, api_state = operate._container(origin, 'api')
    if api_state['state'] != 'running' or operate._run(['docker', 'inspect', api, '--format', '{{.State.Health.Status}}']).strip() != b'healthy':
        raise RuntimeError('Original production API must already be healthy.')
    value = operate._run(['docker', 'inspect', api, '--format',
        '{{range .Config.Env}}{{if eq (index (split . "=") 0) "Service__PublicOrigin"}}{{index (split . "=") 1}}{{end}}{{end}}']).strip()
    if value != b'https://api.zanance.vafadar.pro': raise RuntimeError('A fictional origin cannot be exposed.')
    network_name = origin + '_service'
    network = operate._metadata('network', network_name, '{"labels":{{json .Labels}},"ipam":{{json .IPAM.Config}}}')
    if network['labels'].get('com.docker.compose.project') != origin: raise RuntimeError('Origin network ownership differs.')
    subnet = ipaddress.ip_network(network['ipam'][0]['Subnet'], strict=True)
    if subnet.version != 4 or subnet.prefixlen != 24 or not subnet.is_private: raise RuntimeError('Original private network required.')
    address = str(subnet.network_address + 20)
    proxy, _ = operate._container(origin, 'proxy')
    configured = operate._run(['docker', 'inspect', proxy, '--format',
        '{{range .Config.Env}}{{if eq (index (split . "=") 0) "ZANANCE_CONNECTOR_ADDRESS"}}{{index (split . "=") 1}}{{end}}{{end}}']).decode().strip()
    if configured != address: raise RuntimeError('Original connector address differs.')
    return address


def _file(name, *, credential=False):
    """Inspect file metadata only for credentials; bind mounts must not create missing host paths."""
    if not name: raise RuntimeError('Owner-maintained credential file and original public CA file are required.')
    path = Path(name).absolute()
    if any(p.is_symlink() or (hasattr(p, 'is_junction') and p.is_junction()) for p in (path, *path.parents)):
        raise RuntimeError('Indirect owner file paths are refused.')
    if not path.is_file() or not 0 < path.stat().st_size <= 65536: raise RuntimeError('Original bounded regular file required.')
    if credential and path.is_relative_to(REPOSITORY): raise RuntimeError('Owner credentials must remain outside this repository.')
    return str(path)


def _owned_connector(project, origin):
    """Never stop another connector, project or origin."""
    name = project + '-connector-1'
    labels = operate._metadata('container', name, '{{json .Config.Labels}}')
    if labels.get('com.docker.compose.project') != project or labels.get('pro.vafadar.zanance.tunnel') != 'd313' or labels.get('pro.vafadar.zanance.origin') != origin:
        raise RuntimeError('Connector ownership differs; no mutation allowed.')
    return name


def main():
    """Require explicit existing ownership and owner-supplied runtime paths; never create accounts or DNS."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=('start', 'stop', 'status', 'export-public-root'))
    parser.add_argument('--origin', required=True)
    parser.add_argument('--project', default='zanance-tunnel-live')
    parser.add_argument('--root-file')
    args = parser.parse_args()
    if not re.fullmatch(r'zanance-tunnel-[a-z0-9][a-z0-9-]{0,35}', args.project): raise RuntimeError('Owned connector project required.')
    if not operate.PROJECT.fullmatch(args.origin): raise RuntimeError('Owned origin required.')
    if args.action in ('stop', 'status'):
        name = _owned_connector(args.project, args.origin)
        if args.action == 'stop': operate._run(['docker', 'stop', '--time', '45', name]); print('Owned connector stopped.')
        else:
            print(operate._run(['docker', 'inspect', name, '--format', '{{.State.Status}} {{if .State.Health}}{{.State.Health.Status}}{{end}}']).decode().strip())
        return
    address = _configuration(args.origin)
    if args.action == 'export-public-root':
        # Only Caddy's fixed public certificate is copied. Never enumerate its CA/private-key directory.
        destination = Path(args.root_file or '').absolute()
        if not args.root_file or not destination.is_relative_to(REPOSITORY / 'artifacts') or destination.exists():
            raise RuntimeError('A fresh repository-owned artifacts public-root path is required.')
        if any(p.is_symlink() or (hasattr(p, 'is_junction') and p.is_junction()) for p in (destination, *destination.parents)):
            raise RuntimeError('Indirect certificate destination refused.')
        destination.parent.mkdir(parents=True, exist_ok=True)
        proxy, _ = operate._container(args.origin, 'proxy')
        operate._run(['docker', 'cp', proxy + ':/data/caddy/pki/authorities/local/root.crt', str(destination)])
        if os.name != 'nt': destination.chmod(0o644)
        print('Original public certificate exported; SHA-256 ' + hashlib.sha256(destination.read_bytes()).hexdigest())
        return
    tunnel_id = os.environ.get('ZANANCE_TUNNEL_ID', '')
    try: identifier = uuid.UUID(tunnel_id)
    except ValueError: raise RuntimeError('Owner-created canonical tunnel identifier required.') from None
    if identifier.int == 0 or str(identifier) != tunnel_id: raise RuntimeError('Canonical tunnel identifier required.')
    credential = _file(os.environ.get('ZANANCE_TUNNEL_CREDENTIAL_FILE'), credential=True)
    root = _file(args.root_file or os.environ.get('ZANANCE_TUNNEL_ROOT_FILE'))
    existing = operate._run(['docker', 'ps', '-aq', '--filter', 'label=com.docker.compose.project=' + args.project]).strip()
    if existing: _owned_connector(args.project, args.origin)
    env = os.environ.copy()
    env.update(ZANANCE_CONNECTOR_ADDRESS=address, ZANANCE_TUNNEL_ORIGIN_PROJECT=args.origin,
               ZANANCE_TUNNEL_CREDENTIAL_FILE=credential, ZANANCE_TUNNEL_ROOT_FILE=root)
    operate._run(['docker', 'compose', '-p', args.project, '-f', str(BASE / 'compose.tunnel.yml'), 'up', '-d', '--wait', '--wait-timeout', '60'], env=env)
    _owned_connector(args.project, args.origin)
    print('Owned connector started; inspect Cloudflare connection/readiness separately before claiming public acceptance.')


if __name__ == '__main__':
    try: main()
    except (RuntimeError, OSError, ValueError):
        # No raw subprocess error, arguments, owner paths, credential JSON or configuration dump reaches the log.
        raise SystemExit('Connector operation refused or failed; preserve original state and inspect nonsecret prerequisites.') from None
