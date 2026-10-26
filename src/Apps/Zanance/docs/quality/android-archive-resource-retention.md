# Incremental Android archive resource retention (D-569)

## Observed defect and scope

The not-yet-admitted D-568 original and candidate ARM64 APKs differed by476 Java/JAR
resource entries, including the coroutine exception-handler and main-dispatcher service
registrations. Their DEX, binary manifest and resource table were equal; the four native
runtime/cipher/compression engines and actual financial assembly identities also matched.
Preserve both refused packages and their original comparison. A missing service registration
is a packaging defect; this evidence does not establish the cause or correction of random
Mono/ANR failures. Neither mismatched package was installed for the proposed comparison.

The installed Microsoft.Android.Sdk.Windows36.1.69 BuildArchive task reproduces the defect
with a tiny owned Java service/resource JAR: the first direct task invocation contains both,
and the second removes both despite unchanged inputs. The same installed task passes two
consecutive guarded builds and a changed/removed-input build: revised resource and descriptor
bytes are exact and the removed obsolete entry is absent. These deliberately non-installable
archives use no application database, financial commands, accounts or device. An initial tiny
complete-app experiment did not reproduce the failure: consecutive no-change publishes skipped
the relevant incremental task path. That negative result is retained, not called a reproduction.
An early generated-binding compile refusal and a missing fixture output-directory refusal are
preserved; only the unfinished fixture steps were corrected.

The behavior matches the official [incremental BuildArchive report](https://github.com/dotnet/android/issues/11543).
Its upstream correction targets a later SDK line; that does not prove the locally installed
servicing task corrected. No SDK/workload/global configuration is changed here.

## Repository correction

Directory.Build.targets imports eng/AndroidArchiveFreshness.targets. Before application APK
packaging, validate the canonical generated intermediate APK path and remove only that archive.
The installed SDK then recreates its archive from the current inputs, retaining successful
compilation/AOT work. Do not alter a signed output, manually repack an APK, clean unrelated
intermediates or weaken the runtime/financial acceptance gates. AAB, non-Android and library
builds do not run this target. Reject empty intermediate roots, parent/sibling escapes,
non-APK paths and redirected ancestors/files. Inline-task evaluation and escaping refusals were
corrected before accepting the guard. Nine owned sentinel cases and a separate owned junction refusal pass; no outside sentinel is
deleted and skipped project kinds keep their files. These are build-task checks, not application
unit/acceptance tests or a claim of device qualification.

## Reproduction

Use eng/scripts/Review-AndroidArchiveFreshness.py with a new dedicated --work-directory,
--android-tasks-assembly pointing to the installed official SDK task assembly and
--java-sdk-directory pointing to the approved existing JDK. No download or installation occurs.
The direct-task fixture is eng/tests/AndroidArchiveFreshness/ArchiveRegression.proj with the two
owned Java sources. Full logs/results stay in that private owned work directory. A future fixed
SDK may no longer reproduce the original defect; all guarded resource/removed-input checks must
still pass. Use --guards-only with another fresh directory for the nine deletion-authority cases.
Never turn an assertion into a skip or reuse an earlier successful receipt as a fresh result.

## Complete-app admission refusal and corrected SDK condition

The first complete product build remained missing476 JAR entries although the tiny direct-task
check passed. Preserve that signed output as refused, never adopt it. The initial guard required
OutputType=Exe, but the real Android SDK changes application OutputType to Library after setting
AndroidApplication=true. The correction keys on that actual Android application property. The
fixture now deliberately uses Library plus AndroidApplication=true; all three resource cases,
nine authority cases and the redirected-path refusal pass again after this relevant change.
No previously successful Windows build is repeated: its Android target is excluded.

## Build and remaining gates

Strict Windows Release and corrected complete offline Android builds succeed with zero warnings/errors.
Corrected full APK7b51792c/97075103bytes has384 own AOT/408 ELF,1608 statically16KiB-aligned LOAD
segments, ZIP integrity/alignment and v2/v3 signatures. Both ABI financial/compression assembly
identities equal the current matched-role inputs; four native engines equal accepted8fd5. The
financial PE bytes differ from older8fd5, so that older caller is not an exact financial PE timing
baseline. All1422 Java resource entries match the retained healthy role byte-for-byte, excluding
signature records. No package has been adopted on the phone. The owner's Loyan lease and the independent D-567 reboot/idle reminder cohort remain
untouched. D-568 publication/performance adoption is still separate and unfinished; no speed,
general stability, accessibility, security, iOS/provider/Store or release acceptance is implied.
The broad delivery goal remains active. Local evidence pointer: Zanance-D569-current.txt.


## Prepared comparison roles, without device execution

Guarded independent candidate5d815c7f/57192966bytes has192 own AOT/204 ELF and804 static16KiB
LOAD segments, verified against its own compilation before cleanup. Its206 managed assemblies
have exactly one different PE from original c9d62b35: the deliberately changed App assembly.
Every other PE, both package entry-name sets, all1422 Java resource bytes, DEX/binary manifest/
resource table and four native engines match the original role. Current full shipping7b51792c
has the same actual financial/compression PEs. This fixes package admission only; original and
candidate still require the predeclared actual phone performance/memory/source gates. Both were
compiled with recorded a028049f source metadata plus explicitly frozen uncommitted D568 inputs;
a later documentation/build-guard commit is not falsely represented as their embedded revision.
Do not substitute old8fd5 financial PE bytes or reuse the missing-resource candidate as admitted.


## Owned cleanup and preserved failures

Four successful standard clean commands remove18175 inventoried files/1924315496 logical bytes
from the completed ordinary/private Windows and shipping/independent Android build roots. An
initial independent clean selected the default x64 RID and refused its ARM64-only assets; preserve
NETSDK1047, do not restore/build another RID. Continue only that unfinished Clean with its actual
ARM64 property. Six unused unsigned/resource-designer copies remove252667335 bytes after exact
path/hash inventory. Used signed packages, both original mismatch receipts and compilation/source
proofs remain; the owned test junction is retired without traversing its preserved target. These
are logical byte counts, not measured host free-space gains. No physical or D-567 emulator command,
financial operation, global cleanup or unrelated test is repeated.
