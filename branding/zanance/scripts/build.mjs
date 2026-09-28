// Builds every Zanance brand candidate from the approved reference, in one run:
//   node build.mjs            (from branding/zanance/scripts, after `npm ci`)
// 1. reconstruct the geometry (source/geometry.json) from references/approved/zanance-symbol-reference.png;
// 2. write the master and all SVG variants, lockups, PNG, WebP, PDF, EPS, ICO and platform icon candidates from it;
// 3. write reports/manifest.json. Deterministic: the same inputs and tool versions give byte-identical outputs.
import { readFileSync, writeFileSync, mkdirSync, existsSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { dirname } from 'node:path';
import sharp from 'sharp';
import { Resvg } from '@resvg/resvg-js';
import { reconstruct } from './reconstruct.mjs';
import { symbolSvg, framedSvg, lockupSvg, CANDIDATE } from './lib/svg.mjs';
import { wordmarkPath, eps, ico, pdf, WORDMARK_FONTS } from './lib/formats.mjs';
import { compare } from './compare.mjs';
import { hex } from './lib/fit.mjs';

const ROOT = '..';
const sha = b => createHash('sha256').update(b).digest('hex');
const outputs = [];
function write(path, content, meta = {}) {
  const full = `${ROOT}/${path}`; mkdirSync(dirname(full), { recursive: true });
  writeFileSync(full, content);
  outputs.push({ path, sha256: sha(content), bytes: Buffer.byteLength(content), status: CANDIDATE, ...meta });
}
const render = (svg, width) => new Resvg(svg, { fitTo: { mode: 'width', value: width }, background: 'rgba(0,0,0,0)', font: { loadSystemFonts: false } }).render().asPng();

// Reference hashes before the run (they must not change).
const REFERENCES = ['references/approved/zanance-symbol-reference.png', ...['zanance_brand_identity_system.png', 'glossy_blue_ribbon_z_logo.png', 'zanance_glossy_ribbon_logo.png', 'glossy_charcoal_ribbon_z_emblem.png', 'white_ribbon_z_emblem.png', 'zanance_brand_identity_system_board.png', 'zanance_brand_guidelines_board.png'].map(f => `references/supplemental/${f}`)];
const referenceHashes = Object.fromEntries(REFERENCES.map(p => [p, sha(readFileSync(`${ROOT}/${p}`))]));

// 1. Geometry (the single source).
const geometry = await reconstruct();
const geometryJson = JSON.stringify(geometry, null, 1) + '\n';
const geometryHash = sha(geometryJson).slice(0, 16);
write('source/geometry.json', geometryJson, { kind: 'geometry', note: 'single geometric source of every output' });
const [W, H] = geometry.size;

// 2. SVG symbol variants.
const COLORS = { black: '#000000', white: '#FFFFFF' };
const svgs = {
  master: symbolSvg(geometry, { variant: 'color', geometryHash }),
  flat: symbolSvg(geometry, { variant: 'flat', geometryHash }),
  monoBlack: symbolSvg(geometry, { variant: 'mono', monoColor: COLORS.black, geometryHash }),
  monoWhite: symbolSvg(geometry, { variant: 'mono', monoColor: COLORS.white, geometryHash }),
};
write('source/zanance-symbol-master.svg', svgs.master, { kind: 'svg', viewBox: `0 0 ${W} ${H}` });
write('exports/svg/zanance-symbol.svg', svgs.master, { kind: 'svg', viewBox: `0 0 ${W} ${H}` });
write('exports/svg/zanance-symbol-flat-blue.svg', svgs.flat, { kind: 'svg', viewBox: `0 0 ${W} ${H}` });
write('exports/svg/zanance-symbol-mono-black.svg', svgs.monoBlack, { kind: 'svg', viewBox: `0 0 ${W} ${H}` });
write('exports/svg/zanance-symbol-mono-white.svg', svgs.monoWhite, { kind: 'svg', viewBox: `0 0 ${W} ${H}` });

// Wordmark proposals and lockups. Text colours are the neutral text tokens of the draft (reports/tokens.draft.json).
const TEXT_ON_LIGHT = '#111827', TEXT_ON_DARK = '#F3F4F6';
const wordmarks = {};
for (const key of Object.keys(WORDMARK_FONTS)) {
  const wm = wordmarkPath(key, 'Zanance', 1000);
  wordmarks[key] = { font: wm.fontName, version: wm.fontVersion, package: WORDMARK_FONTS[key].package + '@' + wm.packageVersion, license: wm.license };
  const suffix = key === 'urbanist' ? '' : `-alt-${key}`;
  for (const [bg, color] of [['on-light', TEXT_ON_LIGHT], ['on-dark', TEXT_ON_DARK]]) {
    const svg = lockupSvg(geometry, wm, { textColor: color, geometryHash, note: key === 'urbanist' ? 'Primary proposal.' : 'Alternative proposal.' });
    write(`exports/svg/zanance-lockup-horizontal-${bg}${suffix}.svg`, svg, { kind: 'svg', wordmark: wordmarks[key] });
    if (key === 'urbanist') {
      write(`exports/png/lockup/zanance-lockup-horizontal-${bg}-1200w.png`, render(svg, 1200), { kind: 'png', width: 1200, alpha: true });
    }
  }
}
// Editable lockup source: live text (needs the Urbanist font installed) – never for distribution.
{
  const wm = wordmarkPath('urbanist', 'Zanance', 1000); const scale = (0.40 * H) / wm.capHeight;
  const x = W + 0.22 * H - wm.box.x1 * scale; const y = H / 2 + (wm.capHeight * scale) / 2;
  const editable = svgs.master.replace('</svg>', `<text x="${x.toFixed(2)}" y="${y.toFixed(2)}" font-family="Urbanist" font-weight="700" font-size="${(1000 * scale).toFixed(2)}" fill="${TEXT_ON_LIGHT}">Zanance</text>\n</svg>`)
    .replace(/viewBox="0 0 [\d.]+ [\d.]+"/, `viewBox="0 0 ${(W + 0.22 * H + (wm.box.x2 - wm.box.x1) * scale).toFixed(2)} ${H}"`)
    .replace(/ width="[\d.]+" height="[\d.]+"/, '');
  write('source/zanance-lockup-horizontal-editable.svg', editable, { kind: 'svg-editable', note: 'live text; Urbanist 700 must be installed; not for distribution' });
}

// 3. PNG (transparent, square canvases; symbol = 90 % of the side), on white and on dark grey, and WebP lossless.
const PNG_SIZES = [64, 128, 256, 512, 1024, 2048, 4096];
const FILL = 0.9;
for (const size of PNG_SIZES) {
  const png = render(framedSvg(geometry, { side: 1024, fill: FILL, geometryHash }), size);
  write(`exports/png/zanance-symbol-${size}.png`, png, { kind: 'png', width: size, height: size, alpha: true, symbolFill: FILL });
  if (size >= 256 && size <= 2048) {
    const webp = await sharp(png).webp({ lossless: true, alphaQuality: 100, effort: 6 }).toBuffer();
    write(`exports/webp/zanance-symbol-${size}.webp`, webp, { kind: 'webp', width: size, height: size, alpha: true, lossless: true });
  }
}
for (const [name, bg] of [['on-white', '#FFFFFF'], ['on-dark', '#1F2329']]) {
  write(`exports/png/zanance-symbol-1024-${name}.png`, render(framedSvg(geometry, { side: 1024, fill: FILL, background: bg, geometryHash }), 1024), { kind: 'png', width: 1024, height: 1024, alpha: false, background: bg });
}
for (const [name, svg] of [['mono-black', svgs.monoBlack], ['mono-white', svgs.monoWhite], ['flat-blue', svgs.flat]]) {
  const variant = name.startsWith('mono') ? 'mono' : 'flat';
  const framed = framedSvg(geometry, { side: 1024, fill: FILL, variant, monoColor: name === 'mono-white' ? COLORS.white : COLORS.black, geometryHash });
  write(`exports/png/zanance-symbol-${name}-1024.png`, render(framed, 1024), { kind: 'png', width: 1024, height: 1024, alpha: true });
  void svg;
}

// 4. Vector PDF (colour and mono): page = artwork + 5 % margin.
const MARGIN = Math.round(0.05 * Math.max(W, H));
write('exports/pdf/zanance-symbol.pdf', await pdf(svgs.master, W, H, MARGIN, 'Zanance symbol – ' + CANDIDATE), { kind: 'pdf', page: [W + 2 * MARGIN, H + 2 * MARGIN] });
write('exports/pdf/zanance-symbol-mono-black.pdf', await pdf(svgs.monoBlack, W, H, MARGIN, 'Zanance symbol mono – ' + CANDIDATE), { kind: 'pdf', page: [W + 2 * MARGIN, H + 2 * MARGIN] });

// 5. EPS for the mono and flat versions (flat surfaces clipped to the silhouette). No colour EPS: see the report.
const rgb = h => [1, 3, 5].map(i => parseInt(h.slice(i, i + 2), 16));
write('exports/eps/zanance-symbol-mono-black.eps', eps({ width: W, height: H, margin: MARGIN, title: 'Zanance symbol mono black – ' + CANDIDATE, items: [{ d: geometry.silhouette, rgb: [0, 0, 0] }] }), { kind: 'eps' });
const darkest = geometry.surfaces.map(s => s.mean).sort((a, b) => a[0] + a[1] + a[2] - (b[0] + b[1] + b[2]))[0];
write('exports/eps/zanance-symbol-flat-blue.eps', eps({
  width: W, height: H, margin: MARGIN, title: 'Zanance symbol flat – ' + CANDIDATE, clip: geometry.silhouette,
  items: [{ d: geometry.silhouette, rgb: darkest }, ...geometry.surfaces.map(s => ({ d: s.path, rgb: rgb(hex(s.mean)) }))],
}), { kind: 'eps' });

// 6. Favicon: 16, 32, 48, 256 rendered directly from the vector at each size (symbol = 94 % there, for legibility).
const icoPngs = [16, 32, 48, 256].map(size => ({ size, data: render(framedSvg(geometry, { side: 1024, fill: 0.94, geometryHash }), size) }));
icoPngs.forEach(p => write(`exports/ico/zanance-favicon-${p.size}.png`, p.data, { kind: 'png', width: p.size, height: p.size, alpha: true }));
write('exports/ico/zanance-favicon.ico', ico(icoPngs), { kind: 'ico', sizes: [16, 32, 48, 256] });

// 7. Platform candidates (not installed in the app). Neutral light/dark backgrounds are separate layers.
const BG_LIGHT = '#F4F6F8', BG_DARK = '#111827';
// Android adaptive icon: 108 dp canvas, the symbol must stay inside the 66 dp safe circle. Scale so the farthest
// point of the silhouette from the centre is at 33 dp.
const pts = [...geometry.silhouette.matchAll(/(-?\d*\.?\d+) (-?\d*\.?\d+)/g)].map(m => [parseFloat(m[1]), parseFloat(m[2])]);
const radius = Math.max(...pts.map(([x, y]) => Math.hypot(x - W / 2, y - H / 2)));
const androidFill = ((33 / 108) * Math.max(W, H)) / radius; // fraction of the canvas side for the symbol's larger side
for (const [name, opts] of [
  ['android/zanance-adaptive-foreground', { variant: 'color' }],
  ['android/zanance-adaptive-monochrome', { variant: 'mono', monoColor: '#000000' }],
]) {
  const svg = framedSvg(geometry, { side: 432, fill: androidFill, geometryHash, ...opts });
  write(`platform-candidates/${name}.svg`, svg, { kind: 'svg', note: `symbol fill ${androidFill.toFixed(3)} (inside the 66 dp safe circle)` });
  write(`platform-candidates/${name}-432.png`, render(svg, 432), { kind: 'png', width: 432, height: 432, alpha: true });
}
for (const [name, bg] of [['light', BG_LIGHT], ['dark', BG_DARK]]) {
  const svg = `<?xml version="1.0" encoding="UTF-8"?>\n<!-- Zanance adaptive-icon background (${name}), ${CANDIDATE}. -->\n<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 432 432" width="432" height="432"><rect width="432" height="432" fill="${bg}"/></svg>\n`;
  write(`platform-candidates/android/zanance-adaptive-background-${name}.svg`, svg, { kind: 'svg', background: bg });
  write(`platform-candidates/android/zanance-adaptive-background-${name}-432.png`, render(svg, 432), { kind: 'png', width: 432, height: 432 });
}
// iOS: 1024 px, opaque, no rounded corners (the system applies the mask); symbol at 72 % of the side.
for (const [name, bg] of [['light', BG_LIGHT], ['dark', BG_DARK]]) {
  const svg = framedSvg(geometry, { side: 1024, fill: 0.72, background: bg, geometryHash });
  write(`platform-candidates/ios/zanance-appicon-${name}-1024.png`, await sharp(render(svg, 1024)).flatten({ background: bg }).png().toBuffer(), { kind: 'png', width: 1024, height: 1024, alpha: false, background: bg });
}
// .NET MAUI: MauiIcon background + foreground and MauiSplashScreen sources (same safe area as Android).
write('platform-candidates/maui/appicon.svg', `<?xml version="1.0" encoding="UTF-8"?>\n<!-- MauiIcon background candidate, ${CANDIDATE}. -->\n<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 456 456" width="456" height="456"><rect width="456" height="456" fill="${BG_LIGHT}"/></svg>\n`, { kind: 'svg' });
write('platform-candidates/maui/appiconfg.svg', framedSvg(geometry, { side: 456, fill: androidFill, geometryHash }), { kind: 'svg', note: 'MauiIcon ForegroundFile candidate' });
write('platform-candidates/maui/splash.svg', framedSvg(geometry, { side: 456, fill: 0.9, geometryHash }), { kind: 'svg', note: 'MauiSplashScreen candidate' });

// 8. Comparison with the reference (same scale and position) for the report.
mkdirSync(`${ROOT}/previews`, { recursive: true });
const metrics = {
  color: await compare(svgs.master, geometry, `${ROOT}/previews/difference-color.png`),
  flat: await compare(svgs.flat, geometry, null),
};
outputs.push({ path: 'previews/difference-color.png', sha256: sha(readFileSync(`${ROOT}/previews/difference-color.png`)), kind: 'png', status: CANDIDATE, note: 'error heat map; red = silhouette mismatch' });

// 9. Manifest.
const after = Object.fromEntries(REFERENCES.map(p => [p, sha(readFileSync(`${ROOT}/${p}`))]));
const packageJson = JSON.parse(readFileSync('package.json', 'utf8'));
const manifest = {
  status: CANDIDATE,
  source: { reference: REFERENCES[0], referenceSha256: referenceHashes[REFERENCES[0]], geometry: 'source/geometry.json', geometryHash },
  symbolSize: geometry.size, symbolOriginInReference: geometry.origin,
  pngLayout: { symbolFillOfCanvas: FILL, favicon: 0.94, androidAdaptive: Number(androidFill.toFixed(4)), iosAppIcon: 0.72 },
  tools: { node: process.version, ...packageJson.dependencies, renderer: '@resvg/resvg-js ' + packageJson.dependencies['@resvg/resvg-js'] },
  wordmarks,
  metrics,
  referencesUnchanged: REFERENCES.every(p => referenceHashes[p] === after[p]),
  referenceHashes: after,
  outputs: outputs.sort((a, b) => a.path.localeCompare(b.path)),
};
writeFileSync(`${ROOT}/reports/manifest.json`, JSON.stringify(manifest, null, 1) + '\n');
console.log(`${outputs.length} outputs; geometry ${geometryHash}; IoU ${metrics.color.iou.toFixed(4)}; mean error ${metrics.color.meanAbsError.toFixed(2)}; references unchanged: ${manifest.referencesUnchanged}`);
if (!existsSync(`${ROOT}/reports`)) throw new Error('reports folder missing');
