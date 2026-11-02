# Rejected Home per-account balance reuse (D-758)

## Observed boundary and fixed admission

D754's retained trace locates original LedgerCalculator.Balance in Home projection.
Current Home shares one AccountEntryIndex, but computes an included account's ordinary
balance in currency totals and again for its later account row. The private candidate
retains the first actual result only within that unchanged projection. It keeps the
original Balance formula, checked total order, confirmed-only calculation, excluded
account fallback and final ToDictionary duplicate-id refusal. Reference identity keeps
distinct account records with the same id separate. Nothing is cached across loads.

This is distinct from the rejected [D538 index constructor](withdrawn-account-index-loop.md)
and [D552 cross-algorithm shared index](home-captured-index-projection.md). Neither is
reopened. There is no financial formula copy, truncated source, parallel worker,
schema/migration, runtime/GC/permission change or financial write.

Before execution, require at least5% median component latency improvement for both
10000-entry/20-account and100000-entry/200-account shapes, at most1% allocation growth
and64MiB pair working-set growth. Six fixed alternating pairs, four iterations per
sample and one warmup per role precede a second independent process only after first
admission. Native admission separately requires at least5% complete original Home
command-to-ready improvement in both orders and shapes. Candidate-minus-original
after-PSS/RSS-high-water and candidate own PSS growth must each be <=65536KiB. The first
tenfold pair must pass before reverse/reference/phone; no retuning or resampling rescue.

## Private host and actual product checks

Two actual .NET10.0.14/X64 processes use pure in-memory Q02-shaped objects and the
original Core index, scope, balance and unreviewed-count functions. This is the balance
composition, not the whole Home/read/render task. The workload is all-confirmed;
1536 separate finite contracts cover all eight financial kinds, dates, review flags,
exclusion/archive, repeated records, checked overflow and unchanged input objects.
No app fixture, financial Save, migration or key setup is replayed.

| Process / entries | Original median ms | Candidate median ms | Reduction |
|---|---:|---:|---:|
|71092 /10000|1.22455|0.9486125|22.5338%|
|71092 /100000|7.3622375|6.4074375|12.9689%|
|18696 /10000|1.399175|1.0232625|26.8667%|
|18696 /100000|6.5418375|5.931325|9.3324%|

Allocation growth is0.0618%/0.2051% in the first process and0.0124%/0.2051% in the
second; all fixed host gates pass. Samples/allocations vary within each process and
are retained, not filtered or converted into a predicted native gain. The second
process repeats timing, not the1536 successful contracts. The initial private switch
expression fails compilation and is parenthesized before any host execution.

The proposed product routes only Home's existing worker through one scoped capture;
the original public total API stays available. Preserve its currency-key lookup before
Balance so an invalid currency still fails before a later balance overflow. Fourteen
temporary new actual Core cases and26 affected original cases are selected. First39
pass; one new test incorrectly sets an invalid currency before its fixture builder's
minor-unit conversion. Preserve that arrangement failure, fix it and rerun only the
failed case successfully. Forty distinct product cases pass, not41 distinct cases.
Strict Windows Release and full dual-architecture offline shipping build have zero
warnings/errors. No unchanged full suite or financial acceptance is replayed.

## Native input admission before installation

The first caller manifest path and baseline source removal each fail before any
installation. Correct only those unissued build boundaries. Actual compile-item
inspection then selects exactly the archived original LedgerCalculator and HomeViewModel
for baseline, while the candidate uses the proposed production sources.

Initial signed callers pass package checks but their actual Data PEs differ; reject
them before installation. A read-only metadata inspector first refuses ambiguous
nested-type identities. After including the declaring-type chain it finds22669
matching method bodies/headers/exception metadata, but unequal GUID/table metadata;
that is not whole-PE equality or permission to bypass the byte comparison.

Use a common compiler source map and, for the unchanged Data compilation only, the
original compatible Core reference in its original reference-list position. A first
append-at-end reference attempt remains unequal. Preserving order then produces
actual byte-equal Data, SHA256
44f87d71756338d236425eef3edee25725920ddb2fb372b73a30214ef5dda965,
including the final linked caller inputs. The unchanged foundation also compares
byte-equal. This is a controlled comparison build; ordinary shipping and caller PEs
are not all claimed equal. Core/App intentionally change. No binary or financial
source is rewritten to force a comparison, and no exclusive explanation for every
initial metadata difference is invented.

Final callers also have identical manifest, resources, DEX, retained source, runtime
configuration and selected non-AOT native inputs. Original Mono/compression match the
actual official runtime14 package. Each caller has192 AOT/204ELF/804 static16KiB LOADs;
the shipping candidate has384/408/1608. CRC, ZIP16KiB, offline/nondebuggable and v2/v3
checks pass. Every packaged AOT corresponds to its own emitted loaded segments and
bytes, excluding only the ELF header affected by stripping; whole-file equality is
not claimed. Static alignment is not hardware16KiB runtime qualification.

| Final package | SHA256 |
|---|---|
|Ordinary shipping candidate,99454879bytes; never installed|bf93a4408ba659b6c40395a5b67a6dd81f3d4b6be5d9069f95e7007dd0a6399f|
|Controlled original caller,63594473bytes|3cb994b008eed562345f0608d2f98cd17bf27410d13ba1598d460d9c008e9d87|
|Controlled candidate caller,63598569bytes|b201ceeee5b7b3d94eec7e05de71a7e1c0b30d58c8a288ff0cb3a61eb892f0b7|

## First native pair and terminal decision

Only fresh Zanance_D758_HomeAccountBalance/emulator5584/API36/x64/4096 is used under
shared ADB5038. The retained D743 fictitious asset,37273600bytes/SHA256
aef54ce6b4fd62648a3034db64413291e631dd2269df8a774fb7383d54da513e,
is copied once through original covered startup. Its100000 entries/200 accounts and
manual, notification-disabled plans are not financially reseeded. Decline one actual
owned Not now rationale; no permission is granted. The first ordinary launch's
notification dialog is not Home presentation acceptance.

Original4882 and candidate5328 retain UID10216, CE361905/DE66527 and boot
2ac5cccd-2242-4f88-b9fe-55a5c5034f98. The sole candidate upgrade is signed-r without
reset; its normal launcher starts the new process once. Observe ready Home before
provider source checks, without repeating preparation or replaying the old pre-startup
observer failure. No phone, authentication, clock, idle or reboot command occurs.

Both original command tasks succeed with original Home ready,200 account rows and
100000 complete snapshot entries. The original complete post-startup SQL/control
verifier passes before/after; all32 ledger fields match the same snapshot JSON hash
405A7BA1C142A8F22C7BDF201E6F9FC5E3E602FC38B9CF9BC809048FF0798564.
The two visible ready-Home XMLs captured before timing are byte-equal. This is not an
independent observation of every offscreen account row or a ready-frame timestamp.

| Role | Original Home command-to-ready ms | Before / after PSS KiB | RSS high-water KiB |
|---|---:|---:|---:|
|Original4882|777.1095|505635 /403483|756668|
|Candidate5328|803.5409|389582 /397551|574316|

Gain is-3.4012453%, failing the required >=5%. After-PSS difference-5932KiB,
high-water difference-182352KiB and candidate own growth7969KiB pass the memory gates.
The initial PSS gap116053KiB prevents exclusive code/order/GC/leak attribution. Keep
the fixed latency failure regardless: no reverse/reference/phone, resampling, retuning
or rescue is run. Neither sub-second emulator result accepts physical two-second Home
or general performance/stability. Host gains are not an adopted product improvement.

## Exact restoration and cleanup

Archive the rejected proposal, restore both production files byte-for-byte to their
published originals and remove the temporary API-dependent tests. Git confirms no
product/test changes: zero new permanent cases. The shipping candidate is never Main
adoption. Fresh package/UID is removed; owned alarm/pending/posted records are absent.
Three hash-matched XMLs/101643bytes are deleted. All five OS values,320x640/density160,
boot and empty reverse map remain exact. Own launcher51576/QEMU60312 exit; shared
ADB5038/PID66176 remains. D752's separate17567 was read-only observed at01:18UTC,
still held before its first SDK request; this is not a later continuous-liveness claim.

All cleanup succeeds:12 standard Clean commands remove70180files/4215195681logical
bytes; official owned AVD deletion removes25files/4855510309logicalbytes;25 separately
validated supplementary scopes remove6709files/1368321201logicalbytes. Only new owned
paths are targeted after absolute-path/reparse/signed-artifact guards. Five used signed
APKs, source archives and local evidence remain. There is no new cleanup refusal;
every older refused scope stays frozen and untouched. Logical bytes are not measured
host free-space recovery.

D752 still requires genuine owner readiness before its first sensitive SDK request;
its latest actual status remains held, requested/completed0. D749/D751/D607 terminal
acceptances are not replayed. D756 and D757 each consumed exactly one CI response and
deleted only their own monitor. The full delivery goal and broader native reference
origin, physical speed, security, accessibility, iOS/provider/Store/release gates remain
open. Further optimization requires a genuinely different evidence-supported premise.
