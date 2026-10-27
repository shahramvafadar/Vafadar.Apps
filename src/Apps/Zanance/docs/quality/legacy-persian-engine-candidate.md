# Reviewed legacy Persian engine and cancelled installation (D-587)

Status: legacy candidate closed by the owner's decision; newer compatible option
remains to be investigated. No Persian runtime or spoken acceptance is obtained.

## Concrete host review

The owner initially approves a temporary standalone eSpeak trial, engine/language
choice, exact original restoration and removal of only the fresh installation.
Before any install, confirm that `com.reecedunn.espeak` is absent. Review F-Droid's
suggested1.52.0/versionCode22 APK, SHA256
`0a7822fec54d7f7ae759ffedc9afeaa77f809587b171fb2101998787821ec790`,
10446659 bytes. Its signature verifies, and package/version/size/digest/signer
match the signed repository index. Verify the entry JAR's signature against the
officially published repository certificate fingerprint and the index's signed
digest/size. The metadata lists GPL-3.0-or-later and no recorded anti-features;
that is provenance evidence, not a vulnerability audit or Android16 compatibility.

The actual APK requests no permission, contains ARM64 and three other native ABIs,
has minimum21/target33, and bundles both Persian `fa_dict` and `lang/ira/fa` in its
optimized resource ZIP. Do not mistake absence of a literal assets path for
missing data. The matching source archive's digest/size are verified from the
signed index. Read only selected original source members without extracting an
archive into arbitrary filesystem paths. Voice-data setup reads the bundled raw
ZIP; no engine is linked into or shipped with proprietary Zanance.

## Preserve the real installation failure and owner choice

Dispatch exactly one fresh install after the initial approval. Play Protect shows
the exact eSpeak name and Unsafe app blocked, explaining the older Android target
and missing latest privacy protections. Reading More details exposes Install
anyway, which is not the earlier Zanance-only install-without-scanning option.
Do not silently extend that exception. The owner explicitly chooses cancellation
and investigation of a newer option, not a warning override.

Before cancellation input, the actual installer is already terminal with
INSTALL_FAILED_VERIFICATION_FAILURE and the prompt is gone. The current-window
guard refuses the stale intended acknowledgement; no tap is sent to another app.
Do not label that as an accepted cancellation tap, retry the install or uninstall
an absent package. The exact terminal log and absence of package/process are
retained. No engine/language selection, packaged-data setup, voice download,
audio capture, reader activation or financial command occurs in this attempt.

## Late platform normalization and preservation

The D586 restoration receipt was a point-in-time observation. A later preflight
finds `tts_default_synth` normalized again to the same Samsung SMT default; keep
the originally absent key as the restoration target rather than overwriting the
baseline. Read-only Activity/service inspection finds no remaining TTS Settings
Activity or Samsung engine service. No Activity re-opening, force-stop or system
process kill is needed. Restore only the original absence and observe null at
0.32/2.43/5.50/10.53 seconds, then verify all14 original values/presence. The exact
late writer is not proven; a finite stable observation is not a claim of permanent
global quiescence.

After the rejected attempt, verify MainPID22461/f748/UID10380/CE155299/DE154603/
permissions, English/Light/Home979/all141 public attributes, original caller
metadata/process absence and original14 values/presence. Do not claim survival of
D586 PID18235: the actual D587 pre-install checkpoint already has22461. Public
OS exit metadata records18235 at16:06:26.404 device-local with reason3 LOW_MEMORY,
importance400, status0, trace null and empty state. An initial parser expecting a
different heading returns no record; retain it and parse the actual ApplicationExitInfo
heading. Export no trace, memory/register/state bytes or private file. This is a
natural cached-process exit with selected public-state recovery, not a crash cause,
full database/key comparison, complete low-memory qualification or an effect of
the uninstalled engine. No Main reinstall, reset, financial replay or private
Main database/key read. Only owned diagnostic
XMLs may be hash-cleaned; preserve the used rejected APK, pinned source/provenance
and failure evidence. Previously refused cleanup scopes remain untouched.

## Recorder preparation without replay

Inspect existing container headers and executed-driver fingerprints only; do not
repair, trim, synthesize or rerecord audio to manufacture a pass. Three D585 drivers
configure a110-second capture but forcibly terminate after a12-second final tail;
their observed file-creation-to-outcome intervals are89.269/57.867/95.569 seconds.
Those intervals are before the configured natural limit. Their RIFF/data lengths
remain0xffffffff and the captures remain incomplete. This identifies a host-budget
problem in these three paths, not the cause of every historical capture failure.

The reviewed recorder source uses a wall-clock deadline, not a PCM-duration timer.
A future authorized cohort must record monotonic capture start, wait for that
actual configured deadline plus a bounded observation margin while Main owns the
window, and verify process exit/container finalization. A timeout is not permission
to restart, replay accepted gestures, invent speech or call a partial file complete.
No native replay occurs just to test this preparation.

## Primary sources and remaining boundary

- [F-Droid package and licence](https://f-droid.org/en/packages/com.reecedunn.espeak/)
- [Official repository signing identity](https://f-droid.org/docs/Release_Channels_and_Signing_Keys/)
- [Maintainer language catalogue](https://github.com/espeak-ng/espeak-ng/blob/master/docs/languages.md)
- [Recorder deadline implementation](https://github.com/Genymobile/scrcpy/blob/v5.0.1/app/src/scrcpy.c)

A newer compatible engine or supported voice still requires concrete review and
fresh owner approval before installation/selection. Do not re-sign or relabel the
blocked APK to evade the warning. Persian speech, general A11Y/stability/speed,
iOS/providers/Store/public release and the overall goal remain unfinished. The
separate stale CI-test expectation is handled by D588 without native-success replay.
