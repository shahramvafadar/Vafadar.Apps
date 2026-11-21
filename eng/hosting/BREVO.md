# Zanance transactional identity email

## Owner decision and preparation

On 16 November 2026 the owner selected Brevo and reported domain access ready.
This does not supply a verified sender, dedicated credential, processor agreement,
live recipient or public-service acceptance. Never reuse another project's key.

The owner personally performs account authentication and secret entry:

1. Authenticate the Zanance sending domain and verify its sender in Brevo. Use the
   exact supplied DNS records, preserving unrelated sites and existing mail records.
2. Review transactional availability, actual quota/cost and processor terms. No paid
   subscription or agreement has been accepted on the owner's behalf. Disable
   unnecessary tracking for identity codes.
3. Create a dedicated Zanance **API** key, not an SMTP key. Personally store it as
   UTF-8 text in a private file outside the repository/build context. A trailing line
   ending is accepted. Never put its contents in chat, Git, command arguments,
   environment variables, an image, snapshot manifest or log.
4. Supply only the verified sender address and absolute private-file path. Confirm
   a recipient and a single bounded real-send check separately before actual mail.

Nonsecret environment variables: `ZANANCE_BREVO_SENDER_EMAIL` and
`ZANANCE_BREVO_API_KEY_FILE`. The latter is a **path**, never a key value. Docker
mounts it read-only at `/run/secrets/zanance_brevo_api_key` for API UID1654 only.
The owner must make it readable by that worker and private on the host.

[The ready settings template](brevo.env.example) contains only these two inputs.
The current code does not require a Brevo template ID, additional sender SDK or
another source edit after the verified address and private key file are supplied.
UTF-8 with or without an editor BOM, and an optional trailing line ending, works.

## Fresh origin and maintenance

Follow the original fresh production-authority and Tunnel gates. Use the exact
source-reviewed immutable service image, then add `compose.brevo.yml` after
`compose.yml` and `compose.tunnel-origin.yml`. Never promote a fictional deployment.
Only API receives the secret and mail egress. Database remains private; the Tunnel
origin publishes no host ports. Compose parsing is not runtime acceptance.

For an already admitted public origin, retain its project/gateway/subnet/images
and add `--mail-provider brevo` to `operate.py`. Start/restore/transport renewal
require the same original sender. Snapshots retain only provider/sender metadata;
the external credential file is not archived. A historical snapshot with no mail
authority cannot gain it implicitly. Stop/status do not require the key mount.
Loss of a key therefore does not prevent draining an existing API.

Public worker startup refuses missing sender configuration or an absent,
redirected or oversized key file. Offline `--migrate` does not dispatch mail or
require this secret. Household qualification, billing, entitlements, analysis,
public routing and accounts remain behind their existing independent gates.

## Original queue and data handling

Only confirmation/recovery mail is sent. No contacts, campaigns, financial payload,
password, device credential, purchase, SMS, attachment or mobile SDK is added.
Each protected challenge captures its original provider/sender, language and format. Historical local
mail and envelopes belonging to another sender are refused. Existing PostgreSQL
leases coordinate workers; consumed/expired/accepted challenges are excluded.
No schema change or challenge regeneration is needed.

The fixed HTTPS endpoint is `https://api.brevo.com/v3/smtp/email`. Redirects and
cookies are disabled; platform TLS verification stays enabled. One batch version
has one recipient and escaped HTML/plain-text instructions with request ID/code,
without token URLs. New format-2 instructions support en/fa/de/es/fr/it, including
RTL Persian and isolated Latin request IDs/codes. The original app transport
captures its UI language before awaiting/serializing the request. Missing/foreign
language metadata uses English. Old format-1 envelopes retain their exact original
English body; a later UI change cannot relabel a pending attempt. Published
`IdentityMailV2` resources are immutable: later wording needs a new format/version.

If confirmation expires before mail can be sent, submitting registration again
with the same unconfirmed account's correct password may create one fresh code.
Concurrent/repeated submissions share a still-valid original confirmation. Wrong
passwords, already confirmed accounts and deleted original users never acquire a
replacement code or password change. Anonymous responses remain uniform. Recovery
continues through the original one-use password-reset validation; no automatic
login or device authentication is introduced.
Repeating recovery while a valid reset email is queued or in flight keeps the
original challenge and delivery key. A delivered or expired challenge allows
another explicit recovery request under the existing request limits.

The original UUID is the batch idempotency key. Its documented 30-minute lifetime
exceeds the original 15-minute challenge. Only 201 with a nonempty receipt or the
exact 400 `duplicate_parameter` reply acknowledges provider acceptance. Unknown
returns/errors retain the lease and retry the same original key. The entire
operation has a 20-second deadline, below the one-minute lease; responses are
bounded to 8KiB. Acceptance is not inbox placement. Provider bodies, codes, keys
and recipient addresses are not logged.

Brevo processes recipient/sender, instructions, one-use code, opaque challenge
UUID and ordinary transport metadata. Provider logging/retention requires the
actual account/DPA review; no retention period is invented. Keep the same account
for credential rotation. Another account/sender requires a reviewed queue migration;
there is no exactly-once claim outside the original account/idempotency window.

[Initial integration](../../src/Apps/Zanance/docs/quality/brevo-identity-delivery-d1226.md)
and [complete current flow](../../src/Apps/Zanance/docs/quality/complete-brevo-identity-flow-d1227.md)
distinguish fictional contracts from genuine delivery/public operation. Actual
credentials/sender, account/domain/processor acceptance and the separately scoped
real-send/public-hosting checks remain external inputs, not missing mail code.
The [six-language account-entry disclosure](../../src/Apps/Zanance/docs/quality/brevo-entry-disclosure-d1228.md)
names the hosted mail processor and transmitted data before the original form choices.

Primary references:

- [Transactional endpoint](https://developers.brevo.com/reference/send-transac-email)
- [Batch idempotency](https://developers.brevo.com/docs/heterogenous-versions-batch-emails)
- [Brevo DPA](https://help.brevo.com/hc/en-us/articles/15403782599570-Where-can-I-find-the-Data-Processing-Agreement-DPA)
