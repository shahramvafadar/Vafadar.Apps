# Zanance local identity service

Optional ASP.NET Core identity, durable device grants and private opaque objects. This is a local
foundation for ZCR-ID-01, not financial sync, sharing, billing, public OAuth or a hosted product.
Free/local Plus and the mobile app remain independent; the app does not connect to this service.

## Run the independent local service

On Windows with the already installed `postgres:18.4-alpine` Docker image:

```powershell
./eng/backend/Local-Identity.ps1 -Action Start
./eng/backend/Local-Identity.ps1 -Action Stop
```

The script never pulls an image. It owns `zanance-backend-local`, the separately named volume,
`artifacts/zanance-identity/local` and loopback ports 55474/5047. PostgreSQL trust authentication is
only for this fictional isolated local environment. The migration operator is separate from the
runtime role, which cannot create tables. Nothing uses another project's containers or credentials.
The web process is hidden and uses the build-restored self-contained security runtime from
Directory.Packages.props (10.0.13 at delivery), without changing global runtimes. Start waits for
its own PID/readiness and refuses a live recorded PID; Stop checks the binary and exact private-root command line
before requesting graceful shutdown. A failed drain refuses the snapshot.

Only Development and Integration are accepted. Production/hosted activation deliberately fails;
provider, secrets, sender, transport, agreements, backup destination and spending are still gates.
The local service binds 127.0.0.1, does not configure proxy headers and sends no external mail.

## API boundary

- POST `/identity/register`, `/identity/login`, `/identity/recover`, `/identity/complete`, `/identity/refresh`.
- GET `/sessions`; DELETE `/sessions/{id}`; POST `/sessions/revoke-all`.
- GET/POST `/objects/`; GET/DELETE `/objects/{id}`.
- GET `/health/live` and `/health/ready`, without identity or financial content.

Registration/recovery respond uniformly. Confirm/recovery tokens never appear in a URL, response
or log. They use Identity's 15-minute Data Protection provider and a transactional one-use journal.
Login requires confirmed email and Identity password checks/lockout. Password hashing is the
maintained Identity PBKDF2 hasher at 600,000 iterations; no custom credential cryptography.

Bearer/refresh tickets use the framework BearerToken handler, not JWT/OIDC or a new token format.
Access lasts 15 minutes; absolute device grants last 14 days. Each authorized request rechecks the
Identity security stamp, exact session, expiry and refresh generation. Concurrent refresh consumes
one generation atomically; refresh invalidates the previous generation's access token too.
Password recovery revokes every existing grant. A session id/local profile id does not prove ownership.

Durable PostgreSQL windows permit 30 identity requests and 200 service requests per IP/minute;
these are foundation safeguards, not commercial quotas or a hosted anti-abuse acceptance claim.
Counter rows store hashed partitions. Framework request/body/token logging is not enabled.

Objects are immutable server-created ids with per-request ownership. The actual upload is bounded
at 8 MiB; each identity is bounded at 64 MiB and 100 active objects. PostgreSQL advisory ownership
locks serialize capacity across workers. Reads verify original byte length/hash; corrupted files
return unavailable, never guessed data. Files use framework Data Protection, with persisted keys
wrapped by CurrentUser DPAPI on Windows. Root DACL permits only the current service user; privileged
administrators/current-user malicious code remain outside that boundary. These limits are local
foundation safety bounds, not the unresolved shared-space commercial storage policy OD-15.

The local mail port writes only encrypted fictional outbox envelopes in the private root, with
bounded durable leases. It never sends email. A future sender must implement recipient delivery,
idempotency and processor/credential acceptance before a hosted sign-up flow is enabled.
Deletion stops reads at its committed tombstone, then resumes physical removal idempotently.
Expired counters/sessions/mail are cleaned; uncommitted old object orphans are reclaimed after one day.
Keep the original key ring: missing installation keys refuse startup rather than authorizing replacement.

## Snapshot and isolated restore

```powershell
./eng/backend/Local-Identity.ps1 -Action Backup -Snapshot D:/_Projects/Vafadar.Apps/artifacts/zanance-identity-snapshots/example
./eng/backend/Local-Identity.ps1 -Action Provision -Instance recovered -DatabasePort 55479
./eng/backend/Local-Identity.ps1 -Action Restore -Instance recovered -Snapshot D:/_Projects/Vafadar.Apps/artifacts/zanance-identity-snapshots/example
./eng/backend/Local-Identity.ps1 -Action Start -Instance recovered -DatabasePort 55479 -ServicePort 5052
```

Backup drains the source and leaves it stopped, copies pg_dump plus private objects/key ring,
restricts the snapshot DACL and publishes completion with dump integrity only after all copies.
Restore refuses an existing private root or nonempty target schema. It restores schema/data without
source roles/grants; Start separately applies only destination runtime privileges. Preserve a failed
unpublished restore for diagnosis; do not overwrite existing data to hide failure. Snapshots contain
identity metadata: keep the destination private. They are not portable user financial backups.
DPAPI key files require the same Windows user/host; off-host recovery needs the separately approved
protected key/certificate and hosting backup policy. Do not delete old wrapping certificates or key
ring entries during rotation while they protect retained objects/snapshots.

## Build and checks

```powershell
dotnet build src/Apps/Zanance/Vafadar.Zanance.Api -c Release -p:ContinuousIntegrationBuild=true
dotnet test --project test/Apps/Zanance/Vafadar.Zanance.Api.Tests -c Release -p:ContinuousIntegrationBuild=true
```

Acceptance requires the independently owned `zanance-backend-d272` PostgreSQL fixture on 55473,
`zanance_identity_d272` and a `zanance_runtime` login role, or explicit equivalent test environment
variables. The suite never substitutes in-memory/SQLite for PostgreSQL. It is intentionally outside
Vafadar.Tests.slnf; `.github/workflows/backend-identity.yml` provisions a pinned isolated PostgreSQL
build dependency and runs it on a separately published security-patched runtime. The full solution includes server/test compilation. Linux CI uses a
fictional in-process certificate for protected key-ring round trips, not hosted certificate acceptance.

Regenerate schema and compiled model after server model changes using the design factory, which
opens no database. The repository EF tool is 10.0.12 and reports its version difference from this
server's 10.0.13 runtime; generation succeeded. Do not silently update the existing local app tool.

## Operations and incident handling

Stop before replacing the binary or taking a consistent snapshot. Check readiness after migration
and startup; do not route requests while schema/key/storage access fails. Preserve original private
bytes and snapshots after any recovery error. Revoke affected device grants, then investigate only
content-free health and deployment metadata. Operators must not read ledgers, mail tokens or request
bodies. Restore is to a distinct destination and rollback means selecting the retained prior service,
not destructive down-migration. No paid deployment, provider account or actual user data exists here.

Primary component references:
[Identity API](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0),
[Data Protection](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0),
[Npgsql 10](https://www.npgsql.org/efcore/release-notes/10.0.html).
