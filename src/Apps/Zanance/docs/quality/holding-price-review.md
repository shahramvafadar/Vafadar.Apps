# Original manual holding-price review (D-413)

## Complete operation boundary

Inspection found that the manual total-value form reread quantity only at Save, independently of the
original displayed type/history. Price deletion used only the row identity after confirmation. Both
price writers published Changed directly after committing, so a notification failure looked like an
unsaved operation. This is an original review/accepted-outcome change, not general crash remediation.

HoldingPriceReview captures type, complete type history, valuations and displayed locations in one
actual-file SQLite read. The existing price save/delete writers revalidate that immutable packet in
their actual write transaction, with fresh commercial/household authorization and a current original
draft/confirmation callback. New manual price identity/currency/source are bound and copied; consumed
reviews cannot be reused. No quantity, money, schema, compiled model, storage scale or entitlement is
changed by price registration/deletion. Existing direct price correction/import semantics remain.

The actual HoldingPriceEditor owns raw price/date/mode, coherent quantity conversion, pending delete
confirmation, departure and known acceptance. Source changes retire authority, retain raw inputs and
require explicit Refresh. Successful Save clears only its accepted raw price; deleting an old row
preserves a separate draft. Known post-commit notification or readback failure disables resubmission
and offers read-only Refresh. Busy refresh does not rearm Save early. An ordinary empty departed form
may read fresh history on return; a raw or failed draft still requires explicit refresh. Android
retained-tab departure retires the same authority after existing leave guards.

Feedback is beside the original price input. Three new captions are translated in all six languages.
No provider, permission, export, backup or private-data handling changes were introduced.

## Selective evidence

Fifteen distinct actual application/SQLite flow cases pass in retained selective cohorts: quantity,
currency and price changes, pending confirmation row/draft/departure, Save and delete notification
failures, failed readback and refresh-only retry, cancellation, invalid/range input, foreign identity
and consumed review, pending refresh, and empty versus raw native departure. The first13 cohort had
12 passes and one incorrect boundary specimen: 92233720368547.75 was valid. Only the affected method
was rerun with .76, preserving its original failure. The later two refresh cases and one return case
ran selectively after their changes. An attempted combined method filter selected zero tests; the
correct repeated method options then ran exactly those two. These are test/observer mistakes, not
product baseline failures.74 existing HoldingWritePolicy cases and21 resource checks pass. No full
suite or unchanged D-408..412 accepted money/matrices were replayed.

Windows original run95f69df7295041f19679c9e36d041773/PID47004 invokes the original stale Save, explicit
Refresh, accepted Save and original row delete once. Quantity changes2→3 before refusal; raw60EUR is
kept, refreshed and saved as20EUR/unit. The old25EUR/unit row is deleted through the actual original
row button and an explicit confirmation port; a separate raw70 draft is kept. Complete row refusal,
unchanged original events/basis50 and no ledger entries are checked. Three original live captures and
18 read-only en/fa/de, light/dark,360/412/1280 captures were inspected. Native OS confirmation dialogs
are not claimed by this port-based review.

Final Windows read-only continuation a14af15ab01f46d78389d4623eeaa783/PID28616 validates the original
completed receipts and same actual file, retains original Cases/PID47004 and performs zero commands.
Its18 inspected caption contexts preserve whole current rows and restore language/theme. Each run
uses its own non-input desktop; the owner's Default input desktop remains unchanged. Native caption
text/trim state and field placement are checked; these are not OS scaling or spoken-reader acceptance.

Android original run d71c7c2b800b47c1b67a3a579e165d39/comparison81a6f43694e7495d83a54ffe4706f26a,
PID22730, failed before the first button invocation: the observer selected both the main vertical
viewport and ChoiceChips' horizontal viewport. Original two events, original price and failure are
retained. Continuation selects only the vertical viewport and refuses reseeding or accepted-command
replay. It reviews the retained three-unit fixture, then changes only its type caption as a new
nonfinancial source-change control before the first pending native Save. This differs explicitly
from the original Windows2→3 quantity control; no original event or money is rewritten for observation.

Android retained continuation comparison a8ea63b276464696ad73165958c980f0/PID22978 succeeds with
three original command receipts, three live observations and six read-only caption contexts. Quantity3,
basis50, price20EUR/unit, original events and zero ledger rows are checked. The original price alone
is deleted and raw70 remains. Theme/language and independent base rows are restored. The initial
immediate PID query was empty; PID22978 was read from that same started launch, without restarting it.
The accepted Android report contains a vestigial File=price.png caption label, not an image; no Android
screenshot is captured or claimed. The final observer omits that metadata field without replaying UI.

Accepted independent signed offline x64 Release/AOT package is55,566,072bytes, SHA256
f1bd55086569e10d755fc2f74931d3acd935ea9e873a41dbb3da30529a6bd13a. Its signature, identity and
absence of Internet/debuggable flags were checked. The later metadata-only observer compilation does
not become new native financial acceptance. Final Main signed offline ARM64/x64 APK:
artifacts/android-holding-price-d413-return/pro.vafadar.zanance-Signed.apk,95,215,349bytes, SHA256
d2785f7a0ec5c91518e0abeb8ef7dff0d80b25541a7533b245340eceada9073b. Final strict Windows and Android
builds have no warnings/errors; tests were cleaned after build completion. Main upgrades without reset
on the owned Zanance_Storage_Review AVD, PID22114→23247. The first read observed Loading; the same
already launched PID later shows Home0.00EUR/My account, without restart. Four original OS values
1.0/0/null/0 and412x800/density160 remain exact, and the independent caller is stopped. No Samsung
input, Main private store/key read or phone/profile acceptance is inferred. The final package identity,
signature, absence of Internet/debuggable flags, ARM64/x64 engines and unchanged x64 native hash
20b009b7ce0fc50bf31fe6e321216085a2c9ee3f3e14e0eb6757597380c2c568 are verified. An initial local metadata
check assumed a different engine filename; only the zip-entry read was corrected to libsqlcipher.so.
The first Home assertion similarly ran while Loading and was continued read-only from the same PID.

## Limits

This is not physical Samsung, native OS confirmation-dialog acceptance, every holding/delete/Undo/
archive variant, iOS, release or general ANR/crash/A11Y acceptance. The D-386 priority remains in force.
The Samsung turn stays released; no phone input or Main private database/key access was performed.
Other approved ready development continues separately. Exact-head hosted CI remains a separate one-shot
check after push, not a substitute for runtime evidence.

Local evidence: artifacts/holding-price-d413, original/final artifacts/store-drafts runs, complete
signed APK directories. These private fictitious stores, full logs and derived pictures are not uploaded.
