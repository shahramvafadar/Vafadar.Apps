"""Manage the retained local qualification stack as one Docker Desktop Compose group."""
import argparse
import json
import subprocess
import time

PROJECT = 'zanance-selfhost-privacy-restore-d294'
SERVICES = ('database', 'api', 'proxy', 'provision', 'migrate')


def run(*args):
    """Return bounded Docker metadata without exposing container environments or logs."""
    return subprocess.check_output(['docker', *args], text=True).strip()


def main():
    """Start existing containers only; never rebuild, reprovision, migrate or replace data."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=('status', 'start', 'stop'))
    args = parser.parse_args()
    containers = {}
    for service in SERVICES:
        name = f'{PROJECT}-{service}-1'
        value = json.loads(run('inspect', name, '--format',
                               '{"labels":{{json .Config.Labels}},"state":{{json .State.Status}}}'))
        labels = value['labels']
        if (labels.get('com.docker.compose.project') != PROJECT
                or labels.get('com.docker.compose.service') != service
                or labels.get('pro.vafadar.zanance.hosting') != 'd293'):
            raise RuntimeError('Retained container ownership differs; operation refused.')
        containers[service] = name
    if args.action == 'start':
        # Provision and migration have already succeeded. Docker Desktop may also start these
        # idempotent one-shots; this command deliberately resumes only the existing workers.
        run('start', containers['database'])
        wait_ready(containers['database'])
        run('start', containers['api'])
        wait_ready(containers['api'])
        run('start', containers['proxy'])
    elif args.action == 'stop':
        # Drain clients before stopping their original database; preserve every volume/image.
        run('stop', '--time', '45', containers['proxy'], containers['api'])
        run('stop', '--time', '45', containers['database'])
    for service, name in containers.items():
        print(service + ' ' + run('inspect', name, '--format',
                                  '{{.State.Status}} {{if .State.Health}}{{.State.Health.Status}}{{end}}'))


def wait_ready(name):
    """Honor existing health checks before resuming dependent workers, with a finite deadline."""
    deadline = time.monotonic() + 90
    while time.monotonic() < deadline:
        status = run('inspect', name, '--format',
                     '{{.State.Status}} {{if .State.Health}}{{.State.Health.Status}}{{end}}')
        if status == 'running healthy':
            return
        if not status.startswith('running') or status.endswith('unhealthy'):
            raise RuntimeError('Retained worker failed readiness; original container/data preserved.')
        time.sleep(1)
    raise RuntimeError('Readiness deadline exceeded; original container/data preserved.')


if __name__ == '__main__':
    main()
