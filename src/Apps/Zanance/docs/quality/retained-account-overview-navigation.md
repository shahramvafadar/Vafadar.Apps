# Refresh the original retained Accounts overview (D-577)

## Reproduced behavior and correction

The original Accounts list was opened on a freshly owned fictitious source after an
expense/income, showing source97EUR, destination0EUR and total97EUR. After one original
20EUR transfer and a linked4EUR refund on other tabs, Home showed101EUR and the complete
ledger had four accepted rows. Returning to the same retained Accounts page still showed
97/0/97. A later read-only observation confirmed that this was not a transient loading frame.
The original loader ran only from `Page.OnAppearing`; the existing Shell retained-tab
dispatch included account details and forecast pages but omitted the account list.

`AccountsPage.RefreshForNavigationAsync` now shares an outstanding original load between
appearance and native retained-tab activation. The existing guarded Shell handler invokes
it for the original Accounts page on Shell item/section/content changes, while retaining
the retired-shell check and all other destinations. The existing calculation, store,
financial writers, identity, Back navigation, rows, visual layout and source rules stay
unchanged. No financial operation is performed by this refresh.

## Native result without financial replay

The same original source from [D-576](coreclr-original-ui-feasibility.md) was retained
through the corrected caller upgrade. No opening, account, expense, income, transfer,
refund, seed or key was recreated. The complete protected schema/header/typed-row digest
was exact before and after the correction and every subsequent native context.

The first real retained-tab return showed81/20/101EUR on the same original Accounts page.
An independent observer recorded the actual ShellSectionChanged event and the original
completed page-read task; it did not invoke a refresh or synthesize navigation. Six more
English/Persian/German × Light/Dark contexts reused one original Accounts instance and
displayed the correct locale-specific amounts. Original page identities and actual native
read tasks are retained. This is finite native evidence, not six new unit tests, full
account/concurrency acceptance or a spoken/scaled accessibility pass.

## Strict package, actual byte admission and Main adoption

Strict Windows and complete signed offline Android builds pass without warnings/errors.
The independent experiment does not select the production runtime. The actual product is
Mono/SGen on both ABIs, with no CoreCLR/JIT library or experimental provider in its manifest.
Its actual Core/Data/compression PE images and four native engines match the accepted
623b product byte for byte. Original Java/non-library resources match all1422
paths and bytes. The APK has384 own AOT libraries/408 ELF files,1608 statically16KiB-aligned
load segments, valid ZIP/v2/v3, and no INTERNET permission or debug/profile flag.

Complete product: `artifacts/d577-final-shipping-package/pro.vafadar.zanance-Signed.apk`.
SHA256: `b57725fbd4ffb6f4c11640ed6dd7185de2a4b24865d2302dff88d14b8ffd4b3e`.
Size:97,058,719 bytes. This is a local unreleased candidate, not Store/publication approval.

The first strict output was refused before installation because dependency source-link/
generated-path metadata differed. That metadata alone was corrected; no financial source
or stored comparison was changed. A verifier then selected the wrong historical D575
artifact through an over-broad filename substitution. That refusal remains; the corrected
comparison explicitly binds the actually used623b SHA. A resource-definition refusal that
omitted96 Java metadata resources is likewise retained; the final definition includes them
and excludes only the original three signature-envelope files. No successful suite or
financial command was replayed for these observer corrections. The adapted verifier also
carried an inherited `FrozenSourcesExact` label without a separately recorded whole-app
pre-build freeze in this batch. That label was not an admission predicate and is explicitly
excluded from the measured final admission; actual PE/native/resource bytes, reviewed
source scope and native execution are the evidence, not an unmeasured source-freeze claim.

Main received the complete signed Mono candidate once with no reset. UID10380, CE155299,
DE154603, permissions, Home979 and original system settings were preserved; actual Main
PID12021 is recorded. The original Main Accounts page also performed the real Mono tab
return, preserving979 and its113 native nodes. All content, bounds, roles and commands
matched; one ScrollView drawing-order changed2→1. Main's141 attributes were exact directly
after adoption. After the relevant navigation, one Home ScrollView drawing-order changed
1→2; all other attributes remained exact. Raw snapshots and the initial strict observer
refusals are retained, without calling these final trees bitwise identical or normalizing
financial data. Only the unfinished observation was interpreted; accepted tab actions were
not replayed. Previous three-profile evidence was retained without unrelated re-enumeration;
private Main storage/keys and complete database equality were not inspected or claimed.

The fresh experimental package was retired after controlled process-death/source acceptance.
The retained b1d5 caller's APK/UID/CE/DE/permissions and process absence, Main candidate,
timeout/font/stay-on/reader settings and local evidence were preserved. Owned-output cleanup
is recorded in D-576. No old successful test, unrelated device matrix or accepted money was
repeated; the six new managed cases belong to D-576's actual source-admission policy.

General stability/performance, the two-second target, complete accessibility, other-device,
iOS/provider/Store/public-service and product release gates remain open. D567 and the main
delivery goal remain active.
