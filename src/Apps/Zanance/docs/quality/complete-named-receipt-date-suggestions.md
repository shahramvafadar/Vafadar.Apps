# Complete named receipt dates as unsaved suggestions (D-621)

## Observed boundary and intended extension

D-617's original forms identify Amount/Payee but not Date for28OCT2026. That date is
also the current day, so the visible date alone cannot prove extraction. D-615 had
intentionally added exact named-date recognition only as a merchant boundary; its
historical null-Date assertion was correct for that earlier scope and package.
The remaining QA-05 issue is transferring a complete unambiguous source date into
the existing suggestion/editor review, not changing account openings or saved money.

ReceiptParser already recognizes full day-first month-name rows for the header
boundary but FindDate only searches numeric forms. Extend only the latter's fallback.
Keep its entire numeric search first: the original Gregorian2000..2100 and source
Hijri1300..1500/nearest-calendar inference body are byte-exact. A valid numeric date
still wins regardless of where a named row appears. The merchant body, amount evidence,
32-field ledger/source/model and all financial writers are unchanged.

Only full standalone rows using the four already known forms d MMM yyyy,d MMMM yyyy,
d. MMM yyyy,d. MMMM yyyy are considered after no valid numeric date exists. Use the
same explicit en-US/de-DE/es-ES/fr-FR/it-IT/fa-IR source cultures, not current UI locale.
A spelled month identifies its calendar: Gregorian source years stay2000..2100;
Persian source years stay1300..1500 and use that explicit calendar rather than guessing
lunar from a word. Repeated equivalent rows yield one date; differing recognized
named dates yield no suggestion. Invalid days, missing/two-digit years, contextual
prefixes/suffixes and unsupported years are not guessed. This is not arbitrary
receipt-date disambiguation: the existing numeric precedence remains unchanged.
A null suggestion retains the existing form date; it does not erase an old draft.

## Failing regressions and finite related checks

24 new focused cases include abbreviated/full English, German punctuation/accent,
Spanish/French/Italian names, Persian month/digits, Arabic digits, leap day, invalid
calendar days, incomplete/contextual strings, year bounds, duplicate/conflicting
rows, original numeric priority and invalid-numeric/full-named fallback.13 fail
against the original parser and11 controls pass. Preserve that original XML/log.

After implementation, all24 new cases and9 existing merchant controls pass. The
remaining historical D-615 null-Date assertion fails exactly as expected, now seeing
its printed28October2026. Preserve this failure before intentionally replacing only
that obsolete assertion with the exact printed date and explaining the new scope.
This is not a weakened merchant/amount comparison or a hidden CI failure.

Continue only that changed assertion plus directly affected original date methods:
German/English/Persian numeric calendars, retained OCR-row date, missing-total date,
Spanish numeric date and the original French/Italian date-bearing cases.34 pass.
The successful24 new and9 merchant cases are not rerun after a test-only expectation
change. Final unique passing union is67:24 new and43 existing. No full-suite total,
unrelated amount suite, financial Save or old successful native source is replayed.
Exact original numeric/calendar/merchant method bodies have their own source proof.

## Necessary package checks and retained initial restore failure

The first Windows command scopes -f but not the multi-target restore property and
fails NU1201 before compilation while trying unrelated iOS27 target references.
Keep that log; scope VafadarMauiTargetFrameworks explicitly to Windows, without any
package/workload/global configuration change. That Release CI-mode build finishes
with0warnings/errors. Android's original single build continues independently;
complete Offline Release also finishes without warnings/errors. No successful build
is rerun to replace a wrapper result or to resolve an observation timeout.
After runtime verification, the class XML comment clarifies that nearest-calendar
inference concerns numeric years. An exact single-comment replacement witness keeps
all executable source bytes unchanged; no build or native success is replayed for it.

Full product:
`E:\Zanance.ReviewBuilds\D621-shipping-package\pro.vafadar.zanance-Signed.apk`,
SHA256 a9dda2960fc05607aa1de7c51cefc32e26ab91c3fd0fa994b670a17b5caefa2d,
97071007bytes. Own384AOT/408ELF/1608LOAD static16KiB, CRC/ZIP, offline/nondebuggable,
owner v2/v3 signature and uninstrumented original App checks pass. Selected linked
Core/Data/App PEs change; no false cross-package byte identity is claimed. Runtime
Mono/sqlcipher and compression witnesses remain recorded. Static alignment is not
hardware16KiB runtime or another platform acceptance.

## One new original native source, not a replay or an edited old frame

Create a deterministic fresh1200x1100 PNG from existing bundled Figtree fonts. Its
five complete rows are BAY MARKET,SYNTHETIC RECEIPT D621,27OCT2025,TOTAL EUR7.10,
FICTITIOUS TEST INPUT. Full text bounds are inside the original canvas and the actual
image is viewed. No accepted old photo/receipt pixels are cropped, edited or rescued.
Input39543bytes/SHA256
09d3c70fdab9fb401078aa08bd576b91aa612311f3222b507b21519734bcd67d.
Its past year is essential: the emulator's real local day is28October2026 and the
clock is never changed. No separate direct-reader/control/camera/OCR reread occurs.

Fresh Zanance_D621_NamedDate is API36/x64/page4096, boot
95c0bb6c-df63-466c-b6e7-240ad9b8b078, product UID10216/PID4878. A fresh UID on this
AVD is distinct from same-number earlier retired installations. One complete signed
installation and original ordinary launch only, no selector/run/source preparation
extras. Native baseline records the absent product/caller, all five settings and
physical1080x1920/density420, actually411.428571dp; no exact412dp or OS200% claim.

Create only D621_EmptyWallet,Checking,opening0EUR through original onboarding once,
with durable Start marker before the action. Its opening date remains28October2026;
do not adjust it to permit a later financial write. Decline the optional in-app
reminder offer with Not now, granting no notification permission. Initial Home0EUR
and actual All-history/All-types empty state are observed. This is one fresh account
metadata setup, not zero store writes and not a replay of a prior accepted account.

Push only this fresh PNG after absence/hash/length guards and bind the installed
base APK to the exact a9dda296 digest. Original Home receipt command/source chooser
and actual DocumentsUI UID10103 select that exact filename once. The ordinary
reader/authority/editor route opens New expense with source note explicitly naming
Amount,Date,Payee. Amount7.10EUR and D621_EmptyWallet remain. Without any transaction
Date-field focus or typing, native original boxes show Month10/Day27/Year2025, wholly
inside the viewport, and the actual Payee input shows BAY MARKET. This distinguishes
parsed date from the2026 default; it is not just a callback or parser-only pass.

Cancel and confirm Discard once. Final Home remains0EUR and All-history/All-types
is empty. There is no transaction Save/Import/Undo/payment, account-opening correction,
manual Use amount, stored-entry reread, camera or accepted old financial operation.
A past-date suggestion does not qualify writing before the account's opening or
change original financial validation. No full private database/key equality or
accepted monetary posting is inferred from these public UI observations.

## Exact owned retirement and cleanup limit

All captured settings remain exact, without unnecessary restoration writes:
font1.0,screen timeout2147483647,rotation1/user0,plugged-awake1,original1080x1920/420
and no overrides. Hash-match and remove25 owned XMLs/950020bytes and the one owned
input copy39543bytes, retaining local originals. Uninstall only this fresh product
including retained data. One AVD stop, actual absent serial/ports, official manager
deletion of24 files/5305807696 logical bytes and unchanged retained-AVD configuration
are verified. Physical Samsung/D-607, ADB servers and unrelated sources are untouched.

Standard Clean of only new Core-tests/Windows/shipping outputs removes2789 registered
files/879717062 logical bytes. Supplementary deletion of the four new residual roots
and two unused publish files is rejected before CreateProcess with `blocked by policy`.
All2727 residual files/542567717bytes remain. No subset, alternative method or retry
is attempted, and no earlier refused scope is included. The used signed APK, source,
original failures, focused results and native/checkpoint evidence remain. These are
logical counts, not a measured host free-space claim.

## Status and remaining gates

The finite named-date parser and one original English past-date image-to-form route
are implemented and verified.24 new tests are counted; six source-language parser
cases do not establish Persian image OCR or a six-language physical corpus. Month-
first/labeled/partial named rows, broader corpus/device/iOS behavior and all separate
security/stability/performance/accessibility/provider/Store/publication gates remain
outside this bounded evidence. No new UI layout/resource or EF model exists to
justify an unchanged screenshot matrix, migration/compiled-model rebuild or suite.

Physical no-reset adoption waits for actual completion of D-607's original future
cohort. Preserve its three deadlines, consumed reboot and source-unread state. No
phone command occurs here. Overall delivery goal active; TalkBack remains last.
Publish this finished batch and perform one exact-head CI follow-up after16minutes,
recording the response/consumption and deleting only its monitor on any outcome.
