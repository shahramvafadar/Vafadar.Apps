// Step 2 – split the symbol into its ribbon surfaces. The fold edges are thin bright/dark lines with a strong lightness
// gradient; the surfaces between them are smooth. Connected smooth areas inside the silhouette become regions; edge
// pixels are then given to the nearest region, so the regions tile the silhouette exactly.
import { writeFileSync } from 'node:fs';
import sharp from 'sharp';
import { loadRgba, lightness, blueness, sobel } from './lib/image.mjs';

export const REFERENCE = '../references/approved/zanance-symbol-reference.png';
export const SYMBOL_THRESHOLD = 0.25; // blueness: background/shadow ≤ 0.21, ribbon ≥ 0.3 (see analyze.mjs)
export const EDGE_THRESHOLD = 0.035;

export async function segment() {
  const { width, height, data } = await loadRgba(REFERENCE);
  const n = width * height;
  const L = new Float32Array(n), B = new Float32Array(n);
  for (let i = 0; i < n; i++) {
    L[i] = lightness(data[i * 4], data[i * 4 + 1], data[i * 4 + 2]);
    B[i] = blueness(data[i * 4], data[i * 4 + 1], data[i * 4 + 2]);
  }

  // Silhouette: blue pixels, with enclosed light highlight lines filled (flood the background from the border).
  const outside = new Uint8Array(n);
  const stack = [];
  for (let x = 0; x < width; x++) stack.push(x, (height - 1) * width + x);
  for (let y = 0; y < height; y++) stack.push(y * width, y * width + width - 1);
  while (stack.length) {
    const i = stack.pop();
    if (outside[i] || B[i] > SYMBOL_THRESHOLD) continue;
    outside[i] = 1;
    const x = i % width, y = (i / width) | 0;
    if (x > 0) stack.push(i - 1); if (x < width - 1) stack.push(i + 1);
    if (y > 0) stack.push(i - width); if (y < height - 1) stack.push(i + width);
  }
  const inside = outside.map(v => 1 - v);

  // Smooth areas: inside and away from fold edges.
  const E = sobel(L, width, height);
  const label = new Int32Array(n).fill(-1);
  const areas = [];
  for (let start = 0; start < n; start++) {
    if (!inside[start] || E[start] > EDGE_THRESHOLD || label[start] !== -1) continue;
    const id = areas.length; let area = 0;
    const s = [start]; label[start] = id;
    while (s.length) {
      const i = s.pop(); area++;
      const x = i % width, y = (i / width) | 0;
      for (const j of [x > 0 ? i - 1 : -1, x < width - 1 ? i + 1 : -1, y > 0 ? i - width : -1, y < height - 1 ? i + width : -1]) {
        if (j >= 0 && label[j] === -1 && inside[j] && E[j] <= EDGE_THRESHOLD) { label[j] = id; s.push(j); }
      }
    }
    areas.push(area);
  }

  // Keep real surfaces; tiny specks along the edges are merged away below.
  const keep = areas.map(a => a >= 400);
  for (let i = 0; i < n; i++) if (label[i] >= 0 && !keep[label[i]]) label[i] = -1;

  // Give every remaining inside pixel to the nearest kept region (multi-source BFS).
  const queue = [];
  for (let i = 0; i < n; i++) if (label[i] >= 0) queue.push(i);
  for (let q = 0; q < queue.length; q++) {
    const i = queue[q]; const x = i % width, y = (i / width) | 0;
    for (const j of [x > 0 ? i - 1 : -1, x < width - 1 ? i + 1 : -1, y > 0 ? i - width : -1, y < height - 1 ? i + width : -1]) {
      if (j >= 0 && inside[j] && label[j] === -1) { label[j] = label[i]; queue.push(j); }
    }
  }

  const ids = [...new Set(label.filter(v => v >= 0))];
  const regions = ids.map(id => {
    let count = 0, sx = 0, sy = 0, minX = width, minY = height, maxX = 0, maxY = 0, sr = 0, sg = 0, sb = 0;
    for (let i = 0; i < n; i++) if (label[i] === id) {
      const x = i % width, y = (i / width) | 0; count++; sx += x; sy += y;
      minX = Math.min(minX, x); maxX = Math.max(maxX, x); minY = Math.min(minY, y); maxY = Math.max(maxY, y);
      sr += data[i * 4]; sg += data[i * 4 + 1]; sb += data[i * 4 + 2];
    }
    return { id, count, cx: sx / count, cy: sy / count, box: [minX, minY, maxX, maxY], mean: [sr / count, sg / count, sb / count].map(Math.round) };
  });
  return { width, height, data, inside, label, regions, L };
}

if (import.meta.url === `file:///${process.argv[1].replace(/\\/g, '/')}`) {
  const { width, height, label, regions } = await segment();
  const palette = [[230, 25, 75], [60, 180, 75], [255, 225, 25], [0, 130, 200], [245, 130, 48], [145, 30, 180], [70, 240, 240], [240, 50, 230], [210, 245, 60], [250, 190, 212], [0, 128, 128], [170, 110, 40]];
  const out = Buffer.alloc(width * height * 3, 0);
  regions.forEach((r, k) => { for (let i = 0; i < label.length; i++) if (label[i] === r.id) out.set(palette[k % palette.length], i * 3); });
  await sharp(out, { raw: { width, height, channels: 3 } }).png().toFile('work/regions.png');
  regions.forEach((r, k) => console.log(k, JSON.stringify({ ...r, colour: palette[k % palette.length] })));
  writeFileSync('work/regions.json', JSON.stringify(regions, null, 1));
}
