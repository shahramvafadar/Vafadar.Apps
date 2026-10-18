# Original managed startup boundaries and rejected model overlap (D-362)

## Why this investigation is different

D-360 releases retained navigation fragments, but does not establish a general startup/ANR fix.
D-337 already locates substantial native clock cost in a different, earlier binding sample. Do
not repeat its rejected compression/extraction, clock, runtime or licence-order variants. Instead,
place fixed phase markers around the actual original Application/MauiProgram/App/window/storage
calls in the independent AndroidHome package. Preserve their algorithm order, licence/telemetry,
key protection, full profile preparation, exact KDF and fresh app-lock read.

The markers log only a fixed phase, monotonic elapsed milliseconds and managed thread identity.
They are temporary, independent-package guarded and absent from the ordinary application. These
are instrumented process-cold/warm-cache single samples, not unmodified startup distributions.
Application-entry timing excludes native work before that managed callback. Startup-ready is the
existing storage/access publication signal, not a precise rendered/loaded-Home timestamp.

## Actual phases, without a speculative resource change

| Original process and scope | Application callback body | App XAML resources | Storage worker | After storage until startup-ready | Application entry until startup-ready |
| --- | ---: | ---: | ---: | ---: | ---: |
| PID11784, original onboarding root |500ms |21ms |2087ms |1213ms |4077ms |
| PID12286, retained zero-balance Home |443ms |19ms |1962ms |182ms |2747ms |
| PID12492, model-overlap candidate on that same Home |504ms |19ms |1893ms |176ms |2779ms |

Each exact PID starts from an explicit Activity; no package-replacement receiver starts first.
Package-only historical ANR lines appear in the unfiltered system log alongside the new process
start. They do not identify a new ANR for any of these PIDs and are excluded from the result.
Do not count those retained older failures again or use a fast callback to claim every ANR is fixed.

The first original root is Welcome. Keep it separate from the Home cases. To prepare a meaningful
Home fixture, use the existing independent installation's original three-step wizard once: retain
English/Light/Simple/EUR, create its fictitious My account with zero balance, and decline only the
application's reminder offer with Not now. No system permission dialog or setting is changed.
This is explicit fixture preparation, not an additional acceptance/performance case. Existing
standalone reference/work-editor databases, profiles and keys are not reseeded or reset.

Refine the original Home markers around native protected preparation, the first complete settings
call, root construction and the existing settings/PIN/protection stages. PID12286 reports 1182ms
through protected preparation,777ms for the first settings call,29ms for root construction,70ms
for the fresh lock-settings read and65ms for PIN-state reading. The lock-settings markers remain
on managed thread1. These are distinct boundaries: the777ms includes factory/context construction
and first query work; it is not a measured query-compiler or SQL-only duration. Do not rewrite a
financial/settings reader on that unproven attribution or drop the fresh security read.

## Bounded candidate and rejection

Inside the existing covered startup worker, start only the immutable compiled model's Instance
initialization alongside the unchanged original IAppDatabaseStartup.PrepareAsync. Await both tasks
before any original settings/context read. No database opens early, key or lock guard is removed,
or native/GC/time/cryptographic policy changed. The candidate initializes that model in 46ms.
Its native preparation remains 1181ms, first settings call710ms, root construction31ms, fresh lock
settings 78ms and PIN-state reading 53ms. Overall application-entry-to-ready is 2779ms versus 2747ms
for the preceding original Home case: this pair demonstrates no end-to-end benefit.

This is not a statistically established 32ms regression or a latency distribution. Reject the
candidate rather than replay unchanged runs until a favourable sample appears. The same model
contents alone do not explain the large first settings-call cost. The next unresolved boundary
is actual protected factory/context creation versus first EF settings-query compilation/execution;
split that original path before choosing a production change. Preserve provider, publication,
all-profile, cancellation, settings-row and access guards.

The baseline and candidate complete actual native Home with identical full XML, SHA256
`3131a10ae075af7ad96a5350b4a03f6fc1d34b5ace344f857534507c32a9a223`, My account and0.00EUR.
No financial Save is invoked during the timed Home cases. This UI equality is not a whole-database
comparison. Each original measured process is stopped only after its completed startup and native
result; an observation deadline never causes a restart.

## Qualified artifacts and exact restoration

The three strict independent builds have zero warnings/errors. Their signed APK SHA256 values are:

- First phase boundary: `98b6fe4a0ab5df9f8bc51162c2433a26a68d155a6701e8960c3107ed75f9c01c`.
- Refined original Home: `454b4c92cef4a076a5caed92e0ae793f4e520ab57e2fe5f936028d8ab1718dcc`.
- Rejected overlap: `7356012d417bd0839b58c8b580c2732f39fc73eca1db79a4fca000e23f0fcc4b`.

Each retains 192 AOT libraries and all four native engine entries match the qualified D-360
independent caller byte for byte. The original signing identity, offline/non-debuggable boundaries
and explicit profiling-off build variant remain. No native profiling, memory dump, root, global
configuration, SDK installation or package fork is needed for these fixed markers.

Remove every temporary marker, callback, symbol and metadata-overlap statement. Restore the exact
original source bytes and touch restored build inputs; verify the Git application/build diff is
empty. Restore the already qualified D-360 independent APK with install-r, without clearing its
new one-account fixture or earlier standalone fixtures. Stop only an identified unmeasured
notification-receiver process if restoration starts one. The ordinary main installation remains
D-360 throughout, PID11333, and its final Home0.00EUR XML SHA256 is still
`2f53c71a57dffbebfc9954a0df1ca533ece7d5a0794fabb81edb48bfb1f155e4`.

All owned remote UI XML paths are hash-matched before removing just those files. Restore/check
font_scale1.0, accessibility_enabled0, absent enabled_accessibility_services and
touch_exploration_enabled0; no setting was changed. Samsung is not used in this batch. Local
immutable APKs, original source backups, phase receipts and derived summary remain under
artifacts/startup-stages-d362. No new managed unit tests, full financial suite, ordinary main
build or unchanged successful UI matrix is repeated.

Application/APK remain D-360 with D-361 physical evidence. QA-06, two-second loaded Home, full
native binding/ANR/low-memory, physical workloads, spoken reader, Mac/iOS and release gates remain
open. Neither this measurement nor the rejected candidate is a delivered startup optimization.
