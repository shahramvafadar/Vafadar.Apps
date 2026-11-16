# Original Brevo identity delivery - D1226

## Decision and actual missing implementation

The owner selected Brevo on 16 November 2026 after reporting only domain access
ready. Current `PrivateMaintenance` previously returned when `LocalMailbox` was
false: its only delivery wrote protected local files. A provider account alone
could not supply real identity email. This batch implements that missing path;
no parent status or whole-project completion is inferred.

[Owner preparation and processor boundary](../../../../../eng/hosting/BREVO.md)
records the dedicated sender/key setup still required. No owner secret was read,
copied, entered or requested in chat. No genuine provider request or message ran.

## Complete local sender path

Original register/recover routes capture nonsecret provider/sender authority in
the existing encrypted envelope. Historical local envelopes or a changed sender
cannot be externally drained. The existing PostgreSQL claim/expiry/consumption
policy remains; each row retains its original UUID/recipient/code across retries.
The worker records provider acceptance only after a defined receipt. Unknown
returns leave the original bounded lease; no password/login/financial effect is
replayed. There is no model/migration/compiled-model change.

The original worker uses a fixed HTTPS API, no cookies/redirects/HTTP logging,
one recipient/batch version, escaped body, original batch idempotency identity,
20-second total deadline and 8KiB response bound. The documented duplicate reply
acknowledges the same original request; other status/body combinations do not.
Provider acceptance does not assert recipient delivery or an exactly-once guarantee
outside the original account/idempotency window. Runtime key text is bounded and
loaded from a separately mounted private file, never from operator arguments.

Public worker composition refuses an absent sender or missing/redirected/oversize
key file; offline migrations remain non-sending. The optional Compose overlay gives
only API a read-only secret and egress. Operator start/restore/renewal preserves
nonsecret sender identity in snapshots; a historical absent authority or replacement
sender refuses. Stop remains possible without the key mount. No retained stack,
image, gateway, snapshot, external credential or other project's resource changed.

The source service context now includes the exact existing
`ConfigurationMetrics.Android.targets` imported by the root targets. The current
Linux source build uses a separate 344-file allowlist, without secrets/artifacts,
Dastan or the unrelated protected benchmark input. The incoming three commits were
all Dastan paths and were fast-forwarded before work.

## Actual validation and explicit limits

- Strict Windows-hosted .NET10.0.15 API/test builds: zero warnings/errors. Linux
  source service build uses the pinned SDK10.0.404/runtime10.0.15 and passes.
- 26 distinct new managed conditions pass, zero skipped/errors/failures: 23 initial
  sender/config/envelope/PostgreSQL conditions, one later original HTTP recovery
  route and two final public-file composition conditions. This is versioned evidence,
  not 26 cases all rerun on the last test binary or a full-suite pass.
- Original concurrent workers share one fresh fictional PostgreSQL database. A held
  unknown sender return, another worker, lease expiry and a new worker reuse the
  exact first batch; the second challenge alone advances independently. Expired and
  consumed inputs are not submitted. No login/session/object/financial setup runs.
- One original in-process HTTP recovery route captures the selected sender in the
  protected envelope. The recipient/user are fictional; delivery/background worker
  is deliberately disabled there. This is not a live-socket TLS or authentication test.
- Five new operator contracts and seven directly affected gateway contracts pass.
  They are mocked metadata/operation boundaries, not a real production restore.
  A separate never-started, network-disabled Docker metadata fixture proves the
  actual sender reader preserves `=` inside an address; only the changed metadata
  contract is rechecked after that correction. The fresh fixture is removed.
- The combined Tunnel/mail Compose parses with a fictional file, no published
  ports, API-only secret and nonroot/read-only worker. No Compose service starts.
- Two actual Linux service-process refusals pass in the newly built immutable
  image: absent sender and missing mounted key. Both use network none, read-only
  root, no capabilities/core dumps, new fictional certificate and no database/listener.
  The certificate is retired. This is not real Brevo or production TLS acceptance.

Exact logs, source hashes, used Windows assemblies, image identity and receipts are
retained under `artifacts/d1226-brevo-identity-delivery`. The service image is retained
for the reviewed next step. Fresh PostgreSQL retirement and standard scoped Clean
are recorded separately; no supplementary or frozen cleanup is authorized here.

No Android/iOS/Windows MAUI code, app permission, local financial source, Main/phone,
previous timing/authentication/financial success or consumed one-shot test changes.
Unchanged full mobile builds and old successful suites are not replayed. The owner
has deferred further speed/quality work; it is not substituted for delivery.

Remaining actual work is owner sender/domain authentication, dedicated runtime key,
account/processor/quota review, explicitly scoped real mail and public operation.
Household production entitlement, OAuth, genuine Store/provider, iOS, original fault,
broader performance/accessibility and release remain separate. Existing canonical
parent State values are unchanged; this is finite engineering delivery.
