// Light rims along the fold edges: the reference shows a 1–1.5 px near-white line where two ribbon surfaces meet. They
// carry much of the ribbon's volume, so they are rebuilt as thin strokes: boundary pixels between two surfaces that
// are clearly brighter than both surfaces nearby are chained into lines, fitted with open Béziers and painted with a
// gradient along the line whose stop opacities follow the measured rim strength.
import { fitRun } from './curves.mjs';

const N8 = [[-1, -1], [0, -1], [1, -1], [-1, 0], [1, 0], [-1, 1], [0, 1], [1, 1]];

export function findRims(ctx, surfaceIds, { minStrength = 0.06, minLength = 14 } = {}) {
  const { width, height, data, label } = ctx;
  const L = i => (0.2126 * data[i * 4] + 0.7152 * data[i * 4 + 1] + 0.0722 * data[i * 4 + 2]) / 255;
  const rims = [];
  const ids = Object.keys(surfaceIds).map(Number);
  for (let ai = 0; ai < ids.length; ai++) {
    for (let bi = ai + 1; bi < ids.length; bi++) {
      const a = ids[ai], b = ids[bi];
      // Boundary pixels on side a that touch b.
      const boundary = new Map();
      for (let y = 1; y < height - 1; y++) for (let x = 1; x < width - 1; x++) {
        const i = y * width + x; if (label[i] !== a) continue;
        if (!N8.some(([dx, dy]) => label[i + dy * width + dx] === b)) continue;
        // Rim strength: brightest pixel within 2 px, against the typical lightness of both surfaces within 6 px.
        let best = i, bestL = L(i); const side = { [a]: [], [b]: [] };
        for (let dy = -6; dy <= 6; dy++) for (let dx = -6; dx <= 6; dx++) {
          const xx = x + dx, yy = y + dy; if (xx < 0 || yy < 0 || xx >= width || yy >= height) continue;
          const j = yy * width + xx; const l = L(j);
          if (Math.abs(dx) <= 2 && Math.abs(dy) <= 2 && l > bestL) { bestL = l; best = j; }
          if ((label[j] === a || label[j] === b) && Math.hypot(dx, dy) >= 3) side[label[j]].push(l);
        }
        const median = v => (v.length ? v.sort((p, q) => p - q)[v.length >> 1] : 1);
        const strength = bestL - Math.max(median(side[a]), median(side[b]));
        boundary.set(i, { x, y, strength, color: [data[best * 4], data[best * 4 + 1], data[best * 4 + 2]] });
      }
      for (const chain of chains(boundary, width)) {
        // Keep the stretches where a rim is actually visible.
        let run = [];
        const flush = () => { if (run.length >= minLength) rims.push(buildRim(run, surfaceIds[a], surfaceIds[b])); run = []; };
        for (const p of chain) { if (p.strength >= minStrength) run.push(p); else flush(); }
        flush();
      }
    }
  }
  return rims;
}

// Orders a set of boundary pixels into chains (greedy walk from end points, preferring straight continuation).
function chains(boundary, width) {
  const left = new Map(boundary);
  const neighbours = i => N8.map(([dx, dy]) => i + dy * width + dx).filter(j => left.has(j));
  const out = [];
  while (left.size) {
    let start = [...left.keys()].find(i => neighbours(i).length <= 1) ?? left.keys().next().value;
    const chain = []; let cur = start, prev = null;
    while (cur !== undefined) {
      chain.push(left.get(cur)); left.delete(cur);
      const next = neighbours(cur);
      if (!next.length) break;
      if (prev !== null) {
        const d = [cur % width - prev % width, ((cur / width) | 0) - ((prev / width) | 0)];
        next.sort((p, q) => score(q) - score(p));
        function score(j) { return (j % width - cur % width) * d[0] + (((j / width) | 0) - ((cur / width) | 0)) * d[1]; }
      }
      prev = cur; cur = next[0];
    }
    if (chain.length > 3) out.push(chain);
  }
  return out;
}

function buildRim(run, a, b) {
  // Smooth the pixel chain, fit it, and sample the rim colour/strength along it.
  const pts = run.map((p, k) => {
    let sx = 0, sy = 0, c = 0;
    for (let d = -8; d <= 8; d++) { const q = run[Math.max(0, Math.min(run.length - 1, k + d))]; sx += q.x + 0.5; sy += q.y + 0.5; c++; }
    return [sx / c, sy / c];
  });
  const dir = (i, j) => { const v = [pts[j][0] - pts[i][0], pts[j][1] - pts[i][1]]; const l = Math.hypot(...v) || 1; return [v[0] / l, v[1] / l]; };
  const k = Math.min(4, pts.length - 1);
  const curves = fitRun(pts, dir(0, k), dir(pts.length - 1, pts.length - 1 - k), 0.8);
  const stops = [];
  const count = Math.min(6, run.length);
  for (let s = 0; s < count; s++) {
    const idx = Math.round((s / (count - 1)) * (run.length - 1));
    const lo = Math.max(0, idx - 3), hi = Math.min(run.length - 1, idx + 3); const seg = run.slice(lo, hi + 1);
    const color = [0, 1, 2].map(c => Math.round(seg.reduce((acc, p) => acc + p.color[c], 0) / seg.length));
    const strength = seg.reduce((acc, p) => acc + p.strength, 0) / seg.length;
    stops.push({ offset: s / (count - 1), color, opacity: Math.max(0.2, Math.min(1, strength / 0.25)) });
  }
  // Fade the ends, so a rim never stops abruptly.
  stops[0].opacity = 0; stops[stops.length - 1].opacity = 0;
  return { between: [a, b], curves, start: pts[0], end: pts[pts.length - 1], stops, length: run.length };
}
