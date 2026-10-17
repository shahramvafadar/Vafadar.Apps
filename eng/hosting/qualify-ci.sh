#!/usr/bin/env bash
set -euo pipefail
# Only an ephemeral runner with fresh exact fixture names may execute this complete qualification.
[[ "${CI:-}" == true && "${GITHUB_ACTIONS:-}" == true ]] || { echo 'Dedicated hosted runner required.' >&2; exit 2; }
cd "$(dirname "$0")/../.."
for project in zanance-selfhost zanance-selfhost-restore-files-d293; do
  [[ -z "$(docker ps -aq --filter "label=com.docker.compose.project=$project")" ]] || exit 2
done
[[ -z "$(docker volume ls -q --filter name=zanance-selfhost)" ]] || exit 2
mkdir -p artifacts
compose=(docker compose -f eng/hosting/compose.yml -f eng/hosting/compose.qualification.yml)
"${compose[@]}" build
docker volume create --label pro.vafadar.fixture=hosting-qualification zanance-selfhost-qualification-d293 >/dev/null
"${compose[@]}" up -d --wait database api proxy
docker cp zanance-selfhost-proxy-1:/data/caddy/pki/authorities/local/root.crt artifacts/hosting-proxy-root.crt
# docker cp retains mode0600 on Linux; the public trust anchor must be readable by the independent UID1654.
# Change only this public certificate, never the proxy's private authority or transport keys.
[[ -f artifacts/hosting-proxy-root.crt && ! -L artifacts/hosting-proxy-root.crt ]] || exit 2
chmod 0644 artifacts/hosting-proxy-root.crt
docker run --rm --network none --read-only --cap-drop ALL --cap-add CHOWN --user 0 \
  --mount type=volume,source=zanance-selfhost-qualification-d293,target=/qualification \
  --entrypoint sh zanance-bootstrap:d293 -c 'chown 1654:1654 /qualification'
docker run --rm --network none --read-only --cap-drop ALL --user 1654 \
  --mount type=volume,source=zanance-selfhost-qualification-d293,target=/qualification \
  --entrypoint sh zanance-bootstrap:d293 -c 'chmod 700 /qualification'
stage() { ZANANCE_QUALIFICATION_STAGE="$1" "${compose[@]}" run --no-deps qualification; }
stage initial
"${compose[@]}" up -d --force-recreate --no-deps --wait api proxy
stage cold
stage boundary
stage public-refused
ZANANCE_QUALIFICATION_TRUST_FILE=/proxy-trust/root.crt stage public
stage attachment
python3 eng/hosting/operate.py backup --snapshot zanance-hosting-snapshot-ci
python3 eng/hosting/operate.py restore --project zanance-selfhost-restore-files-d293 --port 14545 \
  --subnet 172.29.85.0/24 --snapshot zanance-hosting-snapshot-ci
export ZANANCE_PROXY_PORT=14545 ZANANCE_SERVICE_SUBNET=172.29.85.0/24 ZANANCE_PROXY_ADDRESS=172.29.85.10
compose=(docker compose -p zanance-selfhost-restore-files-d293 -f eng/hosting/compose.yml -f eng/hosting/compose.qualification.yml)
stage restored-files
python3 eng/hosting/operate.py renew-transport --project zanance-selfhost-restore-files-d293 --port 14545 \
  --subnet 172.29.85.0/24 --snapshot zanance-hosting-snapshot-ci-renewal
stage renewed
# Certificates, financial files, database dumps and key rings stay only on the disposable runner.
printf '%s\n' 'Original hosted clients, refusals, private restore and retained-key renewal passed.'
