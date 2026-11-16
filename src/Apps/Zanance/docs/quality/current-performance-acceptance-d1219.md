# Current Android performance acceptance (D1219)

The owner selected about 2,000 ms for warm reference Home. After the current cold result was reported, the owner selected a 6,000-ms
cold target and explicitly requested further optimization. This qualification
uses current original product sources, including the accepted Home factory and
QuickResource stamp work. D1217 deferred binding and D1218 structural snapshots
are rejected and absent; no private optimization is promoted.

The overall delivery goal stays paused. Latest main is fetched and fast-forwarded
before execution: four incoming commits change dependency/CI documentation,
actions/cache and xunit.v3; Zanance product sources remain unchanged. Before
commit, three further Dastan timeline commits and the shared Temporal YearAxis
are fast-forwarded. No Zanance or relevant shared-library project references
Temporal, and no measured product input changes. Concurrent Dastan work and the
unrelated untracked benchmark source are preserved.

## Measurement contract

The reference is Samsung SM-A176B, API36, ARM64, actual 4,096-byte pages, with
10,000 fictitious entries, 20 accounts and 100 plans in one fresh independent
package. The existing owned API36/X64 2-core/2-GiB emulator retains its original
100,000 entries/200 accounts/1,000 plans; it is never reset or reseeded.

One process-cold launch per device follows confirmed cached state and two process
absence proofs after an owned `am kill`, without force-stop/reset. Device uptime
is read immediately before the normal launcher request. The first eligible native
ready draw requires the current Home, original successful publication, complete
visible non-ellipsized balance, actual window focus and no pending layout. This
includes launcher dispatch and earlier framework work; the uptime floor has
10-ms resolution. Android first-frame TotalTime is reported separately. The
internal bootstrap marker excludes earlier registration/framework work.

Five warm samples use original `//transactions` to `//home` tab navigation and
original Home OnAppearing/publication through that same native ready-draw guard.
They do not construct an alternate Home or warm the constructor separately.
Two new search literals use the actual native input connection once each. The
empty native caption and unique actual row button must be visibly rendered;
the editable input itself cannot qualify a result. The original row Id/title is
verified outside timing. Every individual sample is retained, including slower
samples; there is no outlier removal or rerun for a faster number.

The additional complete source guard covers schema/user/application header
cookies, sqlite_master DDL, table_xinfo, sqlite_sequence where present, all
application typed rows and all 32 ledger fields. A separate legacy guard proves
the retained emulator application source against its historical digest. The two
algorithms are not treated as the same digest. Source and original complete Home
projection are checked across the new workload. No financial UI command runs;
new fictitious reference construction is outside timing and not an accepted Save
replay. Main is neither launched nor upgraded.

## Current reference and stress results

All numbers below retain their actual process/binary lineage. The observer upgrades
change diagnostics only; selected actual compiler bindings prove the same original
App/Home model/Data inputs and stock Mono15. The extra warm observer frame is posted
after the original publication completes; it does not mutate model/layout content.
Native ready draw is not a GPU commit or pixel/glyph/whole-accessibility claim.

| Device / shape | Pre-launch to ready upper ms | Internal bootstrap to ready ms | Protected interval ms | Android first frame ms |
| --- | ---: | ---: | ---: | ---: |
| Samsung reference | 7434.370118 | 6743.560188 | 1862.237960 | 1281 |
| Owned X64 stress | 14214.929200 | 13214.722800 | 5910.669500 | 1793 |

These are one process-cold observation per device with warm OS/file caches, not
power-on/storage-cold distributions or percentile guarantees. The existing-task
forward warning is retained; two absence proofs and a new PID establish process
cold. The internal marker begins near the end of service registration and excludes
earlier work. Subtracting protected time does not isolate post-preparation: the
remainder also includes work before that interval. No exclusive cause follows.

The reference cold result exceeds the newly selected 6,000-ms target by
1,434.370118 ms. Cold acceptance remains open. Successful observations are not
replayed merely to obtain a lower number.

| Warm original Home sample | Samsung reference ms | X64 stress ms |
| --- | ---: | ---: |
| 1 | 784.649692 | 2415.518900 |
| 2 | 750.934576 | 1591.822300 |
| 3 | 757.707384 | 1756.190000 |
| 4 | 747.317961 | 2009.231900 |
| 5 | 746.634038 | 1500.713100 |
| Median | 750.934576 | 1756.190000 |

The Samsung median meets the selected approximate 2,000-ms warm Home target.
All five original completed projections, source cookies and complete source guards
pass. Stress results retain every sample without imposing an invented absolute
ceiling; two samples exceed 2,000 ms. Reported managed allocation is all-thread
allocation over the sample and peak self is process high-water, not exclusive
operation allocation, a leak finding or retained-heap attribution.

| Reference original search | Rows | Publication ms | Actual native result draw ms |
| --- | ---: | ---: | ---: |
| New no-match literal | 0 | 451.250999 | 483.823730 |
| New unique title 009981 | 1 | 404.880038 | 568.835038 |

Each query has one actual input-connection text mutation and original result
publication. Empty caption or original row title and accessible row button are
fully visible, rather than treating the editable input as a result. These are
controlled native commits, not human-keyboard timing. Reference source is exact:
`A9833C5C51A67D5E918C3C36AB88B08124A6C91D28BFCCBAA54520304DAE1708`.

## Failures and non-replay lineage

The initial private phone build refuses a Span across await; hashes are awaited
into arrays before the comparison. The original emulator build remains separate.
Ten actual compiler documents bind each X64 App version and eleven bind each phone
version, including the original reference generator. Used packages, original
compiled sources/PDBs and three own AOT members matched to each actual App APK
remain outside Clean. Every full private App APK is offline/nondebuggable,
192AOT/204ELF/804static16KiBLOAD, v2/v3 and ZIP CRC/alignment qualified. Actual
runtime pages are 4,096 bytes; this is not 16KiB hardware or whole-PE identity.

The first stress search preflight wrongly assumed QA06 identifiers in the retained
independent source. It refuses before text submission. The native tree proves its
actual repeated independent titles; corrected selection targets 195 original
matching IDs, rather than reseeding a unique row. That new original-source search
is interrupted by actual native exit PID8581, reason5/status11. No stress search
latency is accepted. [The bounded existing-exit correlation](current-search-native-exit-d1220.md)
reads that existing event without repeating the search/fault or any register
classifier. Post-inspection original restoration audits the complete retained
source and Home exactly, without another timing or search operation.

The first phone warm observer times out at its first native draw despite actual
ready Home being visible. No warm sample is accepted from that attempt. Binding
the listener to the attached current tree after original publication and asking
for one diagnostic frame completes the five remaining samples. This is an observer
correction, not an asserted product fix or proof of one exclusive timeout cause.

The phone no-match query completes in PID15164 and is retained. Unique title009982
publishes an actual visible one-row result, but the observer expects the row name
on the View instead of its accessibility node and times out; its latency is not
accepted. After one source-preserving observer upgrade, the saved no-match receipt
is read, never reissued, and only a different unique row009981 is measured in
PID18231 using the actual accessibility-node name plus visible title/button bounds.
The cold packet is from PID13916, warm/no-match from15164 and final unique query
from18231, all under the same independent UID10458/source/boot. There is no claim
that all samples ran on one final binary. The resumed final full-source check
closes the source interval across these observer upgrades.

An early host first-frame regex refuses extraction after the actual cold result;
the already saved raw launcher/packet is parsed correctly without a second device
launch. Early terminal window observation likewise precedes Launcher animation;
the same Home input is observed to complete, not sent a second time. These failures
and raw packets remain. No accepted financial Save/Import/Undo/setup, original
successful suite, old language/accessibility matrix or old one-shot interpretation
is repeated. Fresh reference construction writes fictitious fixture rows outside
timing; zero financial UI commands is not a claim of zero fixture construction.

## Terminal preservation and remaining work

Samsung returns to its original Launcher. Original screen-off timeout300000 is
restored exactly after the temporary test turn, with all captured OS/key presence,
locale, geometry, boot and reverse values equal. All three prior Zanance public
package UID/APK identities remain exact. Main is neither launched/upgraded nor
privately inspected. The fresh independent fictitious reference is retained for
the newly requested cold6second work; no account/ledger setup needs recreation.
There is no current authentication or owner-input waiter.

Four fresh remote XMLs totaling200692 bytes are hash-matched and removed; local
originals remain. Three standard Cleans retire27528 registered files/
1664473239 logical bytes from only these private App/SDK build graphs. Used signed
packages, source/PDB/AOT bindings and all raw receipts remain. There is no
supplemental/frozen deletion, alternate cleanup or unrelated-project mutation.
The original owned AVD shuts down normally with exit0, retaining its complete
source/data/keys and consumed markers. Shared ADB5037/PID59860 and original creation
identity remain exact. The full delivery goal stays paused.

Warm reference Home and these reference searches are qualified in their stated
scope. Cold6seconds and the interrupted100k search remain real remaining work;
original fault remedy, broad performance/ANR/security/accessibility/platform/
provider/Store/release acceptance are not inferred. No accepted old financial
operation needs replay to continue this work.
