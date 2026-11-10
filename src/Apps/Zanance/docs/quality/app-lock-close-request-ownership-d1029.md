# Closing cover retains its original request authority — D-1029

## Actual failure and bounded correction

An awaited AppLockService.UnlockedAsync cover close could outlive another SleepAsync
request. Its old continuation then unconditionally cleared the current cover and
requested-lock flag and drained protected pending work. Three new original-service
conditions demonstrate that failure: a replacement cover after visual removal, a
requested cover while the host is unavailable, and renewal of the same cover before
visual removal finishes. One ordinary-close control passes. Original selected host
total4 has3failed/1passed/errors0; preserve those outcomes rather than reinterpreting
them as successful authentication or an actual operating-system modal interaction.

Each requested cover now receives a distinct identity. Unlock captures that identity
and the cover before waiting, then clears state only if both still own completion.
A superseded completion leaves pending work covered and reconciles a missing cover
without authentication. Replacing an already-requested cover also preserves its
already-locked state, so subsequent short resumes cannot create a new grace grant.
The existing30second grace, native authentication, PIN, recovery, financial and
notification routing policies remain unchanged.

## Selected host and running Android evidence

Four new cases and31directly affected AppLockTests/AppLockElapsedGraceTests pass
together on the final strict host binary:35passed/errors0/failed0/skipped0/not-run0,
actual.NET10.0.12. These are4new tests, not35new tests or the full suite. Each renewed
condition also checks two resumes without advancing the synthetic clock; the cover
remains present and pending read-only counters stay0. The ordinary-close control
still releases its counter once.

Fresh API36/x64/4096 UID10216/PID3095 runs the original compiled service with explicit
boolean cover ports and a refusing authenticator. Its ordinary control passes and
the same three original conditions incorrectly unlock and release their counters.
One necessary no-reset upgrade to PID4864, same UID and boot, then runs the corrected
service and all four conditions pass. Neither request is repeated. The final three
renewed conditions remain locked before and after both resumes, with0pending effects;
each original cover closes exactly once and every authentication-call count is0.
Actual Android runtime is.NET10.0.14.

Only the first native phase creates the fictitious schema and AppLockEnabled setting.
The corrected phase adopts that same file read-only before running the remaining
model conditions; setup and successful operations are not replayed. Complete
sqlite_master/schema and typed rows of all55tables/57rows have identical SHA256
`88C5BCC589402ED2273B658D797D0F2AB1F50036F35B78EC2FD8014869705ED2`
before/after both phases and across the upgrade. No financial command, real PIN,
biometric prompt, key operation, camera, export or genuine notification tap occurs.
This is running original-service logic with synthetic host ports, not acceptance of
the original MAUI LockPage/native modal, a real Android lifecycle race or owner auth.

## Build identity and retained packages

Strict Windows, complete offline Android and both private native versions pass with
zero warnings/errors. Seven baseline/Windows/shipping compiler documents and five
corrected private compiler documents match their actual source checksums, including
the baseline archived service and corrected product service. This is finite source
binding, not whole-PE identity or every assembly/path acceptance.

| Package | Bytes | SHA256 |
| --- | ---: | --- |
| Original private native |16181394|`9272b8d0bb587efaa08724ddf10a5d75d0f6981c35a56eb476634edde503dd93`|
| Corrected private native |16181394|`f03afd73fa9323b8ee3214287b23ea1488456307f3f0d540f65376ca1546bb69`|
| Complete corrected shipping |99614623|`a9caf98245e893edca2d91ac90afa2db8a41686549a3242737b0a282341a2f56`|

Both private packages have92AOT/103ELF/401static16KiBLOAD; shipping has384/408/1608.
Offline/nondebuggable/v2/v3/ZIPCRC/alignment pass and original Mono slices remain
exact. Static alignment is not hardware16KiB acceptance. Shipping is prepared,
not Main-installed here; no phone command or fresh Main-liveness claim is made.

## Retirement and continuing delivery

The fresh package is removed only after its corrected terminal result. Original
captured OS values, locale/key presence, boot, geometry and reverse mapping remain
exact without settings writes. All four original owned host handles terminate by
one normal console stop; existing ADB5038/PID65060creation identity stays exact.
Official retirement removes25AVDfiles/4184290728logical bytes. Retained5606/Q02,
terminal fault classification, protected untracked work and frozen scopes stay
untouched.

Five standard Cleans retire5775registered files/1176376498logical bytes;
2985registered files, six userhome files/7732bytes and26host-tmp files/3943264bytes
remain. Used signed packages, source/compiler proofs, original failures and result
receipts remain under the current repository's own artifacts. No supplemental
delete, alternate cleanup or unrelated successful test/financial replay occurs.

The one D1028response reports [CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42831489014)
and [CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42831489001)
completed/success. It was observed later than the15–18minute window, not timely
monitoring; there is no second GET or rerun. D1029has its own pending one-time check.

The [prepared enrollment-continuity decision](prepared-enrollment-continuity-d1028.md)
remains with the owner; no biometric enrollment is changed. Broader security/key/
recovery, original native-fault remedy, whole performance/accessibility/platform/
provider/Store/release requirements and canonical parent States remain open. The
full delivery goal stays active.
