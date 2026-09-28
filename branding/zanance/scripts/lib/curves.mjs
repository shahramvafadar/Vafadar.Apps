// Contour extraction and cubic Bézier fitting.
// 1. marching squares on a smooth field gives sub-pixel closed outlines;
// 2. sharp corners (the tips of the ribbon openings) are detected from the turning angle;
// 3. each run between corners is fitted with as few cubic Béziers as the tolerance allows (Schneider, "An Algorithm for
//    Automatically Fitting Digitized Curves", Graphics Gems 1990), with shared tangents at smooth joins.

/** Closed iso-lines of `field` (width×height, values 0–1) at `level`, as arrays of [x, y] in field pixels. */
export function contours(field, width, height, level = 0.5) {
  const at = (x, y) => field[y * width + x];
  const segments = new Map(); // start key -> list of [start, end]
  const key = p => `${p[0].toFixed(4)},${p[1].toFixed(4)}`;
  const lerp = (x0, y0, v0, x1, y1, v1) => { const t = (level - v0) / (v1 - v0); return [x0 + (x1 - x0) * t, y0 + (y1 - y0) * t]; };
  const add = (a, b) => { const k = key(a); if (!segments.has(k)) segments.set(k, []); segments.get(k).push([a, b]); };
  for (let y = 0; y < height - 1; y++) {
    for (let x = 0; x < width - 1; x++) {
      const v = [at(x, y), at(x + 1, y), at(x + 1, y + 1), at(x, y + 1)];
      const c = (v[0] > level ? 1 : 0) | (v[1] > level ? 2 : 0) | (v[2] > level ? 4 : 0) | (v[3] > level ? 8 : 0);
      if (c === 0 || c === 15) continue;
      const top = () => lerp(x, y, v[0], x + 1, y, v[1]);
      const right = () => lerp(x + 1, y, v[1], x + 1, y + 1, v[2]);
      const bottom = () => lerp(x, y + 1, v[3], x + 1, y + 1, v[2]);
      const left = () => lerp(x, y, v[0], x, y + 1, v[3]);
      // Oriented so that the inside (value > level) is on the left of each segment.
      switch (c) {
        case 1: add(left(), top()); break; case 2: add(top(), right()); break; case 3: add(left(), right()); break;
        case 4: add(right(), bottom()); break; case 6: add(top(), bottom()); break; case 7: add(left(), bottom()); break;
        case 8: add(bottom(), left()); break; case 9: add(bottom(), top()); break; case 11: add(bottom(), right()); break;
        case 12: add(right(), left()); break; case 13: add(right(), top()); break; case 14: add(top(), left()); break;
        case 5: add(left(), top()); add(right(), bottom()); break; case 10: add(top(), right()); add(bottom(), left()); break;
      }
    }
  }
  // Join segments by shared end points, ignoring their direction (holes are handled with fill-rule="evenodd").
  const all = [];
  for (const list of segments.values()) for (const s of list) all.push(s);
  const byPoint = new Map();
  const link = (p, idx) => { const k = key(p); if (!byPoint.has(k)) byPoint.set(k, []); byPoint.get(k).push(idx); };
  all.forEach((s, idx) => { link(s[0], idx); link(s[1], idx); });
  const used = new Uint8Array(all.length);
  const loops = [];
  for (let first = 0; first < all.length; first++) {
    if (used[first]) continue;
    used[first] = 1;
    const loop = [all[first][0], all[first][1]];
    const startKey = key(all[first][0]);
    let cur = all[first][1];
    for (;;) {
      const k = key(cur);
      if (k === startKey) { loop.pop(); break; }
      const next = (byPoint.get(k) ?? []).find(idx => !used[idx]);
      if (next === undefined) break;
      used[next] = 1;
      const s = all[next];
      cur = key(s[0]) === k ? s[1] : s[0];
      loop.push(cur);
    }
    if (loop.length > 20) loops.push(loop);
  }  return loops;
}

const sub = (a, b) => [a[0] - b[0], a[1] - b[1]];
const addv = (a, b) => [a[0] + b[0], a[1] + b[1]];
const mul = (a, s) => [a[0] * s, a[1] * s];
const dot = (a, b) => a[0] * b[0] + a[1] * b[1];
const len = a => Math.hypot(a[0], a[1]);
const norm = a => { const l = len(a) || 1; return [a[0] / l, a[1] / l]; };

/** Resamples a closed polyline to (about) even spacing, which makes tangents and fitting well-behaved. */
export function resample(loop, step) {
  const out = [loop[0]]; let carry = 0;
  for (let i = 0; i < loop.length; i++) {
    let a = loop[i]; const b = loop[(i + 1) % loop.length];
    let seg = len(sub(b, a));
    while (carry + seg >= step) {
      const t = (step - carry) / seg; a = addv(a, mul(sub(b, a), t)); out.push(a); seg = len(sub(b, a)); carry = 0;
    }
    carry += seg;
  }
  return out;
}

/** Indices of corners: points where the direction turns by more than `degrees` within `span` samples. */
export function corners(points, span = 4, degrees = 50) {
  const n = points.length; const turn = [];
  for (let i = 0; i < n; i++) {
    const a = norm(sub(points[i], points[(i - span + n) % n])), b = norm(sub(points[(i + span) % n], points[i]));
    turn.push(Math.acos(Math.max(-1, Math.min(1, dot(a, b)))) * 180 / Math.PI);
  }
  const found = [];
  for (let i = 0; i < n; i++) {
    if (turn[i] < degrees) continue;
    let isMax = true; for (let k = -span; k <= span; k++) if (turn[(i + k + n) % n] > turn[i]) isMax = false;
    if (isMax && !found.some(j => Math.min(Math.abs(j - i), n - Math.abs(j - i)) <= span)) found.push(i);
  }
  return found.sort((p, q) => p - q);
}

function bezier(c, t) {
  const mt = 1 - t;
  return addv(addv(mul(c[0], mt * mt * mt), mul(c[1], 3 * mt * mt * t)), addv(mul(c[2], 3 * mt * t * t), mul(c[3], t * t * t)));
}
function bezierD1(c, t) {
  const mt = 1 - t;
  return addv(addv(mul(sub(c[1], c[0]), 3 * mt * mt), mul(sub(c[2], c[1]), 6 * mt * t)), mul(sub(c[3], c[2]), 3 * t * t));
}
function bezierD2(c, t) {
  return addv(mul(addv(sub(c[2], mul(c[1], 2)), c[0]), 6 * (1 - t)), mul(addv(sub(c[3], mul(c[2], 2)), c[1]), 6 * t));
}

function chordParams(pts) {
  const u = [0]; for (let i = 1; i < pts.length; i++) u.push(u[i - 1] + len(sub(pts[i], pts[i - 1])));
  return u.map(v => v / u[u.length - 1]);
}

function generate(pts, u, t1, t2) {
  const first = pts[0], last = pts[pts.length - 1];
  let c00 = 0, c01 = 0, c11 = 0, x0 = 0, x1 = 0;
  u.forEach((t, i) => {
    const mt = 1 - t; const a1 = mul(t1, 3 * mt * mt * t), a2 = mul(t2, 3 * mt * t * t);
    c00 += dot(a1, a1); c01 += dot(a1, a2); c11 += dot(a2, a2);
    const tmp = sub(pts[i], addv(addv(mul(first, mt * mt * mt), mul(first, 3 * mt * mt * t)), addv(mul(last, 3 * mt * t * t), mul(last, t * t * t))));
    x0 += dot(a1, tmp); x1 += dot(a2, tmp);
  });
  const det = c00 * c11 - c01 * c01;
  let al = det ? (x0 * c11 - x1 * c01) / det : 0, ar = det ? (c00 * x1 - c01 * x0) / det : 0;
  const segLen = len(sub(last, first)); const eps = 1e-6 * segLen;
  if (al < eps || ar < eps) { al = ar = segLen / 3; }
  return [first, addv(first, mul(t1, al)), addv(last, mul(t2, ar)), last];
}

function maxError(pts, c, u) {
  let max = 0, at = Math.floor(pts.length / 2);
  u.forEach((t, i) => { const d = len(sub(bezier(c, t), pts[i])); if (d > max) { max = d; at = i; } });
  return [max, at];
}

function reparameterize(pts, u, c) {
  return u.map((t, i) => {
    const d = sub(bezier(c, t), pts[i]); const d1 = bezierD1(c, t), d2 = bezierD2(c, t);
    const den = dot(d1, d1) + dot(d, d2); return den ? Math.min(1, Math.max(0, t - dot(d, d1) / den)) : t;
  });
}

/** Fits cubic Béziers to an open run of points with end tangents `t1` (out of the first) and `t2` (into the last). */
export function fitRun(pts, t1, t2, tolerance, out = []) {
  if (pts.length === 2) {
    const d = len(sub(pts[1], pts[0])) / 3; out.push([pts[0], addv(pts[0], mul(t1, d)), addv(pts[1], mul(t2, d)), pts[1]]); return out;
  }
  let u = chordParams(pts); let c = generate(pts, u, t1, t2); let [err, split] = maxError(pts, c, u);
  if (err < tolerance) { out.push(c); return out; }
  if (err < tolerance * 4) {
    for (let k = 0; k < 20; k++) {
      u = reparameterize(pts, u, c); c = generate(pts, u, t1, t2); [err, split] = maxError(pts, c, u);
      if (err < tolerance) { out.push(c); return out; }
    }
  }
  split = Math.max(1, Math.min(pts.length - 2, split));
  const tc = norm(sub(pts[split - 1], pts[split + 1]));
  fitRun(pts.slice(0, split + 1), t1, tc, tolerance, out);
  fitRun(pts.slice(split), mul(tc, -1), t2, tolerance, out);
  return out;
}

/** Fits a closed outline: split at corners (or at 4 even points when smooth), keep tangents continuous at smooth joins. */
export function fitClosed(points, { tolerance, cornerSpan = 4, cornerDegrees = 50 } = {}) {
  const n = points.length;
  const cs = corners(points, cornerSpan, cornerDegrees);
  const cornerSet = new Set(cs);
  const breaks = cs.length ? cs : [0, Math.floor(n / 4), Math.floor(n / 2), Math.floor((3 * n) / 4)];
  const tangentAt = (i, dir) => {
    if (cornerSet.has(i)) {
      // One-sided tangent at a corner.
      const k = Math.min(4, Math.floor(n / 20) || 1);
      return dir > 0 ? norm(sub(points[(i + k) % n], points[i])) : norm(sub(points[(i - k + n) % n], points[i]));
    }
    const k = 3; const t = norm(sub(points[(i + k) % n], points[(i - k + n) % n]));
    return dir > 0 ? t : mul(t, -1);
  };
  const curves = [];
  for (let b = 0; b < breaks.length; b++) {
    const s = breaks[b], e = breaks[(b + 1) % breaks.length];
    const run = []; for (let i = s; ; i = (i + 1) % n) { run.push(points[i]); if (i === e && run.length > 1) break; }
    fitRun(run, tangentAt(s, 1), tangentAt(e, -1), tolerance, curves);
  }
  return { curves, corners: cs.length };
}

/** Converts fitted curves to an SVG path (absolute, 2 decimals), optionally offset. */
export function toPath(loops, dx = 0, dy = 0) {
  const f = v => (Math.round(v * 100) / 100).toString();
  const p = q => `${f(q[0] + dx)} ${f(q[1] + dy)}`;
  return loops.map(curves => `M${p(curves[0][0])}` + curves.map(c => `C${p(c[1])} ${p(c[2])} ${p(c[3])}`).join('') + 'Z').join('');
}
