// Step 1 – inspect the approved reference: separate the symbol from the light background and the floor shadow, and
// find the fold edges between the ribbon surfaces. Writes diagnostic maps to work/ (not deliverables).
import { mkdirSync } from 'node:fs';
import { loadRgba, lightness, blueness, saveMap, sobel } from './lib/image.mjs';

const REFERENCE = '../references/approved/zanance-symbol-reference.png';
mkdirSync('work', { recursive: true });

const { width, height, data } = await loadRgba(REFERENCE);
const L = new Float32Array(width * height);
const B = new Float32Array(width * height);
for (let i = 0; i < width * height; i++) {
  const [r, g, b] = [data[i * 4], data[i * 4 + 1], data[i * 4 + 2]];
  L[i] = lightness(r, g, b);
  B[i] = blueness(r, g, b);
}

// Histogram of blueness: background and shadow cluster near 0, the ribbon far above.
const bins = new Array(21).fill(0);
for (const v of B) bins[Math.max(0, Math.min(20, Math.round(v * 20)))]++;
console.log('blueness histogram (0..1 in 0.05 steps):', bins.join(' '));

// Corners are background: report their colours and the shadow row colours.
const px = (x, y) => { const i = (y * width + x) * 4; return [data[i], data[i + 1], data[i + 2]]; };
console.log('corners', px(2, 2), px(width - 3, 2), px(2, height - 3), px(width - 3, height - 3));
for (const y of [440, 455, 465, 475, 485]) console.log('row', y, 'centre', px(260, y), 'blueness', B[y * width + 260].toFixed(3));

await saveMap(B.map(v => Math.min(1, v * 1.6)), width, height, 'work/blueness.png');
await saveMap(L, width, height, 'work/lightness.png');
const edges = sobel(L, width, height);
await saveMap(edges.map(v => Math.min(1, v * 6)), width, height, 'work/edges.png');
console.log('size', width, height);
