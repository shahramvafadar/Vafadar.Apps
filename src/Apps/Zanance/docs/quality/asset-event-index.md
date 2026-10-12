# Original asset event slices (D-178 / AT-177)

Status: implemented and locally verified in Core, Windows and normal Release Android; external acceptance remains open.

## Change

Home previously enumerated the complete holdings-event snapshot for each active asset type.
AssetEventIndex enumerates that unchanged snapshot once, retaining original identity/order in
asset-type slices. Home still calls the original HoldingsLedger.Quantity for each type and date.
No formulas, cross-unit totals, overflow handling, financial writes, schema, permissions, SDK,
network, cached rights, device security or portable-format policy change. The index is projection-local,
not a cache shared across profiles or reads.

Ten AT-177 cases cover all seven event kinds, past/current/future dates, different types, original
identity/order, one source enumeration, unused types, neutral location transfer, retained metadata
and checked intermediate overflow before a later outflow. Main: 3,179 passed, zero failures/skips,
2m 44.364s; test output cleaned. Strict builds have zero warnings/errors; full signed Release built.

## Bounded computation measurement

Independent fictitious Core-only Release process, 500 types, original calculator in both paths:

| Events | Original input visits | Indexed input visits | Original ms | Indexed ms |
|---|---:|---:|---:|---:|
| 10,000 | 5,000,000 | 10,000 | 62.4488 | 4.4046 |
| 100,000 | 50,000,000 | 100,000 | 226.2461 | 5.5359 |

Index construction is included; per-type slices are still enumerated by the original calculator.
All original quantities match exactly. This measures that computation, not whole Home, cold start,
the two-second target, ANR, a physical phone or iOS. No unrelated successful layout matrix is repeated.

## Native scope

Six actual Windows caption contexts, en/fa/de and light/dark at 412 px, retain original asset packets
and repeated captions, with native TextBlock text observed. The first Persian assertion compared
raw model text to native digit-shaped text; retained evidence shows 18k gold versus Persian-shaped
name digits. Final review applies the original NativeDigits mapper; no product digit change.
Original Windows database/sidecars restored byte-for-byte.

Android's initial sample caption was absent because the main profile was open while the fixture
was in the independently owned QA03 Native file. That check is not accepted. Explicit original
profile selection exposes the seeded mass/count caption; known quantities are 13.690 g and 6 tokens,
with neutral location transfer and future event excluded. Six en/fa/de light/dark display/return cases pass. All 22 fixture domain tables match the
expected seed before exact baseline/sidecar restoration. All 23 original Main tables remain unchanged;
Main is reselected, English/System restored and the app stopped. No account/credential or OS setting changes.


## Complete phone-test artifact

Full Release APK: `artifacts/android/zanance-d178-release.apk`, 82,479,019 bytes; SHA-256
`998fa1769988895cd30269ea9456ce30d7551ad04dc648001183f0fee2aa577b`. Package
`pro.vafadar.zanance`, API 24/36, v2/v3 signature verified; ZIP CRCs, both native assembly
stores and app AOT libraries verified. Installed emulator APK matches that hash exactly.
The local test certificate and emulator do not establish phone, iOS or Store acceptance.
