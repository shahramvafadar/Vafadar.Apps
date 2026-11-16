# Current startup profile admission and controlled capture (D1222)

The owner requests continuing cold-start optimization toward 6,000 ms, with the
overall goal paused. D300 permits refreshing the actual Home startup method profile
after relevant startup/dependency changes. D320 requires keeping SDK defaults and
excluding the previously unsafe extra CoreLib roots; a profile refresh does not
waive retained-data/native qualification.

## Initial static admission

The first admission is static, read-only metadata. The unchanged custom profile
SHA 51a838bac4bffe91c093fcae7fd26f9b5bc5cdb57cba251bf4f2bd89dcd09231
contains 87 image/4,669 type/2,720 generic-instantiation/18,833 method records in
2,631,193 bytes. The retained original public Mono aot.h/aot.c format validates
exact end-of-file, record identities and type/image/instantiation references.
Only 1,680 App/Core/Data method metadata records are selected. No profile token,
MVID, method root or production build input is rewritten.

Actual used compiler metadata for the unchanged original HomeViewModel source
753AD8CE4EF5CF6CF0CEAD2A11190FFD44316B35D35599DF6F0C52B4CEA8C456
shows real generated type-name drift:

| Existing custom profile type suffix | Current compiler type suffix |
| --- | --- |
| HomeViewModel/<LoadAsync>d__207 | HomeViewModel/<LoadAsync>d__208 |
| HomeViewModel/<LoadCoreAsync>d__208 | HomeViewModel/<LoadCoreAsync>d__209 |
| HomeViewModel/<LoadBudgetAsync>d__209 | HomeViewModel/<LoadBudgetAsync>d__211 |
| HomeViewModel/<LoadForecastAsync>d__210 | HomeViewModel/<LoadForecastAsync>d__212 |
| HomeViewModel/<LoadPlansAsync>d__221 | HomeViewModel/<LoadPlansAsync>d__223 |
| HomeViewModel/<BuildSlicesAsync>d__222 | HomeViewModel/<BuildSlicesAsync>d__224 |

Ten old nested type names referenced by the profile are absent from current
Home metadata; nineteen current nested names have no exact type root in this
custom profile. This does not prove all those methods uncompiled: SDK defaults,
transitive AOT compilation and actual native method coverage are not evaluated.
Module MVID mismatch is explicitly not used as a rejection criterion. Only type
and method names are read from owned compiler PE metadata; no IL, user strings,
fields, resources, configuration, keys, heap or device state is inspected.
An initial wrong Core output-directory suffix is retained as a reader-path refusal;
only the corrected bounded metadata read completes. No test/build/device/timing or
successful measurement is repeated for this admission.

The next independent prerequisite is an actual current loaded-Home capture in the
owned fictitious emulator, with app-local profiler environment only, preserving
original complete source and SDK defaults. Remove the recorder/environment and
owned forward afterward; qualify an ordinary signed Release candidate before any
promotion. Do not use the SDK capture target that writes global debug.mono.profile,
reintroduce additional CoreLib roots, rewrite profile tokens by hand, reset data,
skip access/crypto/license order, or interpret this gap as a proven latency cause.
At static admission no capture or replacement profile existed. The follow-through
below supplies one actual capture; ordinary compilation and native speed are still
separate gates. The [fixed orphan trial](fixed-orphan-delete-cold-d1221.md) stays
rejected independently.


## One actual current loaded-Home capture

The same owned API36/X64/4,096-byte-page AVD is normally started with its original
100k-entry/200-account/1,000-plan data and keys, without reset or a phone action.
Initial ADB offline and original-source pending observations are preserved; the
same host and PID 2936 continue to a complete source/Home match. No old cold/warm
measurement or failed/successful search is reissued.

A separate strict ordinary-source diagnostic build uses JIT only for recording,
the previously reviewed cached MIT profiler, an app-local AndroidEnvironment and
temporary INTERNET permission for the local recorder. It is not an offline
shipping package. The
SDK default capture target/global debug.mono.profile write is never used.
Initial XML encoding-alias and manifest namespace build refusals are corrected
before installation; no accepted device case is repeated for them.

The actual signed non-debuggable package has zero AOT libraries, 13 ELF libraries,
39 static 16KiB LOAD segments, ZIP CRC/alignment and v2/v3 signatures. Actual original
Mono SHA b1bd7e88a1e6f2bc708cbb24766993ebee2cc7a6ea9d91879881d2ac3d42e034
and eleven source/PDB bindings are retained. The extra x64 profiler exactly matches
the reviewed cache file. This diagnostic shape is not a full shipping build.

One source-preserving installation/launch on the same UID 10216 reaches the actual
original Home in PID 3931, with full projection, current page/focus and complete
visible non-ellipsized balance. A unique marker prevents replay. The recorder is
collected once only after that readiness, before a diagnostic all-row audit can
pollute its method roots. No timing or financial command is accepted.
The unique owned host 5999/device 9999 forward is removed afterward; original
forwards and the empty global profiler property remain exact.

The raw profile contains 82 images/5,991 types/25,695 method roots in 3,315,799 bytes,
SHA a6bd31a88ac6366cbf853a53df6c977f54d147da652c986ef78e03a4f1e16446.
Its format contains only image/type/generic-instantiation/method names/signatures,
not argument values, user strings, heap, financial payloads or keys. Exact EOF,
record identities and all references are validated.

The first skip-only filter output is byte-identical to the raw input and still
contains 5,893 CoreLib/27 observer roots; it is refused before any AOT build. A
corrected positive-inclusion predicate in the cached standard metadata reader/
writer implements the same predeclared exclusions, without another capture or
manual token/MVID rewrite. The corrected candidate has zero extra CoreLib or
recorder method roots, 19,775 methods, 136 actual Home roots, and all seven current
Home async MoveNext type names. Serialized dependency metadata contains 82 image/
5,983 type records; the filtered selected-list summary 77/5,981 is not substituted
for these actual serialized counts. SDK defaults remain separate and required.

Candidate SHA e23df8bb9c9dd4768a2b00839cf4413d646692e5bae33caa2580fb7b8e1bf2f2
covers 2,800,226 bytes. The tracked startup.aprof is unchanged. This candidate is
an actual current method profile, not evidence that all prior unmatched methods
were uncompiled or that they explain the complete startup delay/native exit.

## Recorder removal and preserved terminal source

The prior original-source 5d53bd88 signed APK is restored once without reset.
Actual PID 4284 reports the complete original source and Home, retained accepted
cold/warm markers and no new timing/search/financial command. Complete source is
9B4B012173B945D58A7EA19B7CB8DA4C12B8024AC09D8D666145AD7171CEFC00;
Home projection is 3A22140A3082CE6B84EE60C7B35B20DA7A1922C8944C2D719FFB1689E318681E.
Original OS values, locale/key presence, geometry, new owned-boot identity,
reverse/forward state and UID 10216 are exact. Original ADB 5037/PID59860/creation
identity survives; Samsung/Main and D607 are not touched. The old stress-search
native fault and consumed register classifiers are not repeated or reinterpreted.

Eighteen used source/PDB/managed binary/APK objects and both actual profiles remain
hash-bound. One standard Clean removes 13,500 registered files/695,448,191 logical
bytes, without supplementary/frozen deletion. The VM/data remain available for
ordinary candidate qualification. The overall goal stays paused.

Actual consumption by the matching 10.0.15 AOT compiler, default-profile retention,
ordinary recorder-free Release/native source preservation and the predeclared
comparative Samsung cold gate remain necessary. There is no product promotion,
6,000-ms acceptance, original fault remedy, whole-performance/security/platform or
release claim from this completed capture.
