# Transaction activation failure retirement - D1133

## Actual original failure and correction

[D1129](transaction-keyboard-owner-retirement-d1129.md) and
[D1131](samsung-keyboard-owner-retirement-d1131.md) qualify constructor and departure
cleanup. They do not qualify a failure while the page activates its model before
assigning a new read task. The original RefreshForNavigationAsync marks publication
active, then calls SetActive, acquires the viewport, subscribes Undo and updates
its caption before assigning PendingPublication.

On actual Samsung ARM64, one bounded original model PropertyChanged callback
throws its supplied IOException while SetActive(true) publishes CanAdjustFilters.
PID7406 preserves that exact error but leaves page/model active and the previous
completed PendingPublication stored. A subsequent current refresh returns that
same completed task; the read is not ready and no new publication appears. The
original packet remains Passedfalse, with complete source and route restored.
This is an original component error-path observation, not a naturally occurring
OEM fault, an authentication operation or a diagnosis of the historical native crash.

Only TransactionsPage.xaml.cs changes. A synchronous activation exception now
retires partial publication through the existing retirement method before being
rethrown. If retirement also throws, an AggregateException preserves both exact
errors in activation/cleanup order. The next current refresh can create a new
read. Original asynchronous read/inline failure policy, retired-Shell checks,
financial commands, model/schema, geometry/IME/window policy and runtime/GC remain.

## Three final actual component conditions

One same-UID no-reset continuation reaches PID8631 with all three conditions on
one final private binary:

| New condition | Actual result |
| --- | --- |
| Model activation callback fails | Exact error, page/model inactive, slot clear, new current read ready. |
| Later UndoText callback fails after acquisition | Viewport slot really owned inside callback; owner released, original padding retained, new read ready. |
| Activation and retirement callbacks both fail | Both exact supplied exceptions retained in order; authority inactive and subsequent read ready. |

Callbacks count1/1/2 respectively. Every recovery returns a task distinct from the
previous completed task and displays the retained entry. No old constructor,
retirement, normal keyboard matrix or accepted financial/authentication/photo
control is replayed. These are three finite native component controls, not three
new permanent unit tests, every layout/error route or general stability acceptance.

## Versioned source and admission limits

The separate package pro.vafadar.zanance.keyboardactivationreview is absent before
one installation and gets UID10454. Its retained D802 input is copied once: one
account's20fields, nineteen categories and one Income1234minorEUR entry's32fields,
asset SHA256126a2a28124f3b21d2ecf9ddb59b7836263a28262dea0476797272357bd043b1.
Only fresh onboarding/default-account metadata is arranged once; the two necessary
private upgrades require existing markers/rows/metadata read-only. There is no
financial Save, import, new corpus, account preparation replay or Main private read.

An initial proposed layout-failure control requires a filter tap after retirement;
source review finds filters disabled/results hidden and rejects it before installation.
The corrected model-control observer initially refuses at navigate in PID5682,
before any injected callback. Its exact failed subcondition is historically unknown;
waiting for actual PendingPublication completion/current period/retained row resolves
the admission in the continuation, not a claimed exclusive historical cause or
product startup repair. PID7406 is the actual original failure; PID8631 is final.

Complete actual schema/application-table typed source including all32entry fields
is identical before/after all three used versions and across their continuation:
`5545FF53C72883E66FEB1AE53642F81BA2DF3C34A93C5E9DBE03F3270CFDB773`.
There is no value/cookie normalization. Fresh profile metadata need not equal a
previous cohort. Original UI-thread startup/current Home gates precede controls.
Fifteen actual compiler documents bind each used version; the four original XAML
items are admitted initially and project/XAML remain unchanged. This is selected
source provenance, not whole-PE identity.

## Builds, exact return and retirement

Strict Windows and complete offline Android builds pass without warnings/errors.
Actual Windows/shipping page PDB checksums match the correction. Shipping is
99651487bytes, SHA256
`26ade48ff690dcd0159a24eb619dfafd19309c3531c7958f7380dc0911f2caf0`,
384AOT/408ELF/1608static16KiBLOAD, nondebuggable/offline, v2/v3 and ZIPCRC qualified.
Three used private ARM64 packages pass192AOT/204ELF/804static16KiBLOAD. Stock15
ARM64/x64 Mono hashes remain unchanged; no dependency or runtime substitution,
old managed suite, model/migration or permanent test-count change occurs.

Fresh UID10454 is removed after terminal source/route qualification. One normal
Main return retains PID20264/c44e6ebb. Original raw XML and all141nested Home
attributes are exact, as are captured public packages/UIDs/data inodes/grants,
OS values/key presence/locale/boot/geometry/reverse. Only the fresh fixture's
original reminder introduction is declined once; no permission grant, setting,
genuine authentication, Main upgrade/private DB/key, camera or financial command
occurs. Three remote XML files/111747bytes are hash-cleaned; local raw remains.

Four standard Cleans retire28586files/1970845385logical bytes, with3609registered
files/586670876bytes remaining. Fifteen explicitly validated unused publish copies
retire589237957more bytes; all four actually used/final signed APKs remain exact.
The pre-delete PowerShell parameter refusal is retained and corrected before any
issued marker/deletion; it is not a policy refusal or frozen-scope workaround.
Source, PDBs, signed versions and all actual failed/successful packets remain under
artifacts/d1133-transaction-activation-owner. C7 stays off; Q02/frozen scopes and
terminal cohorts are untouched.

The prepared shipping package is not installed on Main in this step. Main20264 is
only the dated return observation, not future liveness/private source equality.
Original native-fault remedy, whole performance/security/accessibility, later iOS,
provider/Store/release requirements and the full goal remain open.
