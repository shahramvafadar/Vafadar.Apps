# Original lock page follows changed PIN policy — D-1031

## Actual remaining interface failures

[D1030](device-confirmation-current-pin-policy-d1030.md) refuses a device-only
response after app-PIN policy changes, but the already-open LockPage still used
its constructor-time presentation. A fresh running original page demonstrates
two remaining conditions, with one unchanged-policy control:

| Policy while the device-response port waits | Original native page result |
| --- | --- |
| Unchanged, explicit failed response | Correct existing message; PIN/recovery hidden; Unlock enabled |
| Fictitious in-memory PIN becomes configured | PIN entry and recovery remain hidden; message is stale |
| Protected PIN read becomes unavailable | Recovery remains hidden; message is stale and Unlock stays enabled |

All three retain the access cover. These are two actual UI failures, not another
successful bypass after D1030, a crash or a genuine owner-authentication result.
The initial original native packet records one expected control and two failed
expectations. It remains preserved without replaying its inputs.

## Correction on the same original page

Retain the existing recovery button as an initially hidden control when device-only
policy applies. After the awaited device result, update the original PIN entry,
Unlock enabled state and recovery visibility from the current service policy.
When refused, show the existing PIN-entry/unavailable caption before device-only
fallback guidance. No second automatic authentication request is introduced.

The existing PIN verification, recovery confirmation, busy guard, native cover
completion, screen protection, grace, financial and cryptographic policies remain.
No new string, palette, permission, schema, migration, runtime or GC change occurs.
The AppLockService and original host source remain identical across this UI-only
qualification, confirmed by their actual compiler checksums.

## Original MAUI and Android execution

Fresh API36/x64/4096 UID10216/PID3361 runs the original LockPage, MauiAppLockHost,
keyboard viewport, modal navigation, screen-protection helpers, palette and
translated resources. The surrounding application is an explicit private MAUI
ContentPage root, not the product Shell/Home or genuine device authenticator.
Original page appearance starts one deliberately held synthetic response per case.
There is no native credential dialog, owner input, PIN submission or recovery tap.

One necessary no-reset upgrade reaches PID5301 on the same UID/boot. All three
conditions pass on the corrected page. Actual native view presence/shown state,
positive width/height and owned package identify the newly visible PIN/recovery
controls; managed and native Unlock enabled state and translated message agreement
also pass. The unavailable case has recovery visible, PIN hidden and Unlock disabled.
The configured case has PIN/recovery visible and Unlock enabled. The unchanged
control preserves its original result. This is three new native conditions and
zero new managed tests, not a repeated unit suite or an accessibility matrix.

Each case retires only its exact owned top modal using one explicit cleanup pop,
with the access model still locked. That cleanup is not successful authentication
or the original unlock action. The original private root and empty modal stack
are verified after each case. No accepted financial or authentication flow runs.

Only the original phase prepares the fictitious schema and AppLockEnabled metadata.
The corrected phase adopts the retained file without migration, seed or metadata
Save replay. Complete sqlite_master/schema and typed rows of all55tables/57rows
remain equal before/after both phases and across upgrade, SHA256
`5D8B22BD2091F5573AED013B02668921ADE0D05917CEB7AA0EB6F8E7207E2604`.
Each configured-state case performs one required original PinLock Set against
fresh in-memory fictitious storage; that verifier is outside the database digest
and never uses production secure storage. No Main/private database/key, financial
command, real PIN, biometric enrollment or physical phone operation occurs.

## Strict packages and compiler binding

Original and corrected private builds, strict Windows and complete offline Android
pass without warnings/errors. Thirteen original and thirteen corrected private
compiler documents match the selected source inputs; two actual complete-product
compiler documents bind corrected LockPage SHA256
`B49AFB0627DE07C4AAFA25383EC4427482B34F00D9935458685F273A6E2F3399`.
The private observer, original service, host and contract compiler hashes remain
equal across upgrade. This finite binding is not whole-PE/all-assembly acceptance.

| Package | Bytes | SHA256 |
| --- | ---: | --- |
| Original private MAUI |33421800|`53597f965030bf389c20352a97b12e4dbd3ca9621413e6c175fa1b866de49b34`|
| Corrected private MAUI |33421800|`43caaa2eabcde2b20ef0bf79bdd3f5771b5b30d582280b845eb38f128d18e916`|
| Complete corrected shipping |99614623|`eb88c2aada0bb0a51591d20e62b0ddfc6f16d6b817b0d58d2df792a08d3759ab`|

Private packages have148AOT/159ELF/625static16KiBLOAD; shipping has384/408/1608.
Offline/nondebuggable/v2/v3/ZIPCRC/alignment pass and original Mono slices remain
exact. Static alignment is not hardware16KiB acceptance. Shipping is prepared,
not Main-installed. Main24881/6e8f5b61remains only the dated D1025observation.

## Exact retirement and remaining delivery

Remove only the fresh package after its corrected terminal result. Original
captured OS values, locale/key presence, boot, geometry and reverse mapping stay
exact without setting writes. One normal console stop leaves all four original
owned host handles signalled and ADB5038/PID65060creation identity exact. Official
retirement removes24AVDfiles/4290917613logical bytes. Four standard Cleans retire
8578registered files/1133831116logical bytes;3148registered files, six userhome
files/7732bytes and22host-tmp files/3309189bytes remain. Used signed/source/compiler/
failure/result evidence stays in current repository artifacts. No supplemental,
retained5606/Q02, frozen-scope, protected untracked file or unrelated success replay
occurs. The project uses explicit source inputs, never the benchmark wildcard.

The sole D1029response has [CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42838010984)
completed/cancelled and [CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42838010998)
in_progress/null. The sole D1030response has both
[CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42840346277) and
[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42840346497)
in_progress/null. Those are consumed snapshots, not later outcomes or reasons to
rerun successful work. This push has its own separate one-time check.

Genuine enrollment/authentication and the owner-controlled
[D1028 decision](prepared-enrollment-continuity-d1028.md), other transitions,
languages/large text/physical/minimum-platform behavior, glyph/contrast/speech,
whole accessibility/security/key/recovery, original native-fault remedy, performance,
provider/Store/release and canonical parent States remain open. The complete goal
stays active; this bounded UI correction is not whole-project completion.
