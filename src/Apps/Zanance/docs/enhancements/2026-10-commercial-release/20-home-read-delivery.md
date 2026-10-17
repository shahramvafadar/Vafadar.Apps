# D-303 complete Home history and materialization

## Delivered behavior

The existing complete Home read uses a descending physical table scan, then stable in-memory Date/CreatedAt
ordering. Reading every encrypted row through the date index caused repeated page visits beyond the unchanged
32 MiB operation-local cache on the tenfold fixture. History, equal-key reverse-index ties, deleted-row gaps,
financial calculations, original-file authority, transaction lifetime and cancellation are retained. No period
filter, reduced history, SQLite sort file, persistent pragma or weaker cryptography is introduced.

EF's convention chose the parameterless LedgerEntry constructor and generated a new version-7 identifier before
replacing it with the persisted key. The supported compiled-model Customize hook now binds the existing public
Guid constructor. New entries retain their ordinary identifier generation. Actual Android AOT metadata reports
`["id"]`; the prior Windows model-only inspection reported `[]` without opening or creating a database.
The pinned EF command refreshed the compiled model. Schema/properties are unchanged; no database
migration is needed. The generated model identity and hand-written partial extension are tracked separately.

## Same-data runtime comparison

Both native variants use the original production Home page/view model, Release AOT/trim, the same qualified
x64 native cipher, retained encrypted databases and original device keys on the owned emulator. There are two
publications per language (English, Persian, German) for each shape. No fixture or key is regenerated and no
financial Save occurs. The final comparison checks canonical financial presentations as well as complete raw
schema/header/integrity/typed rows against the existing encrypted immutable rollback after timing.

| Shape | Entries / accounts / schedules | Before median | After median | Reduction |
|---|---|---|---|---|
| Reference | 10,000 / 20 / 100 | 3,066.82 ms | 2,252.00 ms | 26.57% |
| Tenfold | 100,000 / 200 / 1,000 | 14,828.37 ms | 7,418.54 ms | 49.97% |

The optimized native full-row stepping takes 11.07 / 74.53 ms; this excludes CLR conversion and sorting.
Managed conversion, projection, revalidation and native layout remain part of the full timings above. Dispatcher
gaps remain observable; these results do not close ANR, low-memory, two-second, cold-start or physical-phone gates.

Independent hidden-window Windows checks also preserve complete portable data and original native balances.
Tenfold first/second publication: 1,273.12/1,240.48 ms before, 1,212.71/1,133.29 ms after. Reference first/second:
646.41/602.68 ms before, 882.12/544.89 ms after. These small samples do not establish a broad Windows speed claim;
Windows remains a development check, outside current commercial delivery scope.

## Relevant verification and retained negative evidence

- All 881 Data tests pass: zero errors/failures/skips. Two new AT-303 cases compare complete original public
  entry packets, dates/timestamps/time-zone offsets, equal sort keys and deletion gaps. Migration, compiled-model,
  materialization and existing financial/data behavior are covered by this one Data run. The earlier 18 focused
  Home cases passed before the constructor change; the changed global materialization justified the one broader
  Data run. The historical 3,753 whole-suite baseline is not represented as a new full run.
- Ordinary Windows and independent Windows/Android strict builds pass with zero warnings/errors. Final main
  APK and running-install evidence are recorded below. Unrelated successful currency, billing, synchronization,
  native upstream and service qualification are not repeated.
- An early Windows launch overlapped its unfinished build and produced file-lock retries; that attempt is excluded.
  Only the subsequent clean build and completed hidden-window run supply the baseline.
- The first Android experiment held a portable backup image during timing. Those durations are excluded. The
  accepted comparison holds no backup image while timing and validates the existing encrypted rollback afterward.
- The clean baseline completed all six cases and raw preservation, then refused final immutable report publication
  because an older phase-only report name existed. Its original failure and all completed timings remain retained;
  those successful cases were not replayed. Reports/progress now include the explicit comparison identity. The
  optimized comparison publishes successfully with the same canonical baseline packets and full preservation.
- The first revised measurement caller had two compile errors (a local name collision and missing metadata local).
  They were corrected before installation; unsuccessful builds are retained, not counted as runtime evidence.

Evidence: `artifacts/home-performance-d303`, especially `android-clean-original.json` (the explicit final-publication
failure with retained completed cases), `android-optimized.json`, `android-comparison-summary.json`, exact-PID
stage receipts, `data-qualified-tests.xml`, strict build logs and `after-clean/home-workload-proof.json`.
The independent comparison/caller records its loaded module identities and APK hash. It never exposes keys,
connection strings, private finance rows or native exception messages. Temporary stage instrumentation is removed
from shipping application sources. Future comparisons need a fresh identity and must never reuse timing after a rebuild.

## Complete main package

Complete main signed Release APK: `artifacts/android-home-read-d303-qualified/pro.vafadar.zanance-Signed.apk`.

- Bytes: 95768479.
- SHA-256: `c5d9177aef4bd3cac4859273db26a3c4dfb4cf9c34159ecf91f1ed0dcee9bad4`.
- Permanent package `pro.vafadar.zanance`; both arm64/x64 embedded AOT and unchanged qualified cipher hashes verified.
- Signature v2/v3 verified with the existing local test certificate, not a Store production certificate.
- Build-AndroidApk.ps1 completes its strict warning check successfully. Ordinary Windows and independent caller
  builds likewise have zero warnings/errors. No signing key, client id, backup password or native secret is emitted.
- The original main emulator installation is upgraded without reset. Exact PID 6606 opens Home with the retained
  My account and original 0.00 EUR. The first native dump request failed while the same launch was starting; the
  subsequent observation of the same PID succeeds with 135 own-package nodes. No restart, Save, settings change,
  flag removal or repeated successful UI matrix is used. This UI check is separate from full raw preservation
  of both independently owned large workloads.
- Main/UI/package receipts: `main-package-proof.json`, `main-launch.json`, `main-ui-proof.json` and the original
  attempt observations in the evidence directory.

No Store, physical-device, iOS, OAuth, real biometric, screen-reader or release acceptance follows from these
local checks or subsequent CI.
QA-06 remains In progress for the explicit remaining performance/platform work.


## Actual one-use hosted result

D-303 commit `f5e673fb5f38a75f703c4c9e22bc0eb5b8dde838` was checked once; its follow-up was deleted.
[CI](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39012610787),
[CodeQL](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39012610825) and
[Self-hosted service qualification](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39012610790)
completed successfully. [Local identity backend](https://github.com/shahramvafadar/Vafadar.Apps/actions/runs/39012610813)
failed in the HTTP purchase-retirement fixture. D-304 reproduces its shared-peer HTTP429 and repairs only the
fixture isolation; the application Home read is unrelated. Do not report the original backend run as successful.
