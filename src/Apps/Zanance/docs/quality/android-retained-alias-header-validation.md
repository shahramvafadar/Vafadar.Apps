# Unknown retained Android key-reference headers — D-1011

Ten further original reference-check failures are corrected without changing any
key envelope format, key protector, decryptor or actual alias-removal authority.
The [D1009 metadata guard](android-retained-alias-reference-metadata.md) remains.
This continuation validates bounded key-slot shape before inferring that a retained
file does not reference an independent alias. It does not authenticate ciphertext.

## Format premise and original failures

Current AndroidDatabaseKeyProtector writes65byte ZKEY/version1 installation-bound
envelopes and82byte ZKEY/version2 independent envelopes. Version2 has purpose0/1
and a nonempty big-endian GUID in bytes6–21. Version1 has no independent GUID field.
The reference checker previously matched only an82byte prefix and otherwise returned
without refusing unknown key-slot contents. A partial independent header could
therefore disappear from its reference decision even when the retained alias bytes
were still present.

The fresh API36/x64/4096 fixture invokes the original RequireUnreferencedAsync and
AndroidProfileRetirementFiles, using only declared zero-filled synthetic bytes and
fixed fictitious GUIDs. There is no real key envelope, keystore operation, alias
enumeration/deletion, Main/private-source, account/database/ledger, authentication
or provider setup. Baseline PID4346/UID10216 uses one consumed baseline marker.

| New negative input | Original observation | Corrected observation |
| --- | --- | --- |
| Empty key slot | Guard succeeds | IOException |
| Version2 truncated at21bytes | Guard succeeds | IOException |
| Version2 complete alias but only22bytes | Guard succeeds | IOException |
| Version2 truncated at65bytes | Guard succeeds | IOException |
| Version2 truncated at81bytes | Guard succeeds | IOException |
| Unsupported purpose with a different fictitious alias | Guard succeeds | IOException |
| Empty independent alias | Guard succeeds | IOException |
| Unknown magic | Guard succeeds | IOException |
| Unknown version | Guard succeeds | IOException |
| Oversized83byte independent slot with a different alias | Guard succeeds | IOException |

Seven directly related controls pass before and after correction: current and
recovered matching independent references refuse; the65byte version1 shape does
not interpret nonce/ciphertext bits as an independent GUID; current and recovered
known different aliases remain nonmatching; an exact captured unknown staged file
is excluded; changing that captured file refuses. Across the17conditions, all four
recognized key/recovered/creating suffixes are exercised. These are native method
conditions, not17new managed unit tests or genuine legacy/ciphertext authentication.

For the exclusion controls, actual native CaptureAsync binds the synthetic file's
object/length/SHA. That is a controlled method input, not genuine durable retirement
or user authority. The intentional changed-file control is compared against its
earlier capture; its post-setup bytes are otherwise preserved exactly. Every other
synthetic byte remains exact, and each fixture installation is removed through exact
nonrecursive cleanup after its terminal observation.

## Narrow correction and same-fixture acceptance

The original reader now takes at most83bytes: the one extra sentinel byte prevents
an oversized file from masquerading as the complete82byte supported format. The
complete65byte version1 shape remains supported. Other inputs must be exactly82byte
version2, use purpose0/1 and contain a nonempty independent alias; unknown contents
refuse before absence can authorize retirement. Existing captured-artifact exclusion
stays before this parsing, and the complete read buffer still wipes in finally.
The format writers, authentication, financial model and migrations are untouched.

One necessary no-reset update preserves the original UID/CE/DE/permissions/boot/public
state. Final PID5000 passes all17conditions in one invocation. Original baseline
failures and both one-shot markers remain; successful inputs are not retried for a
different outcome. No old metadata/path suite, successful financial operation or
actual profile/key deletion is replayed. This is finite reference-check evidence,
not full retirement/key-recovery or physical ARM64/iOS acceptance.

## Actual compilation, package and owned retirement

Both strict SDK builds pass. Original/final packages are17814442bytes each, with
different SHA256 identities:
`af4feaac76dd7dbb3833fcbdf5439c6a1ab24d09c88dc5971a89520a6fac9e2f` /
`f8b91c5d5ef00a2c1e0fe33ea9d604fd82ead111368101a4758687561344db74`.
Both102AOT/112ELF/438static16KiBLOAD/offline/nondebuggable/v2/v3/ZIPCRC/alignment
and original x64Mono checks pass. Same byte length is not byte identity.

Strict Windows and complete offline shipping pass without warnings/errors.
Prepared shipping is99614623bytes, SHA256
`9ef33c3aea965e5027e1b51958df2ddf7e5f4115d24e73830eac53efef5ce937`,
384AOT/408ELF/1608staticLOAD/v2/v3/CRC/ZIP, both original Mono members exact.
It is not installed on Main in this cohort. Seven actual compiler-document bindings
cover three original and three corrected native sources plus corrected shipping
AndroidAliasReferences. Current corrected source SHA256 is
`5E563F34F2AE40E9544F09447B253FABE567519A5811C13FC55A4C0829881F75`.
This is bounded source evidence, not whole-PE or every dependency's binary identity.

Only the fresh package retires after all terminal results. Original five OS values,
locale/key presence,320x640/density160, boot
`6fa389ad-a852-43af-9ca9-c2b52b585f6f` and reverse mapping remain exact. One owned
console stop leaves all four original host handles signalled within the existing
45second bound; ADB5038/PID65060 creation identity remains exact without reset/
start/reconnect/pair. Official retirement removes25AVDfiles/4198905278logical bytes.

Four own-output standard Cleans retire5956registered files/1019655053logical bytes;
2914remain. All1685logged delete targets are inside current own bin/obj roots.
Six scoped userhome files/7732bytes and twenty host-tmp files/3309138bytes remain.
Used signed/PDB/source/raw failures/results are retained. No supplementary/frozen
scope read/retry/alternate cleanup, retained5606/Q02, phone, old-suite, financial or
real authentication action occurs. [D1010](current-alias-reference-main-d1010.md)
remains the last physical Main6904/efcd1cc0 observation, not fresh liveness or
acceptance of this later header correction on Main.

Canonical parent States, actual retirement/recovery, original native-fault remedy,
whole performance/accessibility/security/platform/provider/Store/release gates
remain open. The complete delivery goal remains active.

The sole D1009response reports
[CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42793263683)
completed/cancelled and
[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42793263610)
completed/success. Exact response/consumption receipts remain; no second query,
cancellation-cause inference, successful job-log read or rerun occurs.

The separate sole D1010response reports
[CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42793962511) and
[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42793962162)
in_progress/no conclusion. That observation is preserved without a second query.
