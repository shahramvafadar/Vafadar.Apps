# KPI calendar horizon endpoints (D-929)

## Original failures and correction

Six new host cases all fail on the original KpiCatalog: three commitment horizons
and three yearly horizons construct dates beyond the supported calendar. The original
failures and their stack locations remain in the local baseline log. No database,
financial UI operation, settlement or existing successful fixture is involved.

Commitments now clamps only a forward horizon beyond DateOnly.MaxValue to that last
inclusive day. The existing behavior for ordinary and negative day arguments is
unchanged. Yearly retains the final supported remainder when today is in year9999;
it does not subtract a day from an already clamped exclusive boundary. The previous
full-year calculation remains exact for all earlier years. No amount, recurrence,
payment eligibility, outstanding-payment or settlement rule changes.

The same six new cases pass after correction. They verify the final due date,
partial-payment remainder and unchanged state, and the original plan reference/total
for the final calendar year. Two affected existing K04/K08 cases pass separately;
selected counts are6new+2existing, not a whole suite. No successful financial operation
is replayed. Original comparison-range logic is inspected but not modified or claimed
as another demonstrated failure.

## Builds and actual Android calculations

Strict Release Windows passes with zero warnings/errors. The complete offline Android
APK passes384AOT/408ELF/1608static16KiBLOAD, v2/v3 signature, ZIP/CRC and nondebuggable
checks. Its SHA256 is9714ef2fce1211985818f3d0f2ca78f4021bf8ba500c4dd887e0841ec811aca7.
It is prepared, not installed over physical Main.

An independent SDK-only package references the actual current Core, with no Activity,
MAUI, Data, SQLite or financial writer. Strict27AOT/37ELF/138static16KiBLOAD/offline/
nondebuggable/v2v3/ZIP checks pass. Native APK SHA256 is
c6475b87e5c3bd9f69412dbb74165f2f39aa3755476093f73d55c63b71a95ebd.
Original x64 Mono SHA256 isedfa02c346f7eafc6cab665b27c667a93ecc84b7f40d042110ca4e343b2de8e4.
A shell/run/boot/UID/PID/ranchu/x64-bound provider and exclusive marker admit one
native run. All six new endpoint outcomes pass on actual API36/x64/kernel4096 in a
fresh independent AVD. These are six platform observations, not six more product tests.
The original framework/Core calculations execute; no replacement calculator is used.
Current source/compiler PDBs are retained, without a whole-PE identity claim.

## Preservation and retirement

All new controlled output roots are under the repository artifacts directory.
The fresh AVD boots under the existing scoped SDK with a private user/AVD home;
there is no SDK download, global environment/configuration write or ADB reset.
Original public settings, locale-key presence, size/density, boot and reverse state
match before/after calculation and fresh-package removal. No setting write or UI input
is needed. Only the fresh package is removed and its own emulator stops through its
console. Official scoped AVD deletion retires24files/4364317941logicalbytes, leaving
zero AVD files. Four standard Cleans pass after retaining used signed/source/compiler
and result evidence; Windows-only restore is for Clean, not another build. No
supplemental deletion or prior frozen-scope action occurs.

The retained5606/Q02 environment receives no input/provider/native read/lifecycle
operation. Physical Main and the prepared separate D928 ARM64 layout package remain
untouched. Host, Windows and complete shipping results do not prove physical ARM64,
original Home/Reports endpoint presentation, private Main data or whole-project
security/performance/stability/accessibility/platform/provider/release acceptance.
Those independent requirements remain open; the delivery goal stays active.