# Awaited device confirmation rechecks app-PIN policy — D-1030

## Six actual original failures

Device-only authentication is permitted when no app PIN is configured or known
unreadable. Three original service entry points made that decision before awaiting
their native port but accepted a later successful response without rechecking the
current app-PIN requirement:

| Original entry point | PIN becomes configured during wait | Protected PIN read fails during wait |
| --- | --- | --- |
| AuthenticateDeviceUnlockAsync |Incorrectly accepts|Incorrectly accepts|
| ConfirmAsync |Incorrectly accepts|Incorrectly accepts|
| ConfirmStartupRecoveryAsync |Incorrectly accepts|Incorrectly accepts|

Nine new host cases cover those six negative conditions and one unchanged-policy
control per entry point. The original build has9total/6failed/3passed/errors0,
with actual True where False is required. These are explicit held native-effect
ports, not genuine owner authentication or a demonstrated operating-system exploit.
The original cover stays in place because these cases inspect the policy result
without invoking the separate original close/operation continuation.

## Bounded product correction and host checks

After the awaited device response, require that app PIN state remains both available
and unconfigured before accepting Success. Device-unlock feedback becomes Failed
when that original device-only premise has retired. Covered startup recovery also
retains its existing caller-current/cancellation check. No fallback confirmation,
extra native prompt, PIN removal, cryptographic format, grace interval, financial
policy or platform setting is introduced. The earlier
[D1029 closing-cover correction](app-lock-close-request-ownership-d1029.md) remains.

All9new cases and20directly affected AppLockTests pass together on the final strict
host binary:29passed/errors0/failed0/skipped0/not-run0, actual.NET10.0.12. This is
9new tests, not29new tests or a full suite. Each new case records exactly one held
authentication-port call, no PIN prompt and no cover close. Configured-state cases
use the original PinLock against fresh in-memory fictitious verifier storage;
unavailable-state cases use the original failed secure-state read path. No real
credential, production secure storage or financial operation is exercised.

## Running Android, original failures and same-source upgrade

Fresh API36/x64/4096 UID10216/PID4985/.NET10.0.14runs the original compiled service
through the same nine new conditions. All six changed-policy cases incorrectly
accept and all three unchanged controls pass. One necessary no-reset upgrade to
PID5428, same UID/boot, then runs the corrected service: the six stale confirmations
refuse and the same three controls remain accepted. Both one-shot requests remain
consumed; setup and accepted older cohorts are not replayed.

Only the initial native phase creates the fictitious schema and AppLockEnabled
metadata. The corrected phase adopts that same existing source and does not migrate
or seed again. Complete sqlite_master/schema and typed rows of all55tables/57rows
remain equal before/after both phases and across upgrade, SHA256
`D2D73C67B41508A7173F9D2F93C3EB391728272E6447670E138567C5A41455CF`.
The in-memory fictitious PIN verifier is separate from that database comparison;
each configured-PIN condition performs one necessary memory-only Set, with no
production PIN/key/Keystore write. Every native case has one explicit synthetic
authentication-port call,0PIN prompts/0cover closes. There is no real biometric/
credential request, original MAUI modal acceptance, financial command or phone input.

The first private strict compile refuses two unused prior-cohort callback fields
with CS0649. Preserve that failed build and remove only those unused observer
fields before any installation. No baseline condition or successful input is
repeated to correct the observer. The corrected observer then builds strictly;
only the selected service source and phase/version change for the later upgrade.

## Package and source evidence

Strict Windows, complete offline Android and both final private native builds have
zero warnings/errors. Seven original/private/Windows/shipping compiler documents
and five corrected private documents match their actual source checksums. This is
finite source binding, not whole-PE or all-assembly equality.

| Package | Bytes | SHA256 |
| --- | ---: | --- |
| Original private native |16181394|`d20b59b0a5ce5d75b03e8cb4313f5c56600188fbd787ec831d46b43727af3225`|
| Corrected private native |16181394|`009d8c3d0bfd0c633120dc798857115e627db5649a6ac34f334ae50f1d2e2840`|
| Complete corrected shipping |99614623|`5910157c8c3a1b35d4f1bb4604234dc213d2c957a3d3586810a105d4d4952a63`|

Private packages have92AOT/103ELF/401static16KiBLOAD; complete shipping384/408/1608.
Offline/nondebuggable/v2/v3/ZIPCRC/alignment pass; original Mono slices remain exact.
Static alignment is not hardware16KiB acceptance. Shipping is prepared, not
Main-installed here. Main24881/6e8f5b61is only the last dated D1025observation,
not fresh liveness or physical acceptance of these later changes.

## Exact retirement and remaining delivery

After the corrected terminal observation, remove only the fresh package and verify
original captured OS values, locale/key presence, boot, geometry and reverse mapping
exact without settings writes. One normal console stop leaves all four original
owned host handles signalled and the same ADB5038/PID65060creation identity alive.
Official retirement removes24AVDfiles/4178130185logical bytes.

Five standard Cleans retire5775registered files/1176417255logical bytes;
2986registered files, six userhome files/7732bytes and11host-tmp files/10304bytes
remain. Used signed/source/compiler/original-failure/result evidence remains under
current repository artifacts. No supplemental deletion, frozen-scope action,
retained5606/Q02 work, financial replay or unchanged successful suite occurs.

The owner-controlled [enrollment-continuity decision](prepared-enrollment-continuity-d1028.md)
remains pending; this independent correction grants no security-setting change or
genuine authentication authority. Minimum/physical/platform behavior of this later
change, whole security/key/recovery, original native-fault remedy, full performance/
accessibility/provider/Store/release gates and canonical parent States remain open.
The complete delivery goal stays active. This push has a separate one-time CI check;
an earlier green result is not acceptance of the current change.
