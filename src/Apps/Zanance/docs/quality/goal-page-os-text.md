# Goal-page native OS text and toolbar height (D-449)

## Reproduced native defect

On the owned independent Android emulator at actual OS font scale2.0 and360dp, the original
Persian savings-goal toolbar title has exact text and one native line ending at58px, but its
TextView receives48px with zero compound padding. The native full-rectangle and ellipsis checks
alone miss this last-line clipping. English body/list/detail observations passed before this
Persian failure; they are retained rather than replayed.

The owned AccessibleToolbarTracker now measures the existing native title after child layout.
It grows only its native toolbar when the scaled line needs more height, retaining the native
font, arrow, title, theme and navigation handlers. Each tracker owns its observer and unsubscribes
on retirement. Requests are monotonic within one toolbar lifetime and deduplicated: do not compete
with framework layout by shrinking inside a global-layout notification. A new toolbar starts from
its own ordinary baseline. There is no financial, schema, parser, permission or runtime-policy change.

The pinned MAUI10.0.110 ShellToolbarTracker owns the native toolbar lifetime and page changes:
[upstream implementation](https://github.com/dotnet/maui/blob/10.0.110/src/Controls/src/Core/Compatibility/Handlers/Shell/Android/ShellToolbarTracker.cs).

## Retained original native coverage

The independent Release/AOT caller compiles actual production pages and renderer. It reads the
previous complete fictional secondary source and the already empty independent base; it does not
seed another account, replay a payment or clear data. Actual OS scale2.0,360/412dp, en/fa/de and
Light/Dark cover empty/filled Goals and earmark/quantity detail publication. Loading, complete
captions/rows, unsaved raw draft bindings, local read failure, native Retry and native Back use the
original models and controls. Visible native targets retain at least44dp. Text checks require exact
original content, full native visibility, no ellipsis and enough height for the final native line.
The filled-list navigation frame checks detail readiness only; separate detail frames cover captions.

Each width has24 immutable complete context receipts containing108 observations. At360dp,
36 initial English observations and72 subsequent Persian/German observations are retained. At412dp,
72 initial observations and36 previously incomplete German observations form the completed scope.
The final360dp continuation reads all complete receipts and produces zero new observations.
Neither an already completed context nor an accepted financial command is replayed. The caller can
retain a completed Retry if only a detail's departure/report remains unfinished.

Each context checks original source and independent base rows, with AcceptedWrites0. The final
report is written durably before its public derived log output. A compact report-reader verifies
that exact final file and all24 complete context receipts, then emits counts and a report digest;
it never repeats native actions or reads the shipping app's private database/key.

## Failures and evidence limits

A bounds-only listener initially misses title-child remeasurement; the global-layout listener covers
it. Two qualification candidates are explicitly aborted after their UI becomes unresponsive before
final report delivery. Runnable main-thread state and a null UI hierarchy are recorded; they do not
prove the internal cause or establish that the title-height implementation caused or solved general
Android lifecycle/GC/ANR problems. Successful observations and original data are preserved.

The final412dp continuation returns to a responsive original Home and writes its complete report.
Two of112 public log chunks are lost in a burst. The compact read-only reader verifies the durable
file and complete receipts instead of rerunning scenes. This is an observation-delivery repair,
not another application scenario or managed test. Empty filtered log output must truncate the host
capture, otherwise a previous process's log can masquerade as current output; PID filtering and
explicit file replacement prevent that stale-observation error.

The finite scope proves the observed goal-page geometry. It does not accept overall stability,
startup speed, low memory, speech/intelligibility, full accessibility, physical Samsung, iOS,
cryptographic key hardware, providers, stores or product publication. Existing failure evidence
remains open under the owner's final-qualification order. No new managed test count is claimed;
successful financial/backend/resource suites are not repeated for this native-only change.

## Main delivery and restoration

Windows strict build passes with zero warnings/errors. The final complete Main Offline APK is
96,989,087 bytes, SHA25638f1dc7a16fe5c2475779e38c60db92ab4588e150bed485fa489e3f9d279f1a2,
with384 AOT entries, arm64/x64, verified v2/v3 signatures and no INTERNET permission. It upgrades
Main without clearing data. Original empty Goals is read through the actual Insights tab and returns
to the same Home139 nodes; My account/0.00EUR and the original display remain. No Save is performed.
The superseded intermediate Main build was deliberately stopped before delivery; none of its
artifacts was installed. Exact emulator font1.0, physical412x800,
density160, accessibility0/servicesnull are restored after each completed/aborted scope. The previous
Main Home139 native nodes remain identical excluding transient focus/drawing-order fields. No phone
input or desktop/system-settings change occurs.

The durable final report digests are360dp DE374C27AB267F0AA68F2FA86742AB5B136FC620F7FD5B30078BF31E9EC3034C
and412dp A6433C8A6A1A4F835453EF67A969A34190E9CF4BB4C9493446A9ED66D5C66315. Each compact
receipt verifies24 contexts/108 observations/zero writes and source/base equality. The final360dp
pass has zero new native observations; the final412dp continuation has36. Those counts are native
observations, not newly added managed tests or a new all-tests baseline.
