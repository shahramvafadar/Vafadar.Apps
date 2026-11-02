# Original Google Play pending-task ownership

## D-805 - Observed failure and correction

The existing Android GooglePlayBillingStore.PurchaseAsync refuses a second purchase
while the first SDK task is pending. Its catch previously faulted the shared _payment,
including when the refusing caller had created no task. In the exact original-source
host reproduction, the second call reports OriginalStorePaymentPending, but the first
unexpectedly completes StoreLaunchRefused. One simulated SDK launch occurs; the original
native outcome is lost. This is an actual adapter-logic failure under a controlled SDK
boundary, not a real paid transaction or proof of an observed customer purchase failure.

Keep the existing intent/account binding validation, then reject an already pending task
before connection setup. Retain the existing check under _queries for callers racing
through connection setup. A call keeps a local ownPayment only after creating its new
SDK task; synchronous launch-result refusal and catch can complete only that local
owner. A pre-creation refusal cannot fault the previous purchase. Preserve original
callbacks, observer-only cancellation, continuation intent, SKU/replacement/price,
server verification and acknowledgement policies. No entitlement or successful native
purchase is invented. Existing ambiguous synchronous SDK-throw handling is not newly
qualified as a general native-uncertainty solution.

## Six source-linked regressions and integration

A dedicated Vafadar.Zanance.BillingAdapter.Tests project defines BILLING locally and
links the complete original GooglePlayBillingStore.cs and IBillingNativeStore.cs,
referencing current Core. Include it in the main solution and non-MAUI test filter.
Ordinary offline App.Tests symbols and their existing policy checks are unchanged.
Its typed inert SDK/JNI/main-thread boundaries create no real connection or purchase.
The fixture supplies only local SDK readiness/offer handles; the original PurchaseAsync
creates its own pending task. No purchase token, receipt or verified capability is
created or exported.

One strict build has zero warnings/errors. One actual .NET10.0.14 execution passes
six new cases, no failures/skips: pending second refusal; two callers admitted before
one connection callback; cancelled original observer and resumable continuation;
no readiness reread/reconnection for known pending work; own synchronous launch
exception still faulting its own task; synchronous SDK cancellation retaining
StoreCanceled. The original failing host run and separate passed test execution remain.
Successful old billing sandbox/financial/authentication cohorts are not replayed.

## Actual SDK build and bounded compiled Android observation

The affected full Billing Sandbox Android edition builds strictly with official existing
Xamarin.Android.Google.BillingClient9.1.0.2. Complete signed product APK db2ba079830a4994
has386ownAOT/410ELF/1616staticLOAD across ARM64/x64; independent x64 de9e58afcc0c39fe has
193/205/808. Both pass ZIP CRC, v2/v3 signatures, no-debuggable and ZIP/static16KiB
checks. These are Billing editions with INTERNET and com.android.vending.BILLING,
not the default offline distribution. Original Mono runtime14 ABI digests are unchanged;
static16KiB is not physical16KiB runtime acceptance. Portable-PDB document checksums
for the actual changed adapter match current working source in both builds:
daf82d939824f369fd0db4cc5b54532298a6963b325a163462c889040ec6444c.
The private provider/hook is absent from the product manifest. No whole Core/Data PE
identity or new general Windows/default-offline build is claimed; this Android/BILLING
file is excluded from those unchanged targets, so their successful builds and physical
Main installation are not repeated.

Fresh owned API36/x64/4096 emulator UID10216/PID3755 reaches original OnboardingPage
without account/PIN/ledger setup. The immediate post-launch pidof returns no process;
a following identity observation binds the same original launch. A premature host
status read also refuses the missing binding. Neither failure triggers another launch,
install, restart or successful test replay. StartupPage is observed before original
Onboarding readiness; no forced readiness is used.

The private caller constructs a separate actual compiled GooglePlayBillingStore and
supplies only a synthetic pending TaskCompletionSource and synthetic intent by scoped
reflection. This is deliberately not a real pending Play modal. ContinuePaymentAsync
is pending. One actual PurchaseAsync for another synthetic intent refuses
OriginalStorePaymentPending; the prior task/intent/continuation remain uncompleted and
the actual _client field stays null. Close only that synthetic task after recording,
with cancellation; its continuation closes. No SDK client, connection, purchase launch,
reference, successful outcome or entitlement is created by this observation. The six
host cases cover the separate connection-race/catch ownership logic; this compiled
native case qualifies only preconnection admission, not genuine Google Play UI/payment.

Original account/entry/schedule counts are0 before/after, not whole typed-source/DB
proof. Root remains Onboarding, same PID/UID/boot; five settings, locales, geometry and
ADB reverse state match exactly without setting writes. No phone command, photo,
financial Save/Import/Undo, owner authentication or Main private read occurs.

## Retirement and remaining gates

Only the fresh independent package retires. No UI XML is created. Used signed packages,
original/final private caller sources and all failure/test/build/native receipts remain.
Original host40188/QEMU30644 process handles are latched before one owned emulator stop;
both actually end. Official AVD registry/resolved path is checked inside its fresh root
before deletion;25files/4524356986logicalbytes retire, six files/7732bytes remain without
supplemental deletion. Shared ADB5038/PID66176 is preserved. Five standard Clean operations remove27692files/1539158027logicalbytes;3229files/472159664bytes remain, without supplemental deletion.
All prior frozen/refused scopes stay untouched; no alternate cleanup or reuse occurs.

Default physical Main27d8070b/last observedPID1069 remains the D804 baseline; this batch
makes no fresh liveness assertion or Billing-edition phone adoption. D752 original
interrupted frame and accepted CSV/backup/PDF/photo successors remain retired. This
corrects the finite retained-task defect, not genuine Store/provider acceptance,
whole security/DB/key, original native crash cause, two-second Home performance,
complete accessibility, iOS or release. Overall delivery remains active; no owner
response is needed for this completed engineering boundary.

Related: [execution frontier](remaining-delivery-evidence.md),
[original camera ownership](original-camera-result-ownership.md).
