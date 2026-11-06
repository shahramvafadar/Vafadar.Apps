# Complete original wealth groups and current CI contract (D-896)

## Original defect and bounded correction

Original .NET10.0.14/X64 probe52152 tests all six account orders for each signed
three-account group. Four original OverflowExceptions/eight passes occur despite
representable completed Money totals. Every input account remains unchanged.
Positive [Max,Max,-Max] completes to Max; negative [Min,-Max,Max] completes to Min.
The original long Sum rejects some valid prefixes before cancellation.

NetWorthCalculator now completes each of its five original WealthGroup sums in
checked Int128 and narrows that component to checked long. Account/date/archive
filters, first currency spelling/case grouping/ordinal key order, part enumeration,
debt magnitude, composition, rates, public component bounds and total bounds remain.
An unsupported completed Money component still refuses even when another group
could offset the final total. Positive gross composition of two Max assets still
refuses; an exact net never grants a wider public gross composition.
No writer, migration/model, financial effect, runtime or GC change is made.

18new WealthCompletedGroupTests conditions cover twelve signed source permutations,
four genuine completed-component refusals, original currency/date/archive-zero
scope and retained archived-nonzero members. They pass once in serviced host10.0.14,
once on original Android x64 and once on Samsung ARM64. These are18new conditions,
not54new unit tests. Seven affected existing host cases pass separately. The native
assertion adapter gains ordered-array equality and All propagation;12new adapter
controls pass independently, without replaying the18successful host conditions.

## Actual CI failure correlated and corrected

The one due D895 response reports CI42132626690 failure and CodeQL42132626658 success.
Only failed job127612751914 logs are fetched. Windows/Android jobs are successful,
iOS is skipped. Libraries/tests has one actual failure among7601conditions:
WealthAdjustmentCancellationTests.An_invalid_chronological_balance_is_not_rescued.
The same one failure reproduces locally on the current test binary.

That D862 test predates D875/aa7331c9. D875 intentionally defines Balance as a complete
selected end-of-day net, not storage-prefix validation. The unchanged example's
selected third-day end is Max and its corrections are Max; an earlier selected
second-day end remains Max+20 and must refuse. The renamed single test now asserts
both outcomes, zero remainder and unchanged opening/effects. That changed condition
passes alone. No production Balance/WealthHistory relaxation, entire suite rerun,
workflow retry or financial replay occurs. Previous historical failure is retained.

## Actual original Reports UI and preserved source

Fresh full original-app review UID10262/PID13795 executes the18new calculations with
all20typed source components exact and account/entry/schedule0. One new protected
fixture then creates three Checking opening-metadata rows [Min,-Max,Max], Advanced
presentation and completed onboarding; entry/schedule0. No financial Save command,
Import, Undo, purchase, authentication, photo or old successful fixture is replayed.
The exact post-setup source SHA256 is
2BEEBFB64C3A4441543A8302CF5B111FF21A9B452BBDD7AAD1ABE1799DF0F558.

Private observer compile failures CS1061/CS0019 and its premature pre-window Shell
ArgumentNullException remain. A necessary observer-only no-reset upgrade continues
as13999/sameUID and read-only adopts that exact complete source, without calculation
or setup replay. Its immediate post-route CurrentPage check is premature and returns
InvalidOperationException, while the original Reports tab subsequently appears.
An original generic error dialog is dismissed once and the original reminder
introduction receives Not now/no grant. The exclusive origin of that generic error
is not established. Native UI, not unbound observer-model flags, qualifies the result.

One actual visible Holdings chip selects the original report. Native XML contains
Money/net worth -92,233,720,368,547,758.08 EUR and positive composition
92,233,720,368,547,758.07 EUR with100.0percent. Only the unfinished lower composition
region is scrolled into positive visible bounds. The complete20typed source remains
exact/threeaccounts/entry0/schedule0. Observer page/model fields stay unbound after
the asynchronous route: Ready=false/Failed=true/Cases0 in that continuation packet
are explicitly disqualified as actual report state. The earlier18case success belongs
to13795; it is not fabricated into13999. This proves original report caption values
and bounded visibility, not glyph/ellipsis/whole-layout/accessibility acceptance.

## Current packages, ARM64 and restoration

Strict Windows and full offline384AOT/408ELF/1608staticLOAD shipping pass without
errors/warnings. Shipping764e6fa3 is prepared, not installed on Main. Full original
native reviews192AOT/204ELF/804staticLOAD d539dd62 and continuation48d10795, and
SDK-only ARM64 review6e71d446/27AOT/37ELF/138staticLOAD pass their bounded ZIP/CRC,
static16KiB, offline/nondebuggable/signature checks. Full native/shipping use v2/v3;
min36 SDK-only ARM64 uses v3. Original Mono module hashes remain unchanged.
Six compiler-document bindings and four packaged own-Core AOT bindings connect
current source/tests to the actual artifacts; no whole-PE identity claim is made.

Samsung SDK-only UID10427/PID24075 executes18cases once; ordered pass digest
6671E2C6048981DFBE63AABF14FB6B144E34A70CBD0C9DB260CD5BF504B5B029 matches.
Only that fresh package retires. Original Main12955/2d420ef9/UID10380/CE155299/DE154603,
other original public packages/grants, five OS values, boot and geometry remain exact;
normal launcher return preserves12955. No Main upgrade/private DB/key read occurs.

Only fresh emulator review UID10262 retires; five owned XML167426bytes are hash-bound
before removal. Original emulator5606/public packages/OS/locale-key presence/boot/
geometry/reverse remain exact. Retained Q02 returns normally as the same12833/UID10260;
its original ready frame and full22table/32field source equal the preserved D895
restoration receipt. No reset, new cold performance sample or source preparation.
Seven scoped standard Cleans pass; signed used packages, evidence and private source
remain. No supplemental deletion or previous frozen scope is touched.

## Remaining frontier

This closes the actual complete-wealth-group defect and the stale reported CI test
contract. It does not accept whole-project performance, original native-fault remedy,
security/whole private DB/key, all accessibility, iOS, genuine external providers,
Store or release. D838new-PC approval remains pending; D841reader is not deployed.
D607/auth terminal cohorts remain terminal. D895candidate remains rejected without
rescue. The delivery goal remains active and the next CI is one bounded due check;
independent development does not wait for CI.
