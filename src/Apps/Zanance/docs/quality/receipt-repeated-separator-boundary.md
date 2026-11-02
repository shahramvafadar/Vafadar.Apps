# Incomplete repeated receipt amount separators (D-799)

## Actual defect and correction

2 November2026. A single kind of repeated separator previously passed the complete-
number grammar with a short final group. TryAmount removed every separator: for
example, 1,234,56 became123456 and 1.234.5 became12345. The original receipt reader
could consequently suggest a confident, much larger amount from damaged evidence.

In the original single-kind-separator branch, require a complete three-digit final
thousands group when the separator occurs more than once. Existing preceding-group
validation remains. This is a four-line guard/comment, not a new parser, currency
rule or financial writer. Single decimals, complete repeated thousands and a distinct
mixed decimal separator retain their original meanings. Invalid complete tokens use
the existing damaged-evidence Review path; do not salvage a partial number or silently
replace the damaged row with a later valid total. Merchant/date/source text and the
original explicit financial confirmation remain intact.

## Focused failure and verification

The unchanged production baseline executes24 new cases:20 fail and four valid controls
pass. After the guard, all24 pass. The cases cover eight malformed direct amounts,
their eight whole-receipt outcomes, four valid grouping controls, two damaged-plus-
later-valid totals and two Persian/Arabic digit-normalization inputs. Public test
method summaries are added after execution; assertions and executable logic do not
change afterward.

Six existing common-notation cases and19 existing insufficient/unsafe-evidence cases
also pass in separate exact method selections. Total49 distinct selected cases,
including24 new; no whole receipt suite or unrelated successful test is repeated.
The focused build and original Windows strict build have zero warnings/errors.
Both Android strict publishes succeed with no warning/error diagnostic. No schema,
model, migration, runtime, GC configuration or financial algorithm changes.

## Actual Android parser and source boundary

One fresh official API36/x64/ranchu/4096 fixture uses original current application
startup/DI/Core parser and OnboardingPage, UID10216/PID4491. A private run-bound owned
review invokes the production parser once for four new inputs: malformed comma and
dot totals both return Review/null/no candidates; complete1,234,567.89EUR returns
1234567.89 and123456789minorEUR; malformed total followed by7.10EUR stays Review/null
with only the valid7.10 candidate and both original evidence rows. The merchant header
stays NORTH HARBOR MARKET and no purchase date is invented.

The full20 typed source serializations before/after match exactly in the same process.
No account/plan/ledger setup, financial Save, authentication, camera, OCR, picker,
editor action, setting change or XML capture occurs in that fixture. This is actual
Android Core-parser acceptance, not a new OCR/camera/editor or broad receipt corpus
claim. Original five OS values/boot/geometry/reverse remain exact.

Complete shipping:99458975bytes, SHA256
fa32ba0a826e8f5376d96b26e2b4f0c2d2ccf7b9f8935d0d321024490e5f591d,
384 own AOT/408 ELF/1608 static LOAD checks. Independent native:56856482bytes, SHA256
dac6b90e007876488575482cf04cb2b1e74e7eef93800a8994a37323454ca65e,
192 own AOT/204 ELF/804 static LOAD checks. Both are offline/nondebuggable with actual
v2/v3 signatures, CRC and ZIP16KiB checks. Mono binaries retain their existing hashes;
static alignment does not accept hardware16KiB runtime or fix the original SGen fault.

## Physical adoption and retirement

The phone is unlocked and original Main29901/d5a59bff is foreground before adoption.
Exactly one install-r succeeds, then one normal launcher entry opens Main16195/fa32ba0a.
The immediate Home baseline and upgraded Home have all141 public node attributes
exactly equal. Main UID10380/CE155299/DE154603, public grant/stopped flags, all other
Zanance package identities/code, five OS values, boot and geometry remain exact.
No setting write or private Main database/key read occurs. This one complete shipping
upgrade also adopts D791's already-verified calendar-endpoint correction, without
replaying its tests or financial paths. Historical D790/D787 process/code observations
remain dated history. D752 gets a separate authorized Main-upgrade receipt; its
original package/checkpoints are not launched, released or rewritten.

Only two new owned phone XMLs,103864bytes, are SHA-matched before remote removal and
proved absent; local raw evidence stays. No profiles/financial/authentication success
is repeated. The native fresh package/AVD retires;26 files/4509546083logicalbytes are
removed, host73304/QEMU53628 exit and port5594 closes, sharedADB5038/PID66176 stays.
Four standard Clean operations remove24276 files/1811780506logicalbytes;8372 files
remain across those four new output roots. No supplementary deletion is attempted.
Used signed APKs, used private source and evidence remain; every frozen scope stays
untouched. Counts are logical bytes, not measured host free-space gains.

Original crash cause, complete two-second Home performance, private DB/key/security,
broader accessibility, legitimate visible PDF input, iOS/provider/Store/release gates
stay open. Accepted CSV/backup owner cancellations remain retired. Overall goal active.

Related: [D791 calendar endpoints](occurrence-calendar-endpoints.md),
[D793 accepted cancellations](physical-sensitive-cancellation-completion.md),
[current frontier](remaining-delivery-evidence.md).
