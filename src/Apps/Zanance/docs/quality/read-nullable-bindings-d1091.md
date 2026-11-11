# Cleared read-only sync and goal inputs — D-1091

Following [portable-backup validation](backup-nullable-bindings-d1090.md), two
original read paths still dereference native-cleared values. PersonalSync uses
DeviceName.Length during activation and Address.Length/Fingerprint.Length during
first-connection discovery; GoalDetail reload uses AmountText.Length. Missing
input must not become a source-read failure or escape as NullReferenceException.

## Focused original failures and correction

Eleven new host conditions reproduce seven original failures and four passing
controls: null device name on first activation/return, missing connection fields,
one partial connection and null goal amount fail. Empty and the other partial
connection controls remain valid. All eleven pass on the final host binary under
.NET10.0.15, with zero skipped tests. These are selected cases, not a full suite.

Three guards use string.IsNullOrEmpty. Missing device name retains the existing
translated default. Only a completely absent first connection borrows existing
public defaults; a partial raw address/fingerprint remains untouched. A cleared
goal amount initializes its independent money context while preserving raw input.
Transport, consent, identity, writer, funding, financial precision and receipt
policies remain unchanged. No valid credential, peer or financial write is needed.

Host sync uses the actual stores/coordinator with an explicitly synthetic local
environment: one public-default read at most, zero transport opens/remembered
peers/connection effects and no confirmation. Host goal uses one fictitious goal
metadata row with no account or ledger. Complete discovered typed source is
compared before/after each condition. Previous successful service, authentication
and financial suites are not replayed.

## Preserved native observer lineage

Initial PID3326/UID10216 passes the first seven sync conditions, each with complete
source equality, then refuses the partial-address condition's post-native strict
null assertion. The first failure and one1091owned XML are preserved. The original
page can normalize an absent value while attaching its native control; that first
failure alone does not prove which conjunct changed or an exclusive binder cause.
The correction independently checks exact null plus the other retained field
before attaching the page, then missing-or-empty plus the exact retained field
after attachment. No product/XAML change is made to satisfy this observer.

One necessary no-reset continuation to PID5567, on the same UID/boot, adopts the
existing account/goal read-only instead of preparing them again. Only the two
partial sync conditions continue; both pass before the first goal assertion fails.
That goal assertion incorrectly requires CanAllocate=false in a fixture that now
has a funding account. Original CanAllocate reflects action eligibility; amount
validation occurs in the original command. The no-account host fixture correctly
has false eligibility. Changing product eligibility to satisfy the observer would
change existing behavior, so it is not done. The remaining observer instead checks
eligibility is unchanged across the cleared-input read, without invoking Allocate.

The used packages/callers/PDBs and both actual failures remain separate. No passed
sync condition, metadata preparation or financial command is replayed. This is
versioned native evidence, not eleven cases on one final native binary.

A second necessary no-reset continuation reaches PID6017 with the same UID10216
and boot68f4b5af-55f4-4809-b4f6-22b54a4e2613. Only null/empty goal conditions run;
both preserve exact raw input, ready/no-read-failure state, original eligibility
and complete schema/typed contents. No allocation command is invoked. Thus native
coverage is seven initial sync conditions, two remaining sync conditions and two
final goal conditions, not reruns of successful observations. Both continuations
adopt the original metadata read-only. Source equality is checked within each
phase, not asserted across an unobserved upgrade interval.

The final18node/7058byte XML shows the covered loading surface after explicit model
Retire. It does not qualify ready/error caption visibility, glyphs or geometry.
Those limitations are retained instead of taking another frame for a pass. The
model readiness assertions precede retirement; actual Entry handlers and binding
values are checked in the original page. This is controlled binding/publication
qualification, not OS typing or production route acceptance.

## Packages, provenance and retirement

Strict Windows, complete default Android and optional-sync Android finish with
zero warnings/errors on project SDK10.0.404/runtime10.0.15. Both complete APKs
have384AOT/408ELF/1608static16KiBLOAD, v2/v3/ZIPCRC, no debuggable flag and
99643295bytes. Default SHA256 is
`9fbebe6bd1afccdb1313f7fd72489a0f18c5cd791bd513fcd69abb64bfffdc84`;
optional-sync SHA256 is
`1c1c0926333aba13cb1beea164b322923ee41e5c823b99684adfc1081779de17`.
The latter has INTERNET and no BILLING; it is not offline. Both qualified Mono15
payload hashes match. These complete packages are prepared, not Main-installed.

Three used native packages each pass192AOT/204ELF/804staticLOAD, v2/v3/ZIPCRC,
nondebuggable and INTERNET/no BILLING checks. Their hashes are initial
`5ad5de8598fafbd8ee74e2f609b11857b14fb4646cd2e0cfe294e7715acae12d`,
partial-input continuation
`b571e928ca94b8fd31663bb7a26c42611598c0a7c178c314ed00703d3b6cad1a`
and final goal-only
`9e296da6f651ca2d7480de3b450bd5323a090088fa69929cf99e0cb2e5656cdd`.
Fifteen exact actual PDB document bindings cover both product models in Windows/
default/sync and both models plus each caller in all three native versions.
This is finite source provenance, not whole-PE identity or genuine sync acceptance.

Only the fresh UID is removed, with captured OS/key presence/locale/boot/geometry/
reverse exact. Two hash-checked remote XMLs29002bytes retire; raw local evidence
remains. Both original emulator/QEMU handles signal before official AVD deletion,
retiring24files4575737161logical bytes. Six standard Cleans retire42670files/
2962459738logical bytes;5891files929785354bytes remain in those new build roots.
Signed used/prepared packages, archived source/PDBs and failures stay under
`artifacts/d1091-read-nullable-bindings`. No supplemental delete, frozen-scope
action or new policy refusal occurs. Main remains the dated D1085observation,
not fresh liveness or private database/key equality.

## Scope boundaries

The independent native review uses original PersonalSyncPage/GoalDetailPage and
models with actual Android controls. Its sync environment is synthetic and refuses
remote effects; this does not qualify a peer, TLS/pinning, credentials or genuine
synchronization. Native goal preparation includes one zero-opening account and
one goal solely to expose the original amount Entry; it differs from the host
no-account fixture. Direct metadata preparation is not a financial Save replay.

The adjacent Plans/Rate/Category submission trims already have original validation
guards, and Forecast name is assigned through validated nonblank input. They are
not speculative defects or changed in this batch. Other connected-input paths
without a reproduced failure are not claimed complete.

No phone command, Main upgrade, original fault interpretation, runtime/GC change,
frozen-scope action or terminal-cohort replay belongs to this batch. Canonical
parent States and the overall delivery goal remain open; this is not full
security/stability/performance/accessibility/platform/provider/Store acceptance.
