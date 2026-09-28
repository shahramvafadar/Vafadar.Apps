# Zanance brand tools

Rebuilds the Zanance symbol as vectors from `../references/approved/zanance-symbol-reference.png` and writes every
candidate export from one geometry. Node ≥ 24; dependencies are pinned in `package-lock.json` and installed locally.

```powershell
npm ci
node build.mjs          # source/, exports/, platform-candidates/, reports/manifest.json
node preview.mjs        # previews/review.html (+ .png), renderer comparison, reports/tokens.draft.json
npm test                # structure, raster, PDF/EPS/ICO and reference-hash checks
node serve-preview.mjs  # optional: pdf.js check at http://127.0.0.1:8765/previews/pdf-check-http.html
node integrate.mjs      # app icon, splash, notification icon and in-app symbol (approved geometry only)
node app-assets-sheet.mjs # after Debug builds: previews/app-assets.png from the generated platform images
```

| File | Role |
|---|---|
| `analyze.mjs` | diagnostic maps of the reference (work/) |
| `segment.mjs` | silhouette and ribbon surfaces |
| `reconstruct.mjs` | curves, gradients and rims → geometry |
| `build.mjs` | all outputs + manifest |
| `compare.mjs` | IoU / colour error against the reference |
| `preview.mjs`, `serve-preview.mjs` | review page, screenshots, pdf.js check |
| `integrate.mjs` | writes the app assets (decision D-26); refuses any geometry other than the approved one |
| `app-assets-sheet.mjs` | review sheet of the icons and splash images the MAUI build generated |
| `lib/` | curve fitting, gradients, rims, SVG/PDF/EPS/ICO writers, SVG policy (port of `../tools/check_svg_structure.py`), tokens |
| `tests/` | `node --test` checks |

The master (geometry `ffae7f2ee01a0705`) was **approved by the owner on 2026-09-28**. A rebuild that changes the geometry
produces a new candidate that needs approval again before `integrate.mjs` accepts it. The scripts never modify the
references.
