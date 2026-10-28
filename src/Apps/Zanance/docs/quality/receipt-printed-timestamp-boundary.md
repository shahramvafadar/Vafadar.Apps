# Complete printed receipt timestamps retain the missing-merchant boundary (D-626)

## Actual source defect and bounded correction

A headerless receipt beginning with27 OCT2025 13:42 is admitted as a merchant:
the old boundary recognizes only an entire date row, not its printed clock suffix.
A numeric date/time row instead falls through to an item name such as BREAD.
Neither is merchant evidence. Preserve D-615/D-621's original completed inputs;
these eight distinct timestamp inputs fail against the original production parser.

Remove only a complete trailing24-hour clock with a whitespace delimiter, hour0..23,
two-digit minute0..59 and optional two-digit second0..59 before the existing full-row
date validation. Share that view in the missing-merchant boundary and the complete
named-date fallback. The actual source string and merchant return value are never
trimmed or rewritten. Full date/calendar/year validation still applies; do not infer
a year, accept arbitrary prefixes, repair invalid clocks or parse a timezone/AM-PM.

Keep the original numeric-date search first and the conflict refusal for different
complete named dates. TryAmount, total/currency evidence and numeric/calendar
TryDate bodies remain byte-for-byte equal to the original captured bodies. No
financial command, schema, model, library or platform-setting change is involved.

## Focused original and corrected evidence

The original eight cases cover English/German/French/Italian/Spanish named dates
and three numeric layouts with minute/second or single-digit-hour clocks. All eight
fail the absent-merchant assertion on the real original parser. Preserve that TRX.
Nineteen distinct new corrected cases pass: those eight, three real headers including
clock/date-like names, six invalid/contextual/timezone rows, numeric-date precedence
and conflicting timestamps. Total7.10EUR/710minor and Found/candidate evidence remain.

Run only the two affected existing classes after the source change:10 merchant-boundary
and24 named-date cases pass, including calendar/year and prior numeric precedence.
This is53 distinct final focused cases,19 new, not a full-suite total. A direct-DLL
invocation rejects --filter-class before running any test; preserve that launcher
failure, then use the repository's normal project test command with --no-build.
No successful unrelated financial/crypto/corpus suite or accepted old receipt is repeated.

## Complete package and one original native unsaved draft

Strict Release Windows build and complete offline Android publish exit0 without
warnings/errors. Full signed APK is97071007bytes, SHA256
93fb87c8bd412c651ac41e347050e211e2e3139a11bc0ee2b2f5304d1442d5e7.
384 own AOT/408ELF/1608 static16KiB LOAD checks, CRC/ZIP alignment/v2/v3 and absent
INTERNET/network-state/debuggable checks pass. Static alignment is not hardware16KiB
execution. The same actual installed base APK digest is checked before and after.

One fresh Zanance_D626_Timestamp API36/x86_64/4096 emulator,1080x1920/density420/font1.0,
boot05e6a624-bf04-4bd3-98cc-cbd2364a64e7, has no original package including retained
data before one install and ordinary launcher. It initially appears offline and then
Preparing Zanance; continue the same host79216/process rather than restarting it.
Actual package UID10216 owns every original app window.

One new original onboarding Start creates D626_EmptyWallet/Checking/EUR with opening0,
28October2026. Decline unnecessary reminders; confirm original All/All-types history
empty and Home0 before reading. This is fresh account metadata, not a transaction.
No old account/setup or financial success is replayed.

Create one new1400x950 PNG with27 OCT2025 13:42, BREAD EUR7.10, TOTAL EUR7.10 and a
fictitious-input footer; SHA256
4f7fc3c7bbc424352a5b8ba6a470c4f5f4cdd5d26f84618bff21cd9a5e522d9c,
41405bytes. It is a new generated fixture, not an edited accepted photo or another
camera/OCR baseline. Select it once through original Home/receipt chooser/DocumentsUI.
An exact filename search avoids the owned XML files visible in Downloads. The search
field is an AutoCompleteTextView, not the initially assumed EditText; no duplicate
search-open or file-selection action follows that empty class-only observation.

Original New expense shows Amount7.10EUR, accountD626_EmptyWallet and
From the receipt: Amount, Date, with no Payee claim. The original visible Date boxes
are10/27/2025, not the actual device day2026-10-28. Empty payee is hidden by the original
Simple-mode rule, so the first observer wrongly expects it during scrolling and stops.
Preserve that failure and the same draft. Original More details reveals the existing
field. Its first native bounds are clipped/inverted, and the next scroll clips it at
the top; both strict-interior guards reject them. A bounded small viewport correction
makes the complete empty field visible at[87,442][994,558], inside[0,221][1080,1611].
No field is typed, receipt reread, Use amount pressed, category selected or Save issued.

This qualifies the actual expected unsaved form from that image. The complete raw OCR
clock characters are not separately exported, so the native observation is not a claim
of an exact recognized-text packet or exclusive proof of which parser branch ran.
The focused managed original/fixed cases establish that exact timestamp branch.
Cancel and the original Discard confirmation once, then confirm All/All-types empty,
Home0EUR and the original fresh account. No transaction, Import, Undo or payment occurs.

## Preservation and retirement

Five original OS setting values, physical size/density, boot and installed APK digest
match after the workload. Source suggestion fields are never typed. No physical phone,
D-607 provider/Activity or retained emulator command occurs. Five unfinished D-607 source
hashes and the retained AVD configuration remain exact. Keep all three D-607 deadlines,
consumed reboot and post-reboot unread-source state. Physical no-reset adoption of this
new full candidate waits for actual D-607 completion/fixture retirement.

Thirty-five remote XMLs/1469718bytes and only the new41405byte PNG are hash-matched
before removal; local evidence stays. Only the fresh package/data are uninstalled once;
no alarm or active Notification List entry remains. Preserve system uninstall history.
The stop marker contains an incorrect copied label calling QEMU54376 a D-625 process;
retain it with a separate correction: it is this D-626 child and no PID-kill command was
issued. Actual stop uses only emu kill for5572. Both host/QEMU and serial are absent
before standard avdmanager deletes25 files/5241123780 logical bytes without errors.
The separate5038 ADB server remains; other cohorts are not stopped or booted.

Three standard dotnet clean calls succeed. One supplementary exact-four-root native
PowerShell deletion is rejected before process creation with blocked by policy;
2720files/444548733 logical bytes remain, including six isolated user-home files.
No retry, subset, alternate tool, move or escalation follows. Used shipping APK and
local original receipts stay; logical bytes are not measured host free space.

This finite timestamp/merchant correction is complete; general receipt corpus,
physical adoption, stability/performance, accessibility, iOS/provider/Store/publication
and overall delivery remain open. TalkBack stays last. Publish only this finished
batch, preserve unfinished D-607 changes, and consume/delete only one exact-head
CI monitor after16minutes. Original evidence: %TEMP%/Zanance-D626-current.txt.
