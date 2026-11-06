# Full-range spending-change ranking (D-914)

## Actual failure and narrow repair

The original K11 spending-change calculation already checks the signed difference,
but ranks it with `Math.Abs(long)`. A valid current refund of1minor and previous
expense of MaxValue produce exactly Int64.MinValue. Ranking that supported result
throws OverflowException instead of returning it. This is separate from D859's
already-correct checked difference policy and D912's observed-balance input.

Only the sort key changes to `Int128.Abs((Int128)c.Delta)`. Delta is evaluated with
its original checked subtraction before widening: a genuinely unrepresentable
public change still refuses. No amount, date, refund, review, scope, category mapper,
nullable-category tie order, ledger writer, model, migration or financial policy
changes. An absolute ranking key is not a new public money amount.

## Reproduced and selected host evidence

Twelve new cases on .NET10.0.14/X64 reproduce eight actual failures. Six cover three
supported MinValue decompositions in both input orders, one preserves current
currency/account/confirmed-only filtering, and one preserves original parent/null
mapping at the endpoint. Four already-pass controls preserve both true-overflow
refusals, equal-magnitude nullable-category order, and original zero-period exclusion.
Full original entry JSON/order remains exact around accepted and refused calculations.
These are calculation-only fictitious inputs, not an accepted financial write replay.

All twelve pass after the sort correction. The source assertion then gains a
generated JSON context for complete LedgerEntry payloads on trimmed Android;
the final twelve are rechecked for that changed assertion, not reported as24new
tests. One directly affected original K11 same-days-of-previous-month case passes.
This is12new/1affected selected host evidence, not a full suite or new financial Save.
Original KpiCatalog source, baseline XML/stack/outcomes and final source are retained.

## Strict packages and actual source bindings

Strict Windows and complete offline Android builds have no compiler errors/warnings.
The full APK has384AOT/408ELF/1608staticLOAD, v2/v3 signing, CRC/ZIP alignment and
nondebuggable/offline policy. Used full SHA256 is
`a4677ebaa411153e7aa642d13a3d19a49d87c686d7fd6c87ff56f382a69991c3`,99491743bytes.
The private original x64 app has192AOT/204ELF/804staticLOAD with the same policy,
SHA256 `b233922d55a4db61374c23ca9ddd4d68bbb6df93deb11f5844c93296fc315bf0`,56774428bytes.
The independent ARM64 SDK has27AOT/37ELF/138staticLOAD, minAPI36/v3/offline/nondebuggable,
SHA256 `993baeb99bd89439ddd250a11336fc3f4697abbec7729046f825ee4dcee3f58d`,5836523bytes.
All retain the exact original Mono hashes; static16KiB alignment is not hardware16KiB.

Eight actual portable-PDB checks bind KpiCatalog in five compilers (host, Windows,
full Android, private x64 and ARM64 SDK) and the new test source in its three callers.
The private bootstrap reverses exactly to the original with only declared service
capture removed. The first normalization pattern named a different local variable;
only that comparison pattern is corrected, without changing/rebuilding accepted code.
The private provider/assertion/test sources do not enter the shipping project.
Sixteen actual compiler DLL/PDB files and both current source files are archived.
No whole-PE identity or new GC/runtime/compiler configuration is inferred.

## Original x64 and missing ARM64 conditions

Only fresh package UID10271/PID22854 launches on retained5606/API36/x64/4096.
A premature host observer lacks the still-pending process receipt and refuses
before creating an action marker or sending a provider request. That failure is
retained. The same already-issued launch completes; no reinstall/relaunch occurs.
One finite provider action calls only the12new tracked case methods while original
Onboarding remains the root. Full20typed-source JSON is identical before/after,
with accounts/entries/schedules all0 and source SHA256
`9030977B8AFB117A223D35A395027B7E1FEC7A572D099A1B40A3B83FD671485C`.
There is no setup, financial UI command, Reports-row/glyph acceptance, or old suite.
Fresh UID10271 retires; retained Q02 returns normally at the same PID15609 with
original public packages/OS/locale-key presence/boot/geometry/reverse exact.

The same12case source runs once in the independent actual Samsung ARM64 SDK,
UID10435/PID29300, API36/.NET10.0.14/kernel4096. Content-free ordinal pass digest is
`9EEE5D63A284BC991AB734C4A881F394C4E6E2DE3A3FBFB5C58A7805B5A31AF3`;
source identity is separately bound by compiler checks, not inferred from the digest.
Fresh UID retires and Main27334/public packages/grants/data inodes/OS/extra locale,
key presence, rotation and timezone return exactly. No App/Data/account startup,
authentication, photo or financial input is used by this SDK caller.

## Main adoption, retirement and remaining delivery

One existing full a4677eba package is adopted with install-r, without reset, after
source/APK/development-signer admission. One normal launcher changes Main27334to31259
while UID10380/CE155299/DE154603 remain. First Ready Home979.00/102landscape raw nodes,
all attributes/order, other public packages/grants/stopped flags, five OS values,
boot/geometry and extra locale/key-presence/rotation/timezone match. Only expected
APK/PID change. No second frame, Reload/relaunch or orientation rescue is used.
Two remote XMLs total75732bytes and match their local retained SHA256
`621939179933155b7bbbdd118647ae246c196dbec4257d488556e6a0dde5382a` before retirement.
Private Main DB/key/financial source is not read or declared equal.

Five standard scoped Cleans remove29097files/2149031543logicalbytes, leaving3818files/
601548029bytes. Used signed packages, current sources/compiler files, raw XML and
actual outputs remain. No supplementary/frozen delete or rejected-scope reuse occurs.

D912's exclusive single response reports [CI42170154942 cancelled](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42170154942),
[local identity backend42170155054 success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42170155054),
[CodeQL42170155084 success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42170155084),
and [service qualification42170154821 success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42170154821).
Its marker/raw response/receipt remain; no second request or job rerun is allowed.
D913's exclusive single response after the owner pause is late at 21.967 minutes:
[CI42170954351 in_progress](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42170954351) and
[CodeQL42170954444 success](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/42170954444).
The actual receipt is preserved without relabelling it as a timely check; no second GET.

This qualifies the calculation correction and preserving adoption, not the original
native fault remedy, whole cold/search/migration/reference performance, all reports
UI/glyph/accessibility, private Main DB/key/security, later iOS, genuine providers/
store or release. D838 actual new-PC authority remains pending/D841 undeployed;
terminal cohorts stay terminal and the overall delivery goal stays active.
