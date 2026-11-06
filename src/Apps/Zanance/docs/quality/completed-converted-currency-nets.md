# Complete converted currency nets (D-897)

## Actual original defect

The original RateTable.Combine uses a checked long prefix accumulator after each
individual conversion. Original serviced.NET10.0.14/X64 probe64336 examines all six
orders for each signed three-currency net. Four actual OverflowExceptions/eight
passes occur for valid completed Max or Min. Every original input remains exact.
These are fictitious EUR/USD/GBP amounts and unit rates, not market-rate evidence.
The original compiler document is bound to the saved pre-correction source/PDB.

The correction changes only the combination accumulator to checked Int128 and checks
the completed known net back to long before deciding complete/incomplete publication.
TryConvert remains unchanged: each individual conversion must fit public money,
uses original currency minor digits and midpoint-away rounding, and selects only
original dated direct/inverse evidence. Missing currency order, rate evidence order,
estimate/outdated flags, oldest date, original report-currency spelling and null
incomplete totals remain. A genuinely unsupported known subtotal still refuses even
when another missing currency might offset a guessed amount. No amount, rate,
financial writer, schema/model, crypto, runtime or GC policy changes.

## Selected checks and current artifact binding

20new ConvertedNetCompletionTests conditions cover twelve signed source permutations,
four true completed-known overflow controls including missing-rate inputs, two true
individual conversion overflows, exact missing evidence after cancellation, and
per-currency rounding/future-rate refusal including zero-valued missing currency.
All20pass once on serviced host, once in original-app Android x64 and once on Samsung
ARM64. Five existing RateTableTests pass separately in the same selected host run:
25selected host conditions, not a full suite or60new tests. Complete ordered original
amount/rate properties are compared before/after. The prior assertion adapter is
byte-exact and its successful controls are not rerun.

Strict Windows and full offline shipping pass without warnings/errors. Shipping
3f4ea438 has384AOT/408ELF/1608staticLOAD, v2/v3, ZIP/CRC/static16KiB and nondebuggable
offline checks. Full original x64 faff2585 has192/204/804; SDK-only ARM64 614d4f88 has
27/37/138/min36/v3. Original Mono module hashes remain. Four current compiler-document
bindings plus the original baseline document and four own-Core AOT bindings connect
actual source/tests and package members. No whole-PE or hardware16KiB claim is made.

## Original account overview, not only scalar calls

Fresh original-app UID10263/PID14916 completes20calculations on the empty protected
source, with all20typed components exact/account0/entry0/schedule0. One new fixture
then creates three Checking opening-metadata rows in EUR/USD/GBP with amounts
Min/-Max/Max, two fictitious dated unit rates and Advanced/valuation preferences.
No financial Save command, accepted old fixture, Import, Undo, authentication,
photo, purchase or existing rate operation is replayed.

The original AccountsPage loads once as Ready/three currency totals/CanInteract.
Its original ConvertedText and actual native TextView agree on combined
-92,233,720,368,547,758.08 EUR with estimate and outdated-rate disclosure. Native
bounds/visible bounds [87,843,994,925] are exact, GlobalVisible=true, ellipsis0;
one current XML agrees. Full20typed source SHA256 remains
A97BB1E1F1C095E18A90CC79798A80EC544237305840FC7B21507E42BAC0D74D.
There are three accounts, zero entries/schedules and two rates. The original root
is restored; only this fresh UID and its hash-bound XML37735bytes retire.
This is bounded caption/layout evidence, not pixel/glyph/all accessibility acceptance.

## ARM64, complete Main adoption and retained cohorts

Samsung SDK-only UID10428/PID26118 executes20current original-Core conditions once;
ordered pass digest731017C1DE765B0128E392F313C75D7A53CC4C91749213607E9E4A767D99B58B
matches. Only that fresh package retires, with original Main12955/public state exact.
The already prepared complete shipping3f4ea438 is then admitted with the original
public signer92cf83df and installed exactly once without reset on Main. Current
Main27699/UID10380/CE155299/DE154603 retains Home979EUR and original public packages,
grants, five OS values, boot and physical geometry; only expected APK/PID change.

The initial fixed141portrait-node assumption refuses. Both original saved XMLs
already show landscape rotation1 and102owned nodes, before and after upgrade.
Reevaluation of those same packets proves every raw attribute/order exact. No extra
frame, orientation change, relaunch, reload or passing-frame selection occurs.
The rejected count oracle remains. Two phone XML75732bytes are hash-retired after
local evidence preservation; current Main27699/public state remain exact. The private
Main DB/key/complete financial rows are not read, so their equality is not claimed.

Original emulator5606/public OS/packages/locale-key presence/boot/geometry/reverse
remain exact. Q02 returns normally as the same12833/UID10260, original ready frame
and full22table/32field retained source exact; no cold timing or preparation replay.
Seven scoped standard Cleans pass; used signed packages/source/evidence remain.
No supplemental deletion, frozen scope, original emulator reset/shutdown or R13
reader action occurs. No old successful suite or financial operation is repeated.

## Delivery frontier

This closes the converted-net source-order defect and current complete Main adoption.
It does not prove whole-project performance, original native-fault remedy, security/
whole private DB/key, all accessibility, iOS, genuine providers, Store or release.
D838new-PC approval stays pending and D841reader is undeployed. Terminal D607/auth
cohorts and rejected performance candidates stay terminal. D896CI remains pending
for its one due request; this work does not wait for CI. The delivery goal is active.
