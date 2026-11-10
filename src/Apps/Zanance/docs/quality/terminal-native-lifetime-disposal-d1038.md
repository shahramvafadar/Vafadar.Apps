# Terminal native lifetime disposal — D-1038

Later [D1041 cumulative Main adoption](cumulative-lifecycle-main-adoption-d1041.md)
reuses the exact qualified shipping bytes after owner unlock, without build/test
replay; one no-reset upgrade preserves all141raw Home attributes and public state.

[D1039 minimum-Android and ps16k qualification](terminal-lifetime-minimum-ps16k-d1039.md)
adds four current native contracts on each of two fresh platforms with one signed
package, exact source/restoration and no host/financial/authentication replay.

## Observed original failures

PersonalSyncLifetime.Dispose originally cancels and disposes its mutable current
source under the state lock, without recording a terminal state. Four new host
contracts fail on the original implementation, with no harness errors:

- A throwing cancellation callback prevents resource disposal.
- Resume after Dispose replaces the source before Cancel throws; a subsequent
  capture can regain foreground authority despite the reported exception.
- Resume reentered from cancellation changes the source being disposed and leaves
  a later capture failing with ObjectDisposedException instead of retired access.
- Repeated Dispose throws against the already disposed source.

The original Android implementation independently exposes all4conditions in
PID4096/UID10216, .NET10.0.14. In particular, the late-Resume row has
LateFailure=ObjectDisposedException and FreshAccessAllowedtrue. This is actual
component authority resurrection, not evidence of a genuine network request,
financial mutation, naturally delivered OS shutdown or historical SGen fault.

Dispose now marks the lifetime terminal, clears foreground authority and advances
its epoch under the state lock before any callback. Subsequent native notifications
are no-ops and all fresh captures refuse with OperationCanceledException. It detaches
only its original subscriptions, then cancels the exact captured source outside
the state lock and disposes it in finally. The first cancellation failure remains
observable; repeated disposal is harmless. Normal Change behavior and original
AppLock, networking, authentication and financial policies remain unchanged.

## Focused host and original native evidence

All4new host contracts pass on the corrected strict binary. The7directly affected
[D1037](native-lifecycle-access-notification-d1037.md) resource/access cases also
pass on that same binary after this source change:11distinct selected cases,
.NET10.0.12, zero failed/errors/skipped/not-run. No unrelated suite or successful
financial scenario is replayed. Actual host compiler checks bind the original and
corrected lifetime source and the new test input.

The independent offline Android package compiles the complete original App sources,
XAML and resources with one declared unavailable-authenticator substitution. Each
condition creates an actual PersonalSyncLifetime against original DI lock/profile/
location services. Four distinct instances exercise terminal cancellation, late
notification, reentry and repeated disposal; no app singleton is retired. The
only explicit original-failure cleanup closes the diagnostic wait handle created
by that condition. No account/PIN/plan/goal/ledger setup or authentication occurs.

After the complete first original packet is saved, a public-state read fails to
connect to ADB5038. The existing listener/process65060 and its exact creation ticks
remain live. A subsequent same-PID/OS/focus observation admits that saved packet;
no provider phase, installation, launch, server reset/reconnect/pair or successful
test is repeated. The transport failure remains preserved separately.

A copied upgrade-admission utility initially expects an obsolete result field and
refuses before any device command. Only that unexecuted schema guard is corrected
to require the actual four original failures. One no-reset upgrade then reaches
PID5687 with the sameUID10216 and boot9a53fe5a-2c7a-4057-a4c0-f32782a04888.
All4corrected native rows pass, keeping the first AggregateException observable,
disposing the original resource, refusing fresh capture and containing late/repeated
notifications. No stage or source setup is replayed.

Both original and corrected four-condition phases preserve complete sqlite_master schema and
all54table typed56row source, digest
`9ACAF48F948FD7ACABCE965B77920717B893F28920A84EF19726C078A6AEE88A`.
Accounts, entries, plans, allocations and authenticator calls remain0. The result's
PreparedHere flag refers only to the first private digest marker; it is not account,
PIN or financial preparation. Six actual compiler documents per native build bind
the three product and three declared private source inputs, not whole-PE identity.

Both used native packages are56676258bytes and pass192AOT/204ELF/804static16KiBLOAD,
offline/nondebuggable/v2/v3/ZIPCRC/alignment. Original package SHA256 is
`9b31ee0361e2551ee63a7c362f0d5fc116c4203f55f64e1475988656f8901004`;
corrected is`1f346433978d7d683da966d3ebb5693f082aa12d5e411c85d997e225538f41ed`.
The original x64 Mono slice remains unchanged. Static alignment does not qualify
hardware16KiB, all platforms or historical native-fault repair.

## Complete build, restoration and delivery boundary

Strict Windows and complete dual-architecture offline Android builds finish with
0errors/0warnings. Six actual compiler documents bind the same three named product
inputs across both full builds. Complete shipping is99622815bytes, SHA256
`48108592123b9ac61c89b0557889977709122a69f67df08affbf10c74702e3cc`,
with384AOT/408ELF/1608static16KiBLOAD/offline/nondebuggable/v2/v3/ZIPCRC/alignment
qualified and both original Mono slices unchanged. This cumulative package is
prepared, not Main-installed. No phone command belongs to this independent cohort;
the owner-unlock request remains separate.

Original emulator OS/key presence/locale/boot/geometry/reverse are exact. Only the
new package/UID retires. All four original emulator handles become signalled,
official AVD retirement removes24files/4502664485logical bytes, and original
ADB5038/PID65060 creation identity remains exact. There are no remote XML files,
supplemental/frozen deletions or retained5606/Q02 operations. Used signed packages,
source/compiler/results and original failure evidence remain. Five standard Cleans
retire9705registered files/1523573475logical bytes. 3431registered files,
25host-tmp files/3942147bytes and six userhome files/7732bytes remain. All logged
deletes belong to current owned output roots; no alternate cleanup is attempted.

The sole D1037response at2026-11-10T18:47:30UTC reports
[CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42875978813)
in_progress/no conclusion and
[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42875978821)
completed/success. The marker and original response are retained; no polling,
successful job-log read or rerun follows. This finished change has its own
single post-push check, without waiting idle for CI.

This closes the stated terminal-lifetime contracts, not full Android security,
private Main database/key equality, performance, accessibility, platform/provider/
Store/release or the original native-fault cause. Canonical parent States, D607/D752
terminal checkpoints, accepted successors, frozen scopes and the active complete
delivery goal remain unchanged.
