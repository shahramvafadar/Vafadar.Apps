// Pixel helpers for analysing the approved reference. Everything works on plain arrays, so results are reproducible.
import sharp from 'sharp';

/** Loads an image as RGBA floats in [0,1] plus its size. */
export async function loadRgba(path) {
  const { data, info } = await sharp(path).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  return { width: info.width, height: info.height, data };
}

/** Relative luminance-like lightness (0–1) from sRGB bytes; enough for edge finding. */
export function lightness(r, g, b) {
  return (0.2126 * r + 0.7152 * g + 0.0722 * b) / 255;
}

/** How "blue" a pixel is: the logo is saturated blue, the background and the floor shadow are nearly neutral. */
export function blueness(r, g, b) {
  return (b - r) / 255;
}

/** Writes a single-channel float map (0–1) as a greyscale PNG for inspection. */
export async function saveMap(map, width, height, path) {
  const bytes = Buffer.alloc(width * height);
  for (let i = 0; i < map.length; i++) bytes[i] = Math.max(0, Math.min(255, Math.round(map[i] * 255)));
  await sharp(bytes, { raw: { width, height, channels: 1 } }).png().toFile(path);
}

/** Sobel gradient magnitude of a float map. */
export function sobel(map, width, height) {
  const out = new Float32Array(width * height);
  for (let y = 1; y < height - 1; y++) {
    for (let x = 1; x < width - 1; x++) {
      const at = (dx, dy) => map[(y + dy) * width + (x + dx)];
      const gx = -at(-1, -1) - 2 * at(-1, 0) - at(-1, 1) + at(1, -1) + 2 * at(1, 0) + at(1, 1);
      const gy = -at(-1, -1) - 2 * at(0, -1) - at(1, -1) + at(-1, 1) + 2 * at(0, 1) + at(1, 1);
      out[y * width + x] = Math.hypot(gx, gy) / 4;
    }
  }
  return out;
}
