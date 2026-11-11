# Nullable request-input binding on minimum API and ps16k - D-1058

Two new platform conditions extend the [D1056 binding correction](personal-sync-null-text-binding-d1056.md)
using the exact retained signed private APK,56709026bytes/SHA256
`6294b6fbca409259dee9669e85e253215faeb06980d1cf0188f5cce669ebd550`.
No product/source edit, host test, Windows/full shipping/native build or previously
successful platform condition is repeated. D1052's earlier retirement cases are
separate; this work exercises only the newly corrected nullable binding.

Fresh API24/x64/PID2887/UID10062 runs on kernel/MMU4096. Fresh API35/x64/PID3806/
UID10207 uses the existing ps16k image: getconf reports16384 while filtered kernel/
MMU labels report4kB. This is a userspace compatibility boundary, not hardware16KiB
or managed Environment.SystemPageSize acceptance. Both actual runtimes are.NET10.0.14.
Official existing SDK/images are read-only; every fresh AVD/userhome/temp/output is
under repository artifacts. No new E path, download or global configuration occurs.

Each original PersonalSyncPage/native EditText receives the same fictitious raw
whitespace/NBSP/RTL-mark text through its bound native control, then one actual
MAUI Entry.Text=null assignment. Runtime TextProperty default is null and binding
mode TwoWay. Immediate and settled model values are canonical empty strings,
control text is empty, prior input is not retained, and interaction remains ready.
Two TextChanged callbacks and the original nullable event are observed, with no
escaped exception. Capture1/Open0/close-callback0, original modal cleanup/root
identity are preserved. This is an actual bound
control-property condition, not a physical keyboard-clear or credential-use test.

Complete schema/all54table typed56row source digests match before and after each
single condition. API24 digest is
`E13C9ADDE40DE4066FF82EC64FCFE39852CDAD62AAFB8F7593D8FD508B3CFFCC`;
ps16k digest is
`4EC69A40569A262ED4A01866FBD6F9B98C6D0131CF0013969B305C42C6983FC0`.
Independent cohorts need not share a digest; no cross-cohort equality is claimed.
Accounts/entries/plans/allocations/auth calls remain0. Exact original five OS values,
locale/key presence, geometry, boot and reverse mappings are restored without a
setting write. Each fresh package is retired; there is no XML capture to delete.

Main2807/D1057 remains a prior dated observation; this work sends no phone command.
No old financial/auth/accepted operation, retained5606/Q02 or frozen scope is touched.
The original native-fault cause/remedy, ARM64 new-binding boundary, whole performance/
security/accessibility, genuine external providers/findings access, iOS/Store/release
and full delivery goal remain open. Canonical parent States and terminal D607/D752/
enrollment/financial checkpoints remain unchanged.

Both original emulator process trees finish through one console stop each; all8
original handles become signalled and ADB5037/PID59860creation identity remains exact.
Official AVD deletion retires40files/13416064312logical bytes across the two cohorts.
API24 userhome retains4files/5285bytes and ps16k6files/7732bytes; each host-temp
retains2files/40bytes. No supplemental deletion is attempted.
No new build output exists and no successful build/test is repeated for cleanup.
Signed package/source/results remain preserved. Logical sizes are not measured
disk savings. One post-push CI check is retained without polling or waiting idle.
