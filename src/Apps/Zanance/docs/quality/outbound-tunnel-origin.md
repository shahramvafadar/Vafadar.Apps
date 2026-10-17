# Outbound-only tunnel origin (D-313)

## Delivery and boundary

The owner selected Cloudflare Tunnel for this machine under api.zanance.vafadar.pro. The direct-443
composition remains separately available. compose.tunnel-origin.yml requires fresh production authority,
fixes the API origin and removes every published proxy port. Qualification installations cannot be promoted.

The separate connector joins only that origin's service network at its exact private .20 address and has
its own outbound network. The original Caddy/API .10 authority remains. No host port, host networking,
Docker socket, application/private-CA/key-ring volume or privileged capability is mounted. Official
cloudflared 2026.10.1 is pinned to manifest sha256:a15a3b50b8dabd57ac2090e9484c0247dba9dfa1f0df4d298d4317f9d69eb2d5,
runs as 65532 with a read-only root, disables self-update and binds readiness to its own loopback.
Bounded error-level logs never enable request/header debug logging. Apache-2.0 applies to cloudflared,
not Zanance source. This is a hosting dependency, without a mobile SDK or permission change.

The owner creates the dedicated tunnel and supplies its credential file personally outside the repository.
The operator checks bounded regular-file metadata only; it never parses, copies or prints the credential.
Missing bind sources are refused instead of becoming directories. Existing project/origin labels must
match before start/stop. The origin must already be healthy, production-classified and configured for this
gateway. Only Caddy's fixed public root certificate may be exported to a fresh artifacts path. The connector
receives that certificate alone, with no private authority or application key access.

## Actual transport behavior

Exact-hostname ingress maps to HTTPS proxy:8443 with explicit server/Host names and original public CA;
TLS verification is mandatory. Other hostnames terminate in the local 404 rule. Original Caddy rejects
foreign connector addresses, plaintext external protocol, missing/malformed/multiple client-IP metadata
and wrong authorities. Only its exact connector may assert CF-Connecting-IP. One parsed original address
replaces incoming X-Forwarded-For; original host/Cloudflare headers are removed before the API. The second
TLS hop validates the API CA/name. The API trusts forwarding only from its original .10 proxy.
Same-zone Workers or IP-rewriting transforms require a new boundary review.

Cloudflare terminates public TLS and can process visitor IP, routing metadata and identity request
plaintext. HTTPS is distinct from financial application-layer encryption. Existing encrypted sync/share
payload and key boundaries remain; optional disclosed analysis packets have their own consent rules.
No public traffic, DNS mutation or live connector occurred here.

## Operational continuity

Complete snapshots now retain Gateway and compare it before target creation. This path uses
--public --gateway tunnel; direct mode still requires port443. Start, stop and backup inspect the original
owned proxy command before mutation, refusing implicit local/public or direct/tunnel changes. Old manifests
retain their original direct default. Restore and retained-key renewal propagate the gateway overlay and
immutable service images. Connector credentials are separate and are never restored or repointed by a
database snapshot. Stop the connector before maintenance/cutover, retain the original snapshot and latest
privacy authority, then start a separately owned connector for the exact restored origin after verification.

## Focused evidence

Seven Python cases pass: overlay/address selection, invalid direct/local combinations, changed-original
gateway refusal, snapshot gateway mismatch before target creation, metadata-only credential checks and
missing-file no-creation and matching-command/fictional-API refusal. The final new case alone was run
after the additional API-origin guard; the six successful cases were retained. These are hosting checks, not a new application full-suite count.

A fresh independent zanance-selfhost-tunnel-d313 fixture uses the actual Caddyfile, original provisioned
software TLS identities and a bounded TLS echo upstream. Nine distinct real-socket cases qualify valid
IPv4/IPv6, spoofed forwarding replacement, missing/invalid/multiple client-IP refusal, plaintext protocol
refusal, wrong authority/TLS refusal, untrusted origin CA and foreign connector refusal. The positive case
also checks exact upstream Host, HTTPS and removal of original forwarding fields. Six sequential cases
passed before the caller incorrectly expected HTTP for a TLS-refused foreign name; only its two remaining
TLS cases and the first foreign-connector case then ran. The successful prefix was not repeated locally.
This fixture does not duplicate or qualify financial/identity business algorithms.

An incorrect client-IP placeholder was observed literally upstream and corrected to maintained {client_ip}.
Initial read-only validation lacked its owned writable CA directory and then its public upstream CA; those
fixture failures are retained. Formatter exit1 represented a formatting difference. A null network IPAM list
was handled before fixture creation. No private/source volume was reset after an observation failure. The new Linux CI script initially had
Windows line endings; only its failed shell syntax check was repeated after converting that new file to LF.

Official cloudflared validates the actual ingress and selects the owned hostname/404 fallback. Actual Compose
rendering confirms production authority, no published ports, non-root/read-only connector and missing-bind
refusal. Attaching the operator to the existing fictional deployment is refused before credential access.
Strict independent caller publish passes. Mobile/Windows builds, prior financial and successful hosted
business scenarios were not repeated; APK remains D-312. Receipts: artifacts/hosting-tunnel-d313-preflight.
Hosted CI adds this protocol stage after the existing operator/restore job; its result is separate.

## Remaining external gates

The live origin is not deployed/exposed. Owner-created Cloudflare account/tunnel credentials, DNS route,
external HTTPS acceptance and real confirmation/recovery mail remain outstanding. The existing private
outbox is not real mail delivery. Company/provider setup, legal processor/retention declarations, public
availability, actual phone-network traffic and release acceptance remain open. No paid resource, account,
sign-in or host setting was created.

## Primary sources reviewed 2026-10-17

- https://github.com/cloudflare/cloudflared/releases/tag/2026.10.1
- https://github.com/cloudflare/cloudflared/blob/2026.10.1/LICENSE
- https://developers.cloudflare.com/cloudflare-one/networks/connectors/cloudflare-tunnel/configure-tunnels/run-parameters/
- https://developers.cloudflare.com/fundamentals/reference/http-headers/
- https://caddyserver.com/docs/caddyfile/options#trusted-proxies
- https://caddyserver.com/docs/caddyfile/concepts#placeholders
