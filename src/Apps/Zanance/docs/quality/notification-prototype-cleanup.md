# Unused notification prototype output cleanup (D-584)

## Completed standard cleanup

D-580's final source-project integration does not use the earlier experimental NuGet
packages or their five compilation/reproducibility projects. After all accepted native
cohorts and product/adoption proofs are retained, clean only their registered outputs
under E:\Zanance.ReviewBuilds\NotificationCore: modified-core, repro-first,
repro-second, repro-nodebug-first and repro-nodebug-second.

Preflight reads each actual FileListAbsolute manifest and requires every existing
registered path to stay inside its exact owned project root. All five have zero
outliers. Five standard `dotnet clean` calls succeed, removing all411 registered
files/7,868,802 logical bytes with zero registered outputs remaining. This is not
an estimate of physical disk free-space recovery, a product build or a new test.

The actual live pinned upstream archive retains SHA256
ebe76d05d7cb6387eb7cebaf4fb5bfc22c9e75708ced21c0f24022a26f8fb766 and the used full
signed product remains f7488d1da0bbed5b9910a7ace960ef5f883da008f31dd24e173e9421996ae9c7.
Used APKs, original local failure/native/reproducibility receipts and production source
inputs remain. No global NuGet cache/config, unrelated project, Docker resource, owner
archive, device, key or financial source is changed.

## Additional retirement refused before execution

The proposed guarded recursive retirement of the remaining unused prototype workbench
root is rejected before process creation by automatic approval review. Its only stated
reason is `blocked by policy`. No file is deleted by that command; its planned manifest
and source-definition backup are not created either. Do not report them as completed.

The root still exists. Preserve the refusal and do not retry this scope with another
tool, wrapper, partial deletion or custom Clean target. All earlier rejected scopes
remain untouched. This boundary is separate from the five successful standard cleans;
additional workbench retirement remains incomplete. The broad goal stays active.

## No speculative runtime trial to fill the phone wait

A27October primary-source refresh still identifies
[MAUI10.0.120 as the stable release](https://github.com/dotnet/maui/releases/tag/10.0.120).
Its actual [tagged Android scroll handler](https://github.com/dotnet/maui/blob/10.0.120/src/Core/src/Handlers/ScrollView/ScrollViewHandler.Android.cs)
still reposts a pending-layout scroll request. This supplies no newly released fix to
adopt beyond the existing D-468 pin and does not establish our ANR cause.

The older [bridge report123495](https://github.com/dotnet/runtime/issues/123495) is
closed in favor of [investigation123537](https://github.com/dotnet/runtime/issues/123537),
which is still open; a closed duplicate is not a verified fix. The distinct
[JNI report12765](https://github.com/dotnet/android/issues/12765) remains open as well.
These are upstream reports, not accepted Zanance crash attribution. No raw third-party
stack/memory/register output is copied here. Keep the actual D-561 OS exit reason and
missing native-trace boundary, prior rejected trials and original financial/source
checkpoints. No GC/runtime change, old successful cohort or unchanged build is repeated.

The phone is still explicitly leased to Dev Loyan1. General stability/accessibility,
punctuality, performance, hardware16KiB, iOS, provider/identity/Store/public hosting and
release remain separate gates. No complete project or cleanup acceptance is claimed.
