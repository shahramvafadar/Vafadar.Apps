# Original account-conversion startup publication - D-956

## Actual remaining startup gap

Recent receipt/installment startup failures motivate inspection of the original
`AccountConversionSession`, rather than another successful financial workload.
The original ConvertAsync captures revision/token/day/display before starting, but
sets IsBusy before its cleanup and clears Error without rechecking that captured
owner before entering HoldingStore. Synchronous property subscribers can therefore
retire the owner, change day/display or throw before cleanup is active.

Seven new focused cases execute the actual source-linked session and its original
HoldingStore with a sentinel at the concrete source-factory boundary. The sentinel
counts an attempted context acquisition and refuses before forwarding a connection,
reader, consent or writer. The tests prepare no financial account or entry; original
full20-domain typed source is compared before/after each case. This factory attempt
is not claimed as proof that the old operation completed a SQLite read.

Original results are6failed/1passed/0skipped. Busy subscriber IOException leaves
IsBusy true. Four cases retire/restore the original owner, change its external
predicate, change only injected day or change app language during Busy publication;
each still enters the factory once when0is required. Clearing a retained error and
retiring/restoring that same owner also enters it once. The unchanged current-owner
control correctly reaches the original source boundary once and displays the safe
read-review error. Original failure logs/compiled PDB/source are preserved.

## Complete correction within the original command

Put startup IsBusy publication inside the existing try/finally and check Current
immediately afterward. Clear the internal error state before publishing Error=null,
then check the same captured Current again before source acquisition. The original
lifetime/revision/day/display/external predicate is retained; startup cannot silently
adopt a replacement owner. A startup exception now follows the existing original
failure reporter and guaranteed Busy cleanup.

Original preview/validation, explicit confirmation, actual conversion writer,
accepted-review/outcome capture before notification, retained receipt behavior,
post-commit failure distinction and navigation are unchanged. No schema/model,
financial rule, translation, layout, runtime/GC, native engine or authorization policy
changes. This is not a blanket exception suppression or original SGen/ANR repair.

All7selected cases pass after correction with0skipped. They are7new cases, not a
full suite. The current-owner control runs after the new guard to verify that valid
source acquisition remains reachable; unrelated historical successful tests,
accepted conversion writes and their financial setup are not replayed. Two actual
original and two corrected PDB documents bind the respective session/test inputs.
Strict Windows and shipping/native compiler bindings separately bind their actual
source inputs; a compiler checksum is not whole-assembly byte identity.

## Actual Android session with a separate empty fictitious source

A private SDK-only Release package source-links the original session, original
CommercialFailureMessage, IAppInteraction and AppRoutes, referencing real Data/Core/
Localization libraries and existing selected SQLCipher provider. There is no startup
database work. Only its explicit prepare request creates/migrates a fresh disposable
unkeyed fictitious database; accounts/entries/schedules are all0. This is not a
protected Main profile, encryption-key/authentication acceptance or a native page/
button/navigation qualification.

The reader requires shellUID2000, owned package, ranchu/x86_64, exact run and actual
PID, with exclusive markers for every action. Original session methods are dispatched
through the live Activity onto its UI thread; the original data reader retains its
worker implementation. A rejecting interaction port counts any disclosure/navigation
attempt; all outcomes keep that count0 and no writer is reached.

Fresh API36/x64/kernel4096 uses PID4643/UID10216, boot
`6afb45b9-4983-47fe-9706-a52c6411ae1c`. Four corrected native cases execute once:

| Actual session boundary | Command-specific source factory calls | Terminal observation |
| --- | ---: | --- |
| Current owner, absent original account | 1 | Original localized review error, Busy false |
| Owner retired/restored by Busy subscriber | 0 | Original request ends, Busy false |
| Busy subscriber throws IOException | 0 | Original localized review error, Busy false |
| Prior current-owner error cleared, owner retired/restored | 0 | Original request ends, Busy false |

The last case retains the same current-owner session/error from the first case;
its source preparation and successful read are not repeated to manufacture an error.
These are4new corrected native observations, not four new unit tests or native
baseline reproductions. A separate source observer reads the full source before/
after each case; its independent connections are not counted as command-specific
factory acquisitions.

Canonical in-memory comparison covers both header identifiers, complete noninternal
sqlite_schema type/name/table/SQL, and all54noninternal application/EF table rows.
Storage classes, complete text/BLOB bytes, integer values, floating-point bit patterns,
NULL and duplicate multiplicity are retained; row order is normalized explicitly
ordinally. Equality compares the full packets, not a digest. Only the resulting
SHA`0468342464DF553E6D395F794281F36553DD94455418C46E4D471DA4F83EFCDF` and equality/count metadata leave the
observer. SQLite-internal tables are excluded and no whole physical/private DB/key
claim follows. All source packets remain equal and accounts/entries/schedules stay0.

## Strict packages, source evidence and owned retirement

Strict Windows/full Android/private SDK builds are warning/error free. Shipping
SHA256`c20fa2a6cbcd235a9bbf80a76377b0038f452b2b63f6689a88f260d2142eb864` (99573663bytes) has384AOT/408ELF/
1608staticLOAD. Private native SHA256`284af56d61587b2f0396c56714f6fc680bd84aea4387e83587bd817aa9ab630d`
(16292078bytes) has93AOT/104ELF/405staticLOAD. Both pass offline/nondebuggable/
v2v3/ZIPCRC/ZIP alignment/static16KiB checks. Packaged cipher ABI hashes match the
tracked selectedC5 manifest and original Mono hashes remain. This is not hardware
16KiB, ARM64 runtime or latest Main acceptance. Actual native PDB checks bind all6
original/controller C# documents; original English resource input is archived.

One fresh install/launch continues the same owned boot. Four case receipts pass in
one process without OS/font/display/locale/key/permission writes. Exact five OS
values, locale/key presence, boot, geometry and reverse match before/after. Only the
fresh package is uninstalled, absence checked; normal console stop signals own
host71684. Official AVD retirement removes25files/
4420089302logical bytes,0AVDfiles left. Six small user-home files/7732bytes
remain, together with signed used/prepared packages, source/PDB and failure/outcome
evidence under `artifacts/d956-conversion-startup-publication`. Five standard Cleans
pass:Windows,tests solution,native Android,shipping Android,compiler reader. No
supplementary/frozen deletion or retained5606 input occurs. Existing ADB5038/PID50972/
creationticks639294095614173469 stays exact.

D955one-time CI checkpoint is retained without an early query or waiting for CI.
The owner is away: D944physical7552/UID10439/data/original attempt and Main adoption
remain held; no phone command, authentication question/reissue, install or financial
operation occurs. These identifiers remain the held observation, not fresh liveness.
Terminal financial/check/register and frozen checkpoints remain protected. Original
native fault/ANR, complete performance/security/accessibility, iOS/provider/Store/
release and overall delivery stay open; independent ready work continues.
