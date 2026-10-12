# Current Release startup baseline (D-176 / QA-06)

Measurement only at D-175 application baseline d12e503e2772d68b1e3d578b64d4c957ddc4ee89.
No production code, SDK, schema, native setting, key, permission, commercial switch or APK change.
The same complete signed Release runs on the existing private API 36 x86_64 emulator.

| Sample | Activity-start command wall time, ms | First observed ready Home upper bound, ms |
|---|---:|---:|
| 0 | 9549.44 | 18670.44 |
| 1 | 6755.98 | 14981.63 |
| 2 | 5796.7 | 13107.65 |

Process-cold, warm OS/file caches. Start timer before am start; readiness requires the original
Recorded balance and enabled New expense target in the actual native tree. Includes command and
hierarchy-dump overhead. First missing roots are preserved, not treated as terminal process failure.
Three samples are a bounded baseline, not a two-second claim, precise render profiler, physical-device
measurement or causal attribution. No Save, profile selection, lock change or system configuration.
Final app is stopped. Evidence: artifacts/startup-current-baseline/samples.json and per-sample
activity outputs/actual ready trees. Other earlier startup observations after installation are not
pooled with these process-cold/warm-cache samples.

## Actual source boundary and next implementation

App.CreateWindow synchronously checks/applies migrations, loads original display units (including
legacy preference import), reads settings again, and constructs the Shell/onboarding root before
returning its first covered window. OnStart subsequently reads secure state and queues original links;
foreground auto-post/reminder/cleanup runs independently. The synchronous UI path is confirmed by
source, but the samples do not assign a measured percentage to any one operation.

Next ready engineering: perform original database preparation outside the initial UI call while
retaining a protected loading/failure/retry surface; keep settings/display units and actual-file scope
consistent, and gate original foreground work/navigation after preparation. Preserve PIN failure
protection, original AppLinkRouter/unsaved widget destinations, onboarding and legacy unit handling.
Test actual original ports/SQLite and native startup/failure/retry before claiming delivery. Do not
move database work into Application.onCreate/IMauiInitializeService, sell startup test access, drop
migration/lock checks or reinterpret a quicker blank first frame as a quicker usable Home.

The implementation is not yet changed. QA-06, ANR, exact loaded publication duration and physical/
iOS acceptance remain open. Do not repeat unrelated successful feature tests or rebuild the identical
D-175 APK for this measurement-only record.
