// Curve and gradient fitting for the reconstruction. Contours: potrace (Bézier fitting with corner detection) on a
// smoothed, 4× upsampled region mask; coordinates are mapped back to reference pixels. Gradients: for each surface the
// direction whose 1-D colour ramp explains the pixel colours best, with stops sampled from the reference itself.
import sharp from 'sharp';
import { contours, resample, fitClosed, toPath } from './curves.mjs';

export const TRACE_SCALE = 4;

/**
 * Outlines a 0/1 mask (reference resolution) as cubic Béziers in reference pixel units. The mask is upsampled 4× with
 * a smooth kernel and slightly blurred, so the pixel staircase becomes a sub-pixel iso-line; `grow` (reference pixels)
 * moves the outline outwards by lowering the iso-level. `tolerance` is the largest allowed deviation in reference px.
 */
export async function traceMask(mask, width, height, { grow = 0, tolerance = 0.6, blur = 3 } = {}) {
  const src = Buffer.alloc(width * height);
  for (let i = 0; i < mask.length; i++) src[i] = mask[i] ? 255 : 0;
  const W = width * TRACE_SCALE, H = height * TRACE_SCALE;
  // blur() returns three channels; keep one, and fail loudly if the layout ever changes.
  const up = await sharp(src, { raw: { width, height, channels: 1 } }).resize(W, H, { kernel: 'cubic' }).blur(blur).extractChannel(0).raw().toBuffer();
  if (up.length !== W * H) throw new Error(`unexpected field size ${up.length}`);
  const field = new Float32Array(W * H); for (let i = 0; i < field.length; i++) field[i] = up[i] / 255;
  // A smooth edge ramp is about 2 reference px wide here; lowering the level by 0.25 grows the outline ~0.5 px.
  const level = 0.5 - grow * 0.5;
  const loops = contours(field, W, H, level)
    .map(l => l.map(([x, y]) => [x / TRACE_SCALE, y / TRACE_SCALE]))
    .map(l => resample(l, 0.75))
    .filter(l => l.length > 12);
  const fitted = loops.map(l => fitClosed(l, { tolerance, cornerSpan: 4, cornerDegrees: 55 }));
  return { d: toPath(fitted.map(f => f.curves)), loops: fitted.length, corners: fitted.reduce((a, f) => a + f.corners, 0) };
}
/** Scales every coordinate of an absolute path (M, L, C, Z as written by potrace) and rounds to 0.01. */
export function scalePath(d, s, dx = 0, dy = 0) {
  let axis = 0;
  return d.replace(/-?\d*\.?\d+(e-?\d+)?/gi, m => {
    const v = parseFloat(m) * s + (axis++ % 2 === 0 ? dx : dy);
    return (Math.round(v * 100) / 100).toString();
  }).replace(/\s+/g, ' ').trim();
}

/** Counts path commands and coordinate pairs, for the node-count checks. */
export function pathStats(d) {
  const commands = (d.match(/[MLCZ]/gi) ?? []).length;
  const numbers = (d.match(/-?\d*\.?\d+/g) ?? []).length;
  return { commands, points: numbers / 2 };
}

/**
 * Fits a linear gradient to the pixels of one surface. Tries directions every 3°, projects the pixels, bins them
 * along the axis and keeps the direction with the smallest remaining colour error; returns the axis end points (in
 * reference pixels, 2nd/98th percentile of the projection) and up to `maxStops` stops of the binned mean colours.
 */
export function fitGradient(data, width, pixels, { bins = 32, maxStops = 7 } = {}) {
  const xs = pixels.map(i => i % width), ys = pixels.map(i => (i / width) | 0);
  const cols = pixels.map(i => [data[i * 4], data[i * 4 + 1], data[i * 4 + 2]]);
  const cx = xs.reduce((a, b) => a + b, 0) / xs.length, cy = ys.reduce((a, b) => a + b, 0) / ys.length;
  let best = null;
  for (let deg = 0; deg < 180; deg += 3) {
    const a = (deg * Math.PI) / 180, ux = Math.cos(a), uy = Math.sin(a);
    const t = xs.map((x, k) => (x - cx) * ux + (ys[k] - cy) * uy);
    const sorted = [...t].sort((p, q) => p - q);
    const t0 = sorted[Math.floor(sorted.length * 0.02)], t1 = sorted[Math.floor(sorted.length * 0.98)];
    const sum = Array.from({ length: bins }, () => [0, 0, 0, 0]);
    const bin = v => Math.max(0, Math.min(bins - 1, Math.floor(((v - t0) / (t1 - t0 || 1)) * bins)));
    t.forEach((v, k) => { const s = sum[bin(v)]; s[0] += cols[k][0]; s[1] += cols[k][1]; s[2] += cols[k][2]; s[3]++; });
    const means = sum.map(s => (s[3] ? [s[0] / s[3], s[1] / s[3], s[2] / s[3]] : null));
    let err = 0;
    t.forEach((v, k) => { const m = means[bin(v)]; err += (cols[k][0] - m[0]) ** 2 + (cols[k][1] - m[1]) ** 2 + (cols[k][2] - m[2]) ** 2; });
    if (!best || err < best.err) best = { err, deg, ux, uy, t0, t1, means };
  }
  // Fill empty bins, then keep the stops that matter (greedy: largest deviation from linear interpolation first).
  const means = best.means.map((m, k, arr) => m ?? arr.find(x => x) );
  const offset = k => (k + 0.5) / bins;
  let keep = [0, bins - 1];
  const interp = (k, ks) => {
    const i = ks.findIndex(x => x >= k); const a = ks[Math.max(0, i - 1)], b = ks[i];
    if (a === b) return means[a];
    const f = (k - a) / (b - a); return means[a].map((v, c) => v + (means[b][c] - v) * f);
  };
  while (keep.length < maxStops) {
    let worst = -1, worstErr = 0;
    for (let k = 0; k < bins; k++) {
      if (keep.includes(k)) continue;
      const p = interp(k, keep); const e = Math.hypot(...means[k].map((v, c) => v - p[c]));
      if (e > worstErr) { worstErr = e; worst = k; }
    }
    if (worstErr < 2.5) break; // closer than ~1 % per channel: invisible
    keep = [...keep, worst].sort((p, q) => p - q);
  }
  return {
    angle: best.deg,
    x1: cx + best.ux * best.t0, y1: cy + best.uy * best.t0,
    x2: cx + best.ux * best.t1, y2: cy + best.uy * best.t1,
    stops: keep.map(k => ({ offset: offset(k), color: means[k].map(Math.round) })),
    rms: Math.sqrt(best.err / pixels.length / 3),
  };
}

export const hex = ([r, g, b]) => '#' + [r, g, b].map(v => v.toString(16).padStart(2, '0')).join('').toUpperCase();
