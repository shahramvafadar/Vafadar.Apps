# First plan native publication (D-318)

## Reproduction and cause boundary

On the approved SM-A176B, the original main application saved D318_First_Plan once (3 EUR,
Once, no automatic posting or reminder) but its due list remained blank for 48,333 ms over 13
native observations. No segment switch or second Save was used to hide that failure.

The independent original-source Release/AOT caller reproduced it with a separate protected
installation: one stored schedule, one ViewModel row, the original same ItemsSource, three native
adapter items/children including header/footer, but no realized plan name throughout 50 samples
(about 1.54 to 6.90 seconds). Account/settings packets and zero ledger entries were preserved.
This rules out missing persistence or an absent binding as explanations for that reproduction.

MAUI Controls 10.0.110 is pinned to upstream revision 6c379bf1dc24461a985a093be3fd8a3e9bc6fc00.
Its [original Android recycler implementation](https://raw.githubusercontent.com/dotnet/maui/6c379bf1dc24461a985a093be3fd8a3e9bc6fc00/src/Controls/src/Core/Handlers/Items/Android/MauiRecyclerView.cs)
uses a separate empty adapter and swaps back to the item adapter. Avoid this native empty-to-item
adapter transition for Plans: retain the original collection, complete row template, ItemsSource,
header/footer, selection and commands; show existing empty feedback in its own growing scroll
viewport controlled by the existing IsEmpty property. This is a narrowly demonstrated workaround,
not proof of a particular private pooled-holder collision or acceptance of every upstream path.
No package upgrade, native fork, database/model change, cache or financial algorithm change is involved.

The first Windows large-text visual review also exposed a clipped German Add caption at 360 width.
Reuse the existing WrappingAction with its complete scalable caption, semantic primary appearance
and real command-owning button. Keep the persistent bottom Add action outside the scroll viewport.

## Retained native regression

Independent package pro.vafadar.zanance.androidhomereview, ARM64/API36/4096-byte physical pages.
Run f4ea05c12a9d4af1ae5e01667b00c0f2; baseline f24442de7d5a4909957412776237c765 in PID7602.
After the adapter workaround, comparison c3ae5a8faf734104a18e31eb35ebc6d0 in PID10006 displayed
the first plan in the first sample after the original native editor Save (about 2,167 ms including
Save/navigation). This is a functional observation, not a two-second performance acceptance.

Retain the same database/account/keys and original intent. Before a new empty-to-first-row transition,
write an exclusive receipt containing the original known unpaid definition, then remove only that
schedule through PlanStore's original history guard. Do not reset/reseed the installation or create
replacement keys. The original actual editor/native Save runs once; afterwards there is one 300-minor-unit
schedule, zero ledger entries, the same ItemsSource and unchanged full accounts/settings packets.
The main package is not touched by this independent regression. Its complete signed 192-AOT package
hash is 07f33463bfccc1b5c13af2fd3fcf6815556930dce180b2f3817724165cd24193.
The final caption-only wrapper is separately checked below; the adapter regression is not replayed.

Initial caller compilation errors and a missing performance-marker guard are retained as scaffold
failures. The plan-specific caller uses its original nonredirected root instead of requiring an
unrelated performance-workload marker. The original root/run survive that correction. No failure
report or fixture was erased. A host activity-file parsing error after the final successful main
installation was corrected by selecting the exact resolved component; installation was not repeated.

## Final original running layouts and main upgrade

The independent hidden Windows application pro.vafadar.zanance.planpublication checks the actual
PlansPage/ViewModel against one retained fictitious account and an empty original PlanStore. All 18
contexts pass: English/Persian/German, light/dark, 360/412/1280 widths, process-local 200% font stress.
Actual scaled label glyph bounds, named/enabled native Invoke target, at least 44-unit geometry,
scroll reachability, command identity and unchanged complete stored packets are checked. Only its
own hidden window is captured. The pre-wrapper images/progress are retained separately; post-detachment
baseline target dimensions are not treated as geometry evidence. Final dimensions are captured before
retiring the native page. This does not change or accept OS text scaling/screen-reader behavior.

Final standard Windows build and final complete offline Android Release build have zero warnings/errors.
The initial shared-output Windows copy retries were resolved by sequential build; their log is retained.
No unchanged unit suite, crypto probe, APK/device workload, import or plan-payment scenario was replayed.
The unit baseline/test count is unchanged; this batch adds native regression and layout evidence.

The final complete signed main package is artifacts/android-plan-publication-d318-final/pro.vafadar.zanance-Signed.apk:
95,416,053 bytes, SHA256 7d7742dacbf147743873f1ec02e06e62b64eee87e6b303879fc2a7736a4520ed.
Package pro.vafadar.zanance, min API24/target36, ARM64/x64, 384 embedded AOT assemblies, no Internet
permission or debuggable flag. Signature schemes v2/v3 pass with the existing local test certificate
SHA256 92cf83dfc05c274ad7fc99b820f9f1b272ff46eb5fb32b3039267074ed36167b. ARM64 SQLCipher bytes
remain 48d06bc639afdd119833d998da14976c64fc4fb3a9cc43dac682879cea72475c.

Upgrade on the approved phone succeeded without reset. Main's retained D318_First_Plan appears immediately
in Due without first toggling segments; empty Upcoming feedback and return to the same due row also pass.
The unpaid 3 EUR definition is retained, no Save/payment is repeated, and Main returns to Home with
979 EUR and the original source account. Displayed-state preservation is not a new full database digest.
Exclusive input was acknowledged by the other project before testing and released afterward. No system
setting, personal account, real authentication or unrelated app data was changed.

Local artifacts are under artifacts/phone-plan-first-d318 and artifacts/plan-publication-d318/layout;
keep them untracked. Hosted CI, broad physical-device acceptance, cold/ANR/two-second targets, iOS,
real notification/authentication/provider acceptance, store signing and product release remain separate gates.
