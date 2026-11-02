# Owner-authorized categorical native fault metadata (D-764)

2 November 2026. The owner explicitly approves only the reviewed categorical output
extension on an independent emulator with fictitious data. This narrow exception to
D561 does not authorize raw addresses, registers, memory, stacks, dumps, private Main,
phone work, crash injection, generic stress, a runtime change or financial replay.
The original ordinary D678 fault remains unclassified; no product repair is claimed.

## Implemented boundary

Preserve the original record reader's owner/package/UID/prior-PID/signal/architecture/
requested-thread and module/BuildId/executable-range admission. New interpretation runs
only after that admission succeeds. The original regular-file/no-follow/nonblocking/
4MiB/inode/nanosecond-mtime/size and system1058:1000/mode0660-or0664 guards remain.
The production-only metadata helper is compiled only outside the synthetic selftest;
its condition/body is unchanged. Normalizing that compilation wrapper and the three
explicit extension points reproduces the original source exactly. No old admission
rule is relaxed to produce a category.

The output protocol becomes2 and adds one static label, never a formatted input value:

| Label | Bounded meaning |
|---|---|
| absent | The admitted signal metadata does not report a fault address. |
| near-null | The reported fault address is in the first4096-byte page, after structural map validation. |
| mapped-readable | The reported address lies in a readable mapping in this record. |
| mapped-not-readable | It lies in a recorded mapping without read permission. |
| unmapped-in-record | It lies outside all structurally admitted mappings in this record. |
| refused-unknown | Signal/map metadata is unsupported, malformed, ambiguous, missing for a present fault, overlapping or over the cap. |

The near-null label takes precedence even if a map starts at zero; it is a numerical
category, not proof of a null-object dereference. Mappings are half-open, sorted locally
and rejected on any overlap. At most4096 maps and existing byte/field limits are allowed.
Only SIGSEGV address codes1/2 admit a present-address category. Other native signal/code
combinations stay unknown; absent-address metadata remains explicitly absent.

The [official schema](https://github.com/aosp-mirror/platform_system_core/blob/android16-release/debuggerd/proto/tombstone.proto)
uses proto3 defaults: a present-address flag with omitted zero address means address0,
not missing metadata; omitted false read permission means false. Duplicate scalar
fields, non-boolean flags, invalid ranges, wrong wire types and incomplete scalar
encodings do not become last-value-wins metadata. The
[official builder](https://github.com/aosp-mirror/platform_system_core/blob/android16-release/debuggerd/libdebuggerd/tombstone_proto.cpp#L668)
records map start/end and protection flags; fault metadata is emitted separately near
line900. These branch sources establish field semantics, not exact source identity of
the installed system image. The retained schema matches the prior5096-character source.

Only signal code/presence/address and map range/protection fields are newly interpreted.
Map names, adjacent fault-memory data, registers, memory dumps, abort text and unrelated
thread bodies are skipped rather than decoded or formatted. Raw protobuf stays on the
emulator. The original complete input buffer and new signal/map storage are wiped;
no managed/native fault handler or in-fault allocation is introduced. This is a separate
post-exit reader, not code running inside the failing process.

A mapping does not prove a valid/live managed object. A fault address may be inside an
unaligned word read rather than at the reference's start. Structural validity cannot
prove that the producer included every semantic mapping: hence the deliberately
record-qualified `unmapped-in-record` label. Object type, header, reference value,
earlier writer and original cause remain unknown; no such data is collected.

## Distinct new component qualification

A fresh owned Zanance_D764_FaultClass/emulator-5584 runs API36/x64/ranchu/page4096,
boot080756a0-43f9-46a6-9c85-038a060c0275, shellUID2000. It installs no app and has no
Zanance package. A synthetic-only catalog uses fixed fictitious package/UID/PID/frame
and an all-zero APK placeholder, with synthetic_fixture_only=true. It is not a real
APK binding, OS crash observation or source of actual D678 address metadata. The
separately compiled non-selftest path is unbound and is never deployed or used as
native-fault evidence. The actually used component binary SHA256 is
`e4128fdd51fab53371f5604156c81fc9c24231aa3e5d060398b8b635a28c128b`.

Two C compiler paths finish with -Wall/-Wextra/-Werror and no warnings/errors. Initial
command-line string-macro quoting fails; move only that literal to the owned catalog.
The stricter selftest compilation then exposes an unused production-only metadata
helper; guard its compilation without removing or weakening the production check.
Both failed attempts remain. This is not a Windows/product build, signed APK, AOT,
API24 runtime, hardware16KiB, new product unit-test count or shipping acceptance.

Exactly79 distinct new synthetic inputs pass their actual Android CLI boundary:
70 categorical outputs and9 hard original-admission refusals. They cover all six labels,
zero-address proto3 forms, false/omitted permissions, half-open/adjacent/unordered maps,
high integer boundaries, invalid/duplicate/overlapping fields, unsupported signal codes,
malformed scalars, ignored forbidden payloads and the exact4096/4097 map boundary.
Output keys and frame data are allowlisted; forbidden payload sentinels never emerge.
No old204/four/ten D759 cases or closed native/app/financial matrix is rerun.

The first58 successes are preserved. An observer initially expects an unknown label
for an incomplete fault-address varint, but the original signal parser correctly
rejects the entire record earlier with exit67 and no JSON. Preserve that failed
expectation; correct only the observer to require the exact hard refusal, then run
only the failed/unissued21 cases. The final79 distinct count does not add the failed
observer as an extra product test or relax admission to obtain output.

## Native closure and cleanup limits

The fresh AVD startup initially fails in the Java batch launcher because of its
parenthesized path. Use the same reviewed JDK through its verified short path only
in process-local environment, restoring all previous variables. All owned AVD/user/
emulator directories exist before actual launcher79928 starts; no growing-log workaround
or timeout restart is used. Initial failed path lookups/host command parsing remain
host errors, not product defects. Existing shared SDK/image/NDK/JDK files are unchanged.

After qualification, five OS values, boot,320x640/density160, empty reverse and shell
UID2000 match exactly without a setting write. No app/package, authentication, source
preparation, financial operation, phone command or crash injection occurs. All81 remote
synthetic/component/archive files retire. The same owned launcher exits with no owned
QEMU remaining; shared ADB5038/PID66176 survives. Official AVD deletion removes24files/
4434900128 logicalbytes. Source, input identities and the used component binary are
preserved outside all rejected scopes. No unused full app build exists to Clean.

Automatic review rejects one supplementary three-scope deletion before process with
`blocked by policy`, without a more specific reason. Nine files/561764 logicalbytes
remain. Freeze these exact scopes without retry/subset/alternate/move/escalation/reuse:

- `E:/Zanance.ReviewBuilds/D764-components` — two files/42032bytes.
- `E:/Zanance.ReviewBuilds/D764-owned-avd` — six files/7732bytes after official AVD retirement.
- `C:/Users/shahram/AppData/Local/Temp/Zanance-D764-a405c39b88ce445ebefc2e1e35a6a053/new-component-fixtures.tar` — one file/512000bytes.

These are logical owned-output counts, not measured host free-space recovery. No
older rejected deletion is retried and no frozen artifact is reused for this step.

## Remaining work

The categorical extension is component-qualified only. A later actual original-path
observation still requires its concrete ordinary input admission, real OS native-crash
proof and exact current APK/module binding. This approval does not select a stress
workload, replay an accepted case or justify suppressing D761's incompatible assertion.
D678 reference origin/type/writer and a supported remedy remain open. D752 genuine
owner readiness is still pending; D749/D751/D607 stay terminal. Product/runtime/GC,
financial source and the phone remain unchanged; wider security/stability/performance/
accessibility/iOS/provider/Store/release and the overall goal are not complete.
