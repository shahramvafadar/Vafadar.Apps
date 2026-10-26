# Independent bounded native signal controls

These own C sources are diagnostic inputs, not shipping application dependencies. See the
[D-563 contract and actual evidence](../../../../src/Apps/Zanance/docs/quality/bounded-native-fault-code-observation.md).

Use a separately reviewed official NDK clang/sysroot and an owned local output directory.
Compile `fault_pc.c` as a shared library for the intended exact ABI/API with strict warnings,
PIC, hidden default visibility and16KiB maximum/common page alignment. The two explicitly
exported installation functions are the only native managed entrypoints. Resolve `dl` locally.
Do not add `ZANANCE_FAULT_PC_SELFTEST` to an application observer build.

`fault_pc_origin.c` and `fault_pc_selftest.c` are standalone component controls only. Their own
shared-origin code provides known matched instructions, while the executable exercises refusal,
generated/unmatched signal recovery, original fatal delivery and concurrent bounded capture.
Compile the observer with `ZANANCE_FAULT_PC_SELFTEST` only for those disposable shell controls.
Never bundle the controlled origin or executable into the independent managed APK or shipping app.
Record actual exit codes/process absence separately; a recovered signal is not a fatal crash.

The managed project opt-in requires `ReviewNativeFaultPc=true`, the exact local
`ReviewFaultPcLibrary` for `RuntimeIdentifier`, and its original independent package identity.
The normal signed offline publish script remains responsible for a complete AOT APK. Verify
the installed APK, actual linked financial PEs/runtime engines, observer bytes, original UID/
storage/permissions and retained source checkpoints before requesting the guarded
`home-current-stages` phase. Run an approved finite changed diagnostic cohort, never a seed or
financial replay. Inspect only its named original record using an externally APK-bound module
catalog. Preserve terminal failures and stop rather than rerunning successful contexts.

No unsupported signal disposition, raw pointer/memory export, shipping collector, network SDK,
global security change, source deletion or Main data rollback is authorized by these controls.
