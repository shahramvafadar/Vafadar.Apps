# Bound muted text and archived-category source admission (D-865)

## Named remaining contrast question

A failing numerical pair is not automatically a failing UI control. Before changing
Muted globally, inspect its actual declared text surfaces and the remaining faded
archived-category group. Baseline is f4a21afc; product code, current Main and prior
accepted runtime/layout cases stay unchanged. This closes only the named source
premise for an immediate global recolor, not ZCR-A11Y-03 or whole accessibility.

[W3C Contrast Minimum](https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html)
sets4.5:1 for ordinary text, including placeholders; ratios must not be rounded up to
meet the threshold. Decorative content and inactive controls have different treatment.
This review does not use a disabled-control exception for an enabled archived row.

## Actual current declaration inventory

Read698 tracked App .cs/.xaml sources, excluding bin/obj/artifacts and no untracked or
ignored source. The inventory records hashes and44 direct Muted resource/property
reference lines; these are references, not44 runtime controls. Shared Entry/Editor/
SearchBar styles use transparent backgrounds and Muted placeholders. All48 explicit
placeholder declarations either have a nearest declared CardBackground or belong to
the four fields in shared AccountFormView. Both actual AccountFormView embeddings,
AccountEditorPage and OnboardingPage, are directly inside the original Card border.
Those four shared declarations are not four additional uncovered runtime screens.

AppShell explicitly puts Muted unselected tab text on CardBackground. Remaining
foreground references are semantic icons/progress/chart uses; they do not establish
an ordinary-text-on-PageBackground failure. The exact-key supplementary lookup finds
no extra StaticResource/resource-key override outside the palette declaration/getter.
This is declared source binding, not native color-state-list or visual-pixel proof.

| Declared text pair | Light ratio | Dark ratio |
|---|---:|---:|
| Muted on CardBackground |4.6776711346351965|5.198066119245692|

Keep all22 hypothetical Muted/opaque-surface pairs, including the10 pairs below the
ordinary-text threshold. For example, light Muted/PageBackground is3.945171:1 and
Muted/RefundBackground is4.135342:1. They are real low ratios, but this inventory does
not bind those pairs to an active ordinary-text control. Do not drop those negative
controls or claim that every semantic surface is an acceptable Muted text background.
The already-corrected D806 receipt source uses SecondaryText and is not replayed.

## Archived category label and alpha model

CategoriesPage's archived Border has original opacity0.6 and Card style. Its row-name
Label is16 normal and inherits AmountText from the actual implicit Label style;
it is not SecondaryText at0.6 as in D822's separate former account-description defect.
The shared row still has the original real overlay button/OpenCommand. Model the
normal declared sRGB foreground/card alpha over PageBackground:

| Theme | Modeled foreground/card ratio | Conservative8-bit channel ratio |
|---|---:|---:|
|light|4.780354773916829|4.735050292973626|
|dark|5.662670171431591|5.616871191070698|

These unrounded modeled pairs exceed4.5 without a global palette or row-opacity edit.
The black/white21:1 and equal-color1:1 formula controls pass. These are two formula
self-checks, not new product tests. Native ancestor alpha, GPU composition, actual
foreground state, focus, glyphs, speech, user-selected colors and every screen remain
separate acceptance. No inference that all Android or archived content passes is made.

## Completed source scope and next input

No demonstrated bound source failure supports changing global Muted or this archived
label under the named premise. Preserve existing semantic colors, messages, layouts,
commands and the accepted D806/D822 results. Start a new correction only for an actual
identified bound failure or missing runtime condition, preserving its first evidence.
A broad accessibility parent must not be closed by these source calculations.

One initial guessed original-receipt-source-contrast.md path did not exist; inventory
found actual receipt-source-caption-contrast.md. That lookup failure is not a product
failure. No build, product test/native case, package, device/auth/financial/photography,
TalkBack, OS setting, cleanup, frozen scope, original fault record or R13 read occurs.
Main15014/a214 is only the previous D864 checkpoint, not fresh liveness. Current code's
D863 workflow success remains retained; no successful job logs or rerun are requested.
Original fault/D838, whole performance/security/platform/external gates and the active
overall goal remain separate.
