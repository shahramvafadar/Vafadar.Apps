# Cleared identity form inputs — D-1089

[Portable-backup input continuation D1090](backup-nullable-bindings-d1090.md)
corrects shared local/cloud null-length failure before authentication or portable
creation, preserving original protection and writer guards.

After [retained budget inputs](budget-nullable-bindings-d1088.md), four unsafe
identity-input dereferences remain in the original BillingIdentityViewModel.
Email, challenge and token are trimmed even when hidden in the current mode;
visible password validation also assumes a nonnull string. This is a client input
failure, not evidence of provider authentication or a diagnosis of the separate
native runtime fault.

## All original modes and validation boundaries

Twenty-five new selected host cases cover sign-in, registration, recovery,
confirmation and reset. Each mode receives four independently null inputs and an
all-empty control. The authoritative baseline has18NullReferenceException
failure conditions and7passing controls; these are conditions across four unsafe
captures, not18distinct implementation defects. All25pass on one final binary.
Visible-field errors remain exactly those required by the original mode, hidden
fields do not gain required errors, raw null/empty values remain unchanged, the
form stays ready and there is no pending operation or accepted status.

Only local validation/request variables treat null as empty. Original properties
are not normalized. Input revisions, disclosure/consent, root/page ownership,
password limits, email/challenge/token rules, pre-request secret clearing and
request/receipt/session lifecycle are unchanged. Each case has one supplied
read-only session observation and zero primary identity calls, native consent,
account/financial writes or genuine credentials. Full discovered-table contents
are equal before/after. Retirement still clears password/token and command
authority.

Three directly affected existing registration-consent refusal cases run on the
same final binary: changed literal input and both old-root retirement variants.
All pass with primary registration0. Their inputs are pre-existing fictitious test
values; there is no real sign-in or provider request. This is25new+3affected
selected cases, not28new tests or a complete suite. The initial test-property
identifier compiler refusal is retained; only its local iterator name changes,
and no tests ran in that refused build.

## Original Android controls

A fresh API36/x64/4096 independent package runs UID10216/PID4775. The original
BillingIdentityPage is hosted in a standalone NavigationPage. Its identity
environment is explicitly supplied as available with one false session-presence
observation; every primary method refuses/counts calls. This is not a genuine
configured identity backend, protected session or production route.

The25original validation conditions pass in that same process:20null bindings,
5empty controls,10visible-field null conditions with actual Android EditText
handlers, primary0and session reads1. Hidden-field cases use the original control
bindings without claiming visible interaction. Required errors and every raw
input are checked in process. Complete schema and typed contents of all
discovered tables remain exact; Accounts/Entries/Schedules/Budgets are empty.
No account, purchase, real authentication, financial action or secret entry is
performed, and no primary effect is replayed.

The private package uses the optional Billing build with193AOT/205ELF/
808static16KiBLOAD, valid v2/v3/ZIPCRC and no debuggable flag. It declares INTERNET
and BILLING; it is not an offline package. SHA256 is
`69625d1c71c1fb665f3c3fdc0261c0e89c76121616f34ca1a97936c9aa827c8a`,
size56795452bytes. The qualified x64 Mono15payload matches, and two exact PDB
document checks bind the original changed model and private caller before
installation. Its zero identity-primary counter is not a general network-traffic
or genuine Play/provider acceptance claim.

One final85node/32665byte native XML is retained, and the remote copy is
hash-checked before deletion. All new credential-input conditions use only null
and empty values; no genuine credential is entered or emitted.

Only the fresh UID is uninstalled, with captured public OS values/key presence,
locale, boot, geometry and reverse list exact. Both original emulator/QEMU handles
signal before official AVD deletion, which retires24files/4282660213logical
bytes. No phone command or frozen-scope action occurs; Main remains the dated
D1085observation rather than new liveness/private-source proof.

## Delivery boundary

Strict Windows, complete default Android and complete optional Billing Android
builds finish with no warnings/errors. The default signed offline/nondebuggable
APK has384AOT/408ELF/1608static16KiBLOAD, valid v2/v3/ZIPCRC, size99643295bytes
and SHA256`35f34633eb4deb33ffde7330f130f2b1a39a3f3d07d4bf22fd96ae2bac93d029`.
The optional Billing signed/nondebuggable APK has386AOT/410ELF/1616static16KiBLOAD,
valid v2/v3/ZIPCRC, size100037165bytes and SHA256
`defe7ef776efcb483bf0797c33a987e6824742578aaad35bd7dd871e92a559db`.
It declares INTERNET/BILLING, not offline status. Qualified Mono15payloads match
both architectures in both complete packages. Exact PDB documents bind the
changed model in Windows/default Android/Billing Android as well as the private
caller/model pair. Neither complete package is installed on Main here.

Six standard Cleans pass and retire42751files/2971581513logical bytes;
5903files/931645145bytes remain in those build roots. Signed used/prepared
packages, compiler inputs and evidence are retained. Original ADB59860/
creation639293902901445383 remains; there is no explicit reset/start/reconnect/
pair and no repeat of accepted native inputs.

A guarded retirement of only these three redundant unsigned publish copies is
rejected before process creation with `blocked by policy`:

- `artifacts/d1089-identity-nullable-bindings/android-publish/pro.vafadar.zanance.apk`
- `artifacts/d1089-identity-nullable-bindings/billing-publish/pro.vafadar.zanance.apk`
- `artifacts/d1089-identity-nullable-bindings/native-publish/pro.vafadar.zanance.identitynullreview.apk`

The measurement/plan does not execute and no file is deleted by that command.
Those exact three targets are now frozen, without further inspection, retry,
subset, alternate method, move, escalation or reuse. Signed siblings, compiler
inputs and existing evidence are preserved; all earlier frozen scopes remain
untouched. Their unsigned byte total is not inferred from signed package sizes.

Evidence, signed used/prepared packages, compiler inputs and raw observations
remain under `artifacts/d1089-identity-nullable-bindings`. Default offline and
optional Billing builds are distinct; neither changes the selected commercial
edition or authorizes activation/purchase/publication. Canonical parent States,
terminal authentication/financial cohorts and the full delivery goal are
preserved. Genuine accounts/providers, whole security, original fault, complete
performance/accessibility, iOS/Store/release remain separate open gates.

The exclusive single D1089 response reports
[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43112526054)
completed/success and
[CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43112525610)
in_progress/no conclusion. Exact response, consumed marker and receipt remain;
no second GET, successful-job log or rerun is used.
