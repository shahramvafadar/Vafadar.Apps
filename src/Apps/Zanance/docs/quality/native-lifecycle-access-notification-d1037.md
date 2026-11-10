# Native lifecycle access notification ownership (D-1037)

## Original failures and correction

Original App.OnSleep publishes PersonalSyncLifetime.Suspend before dispatching
AppLock.SleepAsync. A throwing cancellation callback escapes that first call and
prevents the native cover request. OnResume similarly lets a failing Changed
subscriber skip its access transition. These are observed managed lifecycle
defects, not an established cause or repair of the historical SGen/native fault.

NativeAccessLifecycle isolates fallible lifetime publication from dispatch of the
original Sleep/Resume operation. Its dispatched task contains ordinary observer
failures instead of letting them escape an async-void platform callback. Only the
exception type is reported, without messages or private state. Failed publication
or dispatch does not permit new foreground work. Authentication, grace, source
ownership and financial policy remain unchanged. PersonalSyncLifetime.Change now
disposes its retired cancellation source in finally when Cancel throws, retaining
the original failure. Its separate Dispose method is unchanged.

Three new actual lifetime resource cases originally fail for Suspend, Resume and
RetireNavigation. The final strict host binary passes7new selected cases: three
resource contracts and four access-dispatch contracts, on.NET10.0.12. No old
successful suite or financial scenario is repeated. Strict Windows and complete
signed offline Android builds finish with0errors/0warnings.

## Original native methods and retained source

The private Android package compiles the complete original App source, XAML and
resources. One declared unavailable-authenticator substitution and private wrappers
invoke actual protected OnSleep/OnResume methods. Wrappers catch synchronous
failures for observation: this is controlled original-method evidence, not a
naturally triggered OS lifecycle crash or genuine authentication.

Original PID4919/UID10216 exposes AggregateException on sleep, no cover,
IsLockedfalse and one read-only sentinel effect. Resume exposes IOException with
the existing cover locked. One necessary no-reset correction reaches5560 with
the same source. Its observer wrongly expects unlock during resume notification
failure and fails without an accepted partial result. Original Shell navigation
also retires sync lifetime and encounters that same subscriber: the safe result
is retained cover. This failure and first XML remain. A helper-only no-reset to5984
corrects the criterion; two notice calls prove the queued continuation reaches
original navigation, which safely retains its cover.

All3final native conditions pass on.NET10.0.14: cancellation failure on sleep,
Changed failure on resume/navigation, and dispatched AccessChanged failure. Each
has no escaped failure, visible native PIN cover, locked state, zero read-only
sentinel effects and refused fresh access. Removing the fault listener permits
only original fictitious PIN cleanup, without owner credentials or SDK prompts.

Only baseline saves OnboardingCompleted metadata and one fictitious local PIN.
Upgrades adopt the same source without setup replay. Complete sqlite_master schema
and all54table typed56row source equal before/after every condition:
`B52ECAC43490F8B9B1A95D808AFB337250B65C4279A0E25C91956D02289B905D`.
Accounts, entries, plans, allocations and authenticator calls all remain0.
This does not establish Main private-database/key equality.

Private compile refusals for the actual Cancellation property and ambiguous
Android/System OperationCanceledException occur before installation and remain
preserved. Actual compiler bindings cover12original,13corrected and13final private
documents. Each full product build binds App.xaml.cs, PersonalSyncLifetime.cs and
NativeAccessLifecycle.cs. A short Windows App.xaml.cs suffix is ambiguous with
WinUI; the same PDB resolves with the full project-relative suffix without rebuild
or test replay. Finite bindings are not whole-PE identity.

## Packages, phone readiness and retirement

Three used native packages pass192AOT/204ELF/804static16KiBLOAD, offline,
nondebuggable, v2/v3, ZIPCRC/alignment, with original x64 Mono unchanged.
Complete shipping is99618719bytes, SHA256
`92cd76dc760a1b1b13a34d2e32c68fddbdad25e4eacd36ea5abf2e451159b839`:
384AOT/408ELF/1608static16KiBLOAD and the same policy/signature/package checks pass;
both original Mono slices remain exact. Static alignment is not hardware16KiB or
every-device qualification.

Shipping is prepared, not installed on Main. Read-only readiness at
2026-11-10T18:23:35UTC finds Samsung locked, Main PID24358 and the exact existing
ADB identity. No install, launch, app input, setting or authentication follows.
The owner alone unlocks the phone; independent work continues. Last complete Main
adoption remains [D1036](lock-cover-access-notification-failure-d1036.md).

Original emulator OS/key presence/locale/boot/geometry/reverse restore exactly.
Only fresh package/UID10216 and one hash-bound remote XML/7093bytes retire; local
raw evidence remains. Four original emulator handles become signalled; official
AVD retirement removes24files/4543558921logical bytes. ADB5038/PID65060 creation
identity stays exact without reset/reconnect. Five standard Cleans retire5667files/
1125967532logical bytes;7474registered files,29host-tmp files/3947629bytes and six
userhome files/7732bytes remain. Used signed/source/compiler/result/failure evidence
remains. No supplemental deletion or retained/frozen cohort action occurs.

The sole D1036response reports
[CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42866200863)
in_progress/no conclusion and
[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42866200777)
completed/success. That check is consumed; no polling/job-log/rerun occurs.
The first documentation patch has an invalid backlink context and writes nothing;
only that unexecuted edit is corrected, without runtime/cleanup replay.

This closes only the observed lifecycle/access-notification contracts. Native-fault
cause/remedy, broader performance/accessibility/platform/provider/Store/release
gates remain open. Canonical parent States, D607/D752 terminal checkpoints,
accepted successors, frozen scopes and the active full delivery goal remain.
