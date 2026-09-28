// Step 3 – reconstruct the master: trace each surface and the silhouette to Bézier paths, fit their gradients from the
// reference colours, and write source/geometry.json (the single geometric source every export is built from).
import { writeFileSync, mkdirSync } from 'node:fs';
import { segment } from './segment.mjs';
import { traceMask, fitGradient, pathStats, scalePath } from './lib/fit.mjs';
import { findRims } from './lib/rims.mjs';
import { toPath } from './lib/curves.mjs';

// Surfaces in paint order (back to front) with stable names; matched to the segmented regions by position.
const SURFACES = [
  { name: 'upper-fold', role: 'underside of the upper band, seen through the upper opening', near: [164, 130] },
  { name: 'lower-fold', role: 'underside of the lower band, seen through the lower opening', near: [354, 349] },
  { name: 'diagonal', role: 'diagonal face with the upper-right and lower-left turns', near: [245, 251] },
  { name: 'upper-band', role: 'front face of the upper band', near: [287, 76] },
  { name: 'lower-band', role: 'front face of the lower band', near: [228, 397] },
];

export async function reconstruct() {
  const ctx = await segment();
  const { width, height, data, inside, label, regions } = ctx;

  // Symbol bounds (silhouette only; background and floor shadow are excluded by the segmentation).
  let minX = width, minY = height, maxX = 0, maxY = 0;
  for (let i = 0; i < inside.length; i++) if (inside[i]) {
    const x = i % width, y = (i / width) | 0;
    minX = Math.min(minX, x); maxX = Math.max(maxX, x); minY = Math.min(minY, y); maxY = Math.max(maxY, y);
  }
  const origin = [minX, minY];
  const size = [maxX - minX + 1, maxY - minY + 1];

  // Silhouette: 1.5 px smoothing and 0.7 px tolerance – 35 commands at IoU 0.995 (see reports/quality-report.md).
  const silhouette = scalePath((await traceMask(inside, width, height, { blur: 6, tolerance: 0.7 })).d, 1, -origin[0], -origin[1]);

  const surfaces = [];
  const surfaceIds = {};
  for (const s of SURFACES) {
    const region = regions.reduce((a, r) => (Math.hypot(r.cx - s.near[0], r.cy - s.near[1]) < Math.hypot(a.cx - s.near[0], a.cy - s.near[1]) ? r : a));
    surfaceIds[region.id] = s.name;
    const mask = label.map(v => (v === region.id ? 1 : 0));
    const pixels = []; for (let i = 0; i < mask.length; i++) if (mask[i]) pixels.push(i);
    // Surfaces are traced slightly larger, so neighbours overlap under anti-aliasing and no seam shows through.
    // Internal borders come from a pixel assignment that jitters by about 1 px; the real fold edges are smooth, so the
    // surfaces get more smoothing (1.5 px) and a looser tolerance than the silhouette.
    const d = scalePath((await traceMask(mask, width, height, { grow: 0.6, blur: 6, tolerance: 0.8 })).d, 1, -origin[0], -origin[1]);
    const g = fitGradient(data, width, pixels);
    surfaces.push({
      ...s, near: undefined, pixels: pixels.length, path: d, stats: pathStats(d),
      gradient: { ...g, x1: g.x1 - origin[0], y1: g.y1 - origin[1], x2: g.x2 - origin[0], y2: g.y2 - origin[1] },
      mean: region.mean,
    });
  }

  // Light rims along the fold edges (see lib/rims.mjs), in symbol coordinates.
  const rims = findRims(ctx, surfaceIds).map(r => ({
    between: r.between,
    path: toPath([r.curves], -origin[0], -origin[1]).replace(/Z$/, ''),
    x1: r.start[0] - origin[0], y1: r.start[1] - origin[1], x2: r.end[0] - origin[0], y2: r.end[1] - origin[1],
    stops: r.stops, samples: r.length,
  }));

  return {
    reference: '../references/approved/zanance-symbol-reference.png',
    referenceSize: [width, height],
    origin, size,
    silhouette, silhouetteStats: pathStats(silhouette),
    surfaces,
    rims,
    rimWidth: 1.4,
  };
}

if (import.meta.url === `file:///${process.argv[1].replace(/\\/g, '/')}`) {
  const geometry = await reconstruct();
  mkdirSync('../source', { recursive: true });
  writeFileSync('work/geometry.json', JSON.stringify(geometry, null, 1));
  console.log('origin', geometry.origin, 'size', geometry.size, 'silhouette', geometry.silhouetteStats);
  for (const r of geometry.rims) console.log('rim', r.between.join('/'), 'samples', r.samples, pathStats(r.path));
  for (const s of geometry.surfaces) console.log(s.name, s.pixels, s.stats, 'angle', s.gradient.angle, 'stops', s.gradient.stops.length, 'rms', s.gradient.rms.toFixed(1));
}
