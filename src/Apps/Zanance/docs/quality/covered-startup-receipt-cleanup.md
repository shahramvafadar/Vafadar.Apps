# Receipt cleanup finishes under first startup cover (D-704)

## Actual ordering defect and correction

Deleting an entry intentionally retains its orphan receipts for short in-memory Undo;
durable import Undo has its own existing orphan protection. Retain that established
[receipt policy and its preserved negative fixture](ledger-undo-write-policy.md).
D704 does not reinterpret the old incorrect assumption that deletion removes receipts.

The original app purged orphan attachments at the end of its asynchronous starting
foreground task, after automatic posting, reminder refresh and automatic backup. The
interactive page was already published. An entry deleted while that delayed task was
pending could therefore leave a live Undo receipt that this later purge invalidated.
The starting flag also ran that purge after same-process Activity recreation, even
though in-memory Undo does not have process-restart semantics.

Run the unchanged original purge in the covered startup worker, after protected storage
preparation and before settings/page publication. Capture the existing process-ready
state on the presentation thread: after this process has already become ready, another
window's startup skips the purge. Resume/profile foreground posting, reminder refresh
and automatic backup retain their existing sequence without a delayed purge. No startup
financial database work moves into application/window construction.

Keep the existing non-fatal cleanup-error policy. Cancellation of the original startup
is rethrown through its existing ownership token; other non-OOM cleanup errors leave
startup able to continue. A pre-publication retry may retry an unfinished first startup;
no purge is scheduled over new live UI Undo after successful process readiness.

No SQL predicate, attachment format, durable-import protection, deletion/Undo writer,
eight-second offer, financial rule, migration, compiled model, dependency, runtime,
permission, resource string or visible layout changes. In particular, this does not
replace receipts with a new byte-snapshot or alter accepted financial data.

## Published-baseline native failure

A fresh owned API36/x64/4096 emulator starts the original protected application,
UID10216/PID4620. A private scheduling gate holds only the existing purge call before
its original SQL execution. Actual OnboardingPage is published and native XML agrees,
with process-ready=true and startup-not-busy, while purge entry1 remains held and query
execution0. The criterion requiring StartupPage until the first cleanup attempt ends
fails on the actual original application. This is an ordering failure, not an invented
receipt-loss observation or an accepted financial Undo test.

Read all22 typed source collections/records, including attachments, and four catalogue
preference values. Accounts, ledger and receipts are all0. Release that original query
once: it executes against the real original SQLCipher-backed store, removes0 rows and
all22 source collections/catalogue values remain equal. No receipt, account, financial,
profile, import, camera or onboarding-completion fixture is created or replayed.

## Corrected original native cases

A necessary signed no-reset update preserves UID, boot, all22 source collections and
catalogue values; Android labels its launch WARM and4620 becomes5224. The private caller
changes only the scheduling gate around the same original purge. Product startup code
used in that caller matches the production source after removing this gate substitution.

1. Hold the actual purge before its query. Original StartupPage remains visible,
   busy and process-not-ready. Compare all22 source collections/catalogue values with
   the baseline, then release once. The original query executes1/removes0 and only
   afterward original OnboardingPage is published. Source equality remains exact.
2. Change only the owned emulator's actual system font1to2, recreating its startup in
   the same5224 process. The new cover is observed and then onboarding returns; purge
   entry/execution counters stay1. Restoring the original font repeats no purge either.
   This proves the skip after process readiness, not successful financial Undo after
   recreation or general configuration/lifecycle acceptance.
3. After those cases complete and full source is preserved, explicitly stop only this
   fresh owned package to start the distinct non-fatal error branch. Actual pidof
   becomes empty and Android labels the next launch COLD, in5620 with the sameUID/boot/data;
   this is not natural cached death, reboot/idle or a response to an observation timeout.
   Hold the first cleanup attempt under its real startup cover, inject one IOException
   before SQL and release it. The original non-fatal catch permits onboarding; query
   execution is correctly0, failure-injected=true and startup-failed=false. All22 source
   collections/catalogue values remain equal. No installation, reset or successful
   financial operation is repeated for this separate branch.

These are original application ordering/error cases on empty financial data, not new
unit-test counts, actual orphan-row removal, receipt-byte restoration, a broad disk-fault
matrix or all Undo workflows. Existing unchanged deletion/import/receipt unit results
are retained rather than rerun. There is no new managed helper to stand in for the app.
D703's window/model ownership remains intact; this does not establish the cause of
D702's title-only condition or a general crash/ANR, security or performance correction.

## Strict builds and packages

The first corrected native build exposes one missed old named starting argument in
ShowCurrentProfile. Preserve that CS1739 failure; correct only the remaining caller
and continue only the failed build. No failed package is installed. Final strict native,
Windows Debug and complete shipping Android builds have0 warnings/errors.

Full signed offline candidate:
E:/Zanance.ReviewBuilds/D704-shipping-package/pro.vafadar.zanance-Signed.apk,
SHA256558de1bbf8f40383b27cd8dc9b5b69fe69c7546dbc14ee4974c608151e0565a3,
99307423bytes,384 own AOT/408 ELF/1608 statically16KiB-aligned LOADs. ZIP CRC/16KiB,
v2/v3, offline/non-debuggable manifest and actual absence of the review authority/provider
pass. Static alignment is not hardware16KiB execution or physical-phone adoption.

| Used private original caller | SHA256 | Bytes |
|---|---|---:|
| Baseline delayed foreground cleanup |3cfb810c6299d6b86e7887c71ab756dcb0a409054fbf65ae9584c35c10abcd70|56876962|
| Corrected covered cleanup |421f0d0425f563359e5c003bfc194a8835c7d329c99fa68fe165aef43f096834|56876962|

Both private callers have192 own AOT/204 ELF/804 static LOADs, valid v2/v3, ZIP CRC/16KiB
and offline/non-debuggable checks. The controlled release points are not product ports;
they do not create views, replace the cleanup query or enter financial/authentication data.
There is no new language/layout matrix, old successful native/financial replay, managed
unit-test count, Home timing or general API24/device/iOS acceptance claim.

## Restoration and cleanup

Original boot, locale, font, timeout, accessibility-enabled/service-list/TTS values and
1080x1920/density420 remain exact. Final original onboarding/source is observed before
retiring only the fresh package/UID. Five native XML files,84698bytes, match saved hashes
before remote/local deletion. The initial retirement shell wrapper is incomplete and
performs no native action; source/OS/uninstall markers are confirmed absent and5620 still
live before only the unissued retirement continues. No successful input/query is replayed.

Only owned emulator host32792 exits and its named official AVD is deleted:25files,
5431112763logicalbytes. Three scoped standard cleans pass with3161 registered deletion
lines. Windows assets are restored only for its target before its pending Debug clean;
no successful build is repeated. Those are not unique-file or host-free-space totals.

Automatic approval review rejects the sole eight-target supplementary cleanup before
process creation with blocked by policy:845files340167087logicalbytes remain frozen.
The scopes are the new private source/output root, residual AVD root and six unused
publication files. Used signed APKs, used source/receipts/typed packets remain outside.
No retry, subset, alternate method, move, escalation or reuse is permitted; earlier
refused scopes remain untouched.

The phone and D607's three deadlines, consumed reboot and unread post-reboot source
remain untouched. The prepared candidate is not installed there; adoption waits for
actual D607 completion/cleanup. Broader DB/key/security/stability/speed/A11Y/iOS/provider/
Store/release gates remain separate and the overall delivery goal stays active.

## Sole delayed D703 CI response

The exact-head single response shows [CI in_progress/no conclusion](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41239801653)
and [CodeQL completed/success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/41239801703).
Its consumed marker/raw response/receipt are retained and only its schedule is deleted.
No second request or inferred later main-CI pass occurs.
