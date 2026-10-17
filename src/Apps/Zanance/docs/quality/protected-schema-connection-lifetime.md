# One owned connection during protected schema review (D-309)

## Actual cause and bounded correction

The original ProtectedProfileStorage.UpgradeAsync supplies an externally owned, normally closed, unpooled
keyed connection to EF. History-table existence and history reading each independently open and close it;
a pending real migration adds another open. Real SQLite StateChange observations reproduce two physical
opens for current/unknown history and three for a supported old prefix. Every keyed physical open repeats
key derivation. This is an avoidable bounded cost, not evidence for the dominant whole cold-start delay.

The callback explicitly retains the same handle during its history/pending-suffix operation and restores
its original closed state in finally, including failure/cancellation. An already-open caller keeps its
ownership and open state. Original unknown-prefix rejection, migrations, compiled model, maintenance scope,
access cover, SQLCipher KDF and unpooled policy remain. No global cache, lower derivation cost, extra key
lifetime, schema/native dependency or financial-policy change is introduced.

A suspected stale startup-profile MVID is rejected before another recorder run: existing compiler evidence
resolves the recorded methods by assembly/type/signature rather than recorded MVID. The qualified D-305
profile and expected embedded native libraries are retained. No repeated profile capture is claimed.

## Focused regression and original financial preservation

Before the correction, the actual callback class has seven passes and three failures: closed current/old/
unknown require one open but observe two/three/two. Afterward all ten distinct cases pass. Eight new AT-309
cases cover six history/open-state combinations and two cancellations. Existing unknown history retains
its original rows/tables/history. The strengthened existing old-prefix fact writes only original raw
first-schema columns, then preserves original account/entry/settings ids, account link, EUR currency,
250-unit expense and 750-unit balance through actual pending migrations, including new-setting defaults.

The final strengthened fact alone is rebuilt and rerun: one pass; the other nine qualified results are
retained. This is ten distinct related cases across retained runs, eight new cases, not eleven distinct
cases or a new full-suite pass. Strict test compilation and Windows Release build have zero warnings/errors.
No unchanged Data/core/backend/sync/billing/full-suite, native crypto producer or UI matrix is replayed.

Evidence: artifacts/startup-followthrough-d309/tests-before.xml, tests-final.xml, tests-finance.xml,
closed-open-before.json, tests-corrected-build.log, tests-finance-build.log and windows-final-build.log.
The first Android qualifier compile misses its existing security namespace and is corrected; a line-ending
patch preflight fails before writing, so an unchanged compile is not counted as fixed-source evidence.

## Same retained native data

The independent original-source Release/AOT Android caller adds a schema phase. It invokes the actual
production callback under drained ownership on original keyed files, then compares complete raw schema,
headers, integrity and typed rows against each existing encrypted rollback. No duplicate upgrade algorithm,
main data, financial Save, fixture reset or replacement key belongs to the measurement.

Original retained run: 660765480ebf49e49deb0da6e5b2cb48. Before comparison
6c5415d6826b4aed95754ec4f03d78bb, process 11831: reference two opens / 226.1011 ms; tenfold two opens /
285.3532 ms. Both original closed states and complete data comparisons pass. After comparison is separately
identified against the same files/keys. One observation per shape is not statistical performance evidence.
Observation timeout keeps the original live process; no restart or preparation follows a timeout alone.

## Remaining acceptance

QA-06 remains In progress for remaining composition, cold startup, memory/ANR, physical-phone and iOS.
This scoped correction does not establish two-second Home readiness, hardware behavior, Windows commercial
acceptance, OAuth/provider activation, store signing or product release. Existing current main encryption
remains active; old historical plaintext-stage records are not a description of this delivery.

## Final native callback result

After comparison 0d740c97fa2243a284edd6a6a651ff70, process 12159, loaded caller module
2f71bf42-4838-473b-bbc3-94dd5d0e98d8: reference one open / 179.7276 ms; tenfold one open / 194.102 ms.
Both closed caller states are restored and both complete original raw schema/typed-row comparisons pass.
The original before caller module is 97fddd13-bc1a-4fd5-95d1-97673d466705. These are single observations,
not stable medians or whole cold-start speed. Original process 12159 is retained through observation timeout
and stopped only after its terminal positive report. Before/after immutable APK and report identities are
kept in artifacts/startup-followthrough-d309; no repeat of successful before measurement occurs.

## Ordinary complete main package

The ordinary canonical Release build completes without warnings/errors. Complete signed package:
artifacts/android-schema-startup-d309-qualified/pro.vafadar.zanance-Signed.apk, 95,358,709 bytes,
SHA256 34c3d57662286f7b1d3c1b43872e390269d9adba7e12e84352937be36e0e6afb. Actual permanent package
pro.vafadar.zanance, min24/target36, arm64-v8a and x86_64 embedded App/Data AOT libraries are verified.
Signature v2/v3 passes with the existing local test certificate. No profiler is bundled. This configured
connected package declares INTERNET; it is not an offline or store-signed build. Receipts:
main-package-proof.json, main-signature.txt, main-badging.txt and main-apk-build.log in the delivery artifacts.
The Release test filter output is cleaned once after terminal focused verification; no test replay follows.

The original main installation upgrades successfully without reset. Its actual process 12559 first shows
the original Loading cover, then reaches Home with the existing zero-EUR readout (140 own-package nodes).
No account, settings, financial Save or authentication change is made. The initial hierarchy is preserved
separately from the ready hierarchy; Activity launch timing is not Home readiness or a performance claim.
Receipt: main-native-proof.json and main-home-first.xml/main-home-ready.xml. No repeated page matrix belongs
to this nonvisual callback change. The original main process remains healthy after verification.
