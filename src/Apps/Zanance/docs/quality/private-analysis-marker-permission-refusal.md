# Inaccessible private analysis markers — D-1002/D-1003

[D1005 later reuses this exact complete package on Main](current-private-marker-main-d1005.md)
without rebuild/test/financial replay. The prepared/not-installed and no-phone
statements below describe this original D1002/D1003 cohort before that continuation.

Two newly reproduced original storage failures are corrected. An existing marker
behind an inaccessible directory must not look like a fresh scope, and an entry-time
metadata denial must stop a write before changing its backing value. The correction
uses explicit attributes, treating only file/directory-not-found as absence. Other
metadata failures propagate. Existing directory/reparse-point refusals remain.
Private prefix, marker/value format, size/cancellation rules, platform encryption,
client consent, HTTP, wallet and financial rules are unchanged.

## Concrete original failures

This is a new standalone class/backend condition, following the bounded
[D973 storage evidence](private-analysis-native-storage.md). The original
SecureAiStorage source and actual Android MAUI SecureStorage backend are linked;
no original App/profile/database, account, financial entry, credential, provider,
HTTP request or real key loss is prepared. Only declared fictitious values/scopes
belonging to this fresh package are used. Values, ciphertext and keys are not exported.

On API36/x64/4096/.NET10.0.14, fresh UID10216/PID5292, the observer temporarily sets
only its own analysis-state directory permissions to zero, capturing the original
mode beforehand and restoring it in finally. Both original conditions fail:

| Original condition | Actual observation | Required behavior |
| --- | --- | --- |
| Controlled fictitious value removal with its existing marker inaccessible | Read returns null | Refuse unavailable state rather than imply a fresh scope |
| Existing fictitious value with its marker inaccessible | Write throws UnauthorizedAccessException after changing the backing value | Refuse before that backing mutation |

Both original directory modes restore exactly. This does not show a Main exploit
or that ordinary phone permissions were changed. The fixture intentionally creates
an unavailable private metadata condition; it is not genuine keystore corruption.

[File.Exists documentation](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.exists?view=net-10.0)
explains why a false result cannot distinguish absence from denied access.
[File.GetAttributes](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.getattributes?view=net-10.0)
provides the failure distinction used by the correction. The same explicit lookup
guards the private root, marker type and initialized-marker checks.

## Initial observer failure remains separate

The first original SDK process3751 returned no result. Its public OS record is
reason4/status0/importance100; no trace, description, native frames, registers or
memory are read/exported. No product/native-fault cause or repair is inferred.

Its observer incorrectly used SecureStorage.Remove's return value to infer prior
presence. The pinned [MAUI10.0.120 Android implementation](https://github.com/dotnet/maui/blob/10.0.120/src/Essentials/src/SecureStorage/SecureStorage.android.cs)
returns true unconditionally for that operation. This is a concrete observer mistake,
not proof of the exclusive cause of the original process exit. The original run
marker is kept; it is never removed or retried for a result.

One necessary observer-only no-reset update adds bounded exception-class reporting
and saves permission-stage/original-mode metadata before changing permissions. Its
first read-only inspection confirms the initial scopes/root remain unused. Only
new, unconsumed fictitious scopes execute the two original conditions above. The
earlier empty response is not counted as a completed condition or pass. Both original
source lineages retain their used signed package, source/PDB and compiler bindings.

## Same-cohort corrected behavior

One further necessary no-reset update retains UID10216 and boot
`1b7a23c6-76f3-4285-a841-7a5eaa2f0774`; final PID5653 executes the changed storage
class with new fictitious scopes. Both inaccessible-entry conditions now refuse
with UnauthorizedAccessException, return no fresh null and change no backing value.
Both original directory modes restore exactly.

Two directly affected controls additionally pass without another setup:

- An actually absent scope still reads null.
- The now-readable initialized marker still refuses the controlled missing value.

These are two corrected conditions plus two affected controls, not four new product
unit tests or a replay of D973's nine-case suite. New permanent unit-test count is
zero. There is no accepted financial/share/authentication/provider operation to replay.
This qualifies entry-time unavailable metadata, not every concurrent filesystem
interleaving, whole-journal durability, power loss, genuine enrollment/key loss,
OS transfer, complete App/DI integration, ARM64/Windows runtime or iOS behavior.

## Build, provenance and retirement

Final strict Windows and full offline/nondebuggable Android builds pass with zero
warnings/errors. Shipping99610527bytes/SHA256
`78aaf7e000591b4838db10a84ec75fa8b79f3a94409eb9522472adb0a893788c`
has384AOT/408ELF/1608static16KiBLOAD, v2/v3 signatures, ZIP alignment and CRC pass.
Final used SDK package17880194bytes/SHA256
`eef0326fbff8a5cd3fb7bd05cccf59d375dc5eb60db09fe07fd3c574aecc1acd`
has104AOT/114ELF/446staticLOAD with the same signing/offline checks. Original Mono
hashes remain unchanged. Static alignment is not hardware16KiB runtime qualification.

Two compiler-document bindings per original lineage bind the unchanged storage and
its respective observer. Four final bindings cover actual native storage/observer
and Windows/shipping storage sources. Final storage SHA256 is
`ed398df9afd800b7d8e153b451ded83bb3ba377c94bce7923ba5017569c227c9`.
These are source/compiler correlations, not whole-PE identity.

Only the fresh package retires after all permission modes are restored. Five OS
values, locale/key presence,320x640/density160, boot and reverse mappings remain
exact; no public setting write is needed. One owned console stop leaves all four
original host process handles signalled within45seconds. Existing ADB5038/PID65060
creation identity remains exact, without reset/start/reconnect/pair or alternate kill.
Official AVD retirement removes24files/4283708709logical bytes, leaving no AVD files.

Six standard Cleans retire7706registered files/1258289092logical bytes, with3775
remaining. Six scoped SDKuserhome files/7732bytes and one995byte host-tmp file remain.
Used signed packages, original/final source/PDBs, raw bounded results and the initial
failure stay. No XML capture, supplementary delete, frozen-scope read/retry/alternate
cleanup, phone/Main, retained5606/Q02 or terminal native-classification action occurs.

This full shipping package is prepared, not Main-installed. Last dated Main remains
[D99927312/b3fa2bac](current-snapshot-bounds-main-d999.md), not fresh liveness or
private-source equality. Canonical parent States, original fault cause/remedy,
whole performance/security/accessibility/platform/provider/Store/release gates and
the complete delivery goal remain open.

The sole prior D1001CI response is18minutes26.989seconds after its recorded push,
not a timely15–18minute claim. [CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42777613023)
and [CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42777613008)
are both in_progress with no conclusion in that response. Exact response/consumption
marker persist; no second GET, polling, successful job-log read or rerun follows.
