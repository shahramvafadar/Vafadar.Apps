# Isolated current-machine service deployment

D-293 delivers a locally qualified complete deployment batch following D-292. Compose parsing or image
construction alone is not runtime acceptance. The owner approved this machine's Docker
Desktop and vafadar.pro; no paid host, account login, router/firewall change or unrelated project is authorized.

## Actual deployment design

- Explicit `SelfHosted` environment plus `Service:Mode=isolated-self-hosted`; Development/Integration are not exposed.
- Dedicated project networks/volumes and a static, narrowly trusted proxy address. Only proxy loopback port 14543
  is published initially. No database/application port is published. Source and restore targets use distinct subnets.
- Official pinned .NET SDK 10.0.402, ASP.NET 10.0.13, PostgreSQL 18.6 and Caddy 2.11.7 images. Preserve original notices.
- Application and migrator UID/GID 1654, PostgreSQL 70, proxy 1655. Long-running containers drop all capabilities,
  have read-only roots and scoped writable volumes/tmpfs. The network-disabled one-shot provisioner owns only the
  fixed project secret/data roots; it never mounts the Docker socket or a host secret/configuration directory.
- PostgreSQL requires TLS client certificates whose CN equals the requested role. The runtime cannot create schema
  or write migration history. A separate database-owner client performs explicit migrations and finite DML grants.
  Local Unix administration requires the postgres OS identity. No TCP trust, owner password or account credential
  is created, supplied in arguments, logged or remembered. GSS encryption is explicitly disabled because this stack
  selects verified TLS/certificate authentication; this does not disable PostgreSQL TLS.
- The isolated provisioner generates standard RSA/X.509 internal machine identities through .NET maintained APIs.
  The CA/issued material remains in private Docker volumes. These are automatic software machine keys, not owner
  login/password/license inputs. An immutable public fingerprint manifest prevents silent replacement; missing,
  changed, partial or nearly expired material is refused. Never print/export private PEM contents for diagnosis.
- API TLS and Data Protection wrapping identities are distinct. The private key ring/private financial files persist
  independently of images. Caddy validates the internal upstream CA/name; API checks the actual TLS peer and exact
  original authority before accepting one forwarded client address. It never trusts forwarded protocol/host headers.
- The local proxy uses its internal CA without installing trust on the owner's OS. Public qualification copies only
  its public root certificate. Windows curl may require `--ssl-revoke-best-effort` for this offline CA's absent CRL
  distribution points; never use `--insecure`. The original application transport retains exact local leaf pin/name/
  validity/EKU checks. The approved public-origin client uses normal system trust without a leaf pin, allowing
  ordinary public certificate renewal. An untrusted issuer is refused before credentials.

## Current commands and boundaries

`docker compose -f eng/hosting/compose.yml build` builds isolated source images. The Dockerfile-specific context
allowlist excludes all owner secrets, Git metadata, artifacts, other apps and untracked archives before transfer.
`docker compose -f eng/hosting/compose.yml up -d` provisions/migrates/starts only this project. Inspect actual statuses,
health, labels, images, mounted-volume permissions, TLS peer identity, grants and bounded logs before acceptance.
Never use blanket Docker cleanup or remove volumes to retry. Keep failed fixtures and continue their actual state.

Qualification composes original clients/stores/coordinators against the actual proxy/API/PostgreSQL stack. It
requires an empty service or exactly its fictional identity, uses only independent SQLite/qualification volumes,
and refuses a second successful stage. Local relay changes only this caller's namespace, preserving actual TLS
bytes, original transport pin/name checks and all original financial/protocol algorithms. The private mounted
operator authority is consumed only by that software after its fictional-only guard, never printed by an operator.
The app's hosted default and local/public trust explanation are updated in all six languages and qualified
through original Windows/Android native pages. The current signed personal-sync APK is listed in the quality report.

## Safe operations and certificate maintenance

Use `python eng/hosting/operate.py status` for bounded public status. `stop` drains only API/proxy; `start` verifies
the exact original project labels. `backup --snapshot zanance-hosting-snapshot-NAME` leaves workers stopped and
keeps the complete private snapshot only inside Docker. Resume with `start` after reviewing the result.
`restore --snapshot NAME --project zanance-selfhost-RESTORE --port 14544 --subnet 172.29.84.0/24` requires a fresh
separate target and free checked subnet/port; source and snapshot remain intact. Snapshot manifests retain exact
runtime image IDs. Images must remain locally available or be rebuilt/qualified before an upgrade. Never prune
images/volumes as a retry. Docker snapshots on this disk are not an off-machine disaster backup.

`renew-transport --snapshot zanance-hosting-snapshot-PRE-RENEWAL` takes a mandatory complete snapshot, stops this
project's workers/database and renews only internal TLS leaf certificates with the same keys and CA. It retains
the historical protection certificate/key used by the key ring, restarts the stack and requires a readiness check.
Schedule this explicit operator maintenance before the 365-day leaves expire. It is not an unattended updater.
The public fingerprint intent permits completing an interrupted multi-file publication with `renew-transport` in
the provisioner; do not start workers with a pending intent. Partial initial preparation or `.pending` writes
refuse automatic overwrite: preserve the snapshot/volume, inspect metadata and restore into a separate target.
The internal CA lasts ten years; a separately reviewed trust-root migration is required before its final year.
Certificate renewal is not private-key compromise recovery: isolate a compromised deployment and revoke affected
client sessions before an explicitly planned key migration. Do not silently replace key-ring protection material.

For an already approved public deployment, operator commands require `--public --port 443` to preserve its overlay.
Public snapshots cannot silently restore into qualification mode or vice versa. Public Caddy ACME renewal uses
TLS-ALPN on443; external DNS and routing must remain valid. No host trust store change is required.

## Production activation boundary

Never expose the existing fictional projects or their known qualification credentials. Use a fresh separate
project such as `zanance-selfhost-live`, fresh volumes and checked free subnet. The public overlay classifies
its machine authority as production, requires empty private storage and prevents later reclassification. Existing
qualification authorities refuse production promotion. The API independently checks production/fictitious markers
before opening its public listener. Qualification callers whitelist only their three fictional project names.

The overlay prepares `api.zanance.vafadar.pro`, public TLS-ALPN issuance/renewal and external443. It is **not active**.
Before activation, the owner must configure DNS/443 routing to this machine and provide a real mail sender through
the approved identity flow. No account login, mail credential, host firewall/router change or paid host was created.
Do not expose an identity service that cannot deliver confirmation/recovery mail. Local runtime qualification,
private-file restore, retained-key transport renewal and negative authorization cases pass; hosted CI is separate.
See `src/Apps/Zanance/docs/quality/self-hosted-stack.md`. HA, phone/iOS and release acceptance remain open.

## Primary sources reviewed 2026-10-15

- PostgreSQL supported minor versions: https://www.postgresql.org/support/versioning/ (18.6 observed current).
- Certificate/CN authentication: https://www.postgresql.org/docs/18/auth-cert.html
- Framework certificate-wrapped persistent protection: https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-encryption-at-rest?view=aspnetcore-10.0
- Trusted proxy boundary: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0
- .NET official images: https://github.com/dotnet/dotnet-docker/blob/main/README.aspnet.md
- Caddy 2.11.7 maintained release: https://github.com/caddyserver/caddy/releases/tag/v2.11.7
- Caddy TLS and Apache-2.0 license: https://caddyserver.com/docs/caddyfile/directives/tls ; https://github.com/caddyserver/caddy/blob/v2.11.7/LICENSE
- Npgsql TLS/GSS distinction: https://www.npgsql.org/doc/security.html
