# Zanance symbol – vector reconstruction: quality report

**Status: approved by the owner on 2026-09-28** (geometry `ffae7f2ee01a0705`). The app assets are generated from it
(section 9, decision D-26); theme, project names, app id and logic are unchanged.

## 1. Input

| | |
|---|---|
| Only geometry reference | `references/approved/zanance-symbol-reference.png`, 514×489, opaque RGBA |
| SHA-256 before and after the build | `192395dd05dc263835c6540b19182c301e324791aa8e1bb1de8b124083a40d27` (equal; all 8 reference hashes are checked on every build and in the tests against `SOURCE_AUDIT.json`) |
| Supplemental images | used only to look at the wordmark style of `zanance_brand_identity_system.png`; no geometry, colour or measure was taken from any board |
| Excluded from the symbol | the light background and the floor shadow of the photo |

## 2. Method (reproducible, no image generation)

1. **Separation** (`scripts/analyze.mjs`, `segment.mjs`): the ribbon is saturated blue (b−r ≥ 0.3), background and
   floor shadow are near neutral (≤ 0.21), with a gap at 0.25. The silhouette is everything not reachable from the image
   border through non-blue pixels.
2. **Surfaces**: the fold edges are thin lines with a strong lightness gradient. The smooth areas between them give
   five surfaces: upper band, upper fold (seen through the upper opening), diagonal with both turns, lower fold, lower
   band. Edge pixels are assigned to the nearest surface.
3. **Curves** (`lib/curves.mjs`): marching squares on a 4× smoothly upsampled mask → sub-pixel outline → corner
   detection (the two opening tips) → cubic Bézier fitting (Schneider 1990) with tolerance 0.7 px (silhouette) and
   0.8 px (surfaces). Surfaces are clipped to the silhouette, so the outer outline comes only from the silhouette.
4. **Gradients** (`lib/fit.mjs`): per surface, the direction (3° steps) whose 1-D colour ramp explains the pixels best,
   with up to 7 stops sampled from the reference itself.
5. **Rims** (`lib/rims.mjs`): 1–1.5 px near-white lines along fold edges, found where boundary pixels are clearly
   brighter than both neighbouring surfaces; rebuilt as open Béziers with a stroke gradient whose stop opacity follows
   the measured strength.
6. Everything is written from one file, `source/geometry.json` (hash `ffae7f2ee01a0705`); no output is drawn separately.

## 3. Measured result

| Measure | Value | How |
|---|---|---|
| Silhouette overlap (IoU) with the reference | **0.9951** | vector rendered at scale 1 at the symbol origin (51, 29); reference silhouette from step 1 |
| Mean colour difference inside the symbol | **4.07 / 255** (flat version 26.1) | per-pixel mean of |ΔR|,|ΔG|,|ΔB| over 109 482 pixels |
| Paths / commands – master | 12 paths (1 silhouette, 5 surfaces, 5 rims, clip), 168 commands | `lib/svgpolicy.mjs` |
| Silhouette | 35 commands, 2 corners | |
| Gradients | 5 linear, 7 stops each; residual RMS 7.8–14.3 per channel | `source/geometry.json` |
| resvg vs Chromium (Edge 153) | mean 0.20 / 255, 0.28 % of pixels > 8 / 255 (edge anti-aliasing) | `previews/render-*.png` |

Limits of these numbers: the reference is 514×489; sub-pixel details and the exact original vector cannot be proven
from it, and upsampling does not recover detail. The numbers show agreement with this photo, not with an unknown
original artwork. See `previews/difference-color.png` (grey = colour difference ×6, red = silhouette mismatch).

## 4. Remaining differences (honest list)

1. **Shading inside surfaces** is linear per surface. The reference has slightly curved light fields (e.g. the lower
   fold, residual RMS 14.3); the vector is smoother and a little flatter there.
2. **Rims**: five rims are rebuilt (upper band ↔ upper fold, upper-right turn, lower fold ↔ lower band). The faint rim
   at the **lower-left turn** is not detected and is missing; rims are uniform 1.4 px wide.
3. **Opening tips** are slightly rounder (≈ 0.5–1 px) than in the photo because of the smoothing that removes the pixel
   staircase.
4. **Fold borders** follow a smoothed line; where the photo's fold line is not sharp, the vector border may deviate by
   up to about 1 px.
5. **No outer drop shadow** (by design: the master is transparent). A separate presentation version with a shadow was
   not produced.
6. A hair-thin seam of the base colour can appear at the lower-band tip at very large zoom.

## 5. Checks run

| Check | Result |
|---|---|
| `node --test tests/*.test.mjs` – 21 tests (10 = the cases of `tools/test_check_svg_structure.py`) | **21 passed** |
| SVG policy (Node port of `tools/check_svg_structure.py`, same rules) on 16 distributable SVGs; `--flat` rules on mono | passed; no warnings |
| No `<image>`, `feImage`, `data:`, `foreignObject`, script, style, filter, mask, text, font or external reference | passed |
| Mono = exactly one opaque colour; flat blue = no gradient; master = real gradients | passed |
| PNG: RGBA, transparent corners, symbol = 90 % of the side, centred (64 … 4096) | passed |
| WebP lossless = PNG pixels (premultiplied comparison) | passed |
| PDF: no image XObject, no font, page = artwork + 5 % margin (442×458 pt), shading only in colour | passed; pdf.js 5.4.149 operator list: **0 image operators** in both PDFs; pdf.js rendered both correctly (checked in the app browser via `node serve-preview.mjs`) |
| EPS: EPSF-3.0 header, BoundingBox, only `curveto`/`eofill`, no image operators | passed (structure); **not rendered** – see 6 |
| ICO: PNG entries 16, 32, 48, 256 | passed |
| Android foreground inside the 66 dp safe circle | passed (farthest opaque pixel measured) |
| Determinism: two builds in a row | all 52 outputs byte-identical |
| **Python `tools/check_svg_structure.py`** | **not run – Python is not installed** on this machine (only the Windows Store stub). Its rules are ported 1:1 to `scripts/lib/svgpolicy.mjs`; run the original once Python 3.10+ is available. |

## 6. Formats: produced, not produced, limits

* **Produced:** SVG (master, standalone, flat blue, mono black/white, 2 lockups + Outfit alternatives, editable
  lockup source), PNG 64–4096 transparent + on white + on dark grey + mono/flat, lockup PNG, WebP lossless 256–2048,
  PDF colour + mono, EPS mono black + flat blue, ICO 16/32/48/256, platform candidates.
* **Colour EPS: not produced.** It would need PostScript level 3 shadings with soft-mask rims; no EPS/PostScript
  renderer (Ghostscript, Illustrator) is available here to prove the result, so only flat/mono EPS were made. The
  flat and mono EPS were checked structurally but **not rendered**.
* **AI: not produced.** No Adobe Illustrator is available; renaming SVG/PDF to `.ai` is not done on purpose.
  SVG and PDF are the delivery masters.
* No Inkscape, ImageMagick, poppler (`pdfimages`) or Ghostscript on this machine; they were not installed
  system-wide. Replacements: resvg + Chromium (SVG), pdf.js + raw PDF inspection (PDF).

## 7. Small sizes

At **16 px** the colour symbol stays a recognisable Z, but the two openings and the folds merge into a few pixels; at
**24–32 px** the ribbon reads clearly. Mono black is the clearest at 16 px. The favicon uses the colour symbol at 94 %
of its canvas. Proposal (needs approval, not applied): for 16 px use the flat or mono version; the master is unchanged.

## 8. Wordmark

The board's lettering is a geometric sans with a single-storey "a"; the exact font cannot be determined from it.
**Wordmark proposal, needs approval:** Urbanist Bold 700 (Version 1.303, OFL-1.1, `@fontsource/urbanist@5.2.6`);
alternative Outfit SemiBold 600 (OFL-1.1, `@fontsource/outfit@5.2.6`). Distribution lockups contain the text as
outlines; the font file is not included. `source/zanance-lockup-horizontal-editable.svg` keeps live text for editing.

## 9. Integration (done 2026-09-28, decision D-26)

Owner approval: master geometry `ffae7f2ee01a0705`, neutral light background `#F4F6F8`, splash with the Urbanist
wordmark. `scripts/integrate.mjs` writes these files in `src/Apps/Zanance/Vafadar.Zanance.App/` (hashes in
`reports/integration.json`):

| App file | Content |
|---|---|
| `Resources/AppIcon/appicon.svg` | MauiIcon background, `#F4F6F8` |
| `Resources/AppIcon/appiconfg.svg` | colour symbol, far point inside the 66 dp adaptive safe circle; iOS and Windows scale it with `ForegroundScale="1.35"` |
| `Resources/Splash/splash.svg` | Android: symbol only (Android 12+ crops the splash to a circle) |
| `Resources/Splash/splashlockup.svg` | iOS: symbol + Urbanist wordmark as outlines; the unpackaged Windows app has no splash |
| `Resources/Images/zanance_symbol.svg` | in-app symbol (onboarding, More page) |
| `Platforms/Android/Resources/drawable/ic_stat_zanance.xml` | notification small icon, white silhouette (accent `#0C44A8`) |

The Android 13+ themed icon needs no extra resource: MAUI's adaptive icon uses the foreground as the monochrome
layer, and Android takes only its alpha, which is the symbol's silhouette. `previews/app-assets.png` shows the images
the MAUI build generated for every platform (`scripts/app-assets-sheet.mjs`).
## 10. Reproduce

```powershell
cd branding/zanance/scripts
npm ci                      # exact versions from package-lock.json, local node_modules only
node build.mjs              # geometry + all exports + reports/manifest.json
node preview.mjs            # previews/review.html + screenshots + renderer comparison + tokens
npm test                    # 21 checks
node serve-preview.mjs      # optional: http://127.0.0.1:8765/previews/pdf-check-http.html (pdf.js)
node integrate.mjs          # app assets from the approved geometry
```

Linux/macOS: the same commands (Node ≥ 24; `preview.mjs` looks for Edge or Chrome at the Windows paths – set the path
in `preview.mjs` for other systems).
