# Public concurrent-vector fault correlation (D-911)

[D1123 new Tarjan proposal](tarjan-concurrent-source-admission-d1123.md) follows
the subsequently published upstream137047 change. Its predicate is absent from
both admitted original and current15 source; this is finite source admission,
not a runtime patch, repeated classification or proof of this fault's cause.

## New question after the original operand proof

[D908](new-accounts-field-word-provenance.md) establishes the exact original new-PC
reference-element load path. A similar public stack alone must not select a repair.
This step compares one published descriptor with that admitted static path, without
reading the original process's descriptor, R13, map, object, heap or another record.

The [upstream report123537](https://github.com/dotnet/runtime/issues/123537) is open
at6November2026 05:10:51UTC; selected public metadata says last updated8October2026.
Its initial ARM64 report names the copy operation at source137 and vector scan at59,
with descriptor245957. Comments discuss collector/runtime alternatives and other
workloads. Those are third-party observations, not this project's controlled inputs,
same binary identity, reproduced cause or accepted remedy. No suggested configuration
is applied and no upstream report or private project data is submitted.

Only selected metadata/source-line/descriptor facts and a SHA256 of the public
response are retained locally. Public object/register addresses and the full issue
body are not retained. The initial report's exact historical module/source-format
identity has not been established; SDK/version wording alone does not establish it.

## Conditional format calculation

The already admitted immutable descriptor header SHA256 is
`465dde43d4470195248ca8e099e4d3e154e1e81886a7a3c16e873f4cf621ecae`,
VMR `0427b9abbead3fa64c6655eb4290c537397131fa`. Its
[type/subtype definitions](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-descriptor.h#L77)
and [vector dispatcher](https://github.com/dotnet/dotnet/blob/0427b9abbead3fa64c6655eb4290c537397131fa/src/runtime/src/mono/mono/sgen/sgen-descriptor.h#L273)
define different reference-only, run-length and bitmap vector branches. Four new
arithmetic controls cover subtype0..3; these are not product tests or another run of
the existing native diagnostic suite.

| Public printed value interpreted under the admitted current format | Result |
| --- | --- |
|245957 in hexadecimal|0x3c0c5|
|Low3type bits|5, vector|
|Bits14..15|3, bitmap vector|
|Element-size field|24bytes|
|Bitmap field|3|

By contrast, the saved D908 exact original instruction/CFG evidence reaches new
fault PC0x2f3f92 through the reference-only subtype1 branch. The compare at0x2f39b9
requires subtype bits0x4000 before its array-data initializer0x2f39c9 and slot load
0x2f3f51. The bitmap value above would not take that branch under this format.
The shared vector macro emits multiple subtypes at the same source line: matching
source137/59 does not establish an identical instruction path or descriptor subtype.

This is deliberately conditional for the public historical number. It does not
prove the older public binary used this exact descriptor format, that the issues
are unrelated, or that any particular managed class/array, invalid value, writer,
nursery generation or original cause has been identified. No actual project
descriptor value is decoded or inferred from the public number.

## Preserved execution boundary

The public issue's collector/runtime suggestions are not an admitted project patch.
No runtime/GC/configuration, build, SDK install, product test, app launch, device,
authentication, photo, financial operation, cleanup or frozen scope changes. D700's
already qualified card correction is not reapplied or tested again. D838 new-PC
authority remains pending and D841 remains undeployed; old R13 evidence is terminal.

Selected public receipts, arithmetic results and saved-code correlation remain.
The next cause-specific observation must stay bound to this project's actual
approved PC/module and permissions. Main22204/37518cfa and Q0215609 are only dated
D910/D909 observations here. Original fault repair, whole performance/security,
accessibility/platform/provider/release gates and overall goal remain open.
