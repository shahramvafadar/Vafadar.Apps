# Exact numeric boundaries in holdings CSV (D-851)

## Actual defect and original contract

HoldingsCsv.TryQuantity multiplies a parsed decimal by1000; TryPricePerUnit multiplies
by the original currency factor and1000. Decimal.MaxValue therefore throws before the
existing long-range refusal. Decimal.TryParse can also round an overprecise text to
an accepted integral scaled amount. Both violate the reader's original row-error and
lossless holdings-file contract. No actual imported financial data is needed to expose
them: use the public HoldingsCsv.Read with memory-only fictitious types/locations/rows.

24 new boundary cases on the unchanged reader produce9 failures:4 real multiplication
overflows (quantity and EUR/JPY/KWD price) and5 silently accepted rounded values.
The remaining15 controls already pass and are not independently replayed before repair.
Controls include exact long.MaxValue, its next unit, redundant zeroes, smallest units,
leading decimal point/trailing point, excessive precision, exponent and positive sign.
This is read-only public parser work, not an Import/Save or ledger acceptance.

## Change and focused checks

Both original fields delegate to one private positive fixed-point scaler. The existing
invariant NumberStyles.AllowDecimalPoint parser still owns syntax; normalized submitted
whole/fraction digits, not its potentially rounded decimal value, determine stored units.
Redundant leading/trailing zeros remain accepted. A nonzero fraction beyond original
storage precision refuses. Long parsing bounds the exact scaled positive magnitude,
without decimal multiplication/overflow. Price scale remains currency.MinorDigits+3;
quantity remains original thousandths. Optional invalid unit weight retains its existing
null behavior. No format-version, column, currency, export, writer, migration/model,
recurrence, monetary policy or localized error-string change is made.

All24 new cases pass after the correction. Only the two affected existing holdings
roundtrip/bad-quantity reader cases also run and pass:26 selected cases total, not a full
suite. An initial combined class/method filter selects zero cases and is explicitly not
accepted as verification; corrected separate filters produce the exact24/2 counts.
Strict test compilation and Windows build complete without errors or warnings.

## Original Android execution without writes

Use the retained original emulator5606, API36/x64/4096, original boot
6663f98e-6546-4809-a0bc-285f11a96674 and existing ADB5038. No restart/reset/reboot,
locale/font/geometry/permission write, original source/provider/history, native-exit
reader or R13 interpretation is performed. The original D809/D833 packages and data
remain protected. The physical phone receives no command.

Fresh package pro.vafadar.zanance.holdingsnumericreview, UID10243/PID26210, is installed
and normally launched once. The first readiness XML shows Preparing Zanance, not the
assumed Welcome/Next; that observer failure and XML remain. No reader check has been
issued then. The same process continues to actual original Onboarding; only remaining
readiness is captured, with no install/launch/setup replay. A single package/run/PID/
shell-UID/hardware-bound action invokes the original public reader for the24 frozen
boundary inputs. It compares all20 declared typed source components before/after and
retains zero accounts/entries/schedules. No type/location/event/valuation is persisted,
no imported file is written and no financial/UI writer is invoked.

Actual Android packet:24 cases pass, Source20Equal=true, source SHA256
14A8C00BD0CFEACBE83B51FC8DAF5F287ADEB489537F89F8D648212F490C81D2.
This is actual original-reader execution in the private app, not complete native picker,
CSV preview/import/Undo, full schema/cipher/key or independent-device acceptance.

## Strict artifacts and exact retirement

| Artifact | SHA256 | Bytes | AOT / ELF / static16KiB LOAD |
| --- | --- | ---: | --- |
| Complete shipping |016bfff906b7888e72c5ed71e1fe54d9ed702e05449dbc2f79ecf25db4b84f86|99282713|384 /408 /1608|
| Used private x64 |049c9f28f826f06873aa00ec3c050f9b2369f65540cdbb56fca7f158afb2c74c|56721180|192 /204 /804|

Both strict builds are offline/nondebuggable and pass v2/v3 signature, CRC/ZIP and static
alignment checks with original per-ABI Mono. Actual App/Core/Data AOT ZIP members match
their own build artifacts:6 shipping plus3 private. This does not establish whole managed
PE identity, actual ARM64 hardware16KiB, every method or original-fault repair. Used signed
APKs, current source hashes/copies and the private source archive are retained.

Only freshUID10243 is uninstalled. The two owned remote XML files total27210bytes;
exact byte archives and two matching reads bind their removal, with absence verified.
Same native PID is observed through the completed check. All original emulator packages/
UIDs, five OS values, locale-key presence, geometry, boot, reverse and shell identity match
the preflight. The retained emulator is neither stopped nor recreated. Four standard
Clean profiles (selected tests, Windows, shipping, private native) pass; no supplementary
delete, frozen scope, cleanup retry or alternative method is used.

Evidence lives under the existing local Zanance-D851-current.txt pointer. Shipping016bfff9
is prepared, not installed on Main;20144/904 remains the dated D849 checkpoint only.
D607 is terminal, D838's distinct new-PC proposal pending, D841's actual branch unexecuted.
Original fault cause/remedy, whole performance/security/A11Y/platform/provider/Store/release
remain open and the overall delivery goal stays active. No CI wait or old successful
financial/auth/photo/test cohort is used to fill this work.
