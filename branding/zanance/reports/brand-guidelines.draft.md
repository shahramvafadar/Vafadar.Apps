# Zanance brand guidelines – draft

**Status: draft.** The symbol master, the launcher background `#F4F6F8` and the Urbanist wordmark are approved (2026-09-28, D-26); the rest of this guide still needs review. Based on tests of the reconstructed master, not on the concept boards
(their "min 24px", "optimized for all platforms", dates, slogans and "Approved master" are not a technical spec).

## Symbol

* Master: `source/zanance-symbol-master.svg` (transparent, no text, no outer shadow). All exports come from it.
* Never redraw, stretch, rotate, recolour gradients, add effects or place it in a box that changes its proportions.
* **Clear space (draft):** at least 0.25 × symbol height on every side.
* **Minimum size (draft):** colour 24 px high for general use; 16 px only as favicon (folds merge at 16 px). Mono
  black/white is preferred at 16 px.
* Mono = one opaque colour; the openings stay transparent.

## Horizontal lockup – wordmark proposal, needs approval

* Name: exactly `Zanance`. No slogan, date, product description, security claim or "by Vafadar" without approval.
* Proposal: Urbanist Bold 700 (OFL-1.1); alternative Outfit SemiBold 600. Distribution files use outlines.
* Layout (draft): wordmark cap height = 0.40 × symbol height, vertically centred; gap = 0.22 × symbol height.
* Only the wordmark colour changes with the background (dark neutral on light, light neutral on dark); the symbol never
  changes.
* **Minimum size (draft):** 24 px lockup height (tested down to 18 px, readable but tight). Clear space 0.25 × symbol
  height.

## Colour roles (values: `tokens.draft.json`)

Themes are built from **neutrals**. The logo blue is a **Brand** colour for the logo and limited brand moments; it is
not a page background, and green is not the brand colour.

| Role | Use |
|---|---|
| Brand | logo and limited brand moments only (sampled from the master: Deep, Mid, Light) |
| Action | main action, link, active selection – a limited blue |
| Success / Positive | success, positive balance where it means something – green |
| Error / Danger / NegativeBalance | errors, destructive actions, critical warnings – red |
| Warning / Attention | needs review or caution – amber on a light amber background |
| Info | neutral information – teal, distinct from Action |
| Premium (optional) | a possible badge role only; it defines no paid feature |
| Financial.Inflow / Outflow / Transfer | amounts; **separate from Status**: an ordinary expense is not an error, a transfer is neither |

Rules:
* Colour is never the only carrier of meaning: always add a sign (+/−), icon or word.
* Pending is not automatically Warning; income, transfers and balance changes get no success/error meaning from their
  sign alone.
* Contrast measured for every text/status pair in light and dark (`tokens.draft.json`): all ≥ 4.5:1 for normal text.
  Large text (≥ 18.66 px bold or ≥ 24 px) and meaningful non-text parts need ≥ 3:1. The logo and brand name are exempt.
* No UI, XAML or theme was changed; these tokens are a proposal for later work.
