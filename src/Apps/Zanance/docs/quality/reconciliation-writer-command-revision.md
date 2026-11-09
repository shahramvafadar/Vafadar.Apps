# Reconciliation command revision through the writer - D-943

## One actual missing boundary without a financial commit

A new original AccountDetail command confirms its reviewed adjustment, then edits
and restores the reason during original keyed-context acquisition. D940's native
answer guard has already returned; the original session only compares reason text
inside the writer. Restored text passes despite a newer monotonic reason revision.
The baseline reaches EF SavingChanges once. A pre-save sentinel throws there,
preventing any financial commit; complete migrated raw-table equality passes before
the failing expected-zero-save assertion. This proves the missed pre-save boundary,
not an actual baseline financial posting. The fixture is terminal, not replayed.

The original model now passes its existing captured AttemptCurrent predicate into
an optional session parameter. The session checks it before native consent, after
the answer and in the original store's validation callback. Original balance/date,
source/review/access/transaction and money checks remain; no schema or runtime/GC
change occurs. Unbound callers default to null and retain their existing callback
behavior. Known accepted-write handling is not changed or retroactively cancelled.

The unchanged Data writer validates before EF Save and again before Commit, then
publishes the committed outcome through the original committed-notification path.
This ordering is source evidence, not a replay of D941's accepted financial case.

Four selected new host contracts pass: corrected edit/restore-at-acquisition,
retired owner before consent, retirement during consent and still-current owner
reaching the genuine save sentinel. The last control prevents a trivial always-
refuse implementation from passing. Both sentinels stop before persistence; all
raw tables stay exact. No successful old financial/read suite is rerun.

## Original native post-consent boundary

One new original native command on API36/x64/kernel4096 uses PID4044/UID10216,
boot0cccc6d6-e064-4b98-b981-5076855be1e8,320x640/density160. Its disposable source
has one opening-zero account and no entries/plans. Raw observed4.56 and the original
reviewed reason are preserved. The actual original Record adjustment confirmation
is accepted once through its visible native button; no synthetic confirmation reply.

An AsyncLocal command-bound probe consumes exactly one original keyed context after
that answer, changes the reason and restores its original text on the page, then
attaches a pre-save sentinel. Incidental/background/source-query contexts cannot
consume this scope. The corrected original writer refuses before SavingChanges:
OwnedContexts1/SaveReached0/CommandEndedtrue, no financial acceptance. Ready/error
feedback keeps raw4.56 and the original reason; CanAdjustfalse requires a new review.
Full20typed source remains exact, SHA64407ec2bfd4fe079ba3c46c4d16051ce284c8fe5be7823db7d697cea1310b58,
with account1/entry0/plan0. Original root and public OS5/locale key presence/boot/
geometry/reverse restore exactly. This is a controlled original command/context
interleaving, not exhaustive race coverage or physical ARM64 acceptance.

## Builds, retirement and limits

Strict Release Windows and complete offline Android builds pass with zero errors
and warnings. Full shipping APKb39a520f20db743602ab74622c4ade4c5c46ec3f7e847bad9e89a65fff4b2e8c,
99573663bytes,384AOT/408ELF/1608static16KiBLOAD, v2/v3/ZIPCRC/nondebuggable passes.
The used private original-App APKcc9c924db7f654d3b33e18cb2454df2a5059331a1ecfbae667c1724d0e3b098e,
56934306bytes,192/204/804 also passes. Original Mono ARM64f03c/x64edfa is unchanged.
Four actual shipping and six private compiler documents match archived current
sources. This does not establish whole-PE or private Main database/key identity.

Two remote XML47352bytes are hash-matched, retired once and retained locally.
Only the fresh package is removed, its owned host70508 exits after one console stop,
and official AVD retirement removes24files/4507907339logicalbytes. Six small scoped
userhome files remain. The successful uninstall's absent-package pm path result1
initially stops the observer; only the unexecuted absence/stop continuation follows,
with no repeated removal. Five standard Cleans pass; no supplemental/frozen scope
is touched. Used signed APKs, source/PDB archives, raw packets and failures remain.
Existing ADB50972 and its original creation identity remain exact; no phone action.

D942's sole CI response at its due time reports CI in_progress and CodeQL completed/
success; the response also contains dependency workflows with their literal states.
No final CI result, second status request or successful-job rerun is claimed.
D941's investigated failed assertions and D942's two corrected contracts remain
separate terminal evidence; no successful financial fixture is replayed.

The whole delivery goal and original native fault/ANR, whole performance, independent
security/key/private DB, physical ARM64, accessibility, platform/provider/Store and
release gates remain open. Main, phone, terminal native records, retained5606/Q02
and frozen scopes are preserved. No owner authentication is requested here.
