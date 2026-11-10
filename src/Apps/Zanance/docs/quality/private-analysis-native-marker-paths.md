# Native private analysis marker path guards — D-1006

The explicit metadata checks introduced by [D1003](private-analysis-marker-permission-refusal.md)
preserve the original Android path/type refusals. Five new path shapes and the
affected existing directory-marker condition are qualified against current original
SecureAiStorage, without a product change or a replay of the earlier permission or
keystore-loss workloads. This is finite native storage evidence, not whole security
acceptance or six new product unit tests.

## Original entry points and actual observations

A fresh API36/x64/4096 fixture uses package
`pro.vafadar.zanance.markerpathreview`, UID10216/PID4184, boot
`d4ae7c6f-ed89-4a63-8f34-0081ea203b2a`, .NET10.0.14. Only the original storage class
and a private observer execute; no App/profile/database, account, journal submission,
credential, HTTP or financial setup occurs. All links, missing targets and public
sentinels belong to this fresh fixture. No owner directory is substituted.

| Staged shape | Original Read | Original Write | Own keystore entries after refusal |
| --- | --- | --- | --- |
| Regular file at the private root | IOException refusal | IOException refusal | 0 |
| Directory symbolic link at the private root | IOException refusal | IOException refusal | 0 |
| Dangling symbolic link at the private root | IOException refusal | IOException refusal | 0 |
| Directory at the initialized marker | IOException refusal | IOException refusal | 0 |
| Marker symbolic link to a public fixture sentinel | IOException refusal | IOException refusal | 0 |
| Dangling symbolic link at the marker | IOException refusal | IOException refusal | 0 |

Each write is attempted only after its corresponding read refuses. Each case leaves
the target sentinel byte-exact and removes only its staged fixture node/empty own
root. The six cases execute once under the retained request marker. After their
12 refusal-entry observations, a legitimate unknown-scope read returns null and
reaches the actual native backend: one known own keystore entry is present. This
positive control distinguishes path refusal from a backend that always throws.
There is no key-value export, alias enumeration, alias deletion or authentication.

The current storage source SHA256 is
`ED398DF9AFD800B7D8E153B451DED83BB3BA377C94BCE7923BA5017569C227C9`.
Two actual compiler-document checks bind that source and the private observer to
the used native PDB. This is not whole-PE or complete upstream binary identity.

## Build lineage and owned retirement

The initial private build fails because the installed Android binding has no
`Os.Unlink` member. That refusal is retained; only the cleanup binding changes to
the available `Android.Systems.Os.Remove` before installation. The corrected strict
SDK build passes, with104AOT/114ELF/446static16KiBLOAD, original x64Monoedfa,
offline/nondebuggable manifest, v2/v3 signatures, ZIP alignment and CRC. The used
signed package is17876098bytes, SHA256
`2e6e59f05b7d567164260be8054864001d42b05d48e4c99e9242ebe4d0e38e37`.
No Windows/full-shipping build, old managed/native suite or successful financial
action repeats. This SDK evidence is not hardware16KiB or physical ARM64 acceptance.

Five original OS values, locale/key presence,320x640/density160, boot and reverse
mapping remain exact without setting writes. The fresh package retires only after
its terminal response. One owned console stop leaves all four original host handles
signalled within the existing45second bound; ADB5038/PID65060 creation identity stays
exact, without server reset/start/reconnect/pair. Official AVD retirement removes
25files/4129437108logical bytes and leaves no AVD files.

Two own-output standard Cleans retire888registered files/119402431logical bytes;
438remain. All151 logged delete targets resolve inside current own bin/obj roots.
The first path observer wrongly resolves a relative native-clean log path against
the repository: its assertion fails after that Clean succeeds. The retained log is
then interpreted relative to its project; the successful native Clean is not rerun.
Only the still-unfinished compiler Clean executes. Six userhome files/7732bytes and
one host-tmp file/988bytes remain. Used signed/source/PDB/raw results and failures
are retained. No supplementary deletion, frozen-scope access, retained5606/Q02 action
or phone command occurs.

## Delivery boundaries and prior single CI response

Main31522/78aaf7e0 is only the last [D1005 observation](current-private-marker-main-d1005.md),
not a fresh liveness claim. This class/backend qualification does not establish
Main private database/key equality, concurrent filesystem-race protection, hardware
key lifecycle, original native-fault cause/remedy, whole performance/accessibility,
iOS/provider/Store/release acceptance or completion of canonical parent States.
The complete delivery goal remains active.

The sole D1004 response reports [CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42785426042)
completed/cancelled and [CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42785425932)
completed/success. The request, raw response and consumption receipt are preserved.
The push receipt was created later than the successful push operation, so it is not
an exact server-push timestamp and strict15–18minute timing is not claimed. No second
query, cancellation-cause inference, successful job-log read or rerun occurs.
