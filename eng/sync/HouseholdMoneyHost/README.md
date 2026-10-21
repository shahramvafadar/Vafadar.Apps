# Independent native household monetary host

D-436 uses this private Integration host only when every retained Android household device has
legitimately been revoked by completed recovery/ownership/device-removal cases. Those revocations
must remain in force. A read-only fixture is not writable UI acceptance.

The host requires a fresh, project-owned PostgreSQL database named
`zanance_household_money_d436` on `127.0.0.1`, a private root beneath the current temporary directory,
and an exact `fixture.marker` containing `Fictitious independent household money D436`.
`ZANANCE_D436_TEST_DB` supplies only this independently generated fictional database connection;
never use an owner/provider credential, another project's configuration or a command-line password.
A unique public 32-character run id is the second argument. Build with
`ContinuousIntegrationBuild=true` and an alternate-volume artifacts path.

It applies the actual current service migrations and actual identity registration/confirmation on
its own loopback TLS endpoint. The fictional example.invalid identity and synthetic test input derive
from that run. Its own encrypted confirmation mail is consumed internally; no token, session,
connection string or private-key material enters diagnostics. `ready.json` contains public run,
port and certificate digest only. No global trust, public listener, DNS or external mail is enabled.
The admin database identity is confined to this disposable Integration database; this is not a
self-hosted service deployment or a runtime-privilege acceptance test.

The independent Android Home caller built with `ReviewFreshHouseholdMoney=true` owns only
`pro.vafadar.zanance.householdmoneyreview`. Its explicit INTERNET permission is limited to this
fictional TLS enrollment fixture and never enters the signed offline Main APK. It performs actual
native SecureStorage sign-in and signed owner admission through HouseholdWorkspace, then creates
exactly two financial contacts and one KWD1001 private cost with explicit333/668 allocations.
There are no cash accounts, ledger entries or saved repayments. The original admission marker and
actual source guard prevent reseeding. Presentation continuations inspect the same protected rows
and permissions and use the real unsaved repayment editor. They do not change roles or replay a
successful financial operation.

The host stops when its own private root receives `stop`, or after its one-hour bound. Retain original
private fixture keys and checkpoints while investigation/continuation is incomplete. Stop/remove only
this fixture's specifically named container/volume after its work is complete; never clean unrelated
Docker resources. These checks do not accept real providers, public hosting, commercial entitlement,
security audit, all Android devices, iOS, accessibility, stability or release.
