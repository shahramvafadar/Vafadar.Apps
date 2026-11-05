# Exact received decimals and original conflict cards (D-847)

## Observed defects and final contract

Original SyncGraphValidation.ValidCell accepts invariant decimal text solely through
Decimal.TryParse(NumberStyles.Float). That parser can silently round an over-precision
fraction,100+1e-27, Decimal.MaxValue+0.1 or a nonzero underflow to another decimal.
A received row can therefore pass scalar validation before later financial decisions
use a different magnitude. No live profile or successful financial operation is used
to demonstrate this defect.

The original conflict presenter has a second mismatch: NumberStyles.Number refuses
exact scientific text already allowed by the wire contract, while accepting rounded
ordinary text as a complete choice. One shared Core SyncDecimal.TryParseExact now
preserves the original Float grammar and requires equal sign, significant digits and
decimal power after parsing. Normalize only mathematically redundant zeros; zero
sign/scale are not magnitude differences. Checked exponent arithmetic and linear
text processing avoid giant integer powers. No wire payload, schema version/hash,
model/migration, stored financial row, grouping/currency policy or writer changes.

Both the scalar gate and portable/generic decimal review use that same parser.
Keep existing generic numeric display culture unchanged. Initial diff review restores
two unrelated generic-format substitutions before final packages; their earlier
builds remain historical, not final artifact proof. Six-language existing incomplete-
review feedback remains; no new string, translation or product test-count inflation.

## Focused host evidence

Twenty new complete-row admission cases expose8 real original failures. They include
exact minimum/maximum/negative/exponent/zero/trailing-zero controls and malformed or
overflow refusal. Corrected20 pass. Twenty-four new actual complete ExchangeRates
conflict reviews cover four distinct values in six languages, all24 fail originally
and pass after correction: exact1e-28/1.2345e2 versus two rounded fractions/maxima.
Both native-choice permissions and the actual displayed localized value are checked;
input JSON remains exact. FlowFixture performs no account/entry/rate Save for them.

One directly affected existing graph/receipt-parent guard passes without financial
posting. Total44new+1existing selected cases, not a full suite. App tests pass before
the scope-only restoration of unrelated generic numeric formatting; their portable
rate branch and the Core helper stay exact, and final native packages use the final
source. No successful old financial/auth/photo/whole-suite operation is replayed.

## Original Android surface, controlled local input

Use the retained original D809 AVD, Android36/x64/4096, unchanged boot. Fresh private
UID10242 initially runs24789. An observer current-focus selector refuses before any
provider action; switch only to the actual current-focus summary in the same process.
The first original PersonalSyncPage remains covered/not ready: original network
Capture correctly refuses in the offline package. The private ready assertion fails;
this is retained as a correct policy refusal, not a product defect or a pass.

Only the private environment is corrected for this local presentation test: use the
original CaptureAnalysis(false) lock/foreground/profile/catalogue guard, while all
transport, peer, creation, preview and mutation effects throw. The original network
Capture/build policy is unchanged. One necessary no-reset continuation24964 retains
UID10242, preparation marker and exact existing20source before opening the page.
No account/entry/schedule/reference rate or financial setup is repeated or created.

Original PersonalSyncPage/PersonalSyncViewModel and the original presenter render
one controlled in-memory exact-rate conflict, not a real service response. Rate1e-28
has complete native TextView text and both presenter permissions true. The original
horizontal value viewport clips bounds[132,1815,1012,1859] to[132,1815,950,1857]; this
is not full simultaneously visible text/glyph/layout acceptance. No favorable
recapture or changed value replaces that limitation, and no choice button is clicked.

A distinct Decimal.MaxValue+0.1 review makes both permissions false, retains Ready
and displays the existing incomplete-review hint. Initial hint bottom1858 clips at
1857 and is disqualified as complete. One remaining original vertical gesture, with
no value/choice replay, exposes full native hint[87,1400,994,1482], no ellipsis.
All20 declared typed source components remain equal, SHA256
`F112E4D3D5D7BABAEEFA0B580113E40E9EC199A71130B77248A6906A8DF86FE6`.
This is not all schema/cipher/WAL/key/private Main equality or genuine deployed sync,
network edition, native consent or authentication acceptance.

Return to original Onboarding and uninstall only fresh UID10242. All prior emulator
packages/UIDs, five OS values, original absent locale key, geometry/reverse/shell and
boot compare exactly. Original D809/D833 data, terminal old R13 unknown and pending
D838/D841 actual-read boundary stay protected. No phone action or original fault
record/source/provider/history read occurs.

## Strict artifacts and preserved lineage

Initial Windows command restores unrelated iOS targets and hits NU1201 against the
Android-only pinned notification source. Scope only that command to Windows; strict
final Windows passes0warnings/errors. Initial private compilation calls nonexistent
RefreshDisplay; correct only the private observer to the actual translator refresh.
The offline policy refusal above and initial clipped native captions are retained.

Final full offline Android and final local-only private builds pass. Nine actual
App/Core/Data AOT members match build artifacts; no cross-build managed PE identity
is inferred. Original Mono, nondebuggable/offline, v2/v3, ZIP CRC/alignment and
static16KiB LOAD pass; this is not actual16KiB hardware acceptance. Shipping manifest
contains no private review provider. The private bootstrap differs only in its DI
capture and declared local read-only environment; original App sources stay linked.

| Artifact | SHA256 | Bytes | AOT/ELF/static16KiB LOAD |
|---|---|---:|---|
| Used first offline private |fcea2ba5dba72e227e56ee6d00b7c3247a5c7f3c61d16ef36b6da1a21abe0151|56352370|192/204/804|
| Used corrected local-only private |0cfe29cc83cdaeb6ff626eb619846e805da3b015ae339ff5aae93999bed5d1f7|56749852|192/204/804|
| Final complete shipping |a1427c84f855dca61a2ebbd0c81f11f6eb116f5bae7c0d8121faaa4064874af3|99479455|384/408/1608|

Five standard Clean commands across four output roots pass: Core tests347, App
tests237, Windows1172, shipping1289, native1283 deletion lines. These are not unique
file/physical-byte totals. Used signed APKs, exact source, observations and failures
remain; no supplemental deletion, frozen scope, reset/reboot/server/reconnect/pair.
No device XML is created for this provider-bound native observation.

D846's sole CI response records main CI41980028507 completed-success and CodeQL
41980028708 in_progress, with no second request or successful job-log replay.
Shippinga1427c84 is prepared, not installed on Main. D84615641/868f06c0 remains only
the last dated Main observation, not fresh liveness. The overall goal remains active;
original fault, whole performance, security/DB/key, accessibility/platform/provider
and release gates remain open.

Related: [goal precision](exact-goal-percent-input.md),
[manual-rate precision](exact-manual-rate-input.md),
[latest physical Main](physical-goal-percent-main-adoption.md),
[execution frontier](remaining-delivery-evidence.md).
