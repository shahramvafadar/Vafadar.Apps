# Original Android toolbar retirement (D1144)

The [D1142 bounded observations](retained-footer-memory-native-continuation-d1142.md)
leave several weak Shell targets alive after ordinary collections, without proving
their retaining owner or the later signal's cause. This step examines one concrete
application path: AccessibleToolbarTracker subscribes to the global Translator and
unsubscribes in Dispose. No weak-subscription patch is promoted merely from that
source pattern or from a foreign framework issue.

## Exact dependency disposal sources

Four additional public sources at the installed MAUI10.0.120 repository commit
`6aca5366139ed0e1cd05cca3e32b6a887d5a8dd0` match the actual Android package PDB:

- [ShellFragmentContainer](https://github.com/dotnet/maui/blob/6aca5366139ed0e1cd05cca3e32b6a887d5a8dd0/src/Controls/src/Core/Compatibility/Handlers/Shell/Android/ShellFragmentContainer.cs)
  has OnDestroyView recycling/disconnection logic, without the old OnDestroy override.
- [ShellSectionRenderer](https://github.com/dotnet/maui/blob/6aca5366139ed0e1cd05cca3e32b6a887d5a8dd0/src/Controls/src/Core/Compatibility/Handlers/Shell/Android/ShellSectionRenderer.cs)
  calls its cleanup from OnDestroy/Dispose; its realized-root cleanup includes toolbar-tracker disposal.
- [ShellToolbarTracker](https://github.com/dotnet/maui/blob/6aca5366139ed0e1cd05cca3e32b6a887d5a8dd0/src/Controls/src/Core/Compatibility/Handlers/Shell/Android/ShellToolbarTracker.cs)
  removes its registered callbacks and clears context/page/native references in its disposal code.
- [ShellItemRenderer](https://github.com/dotnet/maui/blob/6aca5366139ed0e1cd05cca3e32b6a887d5a8dd0/src/Controls/src/Core/Compatibility/Handlers/Shell/Android/ShellItemRenderer.cs)
  contains its own destruction cleanup. Source presence does not prove all those paths execute.

As in D1140, only these public compiler inputs receive the known LF-to-CRLF
conversion before complete SHA256 comparison; financial source is not normalized.
The cached MIT package and both exact current Android assets targets resolve that
same dependency. The historical [issue31716](https://github.com/dotnet/maui/issues/31716)
is closed as a duplicate/not planned and is not evidence of a missing current fix.
No framework patch, dependency update or runtime/GC change follows.

## One actual native registration question

The new independent fresh ARM64 package uses the original App.CreateShell and
ReplaceRoot methods with an empty fictitious source. This controlled root port
avoids onboarding completion, notification offers and automatic-backup effects.
It is not another C7 context or a replacement for its remaining16conditions.

The first strict private build is never installed. Before the sole native turn,
an observer precondition is corrected: managed Home readiness does not by itself
admit native tracker construction. The final observer awaits the actual original
registration before capturing only weak tracker references. The relevant changed
observer is rebuilt; successful old tests or native inputs are not replayed.

Final APK SHA256 is
`a5750c9f861fb81230a4f9b5fafc3bad9099c7a0219e935c893fed95391f5a83`,
54,296,624bytes. Strict192AOT/204ELF/804static16KiBLOAD, offline/nondebuggable,
v2/v3 and ZIPCRC pass, with stock15ARM64 Mono unchanged. Nine actual App/library
compiler documents bind the original root, renderer, translator and observer.
Initial host assets-selection assumptions fail after those source checks; their
refusals remain. Only the pending exact project-path/assembly-name metadata
admission is corrected, without repeating the nine successful checks or a device
input. The NuGet project name is Vafadar.Zanance.App and its actual project path
is the private NativeReview.csproj. No whole-linked-PE identity is inferred.

Samsung is actually awake/unlocked on its existing wireless transport, with no
new connect/pair/server operation. Fresh UID10457/PID16106 uses the original boot
`6bd9dccf-976f-4a2c-afa0-bfceba0af22e`. On one original root replacement, the
captured old toolbar's global translation registrations fall from1to0. The roots
differ and one weak tracker is still alive at observation. That weak liveness is
not a leak proof; the observer deliberately retains the old root for identity
comparison and performs no forced collection. Reflection is restricted to the
named event and context metadata; no generic object graph, heap, address, register
or private Main data is exported.

The complete original typed source/DDL guard remains
`34C925C660BEE76749F9119D197D4EB0B8A20795798B502011EE92928E0363B1`.
Accounts/entries/schedules stay0; there is no financial or genuine-authentication
action. The actual normal path detaches this registration, so no speculative
application patch follows. This does not qualify C7/x64/100k retention, every
destruction path, native collection, the signal's cause or its remedy.

## Exact return, cleanup and owner-requested pause

The fresh package is removed after the terminal observation. Main15295 returns
normally; its selected raw Home XML and every native attribute map match. Original
public packages/UIDs/inodes/grants and captured OS/locale/boot/geometry/reverse
match exactly. Two remote XML files/103,864bytes are hash-cleaned; local raw stays.
No Main upgrade, private data/key read, settings change, owner prompt or successful
financial/test replay occurs. The retained C7 emulator stays off and untouched.

One standard Clean retires13,692files/809,505,326logical bytes. The reviewed
supplementary removal retires only the new completed build root and three unused
publish files:969files/236,822,753logical bytes, all4targets absent and zero failures.
Used signed APK, source/PDB/dependency proofs and actual terminal receipts remain
under `artifacts/d1144-shell-toolbar-disposal-source`; old frozen targets are untouched.

The owner requests pausing after this current question for an application restart.
The saved handoff preserves the D1142retained manifest,20qualified/16remaining C7
contexts and all terminal operations. No next cohort starts before that pause.
The original fault/performance/security/accessibility/platform/external gates and
full delivery goal remain incomplete.
