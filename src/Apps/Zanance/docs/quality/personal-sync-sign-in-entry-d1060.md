# Original personal sign-in entry inputs - D-1060

## Actual failure and original correction

The original SignIn command reads its request inputs inside RunAsync, after busy/
access feedback. Five controlled original host cases demonstrate a new defect:
changing/changed busy or request-clearing callbacks replace address/email/fingerprint/
device/request input and the original flow still reaches new transport construction.
All five fail the zero-open criterion; the unchanged local-refusal control passes.
No real identity, credential, provider or financial request is involved.

SignIn now captures its public input tuple and request-only text at invocation,
after the unchanged interaction admission and before busy/access feedback. It
validates current authority/public inputs before constructing the new peer and
transport, after request clearing/disposal/open callbacks and after sign-in/device
responses. Changed request-only text is refused before clearing/submission. The
same captured email/device name feed the original client. Request text is still
cleared by the original flow; no trimming, normalization, persistence, logging or
credential policy is added. Newer public drafts remain unchanged after refusal.
Only this original method changes: replacing its body with the archived original
reconstructs the entire original source text. RunAsync/ownership/cleanup, generated
input guards, protocol, schema, money and financial operation bodies remain exact.

## Focused host and actual Android model evidence

Six new permanent host cases pass in one strict.NET10.0.12 binary: four public-draft
callback boundaries, a rejected late request-text boundary and one unchanged local
refusal control. Their baseline was6total/5failed/0errors; final6total/0failed/errors/
skips. One directly affected existing original sign-in/session contract separately
passes on that same final binary, verifying the legitimate synthetic pinned-loopback
request and device publication still work. It uses in-memory synthetic sessions,
not OS/user/provider authentication; no financial operation or broader suite runs.

The actual source-linked Android App/model runs the same six new controlled
conditions once on the original presentation thread, not an actual page/keyboard/
IME interaction. Fresh API36/x64/kernel4096 PID4074/UID10216/boot
`2adeb5e0-5822-463a-b056-dba74126ac58`, actual.NET10.0.14, returns the five changed
requests with Open0 and unchanged control with Open1 at the declared refusing port.
Every condition has Capture2 including its initial local read, empty request input,
no pending work, ready interaction, preserved newer public draft and original root.
No authenticator call, account/entry/plan/allocation setup or genuine HTTP occurs.

Complete schema/all54tables/56typed rows match before/after the six-condition batch,
digest`29BA3641241484EC30DED07F7313B93ABD65AEED5123D1A9A4A45D4FDADECE94`.
The retained terminal equals the original packet; no successful phase is reissued.
These cases do not qualify every retained-session/open callback, edited-and-restored
input history, actual native text binding or public authenticated service operation.
The original binding evidence remains in [D1056](personal-sync-null-text-binding-d1056.md),
with its completed minimum/ps16k/ARM64 boundaries separately recorded.

## Strict outputs, source checks and retirement

Strict Windows, full offline Android and new private Android builds pass without
warnings/errors. Thirteen actual PDB source documents bind final host/native/
Windows/shipping inputs; these are compiler checksums, not whole-PE equality.
Complete shipping99631007bytes/SHA256
`6f635d3124eb5bb89e9a7ee63c63e72104ca4417b6473bef8523e5bbcb6cd956`
has384AOT/408ELF/1608static16KiBLOAD/offline/nondebuggable/v2/v3/ZIPCRC/alignment.
Private56700834bytes/SHA256
`b2a8a2592eb2fffffa2c2b1ba8d4ae921afc5f14b1b2d3d55d70e37a14dfd419`
has192/204/804 and equivalent checks. Original Mono slices remain unchanged.
The new shipping package is prepared, not Main-installed or an online edition.

Exact five OS values/locale/key presence/geometry/boot/reverse restore. Only fresh
UID10216 and its official24file4284757323logical-byte AVD retire; four original
process handles become signalled and ADB5037/PID59860creation identity stays exact.
Five standard Cleans retire9519registered files/1487493753logical bytes, leaving
3624registered files,6userhome files/7732bytes and8host-temp files/7354bytes. Retained
signed packages/PDB/source/results remain hash-exact. No supplemental deletion,
frozen-root read/retry/subset/alternate/move/escalation/reuse or retained5606/Q02
command occurs. Logical sizes are not measured disk savings.

D1057 and D1058 each consume only their own due CI response: CodeQL completed/success
and CI completed/cancelled. Neither is queried again, rerun or treated as broad
acceptance. Main2807/D1059 remains only a prior dated observation; this batch issues
no phone command. Original native-fault cause/remedy, whole performance/security/
accessibility, external findings/provider access, iOS/Store/release and the complete
delivery goal remain open. Canonical parent States and terminal D607/D752/enrollment/
financial checkpoints stay unchanged. One post-push check is retained without
polling or idle waiting.
