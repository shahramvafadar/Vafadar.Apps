# Original authenticated transport tail access (D-1077)

## Remaining custom-send defects

After [D1076](personal-sync-device-revocation-review-d1076.md), a finite review of
all PersonalSyncTransport partial sources identifies four direct HTTP send sites:
original typed SendCoreAsync, EmptyAsync, SignOutAsync and ReadBlobAsync. Typed
calls already revalidate access at SendCore entry; D1076 fixes EmptyAsync after
its session read. The two remaining custom paths can still send after their
original access retires during that same asynchronous read.

Four new original transport cases use real pinned HTTPS to an isolated synthetic
responder. Both stale-access conditions actually reach their server endpoint in
the original source, giving two assertion failures; the two current-access
controls pass. No real provider, owner credential, financial file or database is
used. The final same four cases all pass on .NET10.0.15: stale requests produce
zero sends; current logout and three-byte fictitious blob controls each send once.
SignOut retains exactly one original local synthetic-token removal in both its
success and refusal paths. ReadBlob retains the existing local synthetic session.
Successful older cases and financial operations are not selected again.

The product change adds only cancellation/access checks after SessionAsync and
before request construction in SignOutAsync and ReadBlobAsync. Removing those two
added guard/comment blocks reconstructs the previous complete source text exactly.
Routes, payloads, pinned TLS, shared serialization gate, response bounds/zeroing,
finally cleanup, token-renewal policy and database behavior remain unchanged.
Analysis, market, billing and household typed lanes continue through their existing
common send entry; this finite source admission is not every network path in the
whole application or a runtime-engine/security acceptance claim.

## Original Android execution and lineage

One fresh API36/x64/4096 emulator runs only the two new negative Data-transport
conditions in the actual app process4473/UID10216. Original startup/source loading
and a controlled original Shell remain separate from the synthetic access port.
ReadBlob has one session read, zero removals and retained synthetic session;
SignOut has two reads, exactly one removal and no retained synthetic session.
Both throw the original cancellation before sending, with no returned payload.
The owned pinned loopback endpoint remains at zero requests. Positive controls
are not replayed on Android. No page gesture, OS consent, secure-storage backend,
genuine sign-out, real attachment or authentication is qualified by these calls.

All54original tables/schema and56typed rows remain equal around the workload,
digest `8bd9320620e1ca64f61e184e767979316bdc76f157adc3cfbd0604f6348c2bec`.
Accounts, entries, plans, allocations and authentication calls remain zero. The
private INTERNET-enabled package is only for the loopback fixture and is not the
default offline edition. Initial private compilation fails on the ambiguous
Android/System OperationCanceledException name; that failed build is retained,
the helper explicitly selects System, and only the unfinished native build
continues. No earlier successful host/native case is rerun for that correction.

Final strict Windows, complete offline Android and private native builds pass
without warnings/errors. Ten portable-PDB document bindings cover original Data
transport/store, app startup, declared private helper inputs, host tests/fixture,
and Windows/shipping Data sources. This is source binding, not whole-PE identity.
Android Mono10.0.14 slices remain exact; serviced host10.0.15 is a distinct runtime.
The complete prepared shipping SHA256
`fe4ee42eb9a1a1baa1f315e9369a40535c219676a5d8cb80695b62995a5f1f67`
is99643295bytes/384AOT/408ELF/1608static16KiBLOAD/offline/nondebuggable.
The used private SHA256
`643f4e773e6bc4c61885b72df85042e4b64f04082315c175db55ef1d5e0147fa`
is56700834bytes/192AOT/204ELF/804static16KiBLOAD. Both pass v2/v3/ZIP CRC/alignment;
static16KiB layout is not hardware16KiB execution. Neither package is installed
on Main in this batch; D1075 Main29636 remains only its dated prior observation.

## Retirement and continuing work

Original OS/key-presence/locale/geometry/boot/reverse restore. Only the fresh UID,
owned reverse, generated fixture and official24file4266276237logical-byte AVD
retire. Known emulator/server PIDs, their owned children and5754/5755/19778ports
are absent; no signalled-handle or force-kill claim is made. Original ADB5037/
PID59860 creation identity stays exact without reset/start/reconnect/pair. No
phone command, XML capture, private Main read, account setup or financial write
occurs. Five standard Cleans retire9531registered files/1490449086logical bytes;
4145registered files remain. Used packages, source/PDB and raw failed/successful
results remain under `artifacts/d1077-authenticated-tail-access`. No supplemental
or frozen-scope action occurs; logical sizes are not measured disk savings.

D1074's one consumed response reports CIcancelled/CodeQLsuccess, as does D1075's
one consumed response; neither is queried again. Canonical parent States,
protected untracked benchmark, retained5606/Q02 and terminal D607/D752/enrollment/
financial checkpoints remain unchanged. Genuine providers, full security, original
native-fault cause/remedy, performance, accessibility, iOS/Store/release gates and
the full delivery goal remain open. A single post-push CI read is prepared without
polling or idle waiting.
