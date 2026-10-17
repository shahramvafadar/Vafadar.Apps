#!/usr/bin/env bash
set -euo pipefail
# A fresh ephemeral runner owns this exact protocol fixture. Never expose it or supply tunnel credentials.
[[ "${CI:-}" == true && "${GITHUB_ACTIONS:-}" == true ]] || exit 2
cd "$(dirname "$0")/../.."
project=zanance-selfhost-tunnel-d313
[[ -z "$(docker ps -aq --filter "label=com.docker.compose.project=$project")" ]] || exit 2
[[ -z "$(docker volume ls -q --filter "name=${project}_")" ]] || exit 2
python3 -B -m unittest discover -s eng/hosting/tests -v
docker build -f eng/hosting/Proxy.Dockerfile -t zanance-proxy:d313 eng/hosting
docker build -t zanance-tunnel-qualification:d313 eng/hosting/TunnelQualification
export ZANANCE_SERVICE_SUBNET=172.29.90.0/24 ZANANCE_PROXY_ADDRESS=172.29.90.10 ZANANCE_CONNECTOR_ADDRESS=172.29.90.20
compose=(docker compose -p "$project" -f eng/hosting/compose.yml -f eng/hosting/compose.tunnel-qualification.yml)
"${compose[@]}" up --no-deps --exit-code-from provision provision
"${compose[@]}" up -d --no-deps api proxy
# This bounded readiness observation never restarts the process or regenerates its machine keys.
docker exec "${project}-proxy-1" sh -c 'for i in 1 2 3 4 5 6 7 8 9 10; do test ! -f /data/caddy/pki/authorities/local/root.crt || exit 0; sleep 1; done; exit 1'
mkdir -p artifacts/tunnel-ci
docker cp "${project}-proxy-1:/data/caddy/pki/authorities/local/root.crt" artifacts/tunnel-ci/origin-ca.crt
chmod 0644 artifacts/tunnel-ci/origin-ca.crt
for mode in trusted foreign; do
  address=172.29.90.20
  [[ "$mode" != foreign ]] || address=172.29.90.21
  docker run --rm --network "${project}_service" --ip "$address" --read-only --cap-drop ALL --security-opt no-new-privileges:true     --mount "type=bind,source=$PWD/artifacts/tunnel-ci/origin-ca.crt,target=/trust/origin-ca.crt,readonly"     zanance-tunnel-qualification:d313 client 172.29.90.10 "$mode"
done
cloud=cloudflare/cloudflared:2026.10.1@sha256:a15a3b50b8dabd57ac2090e9484c0247dba9dfa1f0df4d298d4317f9d69eb2d5
docker run --rm --network none --read-only --cap-drop ALL --security-opt no-new-privileges:true   --mount "type=bind,source=$PWD/eng/hosting/cloudflared.yml,target=/configuration/cloudflared.yml,readonly"   "$cloud" tunnel --config /configuration/cloudflared.yml ingress validate
"${compose[@]}" stop --timeout 45 api proxy
printf '%s\n' 'Original tunnel proxy protocol and gateway ownership checks passed; no live Cloudflare connection claimed.'
