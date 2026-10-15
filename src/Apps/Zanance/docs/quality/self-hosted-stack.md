# Current-machine isolated service qualification (D-293)

The complete local deployment follows D-292 (2a888c9c0b891c3a6210b07e1f344ced9e03dac4).
Public DNS/routing, external mail, phone/iOS, billing and publication remain separate unaccepted gates.
Personal sync is server-readable; this is not household E2EE.

## Actual runtime

Official pinned SDK10.0.402, ASP.NET10.0.13, PostgreSQL18.6 and Caddy2.11.7 build original source in
allowlisted contexts excluding owner secrets, Git, artifacts, other apps and untracked archives.
Project zanance-selfhost uses checked private subnet172.29.83/24 and loopback14543 only.
Other projects/resources were neither changed nor reused. Actual database/API health, loopback TLS,
persistent key ring/private files and certificate-authenticated pg_stat_ssl were observed.
Runtime cannot create schema or write migration history; neither role has superuser/createdb/
createrole/bypassrls. No database password is used. Non-root workers have read-only roots and no
capabilities. Network-disabled bootstrap owns fixed volumes, never a Docker socket or host secrets.

Initial proxy operation-not-permitted came from its privileged-port binary file capability.
The derived image removes it while preserving port8443 and all runtime capability drops.
Negative log: hosting-proxy-initial-failure.log. Explicit GSS Disable removes unselected Kerberos
negotiation, preserving VerifyFull TLS. Private-CA curl revocation-best-effort was per-call only;
no insecure bypass or host trust installation occurred.

## Original clients and private recovery

Initial, cold-process, public-system-trust refusal/positive and isolated restore stages pass.
Public positive trust was supplied only inside the caller container; owner DNS/trust was unchanged.
Public client uses system chain/name validation without callback or leaf pin; untrusted CA fails
before credentials. Local transport keeps its pin/name/validity/server-EKU checks.

The first boundary caller changed SNI with HTTP Host; its name refusal is retained. Corrected raw
TLS validates SNI api before forging HTTP headers. Actual forged proxy headers, missing certificate,
wrong-role certificate, runtime DDL and migration-history writes are refused (hosting-boundary-fixed-
qualification.log). Only that failed stage was rerun.

The operator drains only owned workers, checks database sessions and streams binary pg_dump and
numeric-owner private archives through OS pipes to root-only Docker snapshots, never host private
files. Hashes, image IDs and immutable configuration form the restore authority. Receipts prevent
successful-operation replay; nonempty unknown targets refuse overwrite. Prototype snapshot A needs
its original configuration and is refused by current restore.

Snapshot B restored identity/account/entry to zanance-selfhost-restore-d293 (84/24,14544).
The original client then published a real bounded PDF. Snapshot C restored PostgreSQL/private files
to zanance-selfhost-restore-files-d293 (85/24,14545). Original clients verified account12345 KWD minor
units, expense1400 and exact PDF length/SHA256. Source, snapshots and keys were preserved.
Evidence: hosting-restored-private-file-qualification.log.

Transport renewal takes a full snapshot, stops workers/database, renews TLS certificates with the
same keys/CA and retains the historical key-ring protection identity. Actual authentication,
financial values and PDF hashes pass afterward (hosting-renewed-qualification.log). Durable public
fingerprint intent permits continuing certificate publication. Partial preparation or pending-file
writes fail closed for inspection; this is not power-loss acceptance. Missing key was refused in a
temporary memory copy without deleting the original. Completed restore was recognized without
replay. Public promotion of a fictional authority was refused before mutation.

## Focused managed/native checks

Twelve new Data trust cases pass: 3690 D-292 reconciled main executions plus12 =3702 covered main
behaviors, not a new full-suite run. Separate API: ten invalid configurations, two invalid ports,
three fictional/unclassified public-origin refusals pass. Successful old cases were not repeated.
Windows original page/default/help:36 contexts en/fa/de light/dark360/412/1280,18 help dialogs,
29172 glyph observations, original Back and unchanged complete finances. Strict build0warnings/errors.
Android independent Release/AOT original page:12 local/hosted contexts en/fa/de light/dark, six
complete native help texts and actual OK invocations, secure flag retained, no service opt-in and
unchanged complete portable database snapshot. Main installation was untouched.

Initial Android caller selected the last registered settings backup instead of the named database
source. Its pre-UI InvalidDataException is retained; selection was corrected. Observation continued
the same corrected PID after adapting to dialog-title mCurrentFocus and actual button2. No data reset
or successful Windows rerun followed. Generic launch failed; explicit actual component succeeded.
Main-identity strict Windows build passes; final signed APK details are recorded at delivery.

## Activation boundaries

Qualification/production have immutable separate authority/private markers. Public API refuses
fictional, missing or mixed markers. Production requires fresh empty private storage. Known fictional
credentials must never be exposed. Caller whitelist excludes live projects. Public overlay prepares
api.zanance.vafadar.pro and public ACME TLS-ALPN renewal, with validated upstream TLS; it was not exposed.
DNS/443 routing and real mail sender remain external gates. No owner account/login, paid resource,
host firewall/trust change or unrelated resource modification occurred.

The hosted workflow runs original clients, refusal, restore and renewal on a disposable runner and
uploads no private volumes/keys/dumps/payloads. Hosted success awaits the actual run.
## Final current signed artifact

artifacts/android-selfhost-d293/pro.vafadar.zanance-Signed.apk: 89,774,576 bytes; SHA256
ed5337b45ac976639bf421902f4d4872bfdd0ce2ac71f8310d2c497478e98d68.
Package pro.vafadar.zanance, minimumAPI24, arm64-v8a/x86_64, INTERNET for the explicit personal-sync edition.
APK v2/v3 signatures verified; it is complete Release/AOT, not Fast Deployment. Current strict Windows build
passes0warnings/errors in18.62seconds. Phone installation/acceptance and production signing remain separate.
