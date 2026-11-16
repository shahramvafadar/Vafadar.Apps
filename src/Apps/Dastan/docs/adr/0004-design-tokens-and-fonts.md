# 0004. Design tokens, type scale and fonts

- Status: Accepted for the structure (tokens, scale, direction); the palette and the fonts are **provisional**
- Date: 2026-11-16
- Deciders: Shahram Vafadar
- Governing documents: 11 (visual identity direction: no palette, logo or font is selected), 18; M0-10

## Context

The plan asked for a written result on type scale, spacing, colour tokens and fonts for Latin and Arabic script.
Document 11 names a direction to explore – a deep blue or indigo with a warm accent – and selects nothing. The
pages have to work in two scripts, two directions and two themes from the first page on.

## Decision

1. **Colours are tokens with a meaning**, defined once for light and once for dark in `wwwroot/app.css`:
   `--surface`, `--surface-raised`, `--text`, `--text-muted`, `--border`, `--accent`, `--accent-text`, `--focus`,
   `--mark`. No rule uses a colour value directly. The names are the contract of `Vafadar.Web`'s base sheet.
2. **Provisional palette:** indigo on warm paper, amber for focus and marks. Light: text `#1c1b2e` on `#fbfaf7`,
   accent `#3b3a8f`. Dark: text `#ecebf5` on `#14132a`, accent `#b1b0ff`. These pairs are far above the 4.5 : 1
   contrast of normal text (computed from the values; not yet checked with a contrast tool in the browser).
3. **Theme:** the visitor's choice (`data-theme`) wins; without a choice the device decides
   (`prefers-color-scheme`). The dark values stand twice in the sheet for that reason and have to be kept the same.
4. **Type scale:** steps of 1.2 from 0.875 rem to 1.44 rem; the page title grows with the window
   (`clamp`). Body text is 1 rem and follows the visitor's browser setting.
5. **Space:** steps of a quarter of the base size (`--space-1` … `--space-12`). A stand-alone link or control is
   at least 2.75 rem (44 px) high. Text runs in a column of at most 44 rem.
6. **The font follows the language of the text, not of the page** (`:lang()`): an English title on a Persian
   page is set in the Latin stack. Arabic-script text has a taller line (1.9).
7. **Fonts are the system's for now**: no font file is downloaded or bundled. Latin: Segoe UI Variable, system-ui
   and common fallbacks. Arabic script: the stack starts with Vazirmatn, so a device that has it uses it, and
   continues with Segoe UI, Noto Sans Arabic, Geeza Pro, Tahoma.
8. **Direction:** only logical properties (`inline`, `block`, `start`, `end`). The one physical thing, the arrow
   of a relation, has a rule per direction.

## Open – for the owner

* **A bundled Persian font.** With system fonts Persian text looks different on every device (Segoe UI on Windows,
  Noto on Android). Bundling Vazirmatn (SIL Open Font License 1.1, self-hosted, no request to another site) would
  make it the same everywhere. It adds font files to the repository and to every first visit. Recommended, not
  done: it is a design decision and a third-party file.
* The palette, the wordmark and the icon are placeholders until the identity work of document 11 is done. The
  icon (`wwwroot/icon.svg`) is an original provisional drawing.

## Consequences

* A new identity changes token values, not rules.
* Kurdish and Arabic pages need no change in the sheet.
