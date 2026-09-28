// Compares a rendered SVG with the approved reference at the same scale and position (uniform scale 1, offset = the
// symbol origin in the reference). Only pixels of the symbol count: the light background and the floor shadow are not
// part of the geometry. Reports silhouette IoU and mean colour error, and writes an error heat map.
import sharp from 'sharp';
import { Resvg } from '@resvg/resvg-js';
import { segment } from './segment.mjs';

export async function compare(svg, geometry, heatmapPath) {
  const { width, height, data, inside } = await segment();
  const rendered = new Resvg(svg, { fitTo: { mode: 'original' }, background: 'rgba(0,0,0,0)' }).render();
  const r = rendered.pixels; const rw = rendered.width;
  const [ox, oy] = geometry.origin;
  let inter = 0, union = 0, err = 0, count = 0;
  const heat = Buffer.alloc(width * height * 3);
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      const i = y * width + x;
      const sx = x - ox, sy = y - oy;
      const inR = sx >= 0 && sy >= 0 && sx < rendered.width && sy < rendered.height;
      const k = inR ? (sy * rw + sx) * 4 : -1;
      const a = inR ? r[k + 3] / 255 : 0;
      const ref = inside[i] === 1, vec = a > 0.5;
      if (ref && vec) inter++; if (ref || vec) union++;
      if (ref && vec) {
        const e = (Math.abs(r[k] - data[i * 4]) + Math.abs(r[k + 1] - data[i * 4 + 1]) + Math.abs(r[k + 2] - data[i * 4 + 2])) / 3;
        err += e; count++;
        const v = Math.min(255, e * 6); heat[i * 3] = v; heat[i * 3 + 1] = v; heat[i * 3 + 2] = v;
      } else if (ref !== vec) { heat[i * 3] = 255; } // red: silhouette mismatch
    }
  }
  if (heatmapPath) await sharp(heat, { raw: { width, height, channels: 3 } }).png().toFile(heatmapPath);
  return { iou: inter / union, meanAbsError: err / count, comparedPixels: count };
}
