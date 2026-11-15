# Hosted private-Core failure boundaries (D1189)

Date: 2026-11-15. The sole D1187 status response for commitc598dd61 reports
[CI failure](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/43671266347).
CodeQL, Local identity backend, Self-hosted service qualification and the returned
dependency-update workflow report completed-success. Their successful job logs
are not read. The CI job metadata and only its two failed job logs are retained
under `artifacts/d1189-hosted-core-failure-diagnosis`; no status polling or rerun
occurs. Libraries/tests report success; iOS is skipped.

## Actual Windows handoff harness failure

Windows job132681616127 fails in Android APK handoff regressions. Its inert SDK
is intended to qualify four artifact-admission cases without a build. After
D1187 added real Core preparation to the normal APK entry point, that test also
entered the real producer. The producer correctly refuses the fake SDK version.
The artifact assertions were never reached; this is not an app compilation or
financial failure.

The test now copies the actual APK entry/wrapper bytes into its owned temporary
fixture and supplies an inert relative Core-preparation dependency there. The
entry bytes are hash-checked against the source. Production receives no bypass
switch, environment escape or weakened guard. Each of the four artifact cases
must record exactly prepare/restore/publish. A fifth, new preparation-refusal
case must stop before restore/publish even when the fake SDK would succeed.

The changed script passes locally on Windows: all four artifact cases and the
new refusal. No real producer, restore, compile, signing or device command runs.
Its fully resolved GUID fixture is cleaned by the original bounded test cleanup.
Other successful regression scripts and managed suites are not repeated.

## Actual Android failure, inner reason still unknown

Android job132681616180 completes workload installation, native dependency and
plain SDK bootstrap, then stops in the producer's source-proof phase. The Python
runner retains the proof tool's stderr in `source-proof.log`; the job previously
printed only the outer failure/path. The returned log does not expose the inner
reason. No compiler/profile/source/API check is relaxed, and no cause is guessed.

The wrapper now emits at most16lines from an at-most8192byte source-proof log,
only when its failed terminal state and log belong to the current invocation.
This proof reads the newly built public framework only. The original phase log,
nonzero status and final refusal remain intact. No arbitrary app/environment log
is exported. Existing successful preparation/package behavior is unchanged.

One isolated synthetic refusal control exercises the actual changed wrapper with
an inert Python executable and dummy prerequisite filenames. It exposes the
synthetic marker and retains the exact strict outer refusal, with zero real
Python/SDK/build/download/device calls. This tests diagnostic propagation; it is
not the genuine hosted inner error, a successful source proof or Linux acceptance.
Changed PowerShell syntax passes. Producer fingerprint/packageversion3 and all
its source, compiler, runtime, source-document and API guards are unchanged.

The Android bootstrap also records XA0033 about the runner's reported Microsoft
JDK17.0.21.0.2 version format. It completes with zero errors. That warning is
preserved separately; it is not established as the source-proof failure's cause.

No full application/Core build, successful managed test, financial operation,
phone/Main/C7/Q02 action, frozen-scope operation or consumed one-shot is repeated
for these harness/diagnostic changes. Hosted Android acceptance and the actual
inner proof reason await new evidence. Original native fault, whole performance,
security/accessibility/platform/provider/release gates and the full delivery goal
remain open. [D1188](android-retained-minimum-dimensions-d1188.md) independently
records the completed minimum-dimension source/runtime correction.

[D1191](source-sdk-selection-d1191.md) independently reproduces and corrects
source-directory SDK drift. It does not establish that drift as the exclusive
cause of the historical hosted source-proof refusal.
