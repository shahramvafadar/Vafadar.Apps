// Writes every SVG from source/geometry.json. All variants, lockups and icons use exactly the same symbol paths; only
// paint and placement change (uniform scale and translation only).
import { hex } from './fit.mjs';

export const CANDIDATE = 'candidate — owner review required';
const REFERENCE_SHA = '192395dd05dc263835c6540b19182c301e324791aa8e1bb1de8b124083a40d27';
const f = v => (Math.round(v * 1000) / 1000).toString();

/**
 * The symbol as SVG parts (defs + body) in symbol units (viewBox 0 0 w h of the master).
 * variant: 'color' (gradients and rims), 'flat' (one flat colour per surface), 'mono' (one colour, silhouette only).
 */
export function symbolParts(geometry, { variant = 'color', monoColor = '#000000', id = 'zanance' } = {}) {
  const defs = []; const layers = [];
  const darkest = geometry.surfaces.map(s => s.mean).sort((a, b) => a[0] + a[1] + a[2] - (b[0] + b[1] + b[2]))[0];
  // Base: the full silhouette under the surfaces, so no anti-aliasing seam between neighbours shows the background.
  layers.push(`<path id="${id}-silhouette" d="${geometry.silhouette}" fill="${variant === 'mono' ? monoColor : hex(darkest)}" fill-rule="evenodd"/>`);
  if (variant !== 'mono') {
    const clip = `${id}-clip`;
    // Surfaces are clipped to the silhouette: the outer outline comes only from the precise silhouette path; the more
    // smoothed surface outlines only decide the borders between surfaces.
    defs.push(`<clipPath id="${clip}"><path d="${geometry.silhouette}" clip-rule="evenodd"/></clipPath>`);
    const surfaceLayers = [];
    for (const s of geometry.surfaces) {
      let paint;
      if (variant === 'flat') paint = hex(s.mean);
      else {
        const g = s.gradient; const gid = `${id}-${s.name}-gradient`;
        defs.push(`<linearGradient id="${gid}" gradientUnits="userSpaceOnUse" x1="${f(g.x1)}" y1="${f(g.y1)}" x2="${f(g.x2)}" y2="${f(g.y2)}">`
          + g.stops.map(st => `<stop offset="${f(st.offset)}" stop-color="${hex(st.color)}"/>`).join('') + '</linearGradient>');
        paint = `url(#${gid})`;
      }
      surfaceLayers.push(`<path id="${id}-${s.name}" d="${s.path}" fill="${paint}" fill-rule="evenodd"/>`);
    }
    layers.push(`<g id="${id}-surfaces" clip-path="url(#${clip})">${surfaceLayers.join('')}</g>`);
  }
  if (variant === 'color') {
    geometry.rims.forEach((r, k) => {
      const gid = `${id}-rim-${k}-gradient`;
      defs.push(`<linearGradient id="${gid}" gradientUnits="userSpaceOnUse" x1="${f(r.x1)}" y1="${f(r.y1)}" x2="${f(r.x2)}" y2="${f(r.y2)}">`
        + r.stops.map(st => `<stop offset="${f(st.offset)}" stop-color="${hex(st.color)}" stop-opacity="${f(st.opacity)}"/>`).join('') + '</linearGradient>');
      layers.push(`<path id="${id}-rim-${k}" d="${r.path}" fill="none" stroke="url(#${gid})" stroke-width="${geometry.rimWidth}" stroke-linecap="round" stroke-linejoin="round"/>`);
    });
  }
  return { defs: defs.join(''), body: `<g id="${id}-symbol">${layers.join('')}</g>` };
}

function document(viewBox, width, height, title, comment, defs, body) {
  return `<?xml version="1.0" encoding="UTF-8"?>
<!-- ${comment}
     ${CANDIDATE}. Rebuilt as vectors from references/approved/zanance-symbol-reference.png (sha256 ${REFERENCE_SHA}). -->
<svg xmlns="http://www.w3.org/2000/svg" viewBox="${viewBox}" width="${f(width)}" height="${f(height)}">
<title>${title}</title>
${defs ? `<defs>${defs}</defs>\n` : ''}${body}
</svg>
`;
}

/** The symbol alone: viewBox = symbol bounds in reference pixels, no margin, no background, no outer shadow. */
export function symbolSvg(geometry, opts = {}) {
  const [w, h] = geometry.size; const p = symbolParts(geometry, opts);
  return document(`0 0 ${w} ${h}`, w, h, 'Zanance', `Zanance symbol – ${opts.variant ?? 'color'}; viewBox = symbol bounds (${w}×${h}), no margin. Geometry ${opts.geometryHash ?? ''}.`, p.defs, p.body);
}

/**
 * The symbol centred on a square canvas: the symbol's larger side is `fill` of the canvas side (uniform scale only).
 * `background` fills the whole canvas (for app-icon backgrounds); omit it for transparency.
 */
export function framedSvg(geometry, { side = 1024, fill = 0.9, background, ...opts } = {}) {
  const [w, h] = geometry.size; const s = (side * fill) / Math.max(w, h);
  const tx = (side - w * s) / 2, ty = (side - h * s) / 2; const p = symbolParts(geometry, opts);
  const bg = background ? `<rect width="${side}" height="${side}" fill="${background}"/>` : '';
  return document(`0 0 ${side} ${side}`, side, side, 'Zanance', `Zanance symbol on a ${side}×${side} canvas; symbol = ${Math.round(fill * 100)} % of the side, centred. Geometry ${opts.geometryHash ?? ''}.`,
    p.defs, `${bg}<g transform="translate(${f(tx)} ${f(ty)}) scale(${f(s)})">${p.body}</g>`);
}

/**
 * Horizontal lockup: symbol + wordmark outlines. Layout (draft): cap height of the wordmark = 0.40 × symbol height,
 * wordmark vertically centred on the symbol, gap = 0.22 × symbol height. The wordmark paint changes with the target
 * background; the symbol does not.
 */
export function lockupSvg(geometry, wordmark, { textColor, id = 'zanance', title = 'Zanance', geometryHash = '', note = '' } = {}) {
  const [w, h] = geometry.size;
  const scale = (0.40 * h) / wordmark.capHeight;
  const gap = 0.22 * h;
  const textX = w + gap - wordmark.box.x1 * scale;
  const baseline = h / 2 + (wordmark.capHeight * scale) / 2;
  const width = w + gap + (wordmark.box.x2 - wordmark.box.x1) * scale;
  const p = symbolParts(geometry, { id });
  const text = `<path id="${id}-wordmark" d="${wordmark.d}" fill="${textColor}" transform="translate(${f(textX)} ${f(baseline)}) scale(${f(scale)})"/>`;
  return document(`0 0 ${f(width)} ${h}`, width, h, title, `Zanance horizontal lockup; wordmark ${wordmark.fontName} (${wordmark.license}) as outlines – wordmark proposal, needs owner approval. ${note} Geometry ${geometryHash}.`,
    p.defs, `${p.body}${text}`);
}
