# Dastan brand assets

The logo of Dastan – a symbol and the wordmark "Dastan" – as vector masters, and every file made from them.
The owner chose the logo on 2026-11-21 (decision D-082 in
`src/Apps/Dastan/docs/product/12-decisions-questions-and-change-log.md`;
[ADR 0026](../../src/Apps/Dastan/docs/adr/0026-logo.md)).

## The source of truth

`source/dastan-geometry.json` is the one source of the geometry: the open D, the dot and the page of the symbol,
and the outlines of the six letters of the wordmark, in one system of units. Everything else in this folder and
every icon of the site is written from it by `scripts/build.mjs`. Do not edit a generated file; change the
geometry and run the script.

The geometry was reconstructed from the approved picture, `references/dastan-logo-approved.webp` – from the
**large logo at its top**; the smaller examples on that picture are uses, not sources. Its outlines were traced
at the half of the coverage of each pixel and fitted with cubic curves (172 segments in all; the dot is a
circle; upright and level edges are exactly upright and level). The original vector file of the logo does not
exist here, and the font of the wordmark is not known: the wordmark is artwork, kept as outlines, and no font is
named or needed. Measured against the picture, 2.6 % of the pixels that have ink in either differ – its soft
edges (`previews/comparison.png`, `previews/report.json`).

## Colours

| Name | Value | Use |
|---|---|---|
| Indigo | `#3B3A8F` | the symbol; the ground of the app icon |
| Ink | `#1C1B2E` | the wordmark on a light ground |
| White | `#FFFFFF` | the logo on a dark ground; the symbol on indigo |
| Ivory | `#FBFAF7` | the light ground of the site – a ground, never part of a logo file |

Flat colours, no gradient, no shadow. The logo files have no ground of their own.

## Files

| File | What it is | Use it |
|---|---|---|
| `source/dastan-symbol.svg` | the symbol, indigo – master | as the reference of the symbol |
| `source/dastan-logo.svg` | the symbol and the wordmark side by side, indigo and ink – master | as the reference of the logo |
| `exports/svg/dastan-logo-color.svg` | the logo in colour | on a light ground |
| `exports/svg/dastan-logo-white.svg` | the logo in white | on a dark ground, on indigo |
| `exports/svg/dastan-logo-black.svg` | the logo in black | where only one colour can be printed |
| `exports/svg/dastan-symbol-{indigo,white,black}.svg` | the symbol alone | where there is no room for the wordmark |
| `exports/svg/dastan-app-icon.svg` | the white symbol on an indigo square with round corners | app icon, `favicon.svg` |
| `exports/svg/dastan-app-icon-maskable.svg` | the white symbol on a whole indigo square | where a platform cuts the shape itself |
| `exports/svg/dastan-app-icon-small.svg` | the small-size variant, see below | 16 and 32 pixels only |
| `exports/png/dastan-logo-{color,white,black}-{512,1024,2048}.png` | the logo, the width in pixels in the name, no ground | where an SVG cannot be used |
| `exports/png/dastan-symbol-{indigo,white,black}-{256,512,1024}.png` | the symbol, the width in pixels in the name, no ground | the same |
| `exports/icons/icon-{192,512}.png` | app icon, round corners, nothing outside them | manifest, purpose `any` |
| `exports/icons/icon-maskable-{192,512}.png` | whole square, opaque | manifest, purpose `maskable` |
| `exports/icons/apple-touch-icon.png` | 180 pixels, whole square, opaque | the home screen of iOS |
| `exports/icons/favicon-{16,32,48}.png`, `exports/ico/favicon.ico` | the icon of a tab; the ICO holds the three | `favicon.ico` |
| `previews/comparison.png` | the reference, the master, one over the other, the variants, the actual sizes, the masks | to look at |
| `previews/report.json` | the size and the least alpha of every PNG, and the measured difference from the reference | to check |

A square icon shows the symbol alone – never the wordmark made small.

**The small-size variant.** At 16 pixels the page of the symbol is thinner than a third of a pixel and the dot
is two pixels. For 16 and 32 pixels only, the page is drawn with an outline of 16 units and the dot is 5 units
larger (`small` in the geometry), and the symbol stands a little larger in its square. The D, the dot and the
page are all there, apart from each other; the master is unchanged. `previews/comparison.png` shows both side
by side, at their size and six times as large.

**The maskable icon.** The whole symbol, with its dot, is inside the circle whose radius is 40 % of the square:
half the diagonal of the box of the symbol is 94 % of that radius.

## Clear space and sizes

* Around the logo, keep free at least the diameter of the dot (58 units, a sixth of the height of the symbol)
  on every side.
* Keep the proportions: never stretch, turn, outline or recolour it; never redraw it. On a page written from
  right to left the logo is the same: the D and the wordmark are Latin, and a drawing is not mirrored.
* The logo is readable from a height of 24 pixels; below that, use the symbol alone.

## On the site

`src/Apps/Dastan/Vafadar.Dastan.Web` draws the logo itself, from `Site/BrandGeometry.cs`, which the script
writes: in the head of every page (`Components/Shared/Logo.razor`, indigo and ink on a light ground, white on a
dark one) and in the picture of a shared link (`Site/PreviewPicture.cs`). The script also places `favicon.svg`,
`favicon.ico` and the five icons of `wwwroot/icons`. The site has no dependency for the logo.

## Making everything again

```powershell
node branding/dastan/scripts/build.mjs            # the SVG files and Site/BrandGeometry.cs – Node alone
node branding/dastan/scripts/build.mjs --raster   # also the PNG and ICO files, the icons of the site, the sheet
```

A PNG is a drawing of an SVG by a browser. `--raster` uses the Chromium of the test container of Dastan
(`docker compose -p test -f src/Apps/Dastan/compose.yaml up -d browser`; another name of the container:
`DASTAN_BROWSER_CONTAINER`). It downloads and installs nothing. The script refuses to write an SVG that holds a
picture of pixels, a script, a text or anything fetched, and fails when a PNG has not the size or the ground it
should have. `LogoTests` of the site fail when the site draws another geometry than this folder holds or serves
another icon than this folder made.

## What is not here

The original vector file and the name of the font of the wordmark; formats for print (PDF, EPS); a guide of the
brand beyond this page; registration of the mark. Nothing was tried on a phone or as an installed app – see
ADR 0026.
