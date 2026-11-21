# Isolated current-machine service deployment

## Owner-selected Brevo identity sender (D1226)

Brevo is selected for transactional identity confirmation/recovery. See
[the dedicated preparation and operational boundary](BREVO.md). The original
protected outbox, sender-bound envelopes, optional API-only secret/egress overlay
and public-startup refusal are implemented. No real key, mail, DNS/account change
or public deployment has been performed; all earlier acceptance remains scoped.

D-293 delivers a locally qualified complete deployment batch following D-292. Compose parsing or image
construction alone is not runtime acceptance. The owner approved this machine's Docker
Desktop and vafadar.pro; no paid host, account login, router/firewall change or unrelated project is authorized.

## Actual deployment design

- Explicit `SelfHosted` environment plus `Service:Mode=isolated-self-hosted`; Development/Integration are not exposed.
- Dedicated project networks/volumes and a static, narrowly trusted proxy address. Only proxy loopback port 14543
  is published initially. No database/application port is published. Source and restore targets use distinct subnets.
- Current source-build inputs pin official .NET SDK 10.0.403 and ASP.NET 10.0.14 by immutable image digests
  (D-556), plus PostgreSQL 18.6 and Caddy 2.11.7. Preserve original notices. A source build does not authorize
  replacing the retained deployment with newer application code or migrations.
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

## Source builds and the retained deployment (D-556)

[Service.Dockerfile](Service.Dockerfile) builds current API/bootstrap sources; the three qualification
Dockerfiles use the same SDK/runtime pins. [ServiceRuntime.Dockerfile](ServiceRuntime.Dockerfile) instead
copies the existing immutable image's application and probe files onto ASP.NET 10.0.14. These are distinct
operations: the completed retained-stack servicing preserved the original application files and did not
apply newer source features or schema changes. Do not run a source rebuild or migration to reproduce it.

The [D-556 qualification record](../../src/Apps/Zanance/docs/quality/service-runtime14-exact-application.md)
records the completed runtime-only adoption and HTTPS health observation for
`zanance-selfhost-privacy-restore-d294`, using loopback port 14645. Port 14543 above is the base Compose
default, not that retained project's port. These are dated acceptance observations, not a fresh health
claim. Preserve its original immutable rollback image and private snapshot dependencies. Use the guarded
`desktop.py` lifecycle commands below for the retained group; do not recreate completed qualification
one-shots or replace its image/configuration with a default `compose up`.

Public DNS/TLS/mail, real provider/account acceptance and publication remain separate gates. Runtime14
servicing does not establish them or full private database/key equality.

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
Before copying and before publishing a completed snapshot, the operator checks
that the original clients are still drained. An independent live private-volume
mount or a changed deletion-marker inventory prevents publication.

The isolated Docker qualification of `2180409e` exercised this refusal
with the packaged `0a11729f` service. A synthetic pending request and its
matching marker were added after the actual database dump, before the
private archive was copied. The operator rejected the changed inventory
and left both archives without a completion receipt. The source API and
proxy remained stopped. The fixture and its incomplete snapshot were
retired. Evidence is in `artifacts/zanance-private-backup-change-2180409e`.
`restore --snapshot NAME --project zanance-selfhost-RESTORE --port 14544 --subnet 172.29.84.0/24` requires a fresh
separate target and free checked subnet/port; source and snapshot remain intact. Snapshot manifests retain exact
runtime image IDs. Images must remain locally available or be rebuilt/qualified before an upgrade. Never prune
images/volumes as a retry. Docker snapshots on this disk are not an off-machine disaster backup.

If a restore is interrupted, continue with the same snapshot and target.
The operator refuses `start`, `backup` and `renew-transport` for a target
whose restore receipt is incomplete. `status` and `stop` remain available.

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

The historical direct overlay prepares public TLS-ALPN and external443; it is **not active** and is not
required for the owner-selected Tunnel path below. The direct alternative would require DNS/443 routing.
Both paths require a real confirmation/recovery mail sender before exposing the identity service. No account login, mail credential, host firewall/router change or paid host was created.
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

## Permanent household deletions during historical restoration (D-294 in progress)

An old database/private-volume snapshot is not the latest privacy authority. Before restore, stop and drain the original snapshot source project's api/proxy and other private-storage clients. Preserve that project's latest private volume separately. The operator requires this exact original project, verifies its owned stopped containers/volume and refuses a missing/live source instead of treating it as an empty deletion ledger.

Before the restored service starts, a deletion-v1 image runs its offline --merge-household-deletions command with the latest original private volume read-only and the restored private volume writable. The merge validates and adds only immutable public-metadata deletion markers; it never copies keys, mail or financial objects and refuses conflicting existing originals. Restore checks both projects and their exact private volumes before merging and starting the target. A live one-off volume mount also blocks these steps. Keep all private-storage clients drained throughout the operation. Historical images without the immutable capability label cannot start when newer source deletion markers exist; an explicit separately qualified upgrade is required.

Restore compares the original deletion-marker inventories before merging and
again before starting the destination. It calculates per-type hashes of marker
filenames inside the read-only helper and returns only those hashes. If the
inventory changes, the destination stays stopped and the copied state remains
available for continuation. The service still validates marker contents
during the merge.

Account identity erasure publishes a separate opaque receipt marker before the
database commit. Restore transfers these exact markers from the stopped latest
source using an image with the matching `pro.vafadar.zanance.account-privacy`
capability. The v1 image supports its original receipt-only marker format. The
v2 image also supports markers bound to the original identity. An independent
`.analysis-retired` marker requires `receipt-fence-v3`, even when the account
has no completed identity erasure marker. These markers protect already erased
analysis content while the rest of account deletion is pending.
An acknowledgement marker with the `.request` suffix requires
`receipt-fence-v4`, which also supports the earlier marker formats. When the
original identity matches, reconciliation of this marker restores only the
pending request and its original timestamp.

### Packaged pending request restore verification (2026-11-21)

The Linux service image from source `e7d45753` advertises `receipt-fence-v4`.
Its immutable image ID is
`sha256:578f702eba7d6c717ad74a4c77ead87c9edea9e5b9535b2d7d7d9bce9f3f3c65`.
The offline merge ran as UID 1654 with a read-only root, no network, and the
original fictional marker volume mounted read-only. The merged marker matched
the source byte for byte. The packaged service refused a database without the
acknowledged pending request.

An Integration-mode fixture used a persistent synthetic protection certificate
to run the packaged reconciliation command against an isolated PostgreSQL
database. It restored the exact receipt, timestamp and original account binding.
The account remained present with zero completion records. Internal readiness
then returned HTTP 200. This verifies the packaged commands and startup fence
without starting a public or SelfHosted Compose deployment. All fixture
containers, volumes and the isolated network were retired.

The first build context could not enumerate an old artifact path. The image was
then built from the committed allowlisted source. The first fixture subnet
overlapped an existing Docker network and was refused before creating state.
An ephemeral Integration protection key also refused the separate reconciliation
process. The certificate-backed fixture reused the exact merged marker bytes.

### End-to-end operator restore with a later pending request (2026-11-21)

The operator in `4321ac9a` copied a private snapshot from a fresh fictional
Compose source into a second, loopback-only project. The source snapshot
contained one synthetic identity and no deletion request. With its API and
proxy stopped, the fixture then added an exact synthetic pending database row
and a format-1 `.request` marker to the current source. The marker follows the
service contract. This fixture did not exercise public request submission.

The original restore procedure used the immutable `e7d45753` service image
with `receipt-fence-v4`, merged the marker, ran the packaged reconciliation
through Compose, and started the target. The target retained the original
identity, opaque receipt and UTC request time, with zero completion rows.
The source and target marker hashes match. The API reported healthy and
the target proxy returned HTTPS 200 for readiness using its own public
fixture CA and hostname verification. The original API and proxy stayed
stopped throughout the restore.

An occupied subnet was rejected before creating resources. Docker's default
address pool was exhausted during source provisioning. The same empty
fixture continued using explicit, nonoverlapping database and edge subnets
in a fixture-only Compose overlay. The proxy has no Compose healthcheck.
The final read-only verification used its HTTPS readiness route after the
initial observer expected a health status and the Windows client required
revocation data that the isolated fixture CA does not publish.

The receipt and retirement evidence is in
`artifacts/zanance-operator-restore-chain-4321ac9a`. Both fixture projects,
their volumes and networks, and the synthetic snapshot and restore receipt
were retired. No live deploy, external mail, provider or ledger action ran.

### Linux migration preserves older pending requests (2026-11-21)

The Linux service image built from `0a11729f` has immutable ID
`sha256:e54a2d4969325e914352d23162f987d724ca473239f37973a9ffdebc164ed1eb`.
Its packaged `--migrate` ran with a read-only root and UID/GID 1654 on an
isolated Integration fixture. The database used the earlier packaged schema
and contained one committed synthetic deletion request without a marker.
The new image wrote the expected marker as UID/GID 1654 and preserved the
original identity, receipt and timestamp. The completion table stayed empty.

The first fixture setup was refused before database creation because its
restricted initializer tried to set the mode after transferring ownership.
The admitted fixture set the mode first. Its PostgreSQL process, network and
private volume were retired. Evidence is in
`artifacts/zanance-legacy-receipts-image-0a11729f`. This was an isolated
package migration with synthetic data. Production upgrade and activation
remain separate.

### Restore startup and operator requirements

An older image is refused before target creation. A capable application refuses
to start if a transferred completion marker lacks an active-service completion
or an acknowledgement marker lacks its exact pending request or completion.
This prevents serving a database that predates either event. When the isolated
target has the original matching pending requests, use the same restore with
`--reconcile-account-deletions` to confirm a reconciliation pass before startup.
It processes only matched receipts, removing restored private service copies via
the original retirement stages. Incomplete work leaves the API stopped and the
restore continuable. The v2 marker binds the receipt to a digest of the immutable
service ID without storing the ID itself. A backup preceding the request can
reconstruct that request only when exactly one existing identity matches and no
conflicting receipt exists. Unmatched or conflicting identities and legacy
markers without a pending request stay fenced.

### Standalone analysis marker restore qualification (2026-11-21)

The `db827ec4` service build, image
`sha256:5aeccc7aa4b73f3e620b25aa5ec3717d857fe431bbad0b7f4af1f481b5bcc310`,
passed a full local Compose restore with PostgreSQL and the original private
volume. The source `zanance-selfhost-analysis-marker-src` used one fictional
completed result with a confirmed provider receipt and a pending account
deletion. Its snapshot preceded local analysis erasure. After that erasure,
there was one standalone analysis marker and zero identity completion markers.

The operator merged the marker and reconciled
`zanance-hosting-snapshot-analysis-marker-review` before starting
`zanance-selfhost-analysis-marker-dst`. The marker's bytes matched in source
and target. Both protected content fields were empty in the restored database.
The original terminal state, charged units, confirmed receipt and pending
request remained. The restored internal TLS readiness probe passed and the
proxy was bound to loopback port 14782. The first source launch met exhausted
automatic Docker address pools. The review continued on explicitly scoped free
database and edge subnets using the same source project.

Both review stacks are stopped and the snapshot is retained. The qualification
covers a snapshot containing the original provider confirmation. Public
deployment, actual provider retention and off-machine disaster recovery remain
open.

The current API command passed Windows, idempotence, conflict/no-overwrite and invalid-directory cases. The freshly published image zanance-service:d294-household-privacy also ran the exact merge as UID1654, read-only root, no network and no capabilities. The subsequent independent full hosted compose/snapshot lifecycle now also passed, as recorded below; identity-erasure and whole D-294 acceptance remain open. Do not replace or discard the latest original private volume merely to restore an older backup successfully.
## D-294 full household hosting lifecycle qualification

The independent local source zanance-selfhost-privacy-d294 and target zanance-selfhost-privacy-restore-d294 completed actual reverse-proxy/TLS/PostgreSQL/private-volume restoration. The before-deletion snapshot was restored only after owner deletion, worker cleanup and a newer original private snapshot. The operator merged the latest private deletion authority before target startup. A live source was first refused without creating target volumes. The deleted room remained inaccessible/retired through its original identity receipt; the independent room's exact ciphertext still decrypted unchanged.

The nonsecret Service__Household__LocalQualification setting is now captured in the manifest and preserved by start/restore/transport maintenance. Missing old manifests default it off; public+household snapshots are refused and the service still requires an independently classified fictional localhost installation. This corrects lost local feature configuration, not public hosting or paid activation. Qualification code and overlay are HouseholdQualification and compose.household-qualification.yml. The original restore uses two exact fictional identities, independent encrypted client state and actual signed opaque service packets; it does not assert main financial graph/rendering acceptance. Separate main-identity/main-retain and android-identity/android-retain stages may prepare only their additional exact example.invalid identities in the restored fixture; they never replace either original room/client key or replay completed restore stages.

Completed one-time stages prepare/delete/restored must not be replayed. Public fixture CA certificate is artifacts/household-hosting-d294-root.crt, separate from the old D-293 qualification file. Qualification volume is zanance-household-hosting-qualification-d294. Preserve before/after snapshots zanance-hosting-snapshot-household-before-d294 and zanance-hosting-snapshot-household-after-d294. Source workers stay stopped; no old D-293, Darwin or Loyan resource changed. The original restore used immutable9152bbf4f36448b46bae6b7d08ed559a3057494547b5f1574a1c7ae98e3e3156. The restored target subsequently received privacy-checkpoint image bcde13398c51795efb8a41b91c6f5f87a5436f1f6f828e48823c704fe32bb910 after a complete pre-upgrade snapshot zanance-hosting-snapshot-main-lifecycle-pre-upgrade-d294; it is healthy. Main Windows native cancel/retained deletion/cold receipt and18 deleted-state layouts passed separately, with all local financial columns preserved. Android main acceptance remains open. This is not identity erasure, public mail/DNS, phone/iOS, whole household or release acceptance.

The subsequent independent main Android Debug privacy lifecycle now also passed: six native translated/themed confirmations, retained owner deletion, cold original operation/hash/time receipt, exact-room server retirement and local account/entry preservation. The control record remained intact. Its later audit/deadline display-only refinement used a new signed binary and a single lifecycle observation, with no deletion/receipt replay. These scoped results do not close broader main, identity-erasure, Release/AOT, phone/iOS or whole D-294 gates. Original source workers remain stopped; target workers and all original snapshots/keys are preserved.

The household retirement worker reports only an aggregate count of unavailable private entries. It never logs
room/identity ids, file paths, protected bytes or exception messages. Unknown/corrupt markers remain untouched;
unrelated room retirement continues under separate transactions. Offline restoration still refuses unknown
source entries, and unreadable/redirected roots do not become an empty deletion ledger (D-294 in progress).


## Local billing service migration (D-295)

The additive BillingPurchaseAuthority service migration adds seven PostgreSQL tables; no financial SQLite model
changes. Take the original complete drained private snapshot before applying it, preserve the original protection/
signing keys and latest household deletion markers, and run the existing migration operator before the runtime.
The current isolated local deployment includes this migration and bundled service notices; Billing:LocalQualification
remains false there. Genuine provider credentials/products, public callbacks and commercial activation were not
configured. Do not promote a fictitious qualification stack or silently expose local billing endpoints publicly.
See src/Apps/Zanance/docs/enhancements/2026-10-commercial-release/12-billing-delivery.md for immutable image/snapshot,
APK and exact local evidence. Unchanged whole historical-restore acceptance was not replayed.


## Approved outbound tunnel path (D-313)

The owner chose Cloudflare Tunnel instead of direct443 for this machine. The old direct overlay is optional,
not the current activation plan. No live connector/DNS is configured. Finish owner/company account setup,
real confirmation/recovery mail, processor declarations and original public acceptance before exposure.
Do not expose any existing fictional project or use another project's Cloudflare credential.

Use compose.yml plus compose.tunnel-origin.yml for a fresh separately named production origin. Build its
source images first; check a free private /24 subnet, then set ZANANCE_SERVICE_SUBNET, ZANANCE_PROXY_ADDRESS
(network +10) and ZANANCE_CONNECTOR_ADDRESS (network +20) consistently. This overlay removes all published
ports. Never combine it with compose.public.yml. Production markers and API origin checks refuse reuse of
qualification authorities; preserve refused volumes and investigate without resetting them.

The owner personally creates a locally managed tunnel scoped to api.zanance.vafadar.pro, configures its
DNS route and stores its JSON credential outside this repository. Never paste credential contents into a
command, chat, environment variable, log or tracked file. Do not use a temporary quick tunnel. Require
external HTTPS, no same-zone Worker/header rewriting, and no caching of identity/financial API responses.
Public TLS ends at Cloudflare: IP/routing/identity request processing needs the corresponding privacy review.

After the fresh origin is healthy, export only its original public root:

```
python -B eng/hosting/tunnel.py export-public-root --origin zanance-selfhost-live --root-file artifacts/tunnel-live/origin-ca.crt
```

The owner supplies nonsecret ZANANCE_TUNNEL_ID and runtime path ZANANCE_TUNNEL_CREDENTIAL_FILE personally.
Set ZANANCE_TUNNEL_ROOT_FILE to that exported public certificate, then:

```
python -B eng/hosting/tunnel.py start --origin zanance-selfhost-live
python -B eng/hosting/tunnel.py status --origin zanance-selfhost-live
python -B eng/hosting/tunnel.py stop --origin zanance-selfhost-live
```

The independent zanance-tunnel-live project joins only that origin's service network. The UID65532
read-only connector gets its credential file and public CA, never application/private CA volumes. Bind
sources must exist; no host port, Docker socket or credential-content diagnostic is allowed. The official
2026.10.1 image is digest-pinned. Health uses the connector's own loopback ready endpoint; it proves a
connector connection, not public hostname, mobile traffic, mail or financial acceptance.

For this origin, all original operator commands require --public --gateway tunnel and its original
--subnet. Stop the separate connector before backup/renewal/cutover. Complete snapshots retain Gateway
alongside immutable service image identities, private data and latest deletion authority. Direct/tunnel
or local/public mismatches are refused before destructive work; old direct manifests keep their original
meaning. Snapshot restore does not restore owner connector credentials, reconnect the old gateway or
change DNS. Retain original authorities; verify the restored origin and original public CA, then start a
separate matching connector project only after stopping the old connector. Never run two active origins
for the same tunnel during a cutover. Credential loss/revocation is an owner Cloudflare operation, separate
from database or application key recovery. Do not prune the previous state as a retry.

Focused protocol/operator evidence and primary sources:
src/Apps/Zanance/docs/quality/outbound-tunnel-origin.md. CI's qualify-tunnel.sh uses an independent no-port
TLS echo fixture, not a Cloudflare account or production financial data. It does not replay the mobile APK.

## Optional approved daily ECB source (D-389)

Add compose.ecb-reference.yml explicitly to select Market:Source=ecb and API-only outgoing edge
connectivity. No new listening port is published; the base configuration remains unconfigured and
network-isolated. This overlay is statically validated, not an assertion of a rebuilt/running
container or a publicly accepted deployment. Existing fresh-storage, DNS/TLS/mail and company/store
gates remain. See ../../src/Apps/Zanance/docs/quality/ecb-reference-integration.md.

## D-502 Docker Desktop lifecycle consolidation

The owner requested cleanup of unnecessary resources and one manageable group on 2026-10-23.
The retained group is `zanance-selfhost-privacy-restore-d294`, containing database, api, proxy and
the two successfully completed provision/migrate one-shots. Its historical project name is retained
because the original qualification identities/volume ownership and restore authorities depend on it.
No production/public service was activated. Docker Desktop can start/stop the group as one unit.

For an ordered resume of the original long-running workers only:

```powershell
python eng/hosting/desktop.py status
python eng/hosting/desktop.py stop
python eng/hosting/desktop.py start
```

These commands validate exact project/service/hosting labels, never build or recreate containers,
stop clients before the database, and wait for database/API health before dependent startup.
They intentionally do not replay the completed provision/migrate commands. Existing `operate.py`
remains the separately guarded snapshot/renewal/restore operator; do not run a default `compose up`
against an old project to recreate retired fixtures or replace the retained image/configuration.

58 completed containers, 9 unnecessary image IDs and 16 unused networks were removed. All 192
engine volumes were preserved, including nine complete private snapshots. Their 16 distinct
helper/runtime dependencies were protected from deletion; nine are locally available and seven
were already absent. None of the removed image IDs intersects this dependency set. Snapshot manifests were
read only inside isolated no-network helpers;
private archive/key payloads were not exported. Archived fixture containers no longer exist:
restoring an old snapshot still requires reconstructing its exact original stopped source ownership
from retained configuration/volumes before the existing privacy-source guard can pass. This cleanup
is not another restore acceptance and does not waive that guard.

Future qualification callers should use `--rm` when no continuation is required. Keep failures and
unfinished fixtures only while needed; after a terminal result retain bounded evidence and remove
the disposable runtime resources. Review ownership, mounts, retained recovery dependencies and
other users of an image before deletion. Never prune the shared engine's global cache/images/volumes.
The 323GB shared build cache was not attributed exclusively to this repository and remains untouched.
See `src/Apps/Zanance/docs/quality/docker-lifecycle.md` for the actual stop/start verification.
# Physical Docker disk maintenance

Resource deletion and physical VHDX compaction are separate operations. For the owner-approved
shared outage, run `eng/scripts/Compact-DockerDisk.ps1` manually in elevated Windows PowerShell
with the configured `-DiskPath` and `-ApprovedSharedOutage`. Non-admin execution refuses before
stopping services. Preserve its private receipt and verify application health after restoration;
matching container states alone is not readiness. See the D-505 follow-up in
`src/Apps/Zanance/docs/quality/docker-lifecycle.md`. Never delete the shared VHDX or globally prune
other projects to recover space.

### Observed disk state after the D-505 handoff

On24October09:38UTC the configured shared VHDX measures45.84GB instead of643.44GB and build
cache accounting is0B. No operator-script receipt is present; the external execution/actor is
unverified. All31 container IDs/original20-running set,190 volumes and nine locally available
protected image dependencies are preserved; the retained database/API report healthy. Do not
repeat disk maintenance to reproduce this completed external state change. Historical restore
and full application acceptance remain separate; see the dated docker-lifecycle follow-up.
