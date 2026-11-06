# Original loan estimates at the calendar horizon (D-924)

## Observed failures and product correction

Three new original Core cases fail with AddMonths/ArgumentOutOfRangeException:
December9999 day30/day31 and an October9999 multi-month schedule. Two additional
original-view-model/SQLite cases fail Ready: November's available last installment
and December's unrepresentable next-month start. These failures precede the product
change and are retained. No financial operation is used to establish them.

LoanSchedule now distinguishes IsDateLimited from the existing underpayment and
600-installment guard. Schedule stops before calculating money for an unavailable
dated row, retaining every feasible installment, exact principal/remaining/interest
and original first-date month-end anchoring. A payoff on the last supported date
remains complete. The existing50-year cap retains precedence when both bounds
would be crossed by the next row. No dates are clamped into fictitious installments.

The original reviewed reader uses ScheduleFromNextMonth rather than adding a month
before entering the calculator. At December9999 a cleared debt remains paid,
underpayment keeps its old outcome, and a positive amortizing balance has an empty
date-limited estimate. Existing interest and accumulated-money overflow guards stay.
There is no model/migration, ledger writer, rate/annuity formula, clock, entitlement,
source ownership or confirmation-policy change.

Both original projections, AccountDetailViewModel and LoanScheduleViewModel, choose
the new Loan_DateLimit caption instead of incorrectly attributing the calendar
boundary to repayment beyond50years. English/Persian/German/Spanish/French/Italian
translations are supplied through the original resource process. Layout, commands,
installment writes and the essential underpayment warning are unchanged.

## Focused host and build evidence

Fourteen new Core cases and seven new original flow cases pass after correction;
the latter cover all six translations and the separate empty final-month schedule.
Three existing directly affected accumulated-interest overflow cases pass. These
are21new tests plus3existing selected tests, not a full-suite or financial replay.
All tests are host x64; they are not all executed natively on each Android ABI.

Strict scoped Windows and full offline Android builds pass without warnings.
Initial Windows restore accidentally included the ambient iOS target and failed
NU1201; the subsequent explicit Windows-target build passes. The private Android
observer's extra closing parenthesis fails compilation before deployment; that
local observer syntax is corrected before its sole installation. Fresh AVD's first
public readiness attempts encounter offline/boot-not-ready; the same original boot
continues, without reconnect/reset/relaunch. Keep these separate from product tests.

Full signed shipping APK SHA256:
f2c1835f349a01856f36bc9b4d8f08828fb26ffaf50887d948cb995d64a06643,
99491743bytes,384AOT/408ELF/1608static16KiB LOAD. Independent native APK SHA256:
04c1c3f0e33d6877c22c5bf6c69acfecfd111cf066693fc8b64ac9c63ce1e41c,
56745756bytes,192AOT/204ELF/804static16KiB LOAD. Both pass v2/v3, ZIP/CRC,
alignment, offline/nondebuggable checks and retain the original Mono. Static16KiB
alignment is not a runtime16KiB acceptance claim. Shipping is not installed on Main.

Six actual portable-PDB document checks match the four changed original source
files and two private observer/bootstrap files. This is document-checksum evidence,
not whole-PE identity. Current original app/XAML sources are linked into the private
app; only the loan reader receives a private fixed UTC clock. Global app licensing,
startup/authentication clock and actual Android system time remain original.

## Original native pages, complete source comparison and retirement

Fresh Zanance_D924_LoanCalendar/emulator5612 is actualAPI36/x64/kernel4096 with its
original320x640/density160 geometry. One launch, UID10216/PID4926, prepares one
new fictitious loan metadata row and no ledger entry or schedule definition.
No financial UI Save, installment payment or accepted old fixture is replayed.

The same process displays four distinct current native contexts in order:
November9999 LoanSchedulePage with one valid installment; AccountDetailPage with
the new warning; then December9999 LoanSchedulePage with zero installments and
AccountDetailPage with that warning. The two loan-reader dates are private injected
conditions, not device-clock changes. All four original projections are Ready,
not failed, with the expected summary. Each current native name/summary TextView
has fully visible bounds and ellipsis0. No earlier successful context is repeated.

The full original20component typed source is captured after the single fixture
preparation and compared exactly before normal return to original onboarding.
SHA256BA9CFD9ACAD93CFE0B8A29FEEAB6A663AD965501EA90C9838EDCF3CD9C34727C
remains identical. This includes an empty ledger, not a populated32field financial
corpus or Main database comparison. Native six-language/theme/OSfont2/ARM64
matrices and whole accessibility are not claimed by these four observations.

Only fresh UID10216 on5612 is uninstalled; original public settings/locale-key
presence/boot/geometry/reverse match. Retained5606 public state and Q02PID15609
remain unchanged. The fresh emulator parent78104 is stopped through its own
console after actual creation/name admission; an initial pre-action typed-JSON
timestamp comparison refusal is preserved, with no stop before correction.
Official scoped AVD deletion retires26files/4413208080logicalbytes, zero AVD files
remain. Original ADB5038/PID50972/creation2026-11-04T17:19:21.4173469Z remains.

Six standard project Cleans pass. The first Windows Clean partially removes output
then encounters NETSDK1005 because the Android assets target is current; explicit
Windows-target restore allows completion without another build/test. Used signed
APKs, compiler PDBs, exact sources and observations remain. No supplementary delete,
frozen-scope action, old successful suite, phone command, auth/photo/provider,
financial UI operation or ledger write occurs. This fixes the verified date boundary, not the original
native fault, ANR, whole performance/security/platform/provider/release gates.
