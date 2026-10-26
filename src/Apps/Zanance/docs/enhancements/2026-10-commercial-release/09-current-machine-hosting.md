# Current-machine container hosting

Owner-approved on 2026-10-15, within D-292's current delivery plan. Host the project and its
dependencies on the existing machine's Docker Desktop and initially use vafadar.pro. A dedicated
app domain may follow later. This is deployment authority, not proof that public hosting exists.
No paid host, account creation, DNS credential access or unrelated-project changes are authorized.

## Observed starting point

Docker Compose v5.5.1 is available. Existing running containers belong to other projects and must
not be reused, reconfigured or stopped. Zanance's current backend script runs a Windows process
plus an isolated PostgreSQL fixture; that is not the requested container application deployment.
The fixture's trust authentication is fictional-test infrastructure and must never be copied into
the hosted stack. The service currently accepts only Development/Integration and loopback TLS;
clients currently accept the explicit local addresses. Those deliberate guards require an approved
deployment implementation, not an environment-name workaround or arbitrary forwarded-header trust.

Public DNS inspection found Cloudflare authoritative nameservers for vafadar.pro. The observed A
and AAAA queries supplied no address record for the root. This proves neither control-panel access,
an app hostname, reachable inbound ports nor valid public TLS. No DNS record was changed and no
Cloudflare/other-project credential or tunnel configuration was read. Proposed working service
hostname: api.zanance.vafadar.pro; preserve the root and any existing sites. Verify actual records
again at deployment. The exact subdomain is operational configuration within the approved domain.

## Complete deployment batch after the current personal-sync delivery

1. Create a uniquely named Compose project with dedicated networks, PostgreSQL/private-service
   volumes and runtime users. PostgreSQL has no public port. Pin official supported image versions
   and digests after current security/licence review; do not use another project's resources.
2. Build the original service in a reproducible multi-stage image, run without root and separate
   the one-shot schema migrator from least-privilege runtime. Select an explicit self-hosted mode
   with a configured public origin; do not make Integration or cleartext finance an exposure mode.
3. Persist protected key material and private files across container replacement. Linux must use
   maintained certificate-backed protection with private runtime secrets, not Integration's
   ephemeral provider or Windows-only DPAPI material copied into Linux. Verify restart and restore
   with the same original keys; refuse silent replacement when keys are missing. Never print,
   commit or pass owner credentials as command arguments. Any necessary owner account entry is
   performed personally when a concrete deployment is ready.
4. Configure a dedicated reverse proxy and public TLS with strict origin/host validation. Trust
   forwarded HTTPS only from the actual known proxy. Preserve a distinct local pinned mode and
   design the configured public-service certificate renewal path explicitly; do not silently
   disable certificate/name validation or make a new leaf certificate bypass reviewed authority.
5. Start and qualify the isolated stack on loopback before public routing: actual image/container
   ownership, health, database grants/migrations, storage persistence, identity/session boundaries,
   encrypted transfers and failure/restart behavior. Compose config parsing alone is not readiness.
6. Drain, back up and restore the actual database/private keys/files into a second isolated target.
   Verify authentication/financial preservation there and retain the source for rollback. Include
   logs without credentials, finite storage/backpressure and operator recovery instructions.
7. After those gates, configure/verify the approved hostname, TLS and reachable routing. Router,
   firewall or global system changes and account authentication remain explicit owner actions or
   separately authorized operations. Do not infer those changes from permission to create project
   containers. Public exposure, platform/store acceptance and commercial activation remain distinct.

No new containers, public service, external mail sender or public DNS entry have been created by
this decision record. The existing uncommitted personal-sync batch is completed first; this
dependency order avoids deploying an unfinished loopback prototype. A desktop host's power,
sleep, restart, backup and network availability must be measured; it is not an HA commitment.

## D-301 Linux qualification trust-anchor permissions

The independent hosted caller UID1654 must read the copied public Caddy root anchor. Linux docker cp retains
mode0600 under the runner owner, unlike the earlier Windows host bind behavior. The CI-only script validates
the regular non-link copied root.crt and sets only that public file to0644. It changes no private key, deployed
volume, TLS verifier, production endpoint or owner installation. A separate labeled Linux fixture proves the
same-UID denial/correction; hosted full clients/private restore/transport renewal remain the actual gate.

## D-556 Runtime-only servicing of a retained deployment

Canonical sources can be newer than a retained deployment. Use ServiceRuntime.Dockerfile with
verified immutable ORIGINAL_SERVICE_IMAGE/ORIGINAL_SERVICE_ID, pinned official base and a minimal
context with no private state. Compare every /app and /probe hash and actual runtime versions.
A new source build alone does not authorize its feature/schema rollout. Qualify encrypted
confirmation through compose.runtime-servicing.yml only on its exact fresh project, empty owned
environment and original production TLS client. Preserve successful-stage markers; a one-shot
exiting0 must not trigger another startup/registration. Do not use --wait for the completed init job.

Before retained adoption reproduce the original Compose hash from allowlisted public settings,
use the existing drained private snapshot operation, preserve gateway/household/image/mount
identity, then replace only API with --no-deps/--no-build and restart the same original proxy.
Retain exact rollback image/snapshot and explicit immutable runtime image for later startup;
never substitute default mutable tags. Verify actual runtime/health/TLS/other container identities.
No public promotion or real authentication is implied.
[Finite qualification and cleanup](../../quality/service-runtime14-exact-application.md).
