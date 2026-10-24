# D-519 - Isolated Android runtime and original SUPA large-text qualification

## Authority and scope

On24October2026 the owner explicitly accepted the presented SDK terms and authorized the three
free official packages only in `E:\Zanance.ReviewBuilds\AndroidCompatibility\sdk`: emulator,
Android24 default x86_64 and Android35 Google APIs ps16k x86_64. Keep the earlier licence refusal
and this later acceptance as separate events. Official stable package metadata resolves emulator
37.2.12, API24 image revision8 and ps16k image revision5. Existing official platform-tools were
copied locally to satisfy emulator SDK-root validation, with identical adb hashes; no additional
package download, general SDK mutation or global installation occurred. The original general SDK
licence digest is unchanged. Existing ADB server5038/PID76812 is reused without restart.

The source/paging/financial/phone cohorts accepted in D-516 are retained. This batch adds only
new double-size Windows page observations and first-run runtime checks on two new owned AVDs.
It does not replay accepted financial commands or touch the reserved physical phone.

## Windows page text

The existing original SUPA review fixture is cloned using a read-only SQLite backup, preserving
the D-516 original source digest. The clone uses the original complete typed-row checkpoint;
no source account or financial row is seeded again. A DEBUG && WINDOWS-only continuation adds
an explicit process-local scale2 mode and actual native font, layout and endpoint assertions.
Unchanged native Help and import-confirmation dialogs are deliberately not repeated.

All36 contexts (six languages, Light/Dark, actual360/412/1280 page widths) complete on PID67284.
The324 native caption observations have an actual font-size ratio2. The eight text blocks taller
than the viewport have their final line reached through the native Scroll pattern and captured.
Both footer actions retain enabled native targets at least44 units wide/high, complete allocated
captions and a reachable scroll viewport; the minimum viewport is586.67 units. All fixture/base
typed rows remain equal; financial writes and native Import invocations are zero. The terminal
36-context receipt is retained. The separately observed process exit code is unavailable, so no
numeric exit0 claim is made. The strict Windows build has zero warnings/errors.

Real Windows system text scale remains1; this is process-local layout stress, not actual Android
OS200%, spoken screen-reader or full accessibility acceptance. Source/warning/consent/footer
screenshots are retained; the long German word wraps onto its final line without ellipsis.

## Actual Android environments and original app

Use the complete unchanged D-516 signed Offline dual-ABI candidate:
SHA256 `d10ed55e30ca8db3b83fad859c177669ac908018715e2a4d9fa8abda54a51b62`.
Its prior384AOT,408ELF/static alignment, ZIP and v2/v3 checks are retained, not rerun.

| New owned AVD | Actual runtime | Page size | App PID | Result |
| --- | --- | --- | --- | --- |
| Zanance_D519_API24 | Android7.0 / API24 / x86_64 |4096bytes, KernelPageSize and MMUPageSize from actual process smaps |2754| Install, startup and bounded navigation pass |
| Zanance_D519_PS16K | Android15 / API35 / x86_64 |16384bytes from getconf PAGE_SIZE |4551| Install, startup and bounded navigation pass |

Each AVD has its own project-local user/data directory. Hardware acceleration is the already
available WHPX; no driver or system feature is installed. Observed display is320x640/density160;
this is a runtime smoke cohort below the application's360 layout-review minimum, not a new layout
acceptance matrix. API24 font_scale is unset, API35 reports1.0. No OS value is changed.

Original first-run setup is invoked once per new AVD, with one default fictitious My account and
zero balance. The API35 reminder explanation is an owned Zanance dialog: choose Not now without
opening or granting a system permission. Original Home0.00EUR, Plans empty, Budget empty, Reports
period overview, Transactions empty and Home return are observed. Both initial app PIDs remain
unchanged through final observation. No ledger entry, Import, Undo, plan or budget is saved.
The captured app logs contain no matching Zanance FATAL EXCEPTION, native fatal marker or ANR
for these bounded sessions; unrelated system Google API messages are not product failures.

Initial activity observations are API35 COLD2026ms and API24 TotalTime1117ms. These are activity
wait observations, not full covered-startup/Home readiness or a two-second performance result.

## Failures and boundaries

The first avdmanager attempt read the general SDK and rejected the image path before creating
an AVD. Its documented process-local tools-directory property selects the isolated SDK; existing
SDK files remain unchanged. The first API24 emulator process exits before guest boot because
isolated platform-tools is absent. Copying the existing official tools fixes root validation;
only the incomplete guest launch continues. No accepted app setup is replayed.

The first API35 foreground guard refuses the owned reminder dialog because its window title
lacks the package name. Hierarchy package ownership and focused activity are subsequently proved
before the original Not now action. This is a controller observation failure, not a product crash.
Legacy API24 adb shell cleanup returns transient255 during fast sequential calls; original
failures are retained, output-bearing presence checks and spaced commands continue only cleanup.
No app startup, navigation or financial cohort is repeated to repair the cleanup observer.

The cleanup covers33 owned remote XMLs:20 removals have final per-file hash receipts;13 are
already absent after the earlier interrupted cleanup, whose removal path required an exact hash
match. That partial absence is recorded separately, without inventing replacement receipts.
Local evidence is retained, and only
the two newly installed AVD app fixtures are uninstalled. Both owned emulators are stopped; the
SDK and AVD definitions remain useful for further approved compatibility work. The physical
phone, Main candidate/profile/data, desktop applications and existing ADB servers are untouched.

The result qualifies bounded x86_64 minimum-API and16KiB startup/navigation, not all native SDKs,
crypto/recovery/migration/backup/OCR paths, every library load, ARM64 page-size behavior, hardware
power loss, general ANR/crash freedom, performance, physical devices, OS scaling, spoken TalkBack,
iOS, providers, Store or release. No product schema, dependency, permission, financial behavior or
production SDK changes. New-head hosted CI remains pending until its own one-shot check.

D-516 exact-head `2c823a685511607b28d44dc63b0f3f74ebba0514` has four completed successes:
[CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40127810828),
[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40127810845),
[self-hosted service qualification](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40127810844)
and [local identity backend](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/40127810871).
The one-shot monitor is consumed/deleted; no second request is made.
