// Draft colour roles (reports/tokens.draft.json). Neutrals carry the light and dark themes; the logo blue is a brand
// colour for limited use; action, status and financial meanings are separate roles, even where colours are close.
// Brand values are sampled from the reconstructed master (its gradient stops), not taken from any board.
import { hex } from './fit.mjs';

export function brandColors(geometry) {
  const stops = geometry.surfaces.flatMap(s => s.gradient.stops.map(st => st.color));
  const lum = c => relativeLuminance(hex(c));
  const sorted = [...stops].sort((a, b) => lum(a) - lum(b));
  return { deep: hex(sorted[0]), mid: hex(sorted[Math.floor(sorted.length / 2)]), light: hex(sorted[sorted.length - 1]) };
}

export function draftTokens(geometry) {
  const brand = brandColors(geometry);
  return {
    status: 'draft — owner review required; no UI, XAML or theme has been changed',
    brand: { 'Brand.Deep': brand.deep, 'Brand.Mid': brand.mid, 'Brand.Light': brand.light, note: 'logo and limited brand moments only; never full-page backgrounds' },
    light: {
      'Neutral.Background': '#F7F8FA', 'Neutral.Surface': '#FFFFFF', 'Neutral.Border': '#D0D5DD',
      'Text.Primary': '#111827', 'Text.Secondary': '#4B5563',
      'Action.Primary': '#1463D1', 'Action.OnPrimary': '#FFFFFF',
      'Status.Success': '#15803D', 'Status.Error': '#B42318', 'Status.Warning.Foreground': '#8A4B08', 'Status.Warning.Background': '#FEF3C7',
      'Status.Info': '#0E7490', 'Premium.Optional': '#6D28D9',
      'Financial.Inflow': '#15803D', 'Financial.Outflow': '#B42318', 'Financial.Transfer': '#475467',
    },
    dark: {
      'Neutral.Background': '#0F1115', 'Neutral.Surface': '#171A21', 'Neutral.Border': '#343A46',
      'Text.Primary': '#F3F4F6', 'Text.Secondary': '#A9B0BC',
      'Action.Primary': '#6AA8FF', 'Action.OnPrimary': '#0B1220',
      'Status.Success': '#4ADE80', 'Status.Error': '#F97066', 'Status.Warning.Foreground': '#FDB022', 'Status.Warning.Background': '#3A2A0A',
      'Status.Info': '#22D3EE', 'Premium.Optional': '#B692F6',
      'Financial.Inflow': '#4ADE80', 'Financial.Outflow': '#F97066', 'Financial.Transfer': '#C0C6D0',
    },
  };
}

export function relativeLuminance(h) {
  const [r, g, b] = [1, 3, 5].map(i => parseInt(h.slice(i, i + 2), 16) / 255).map(c => (c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4));
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

export function contrast(a, b) {
  const [l1, l2] = [relativeLuminance(a), relativeLuminance(b)].sort((p, q) => q - p);
  return (l1 + 0.05) / (l2 + 0.05);
}

/** Pairs to check per theme: [foreground role, background role, minimum ratio, kind]. */
export const PAIRS = [
  ['Text.Primary', 'Neutral.Background', 4.5, 'body text'], ['Text.Primary', 'Neutral.Surface', 4.5, 'body text'],
  ['Text.Secondary', 'Neutral.Surface', 4.5, 'body text'], ['Action.Primary', 'Neutral.Surface', 4.5, 'link text'],
  ['Action.OnPrimary', 'Action.Primary', 4.5, 'button label'], ['Status.Success', 'Neutral.Surface', 4.5, 'status text'],
  ['Status.Error', 'Neutral.Surface', 4.5, 'status text'], ['Status.Warning.Foreground', 'Status.Warning.Background', 4.5, 'status text'],
  ['Status.Info', 'Neutral.Surface', 4.5, 'status text'], ['Premium.Optional', 'Neutral.Surface', 4.5, 'badge text'],
  ['Financial.Inflow', 'Neutral.Surface', 4.5, 'amount text'], ['Financial.Outflow', 'Neutral.Surface', 4.5, 'amount text'],
  ['Financial.Transfer', 'Neutral.Surface', 4.5, 'amount text'], ['Neutral.Border', 'Neutral.Surface', 1.0, 'decorative border (no requirement)'],
];

export function contrastTable(tokens) {
  const rows = [];
  for (const theme of ['light', 'dark']) {
    const t = tokens[theme];
    for (const [fg, bg, min, kind] of PAIRS) {
      const ratio = contrast(t[fg], t[bg]);
      rows.push({ theme, foreground: fg, background: bg, colors: [t[fg], t[bg]], ratio: Math.round(ratio * 100) / 100, required: min, kind, pass: ratio >= min });
    }
  }
  return rows;
}
