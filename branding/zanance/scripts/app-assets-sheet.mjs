// Review sheet of the icons and splash images the MAUI build actually generated (obj/…/resizetizer), with the Android
// launcher masks. Writes previews/app-assets.png. Run after a Debug build for android, ios and windows.
import { readdirSync, existsSync } from 'node:fs';
import { join } from 'node:path';
import sharp from 'sharp';

const OBJ = '../../../src/Apps/Zanance/Vafadar.Zanance.App/obj/Debug';
const find = (dir, pattern) => { const d = join(OBJ, dir); if (!existsSync(d)) return null; const f = readdirSync(d).find(n => pattern.test(n)); return f ? join(d, f) : null; };
const BG = '#F4F6F8', TILE = 220;

async function tile(input, { mask = null, background = '#FFFFFF', label = '', size = 180, tint = null } = {}) {
  let img = await sharp(input).resize(size, size, { fit: 'contain', background: { r: 0, g: 0, b: 0, alpha: 0 } }).png().toBuffer();
  if (tint) { // themed icon: Android uses only the alpha of the layer
    const { data, info } = await sharp(img).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
    for (let i = 0; i < data.length; i += 4) { data[i] = tint[0]; data[i + 1] = tint[1]; data[i + 2] = tint[2]; }
    img = await sharp(data, { raw: info }).png().toBuffer();
  }
  if (mask) {
    const svg = Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}"><rect width="${size}" height="${size}" rx="${mask * size}" ry="${mask * size}"/></svg>`);
    img = await sharp(img).composite([{ input: svg, blend: 'dest-in' }]).png().toBuffer();
  }
  const text = Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="${TILE}" height="30"><text x="${TILE / 2}" y="20" font-family="Segoe UI, Arial" font-size="12" text-anchor="middle" fill="#374151">${label}</text></svg>`);
  return sharp({ create: { width: TILE, height: TILE + 30, channels: 4, background } })
    .composite([{ input: img, left: (TILE - size) / 2, top: (TILE - size) / 2 }, { input: text, left: 0, top: TILE }]).png().toBuffer();
}

const android = 'net10.0-android/resizetizer/r/mipmap-xxxhdpi';
const fg = find(android, /^appicon_foreground\.png$/), bg = find(android, /^appicon_background\.png$/);
const adaptive = await sharp(bg).composite([{ input: fg }]).png().toBuffer();
const iosSet = 'net10.0-ios/iossimulator-x64/resizetizer/r/Assets.xcassets/appicon.appiconset';
const iosIcon = find(iosSet, /1024/) ?? find(iosSet, /60x60@3x/);
const iosSplash = find('net10.0-ios/iossimulator-x64/resizetizer/sp', /^splashlockup_.*@3x\.png$/);
const winIcon = find('net10.0-windows10.0.19041.0/win-x64/resizetizer/r', /^appiconLogo\.altform-unplated_targetsize-256\.png$/);
const winSmall = find('net10.0-windows10.0.19041.0/win-x64/resizetizer/r', /^appiconLogo\.altform-unplated_targetsize-32\.png$/);
const androidSplash = find('net10.0-android/resizetizer/sp/drawable-xxxhdpi', /^splash\.png$/);

const splashTile = async (input, label, circle) => {
  const w = 440, h = 250; let img = await sharp(input).resize({ width: circle ? 150 : 340, height: 150, fit: 'inside' }).png().toBuffer();
  const m = await sharp(img).metadata();
  const overlay = circle ? [{ input: Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="${h}"><circle cx="${w / 2}" cy="${h / 2 - 15}" r="75" fill="none" stroke="#9CA3AF" stroke-dasharray="4 4"/></svg>`), left: 0, top: 0 }] : [];
  const text = Buffer.from(`<svg xmlns="http://www.w3.org/2000/svg" width="${w}" height="30"><text x="${w / 2}" y="20" font-family="Segoe UI, Arial" font-size="12" text-anchor="middle" fill="#374151">${label}</text></svg>`);
  return sharp({ create: { width: w, height: h, channels: 4, background: BG } }).composite([{ input: img, left: Math.round((w - m.width) / 2), top: Math.round((h - 30 - m.height) / 2) }, ...overlay, { input: text, left: 0, top: h - 30 }]).png().toBuffer();
};

const row1 = [
  await tile(adaptive, { mask: 0.5, label: 'Android adaptive – circle' }),
  await tile(adaptive, { mask: 0.3, label: 'Android adaptive – squircle-like' }),
  await tile(adaptive, { mask: 0.12, label: 'Android adaptive – rounded square' }),
  await tile(fg, { mask: 0.5, background: '#E8EEF6', tint: [30, 58, 110], label: 'Android 13+ themed (alpha of fg)' }),
];
const row2 = [
  await tile(find(android, /^appicon\.png$/), { label: 'Android legacy appicon.png' }),
  await tile(find(android, /^appicon_round\.png$/), { label: 'Android legacy appicon_round.png' }),
  await tile(iosIcon, { mask: 0.2237, label: 'iOS app icon (mask = preview only)' }),
  await tile(winIcon, { label: 'Windows appiconLogo 256 px' }),
];
const row3 = [await splashTile(androidSplash, 'Android splash (dashed = Android 12+ circle)', true), await splashTile(iosSplash, 'iOS splash (symbol + wordmark)', false)];
const small = await sharp({ create: { width: 880, height: 70, channels: 4, background: '#FFFFFF' } })
  .composite([{ input: winSmall, left: 20, top: 19 }, { input: Buffer.from('<svg xmlns="http://www.w3.org/2000/svg" width="700" height="30"><text x="0" y="20" font-family="Segoe UI, Arial" font-size="12" fill="#374151">Windows 32 px taskbar size, real pixels (generated appiconLogo targetsize-32)</text></svg>'), left: 70, top: 20 }]).png().toBuffer();

const W = 880;
const place = (tiles, top, width) => tiles.map((t, i) => ({ input: t, left: i * width, top }));
await sharp({ create: { width: W, height: 250 * 3 + 70, channels: 4, background: '#FFFFFF' } })
  .composite([...place(row1, 0, 220), ...place(row2, 250, 220), ...place(row3, 500, 440), { input: small, left: 0, top: 750 }])
  .png().toFile('../previews/app-assets.png');
console.log('previews/app-assets.png');
