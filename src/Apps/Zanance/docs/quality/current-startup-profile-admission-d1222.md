# Current startup profile name admission (D1222)

The owner requests continuing cold-start optimization toward 6,000 ms, with the
overall goal paused. D300 permits refreshing the actual Home startup method profile
after relevant startup/dependency changes. D320 requires keeping SDK defaults and
excluding the previously unsafe extra CoreLib roots; a profile refresh does not
waive retained-data/native qualification.

This first admission is static, read-only metadata. The unchanged custom profile
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
No capture, replacement profile, new AOT build or speed acceptance has occurred
in this admission. The [fixed orphan trial](fixed-orphan-delete-cold-d1221.md) stays
rejected independently.
