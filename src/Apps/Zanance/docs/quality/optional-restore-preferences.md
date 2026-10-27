# Optional portable restore preferences (D-575)

## Actual defect and correction

The original D574 owned 10000-row protected package omitted the optional
`display-settings.json` source. Original restore preflight threw `JsonException`
before reserving restore ancestry. In C#, the conditional expression selecting
`ReadOnlyMemory<byte>` or `null` selected the implicit nullable-array conversion:
an omitted source became present zero-length memory. A direct reproduction
confirmed `HasValue=true` and length zero; original package preference capture
reproduced the same JSON failure.

Centralize original validated-package preference lookup in
`SqlCipherRestorePreferences.CapturePackagePreferences`. Start with explicitly
nullable absent memory, assigning only an actually present entry. Use it in
protected active-profile preflight, the original generation-aware receiver and
key-loss recovery staging. An omitted source preserves existing allowlisted
choices. Present empty/malformed JSON still refuses, explicit null still removes
only its allowed preference, and device security is never imported. No schema,
financial algorithm, permission, feed, SDK or network behavior changes.

## Selected verification and original native outcome

Four new AT-575 cases cover omitted optional data, present empty/malformed JSON,
explicit removal, retained omitted keys and excluded device security. The genuine
omitted-source regression failed first; the other three cases passed. After the
fix only that failed case ran again and passed. All four distinct final behaviors
passed across those selected executions; no full-suite count or replay is claimed.

The original independent Samsung UID10381/profile was created once. Before
continuing the failed restore, original native metadata had to prove a single
selected initial history, zero restore intents and an empty fresh target. A
second durable request belongs only to that verified rejected preflight; uncertain
or previously accepted restores refuse. The corrected original whole-package
receiver returned once and complete copied typed rows matched the retained donor.
Subsequent observers never restored, seeded or posted money again. Later native
source checks match every prepared typed row, financial/read revision and final
published cache cookie. Retained protected donor files remain unchanged.

The original first-load display-unit migration changes only null DisplayUnits
and UpdatedAt, using the existing independent device legacy choice. Its separate
D574 guard checks actual native column names, integer UTC ticks, bounded actual
save time and every other column/row; initial textual-timestamp assumptions are
preserved as observer refusals. The final seven affected storage-contract guards
pass, independently from six earlier cold-lifetime policy cases. These are
benchmark admission cases, not extra shipping application tests or a financial
write replay.

Strict Windows and the complete two-ABI offline signed product pass with zero
warnings/errors. The package retains384 own AOT modules,408 ELF files,1608 static
16KiB load segments, valid ZIP/v2/v3 and1422 exact original resource bytes. Native
engines and Core/compression code are unchanged. Data intentionally contains the
optional-source correction; compare actual PE bytes against its freshly built
product role rather than claiming equality to the previous Data assembly.

The complete signed candidate was adopted once without reset on Samsung Main: 9856 to 11430. Actual package SHA256 `623b5ca02a0554dad09917219d762ca3d221533710ae26931700ffb5dc72cdba`, UID/CE/DE/permissions, original system settings and all141 Home attributes/Home979 are verified. No financial command or original-profile modification was issued. The independent D574 candidate remains stopped with its one newly owned reference profile retained; it has not been rolled back or falsely declared cleaned up.

## Preserved engineering refusals

A relative ArtifactsPath used during the Windows check left generated source
inside project directories. Standard clean completed but retained some generated
metadata. Removal of those owned residual directories was automatically rejected
before process launch with `blocked by policy`; it did not run and no alternative
deletion was attempted. They remain (449 files/118359992 logical bytes), and no recovered-space claim follows.
Repository default source discovery now excludes generated artifacts trees;
explicit original-app benchmark links also exclude that app's output tree.
Ordinary evaluated source items contain no old generated inputs without a private
source-exclusion import. Absolute output paths are used for later builds.
The actual commit hook initially misclassified ignored output as missing source.
It now recognizes artifacts alongside bin/obj, while also refusing staged artifacts.
Three disposable-repository cases execute the actual hook: generated output is
allowed as ignored output, ignored real source still refuses, and staged generated
output still refuses. Identity, secrets and attribution checks remain enabled;
no real commit/index/global configuration or deletion is changed by that review.

The first final-package identity check refused different Core PE metadata before
installation. Captured SourceLink URL and generated virtual paths, not merely
assembly revision text, must match when comparing unchanged code. Bind that
explicit original context before actual SourceLink generation and retain frozen
source hashes plus the intentional restore changes; never patch assembly bytes.
An incorrectly renamed compilation-context import was absent and the SDK ignored it; a later build requires its actual existence/hash before invocation. Original failed packaging, observation/encoding and request receipts are retained.

## Acceptance boundaries and unfinished work

D574's loaded-cold cohort remains open. Two real controlled background-process
kills and new COLD activities were observed, but this device's startup records
contained PID0. The strict current-PID predicate refused both. Native diagnostics
8296.83434/7715.44034ms and initial covered frames1412.943038/1091.214114ms are
retained as unaccepted metadata, not accepted cold benchmarks or a two-second
result. The original SDK also strips Intent extras; request identity belongs to
the actual shell-protected activity plus independently valid native provenance.
Do not repeat the accepted restore or either refused SDK request without a new,
independently validated measurement mechanism. The original reference profile,
prepared snapshot and source/operation checkpoints remain retained; tenfold
preparation has not run. After the independent process was quiesced, Main installation caused an ordinary unrequested independent startup as PID11288. No review-phase packet was emitted. It was stopped once only after actual cached=true/adj900. Last full source equality belongs to9334; full source after11288 is unread, not assumed matched. The retained profile/checkpoints remain open. D567's protected reboot/idle emulator is untouched.

This correction is not complete backup/key/database security acceptance, general
ANR/crash remediation, audible accessibility, iOS, provider/public hosting,
Store approval or publication. The broad goal remains active.
