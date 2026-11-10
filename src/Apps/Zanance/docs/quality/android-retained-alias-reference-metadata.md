# Retained Android alias-reference metadata refusal — D-1009

[D1010 later adopts this exact complete package on Main](current-alias-reference-main-d1010.md).
The prepared/not-installed and no-phone statements below describe the original
D1009 cohort before that separate continuation.

Three original reference-check failures are corrected before native alias retirement.
`Directory.Exists` silently treated an inaccessible recovered installation, a file
in place of its key directory and a dangling key-directory link as missing references.
The guard now inspects attributes explicitly: only file/directory-not-found means
absence; unavailable metadata propagates, and non-directory/reparse roots refuse.
Original envelope/header matching, ownership, exclusion captures, wipe and actual
retirement authority remain unchanged. This is a separate component from the
[private analysis marker correction](private-analysis-marker-permission-refusal.md).

## Three actual original failures, without deleting a key

A fresh API36/x64/4096 fixture links the original AndroidAliasReferences and
AndroidProfileRetirementFiles. It invokes only RequireUnreferencedAsync, not any
alias-removal, key-store or profile-retirement operation. Each case creates its own
recovered installation inside the fresh package and a declared synthetic82byte
header where needed: ZKEY, a fixed fictitious GUID and zero-filled remaining fields.
This is not a real protected key envelope, credential or captured Main data.

Baseline PID4005/UID10216, boot `7fbe0c02-28f2-43a2-a2e6-68287e91f35a`,
.NET10.0.14, produces the following original results:

| Condition | Original result | Required result |
| --- | --- | --- |
| Recovered installation mode zero, containing a matching synthetic reference | Guard returns successfully | Refuse unavailable metadata |
| Regular file at its database-keys path | Guard returns successfully | Refuse changed root type |
| Dangling symbolic link at its database-keys path | Guard returns successfully | Refuse redirected root |
| Genuinely absent database-keys directory | Guard returns successfully | Preserve legitimate absence |
| Accessible directory containing the matching synthetic header | IOException | Preserve reference refusal |

All staged header bytes remain exact and original modes restore in finally before
exact nonrecursive fixture cleanup. The first three results demonstrate unsafe
absence inference in the original pre-deletion guard. No real key deletion, data
loss, Main exploit or full retirement journey is performed or claimed.

## Corrected same-source entry conditions

One necessary no-reset update retains UID10216, CE361077, DE66371, permission flags,
boot and original public state. Final PID5192 passes all five conditions: the first
throws UnauthorizedAccessException, the next two throw IOException, genuine absence
still succeeds and an accessible matching header still refuses. The same Android
directory-relative reader remains source-exact. These are five native method
conditions with two directly affected controls, not five new managed unit tests.
Original baseline invocation is not replayed after its one-shot marker is consumed.

Before that update, a private package observer assumes the old userId metadata key;
the actual SDK reports appId. It refuses before update/launch/case execution. Only
the public UID reader changes to exact pm-list-packages output, and the still-unissued
update continues. The failure is preserved; no original case or successful setup
repeats to compensate for it.

## Complete build/source evidence and retirement

Strict Windows and full offline shipping builds pass without warnings/errors.
Prepared full shipping is99610527bytes, SHA256
`efcd1cc0775727bc5adfb6cda911d004965e56cceadd2bb5d672888a929b77f2`,
384AOT/408ELF/1608static16KiBLOAD, v2/v3, ZIPCRC/alignment and offline/nondebuggable
policy. It is not installed on Main in this cohort. Both original Mono members
remain exact. Static alignment is not hardware16KiB runtime acceptance.

Baseline/final SDK packages are17806250/17810346bytes, SHA256
`baa7e5ce1e743e770fdb406f372ba4c0a220ef40b0e18fc34eb41c5e6a7bccde` /
`2732206239dc8dd2ece5c72821851a2aefe32e66cb1db57fbe3196e8899b9bd4`;
both102AOT/112ELF/438staticLOAD/offline/nondebuggable/v2/v3/CRC/ZIP pass.
Seven actual compiler-document bindings cover three baseline sources, the same
three corrected sources and corrected full-shipping alias-reference source.
Current corrected source SHA256 is
`0C0F95A2CF5CD2C3CBD39C7BF861D3EE3B89444C32381184DAAA4D3990B99F5D`.
They do not establish whole-PE or every upstream dependency's source identity.

Only the fresh package retires after terminal results. Five original OS values,
locale/key presence,320x640/density160, boot and reverse mapping remain exact.
One owned console stop leaves all four original host handles signalled within the
existing45second bound; ADB5038/PID65060 creation identity stays exact without
reset/start/reconnect/pair. Official retirement removes24AVDfiles/4238357787logical
bytes. No retained5606/Q02, phone/Main, authentication, financial or old-suite action
occurs. [D1008](private-analysis-marker-permission-arm64.md) remains the last physical
Main31522/78aaf7e0 observation, not a new liveness/private-source claim.

Four own-output standard Cleans retire5956registered files/1019596113logical bytes;
2913remain. All1685logged delete targets are inside current bin/obj roots. Six scoped
userhome files/7732bytes and five host-tmp files/6603bytes remain. Used signed/PDB/source,
raw failures and results remain. No supplementary/frozen-scope read/retry/alternate
cleanup or whole-artifacts-clean claim is made.

Canonical parent States, original native-fault cause/remedy, whole performance,
accessibility/security/platform/provider/Store/release gates remain open. The full
delivery goal remains active; this guard correction does not close those requirements.

The sole D1006response reports
[CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42789001888)
completed/cancelled and
[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42789001929)
in_progress/no conclusion. Exact response/consumption receipts remain; no second
query, cancellation-cause inference, successful job-log read or rerun occurs.

The separate sole D1008response reports
[CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42790802374) and
[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42790802131)
in_progress/no conclusion. It is preserved as that observation, with no second query.
