# Shell-bound display rebuilds (D-705)

## Actual defects and implementation

The original full application on a fresh owned API36/x64/4096 emulator demonstrates
three separate ownership failures before the production change:

1. A theme rebuild queued at Home resumes after Settings has opened. It replaces
   the actual Settings page with Home without Back or Save. The retained original
   model still contains raw `123.4500`, but its form has disappeared.
2. An original report-navigation token survives normal root replacement. Its global
   in-flight count blocks the new graph; completing the old token then incorrectly
   retains the new graph's recipient tab.
3. A held old-root display callback adopts the newly published root. Global queue
   coalescing suppresses that new root's own request; releasing the old callback
   rebuilds a graph it did not originally own.

`App.xaml.cs` now keeps deferred/queued/report-handoff state per original AppShell in
weak-key storage. Report tokens capture their creating Window/Shell and complete only
in that live graph. Queued rebuilds capture their original window/root, reject retired
owners and recheck tab/editor eligibility immediately before replacement. Navigation
uses the actual replacement instance. ShellRebuilt is raised only for a successful
current replacement, not a deferred or rejected callback. Existing lock-queue FIFO,
report/forecast retained-work protections and native navigation retirement remain.
D-703/D-704 startup changes, storage, schema, money, translations and dependencies do
not change.

## Original native before/after

The private full-application caller adds only an explicit one-shot wait before the
original rebuild body plus bounded status/read/navigation hooks. Baseline PID4507 and
fixed PID5682 share UID10216, boot and all22 typed source collections/records plus four
catalogue values through one necessary signed no-reset update. Original startup
prepares empty data; normal ShowMainShell exposes the original empty Home without
completing onboarding, creating an account/profile or preparing financial fixtures.
The original optional notification offer is declined once through its actual button.

The Settings draft is assigned through its original bound view-model property, not
claimed as keyboard-entry acceptance. Actual original page/root identity, native XML,
full source and callback events distinguish the result. No Save/Import/Undo/payment,
financial report query/filter fixture or accepted historical layout matrix is repeated.

The fixed native cases prove:

- an old report token cannot block or retain a replacement graph;
- a current token still protects its original Transactions recipient until ordinary
  Home departure, using empty original tabs rather than replaying financial reports;
- releasing a queued rebuild with Settings open preserves the exact page/root and
  raw draft, with no ShellRebuilt event;
- ordinary original Back subsequently permits exactly one deferred rebuild;
- a new-root request survives an older held callback and executes once in the
  existing lock queue's FIFO order;
- with no new request, releasing an old-root callback has zero rebuild/root effect.

Full22-source/catalogue comparisons remain exact. Final original Home, System theme
and absent `ui.theme` preference are restored. Temporary app theme changes never alter
OS clock, locale, font, display, accessibility, TTS or timeout.

## Failed observations retained

An initial observer incorrectly expected a new-root request to run concurrently with
an older held job. AppLock intentionally serializes unlocked work in FIFO order; that
expectation is disqualified, the failed packet remains, and the same pending job is
released once. A separate no-new-request variant proves the old callback itself has
no effect. No queue policy is changed to satisfy the observer. One host Python path
omits the intermediate runtime directory and starts no Python/native action; only the unissued call is
continued with the actual executable. A final state-only packet has Source=null;
that means unread, so a separate full source observation supplies the comparison.
Neither observer failure restarts the application or replays accepted operations.

## Builds, artifacts and retirement

Strict Windows completes with0 warnings/errors. The full signed offline dual-ABI APK
is `89e837381203c79fd24fe56e8a7009df7238ec7e19598f5f95962098437ac519`,
99311519bytes,384 own AOT/408ELF/1608 statically aligned LOADs. CRC, ZIP16KiB,
v2/v3, non-debuggable/offline and actual manifest absence of private review authority
pass. Static alignment is not hardware16KiB acceptance.

Used private baseline `bff8349b200174accaa52c353534531257722adda34234390c93592f72583355`
(56881058bytes) and fixed
`6a1f242e525de814715837444a66261abc448f7b168d57af059aff82f05e46d2`
(56885154bytes) each pass192ownAOT/204ELF/804staticLOAD/offline/v2v3/CRC/ZIP checks.
The fixed original App source matches production after removing only its explicit
entry wait. No new managed unit-test count is asserted and successful old suites are
not repeated.

All original OS/boot/1080x1920/density420 values compare exactly. Six owned XML files,
148163bytes, are hash-matched before local/remote deletion. Only the fresh UID10216
is uninstalled. Host60488 exits, owned qemu and port5572 are absent before scoped
avdmanager retirement:25files5446513699logicalbytes. Three standard cleans succeed
with3161 registered deletion lines; Windows target assets are restored before its
pending Debug clean. These are not unique-file or host-free-space totals.

Automatic approval review rejects the sole10-target supplementary cleanup before
process creation with `blocked by policy`:5869files908636458logicalbytes remain
frozen. Targets are this cohort's private source/output root, residual AVD root,
used-source archive bin/obj copies and six unused publication files. Actual used
source files, signed APKs and evidence are preserved. No retry/subset/alternate/move/
escalation/reuse is allowed; earlier refused scopes are untouched.

The phone and D607's three deadlines, consumed reboot and unread post-reboot source
are untouched. This candidate is prepared, not physically adopted; adoption waits
for actual D607 completion/cleanup. The demonstrated ownership defects are fixed,
not an exclusive cause of D702 or general native crashes. Broader DB/key/security,
stability, two-second performance, accessibility, iOS, providers, Store and release
gates remain separate. The overall delivery goal stays active.

## Previous sole CI response

D704's sole exact-head response, actually22.9745minutes after scheduling, reports
[CI completed/success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41242034758)
and [CodeQL completed/success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41242034740).
Its marker/raw response/receipt remain and its schedule is deleted. No second GET or
D705 CI success is inferred.
