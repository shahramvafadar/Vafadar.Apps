# Personal synchronization initialization review (D-1074)

## Defect and correction

The original recovery command accepted eight invalid continuations: three retired
catalogs after the claim response and five acknowledgements changing the reviewed
profile identity, name, cursor, initialization state or bootstrap owner. The local
connection acquired those unreviewed responses. Nine actual original-command host
cases established eight assertion failures and one unchanged positive control.

Recovery now captures device, peer and identity at command entry. Its original
request validator retains the selected profile and current catalog through
transport waits and the original SQLite writer. The acknowledgement must equal
the reviewed record with only bootstrap ownership transferred to the selected
device. After the original transaction accepts the connection, selection checking
ends so its known receipt and catalog refresh remain valid. No schema, protocol,
financial policy, runtime, dependency, caption or feature enablement changes.

## Focused verification and lineage

The nine new command cases pass in one final host binary on .NET10.0.15. Three
additional new cases retire Profiles, Devices or DeviceItems inside the actual
SQLite SavingChanges event; all three pass in a later test-extended binary, with
one claim/event each, unchanged original connection, no notification and zero
entries. These are twelve distinct new selected cases across two binaries, not
one twelve-case run or a full-suite result. Earlier successful cases were not
replayed. The original baseline XML records eight failures/one pass; an observer
initially read totals from the wrong XML level and lost the original test-process
exit code. The saved XML was admitted without rerunning the baseline. A writer
harness namespace compile failure ran no tests; only that harness was corrected.

Strict Windows and complete shipping Android builds have zero warnings/errors.
Portable-PDB document checks bind eighteen compiler documents, including six
model bindings, to their actual inputs. The model SHA256 is
`e3b92c6d08bd95deB4b3cf3d9c82f26820864f56cf2e2a4076594465dc82ad94`.
The first final host DLL/PDB was preserved before the later writer build.
These are source bindings, not whole-PE byte identity.

## Original Android command qualification

One fresh API36/x64/4096 emulator, UID10216/PID4718, executes eight new negative
conditions through the original model/page. Three catalog retirements occur in
the synthetic session-token read before sending and correctly produce no HTTP
claim. Five changed acknowledgements use actual pinned HTTPS to an owned loopback
fixture, whose claim list is exactly3,4,5,6,7. They are distinct from the host's
post-response catalog timing. The positive control is not replayed on Android.
Every case finishes without a connection notification or pending operation. The
original error label and actual native TextView.IsShown are observed; this is not
glyph, pixel, complete-bound, genuine authentication or OS-consent qualification.

All54 original tables/56 typed rows remain exact, source digest
`29225e8e10841d199009d18f12e15889c581229fb683b9bb851d3b1cf1ffcd19`.
Account, entry, plan, allocation and authentication calls remain zero. The private
native package explicitly permits INTERNET for the loopback fixture and is not
offline; the shipping feature remains disabled. Host uses serviced .NET10.0.15;
Android retains original Mono10.0.14, with no native-fault remedy claim.

## Packages and retirement

The used private signed package `5a35ab388397f01cb675611d3789a47003ffd9f7625276d518a4d01023545d3d`
has192AOT/204ELF/804static16KiBLOAD. The complete prepared shipping package
`2569f0bd11f3be04ab37e3b3d4e6104bff6372f9cb88b8ea9998b4893e425d1f`
has384AOT/408ELF/1608static16KiBLOAD, no INTERNET, nondebuggable, v2/v3 signatures,
ZIP CRC/alignment checks. Both preserve original Mono slices. Shipping is not yet
installed on Main; D1068 remains only a dated previous phone observation.

Original OS/key-presence/locale/geometry/boot/reverse state is restored. Only the
fresh UID, scoped reverse and official25file4267783758logical-byte AVD retire.
The fixture stops through its owned endpoint. Known emulator/server PIDs, children
and owned ports are absent; original kernel handles were not captured, so this
is not a signalled-handle claim. No XML capture was needed. Six standard Cleans
retire9546registered files/1490757188logical bytes;4153registered files remain.
Used signed packages, source, PDBs and raw evidence remain under
`artifacts/d1074-initialization-review`. No supplemental deletion or frozen-scope
action occurs. Logical sizes are not measured disk savings.

D1072's consumed response reports service qualification/CodeQL success and CI
cancelled; D1073's consumed response reports CI/CodeQL success. Neither is queried
again. Canonical parent States, protected untracked benchmark, retained5606/Q02,
terminal D607/D752/enrollment/financial checkpoints and the full delivery goal
remain unchanged. Main adoption, genuine providers/authentication, private DB/key,
whole security, original native-fault cause/remedy, performance, accessibility,
iOS, Store and release acceptance remain separate.


## Subsequent complete Main adoption

[D1075](current-initialization-review-main-d1075.md) adopts this identical complete signed package once from an actual unlocked ready Home, preserving both raw Home XML/all141attributes and captured public state without private Main reads or test/financial replay.
