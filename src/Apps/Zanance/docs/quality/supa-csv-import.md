# D-516 - Original SUPA CSV cashflow import

## Original evidence

Checked24 October2026 against Subsembly's [SUPA4.0.0 specification](https://subsembly.com/download/SUPA.pdf),
dated11 August2026, SHA256
`ab743e8df7dd336bd8dd6defda71bcabfad1bf39f0180286bb69d9ef09f58fe9`.
The original document states CC BY-ND3.0 DE; keep its unmodified private research copy and link
the publisher. No document, third-party implementation or proprietary SDK is bundled.

Sections3,4.4 and5.2 define UTF-8 comma-separated Ntry records, ISO dates, unsigned amounts,
explicit currency and CRDT/DBIT direction. Booking status defaults to BOOK when omitted; PDNG
and INFO are separate states. Original row/account identifiers are optional. Both true and false
BtchBookg values identify batch relationships. The [original Banking4 manual](https://subsembly.com/download/Banking4Desktop.Handbuch.pdf)
distinguishes its Subsembly CSV booking export from configurable visible-column export. Its
documented export options do not qualify direct-bank CSV schemas or every customer file.

## Application boundary

Implement the approved other-app CSV backlog through the existing import review/writer/Undo
pipeline. Require explicit source selection, ordinary posted-cashflow confirmation, complete
review, the same original target/file/authority and transactional revalidation. Refuse pending,
informational, return/reversal and batch/detail records; never guess transfers or linked refunds.
Unknown column contracts require explicit mapping. Do not add a second financial writer.

Use original Amt/AmtCcy without conversion and retain instructed original currency/amount and
source notes. Charges/tax metadata must not create another financial movement. Keep source account
identifiers out of stored notes; display only the identifier suffix when present. Source narrative
can itself contain personal references and remains local financial data under existing protection.

Stable reimport identity requires both original software row ID and a supplied source-account
identity, scoped to the chosen Zanance target. Length-prefix identity parts so embedded separators
cannot conflate sources. Without that evidence, keep distinct new row identities and surface equal
amount/date candidates for explicit duplicate review; do not silently discard equal real expenses.
Later user edits are never overwritten. Source categories are retained as notes, not guessed maps.

The separate chooser fixes SUPA encoding to UTF-8. The legacy encoding choice explicitly applies
to StarMoney. Preserve the original StarMoney action/contract, generic mapping, aggregate-overlap
decisions and durable Undo. Add a dedicated explanation/example in all six existing languages;
no new account, dependency, network operation, entitlement or commercial activation is introduced.

## Focused verification and preserved failures

Strict actual-source App.Tests builds have zero warnings/errors. Sixty-seven distinct new cases
pass in selected executions:37 flow,21 protocol/bounds,3 source-privacy and6 format/history-consent
cases. Twenty-nine affected StarMoney cases and23 resource completeness/usage cases pass. An
initial resource run outside the repository fails its ancestor-based RepositoryPaths lookup;
retain it as a runner-location failure, not a product or hosted-CI diagnosis. Two privacy cases
first fail against owner-account note leakage, then three pass after excluding structured AcctId
and Ownr fields. The six real translations explicitly bind repeat identity to the same software
history/account. No successful case is replayed to obtain the later results.

The original Windows page completes36 contexts: six languages, both themes and actual360/412/1280
widths. Native picker/consent, complete27-row paging with final-page navigation, full source/warning/
consent captions, Help/OK and original import-confirmation Cancel preserve every typed source row;
zero financial writes. PID30028 exits0. Visual inspection finds the new redundant Example prefix;
remove only that prefix in all six resources because shared Help supplies it. PID74676 checks only
12 changed narrow Help contexts, exits0, zero picker calls, unchanged original source. Preserve the
36 accepted contexts rather than repeating picker/consent/Cancel for a Help-only correction.

## Original Android source, terminal failures and retained continuation

The independent ARM64 Release/AOT caller compiles original production sources/XAML behind a new
SUPA-only package/phase. Its initial final-Help build rejects unguarded API29 WindowInspector use;
add the real API29 precondition. This does not qualify API24. Before any execution, the caption
observer also requires exact native text after the existing digit transformation, complete last
layout line, no ellipsis and visible bounds; geometry alone is not text acceptance.

After the owner restores the actual owned foreground, verify Main31197/D-515, Home979/all141 public
tuples, original three profiles and recorded OS. Record300000ms original timeout, temporarily set
only1800000ms under D-517 and read it back. No authentication, clock, other OS setting or private
Main database/key is accessed.

Use one independent UID10395/run d32510fc32a4436ca53794344030bc64 source with one fictitious zero
Checking account and two original cashflow source rows. No source preparation, account or financial
operation is repeated. Preserve these actual terminal observer failures and their immutable APKs:

| Process | APK SHA256 | Actual terminal observation |
| --- | --- | --- |
|27924|031d2fdee40e1fd60898e155776b29b532114257de59cbed30a8d362b99bca96|Single Label search is ambiguous because common WarningText repeats the preset warning; scope the observer to the original BankPresetReview owner.|
|29386|ca509069d3a4bf4ea5798e5881433b32853a821d80729b1b0579a9aec894257e|Consent observer wrongly interprets CompoundButton.PerformClick=false as no toggle; preserve the terminal packet and inspect actual state instead.|

The [original Android implementation](https://raw.githubusercontent.com/aosp-mirror/platform_frameworks_base/master/core/java/android/widget/CompoundButton.java)
changes checked state before returning click-listener handling. Never retry that click on false.
Require original focused/enabled/initially unchecked control, invoke once, then require native
Checked, original BankAcknowledged and CanRunImport. This changes only the observer. Stop only each
terminal independent process after preserving its complete packet, retain its source, then upgrade
that same package without reset and verify unchanged UID. A timeout is never treated as terminal.

PID30097/APK4f86ba6e22852a01e4e4e7d919e305e2386b493594afa3b22dfb168d3db8ad74 completes all six
en/fa/de and Light/Dark contexts at actual384dp, normal unchanged OS scale. Eighteen complete native
source/warning/consent captions, six full Help bodies/OK actions, six original picker-button actions
and six one-click consents pass. All six listener-handled values are false while actual Checked and
original import eligibility are true. Every typed fixture/base row and mapped-table presence remain
unchanged. No native Import/Undo or ledger writer is invoked; managed original-source tests cover
those financial flows. The selected-file effect is fictitious, so external picker/customer-file
acceptance is not inferred. Retain per-context packets and complete final report. No accepted native
context, source seed or financial command is replayed; incomplete failed observations remain separate.

## Complete signed candidate and original Main adoption

Strict Windows and canonical complete Offline Android builds have zero warnings/errors. The final
signed dual-ABI shipping APK is96,579,317 bytes, SHA256
`d10ed55e30ca8db3b83fad859c177669ac908018715e2a4d9fa8abda54a51b62`.
384 AOT/408 ELF,1608 statically16KiB-aligned LOAD segments, ZIP CRC/16KiB layout, v2/v3,
non-debuggable Offline permissions and the unchanged D-515 certificate pass. Static alignment is
not16KiB runtime acceptance. The final independent caller separately has192 AOT/v2/v3/Offline/CRC.

After retaining complete native source/report evidence, uninstall only the new independent UID10395
package and verify its absence. Return original Main31197. Confirm the three original profiles and
current Main through original UI, then perform one complete signed -r shipping upgrade without
reset. The installer exposes the underlying unrelated Business window; send no input there. Return
only the normal owned Main Activity. Its launch is WARM/626ms, not cold or complete loaded-Home time;
the first observation remains covered, and the same new31882 later exposes the original complete
Home. No timeout, empty initial frame or old ANR is relabelled as success or repaired by restart.

Main31882 matches Home979/all141 original public tuples, the same three profiles/current Main,
English/Light presentation, final installed APK and recorded font1.0/size1080x2340/density450/
accessibility0/empty services. The original SUPA chooser is present with a full-sized target and
its complete English Help matches the independent actual-source receipt; native OK and Back return
Home. Do not invoke the Main file picker or financial commands. No new Main profile is created or
deleted, and no full private database/key comparison is claimed.

Restore timeout exactly300000ms/read back, keep stay_on15 and all other recorded OS values. Verify
final Main31882/Home979/all141 nodes and original profiles again. Hash-match28 new remote XMLs to
local originals, delete only their exact D-516 names and verify absence. Retain local source/failure/
lineage receipts. Explicitly hand back the completed physical turn before host-only finalization;
subsequent other-project device work is outside this preserved final baseline.

## Cleanup and hosted boundary

Standard cleanup succeeds for all four dedicated test outputs, Windows, shipping and independent
native outputs. Remove only two newly owned unused shipping copies after exact path/hash checks,
97,187,510 bytes. Preserve the signed shipping package and three immutable actually executed native
APKs/failure packets; do not retry historical blocked deletions. No new SDK/dependency, schema,
permission, network flow, account, entitlement or commercial activation is added.

D-515/D-517 and the D-518 exact fixture-list correction are separate finished commits. D-518's sole
exact-head request observes [CI40123657768](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40123657768)
in_progress and [CodeQL40123657769](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40123657769)
completed/success. Consume/delete that monitor without polling or claiming final CI success.
D-516 local engineering/native delivery is complete; new-head hosted CI follows its finished push.
Original bank/customer files and external picker, API24/16KiB runtime terms, general ANR/crash,
whole-Home performance, spoken accessibility, Mac/iOS, provider/Store/expert and publication remain
separate open gates. No old successful business suite, financial cohort or rejected performance
experiment is repeated.


## D-526 - Later original Samsung financial acceptance

The owner-selected original SUPA file now completes one actual Samsung import, preserved cold
read and durable Undo with opening900 ->922.50 ->900, original full narrative and Main restoration.
This closes that finite original file/import/Undo subgate; customer files/banks/other relationships
and full platform/release acceptance remain open. No successful D-516 suite/build/financial cohort
is repeated. See [complete later receipts and limits](physical-supa-import-undo.md).
