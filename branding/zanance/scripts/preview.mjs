// Builds previews/review.html (offline; every image is a built file), the PDF check page, their screenshots via headless
// Edge/Chromium (second, independent renderer), and the resvg-vs-Chromium comparison. Run after build.mjs.
import { readFileSync, writeFileSync, existsSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';
import sharp from 'sharp';
import { Resvg } from '@resvg/resvg-js';
import { draftTokens, contrastTable } from './lib/tokens.mjs';

const ROOT = resolve('..');
const manifest = JSON.parse(readFileSync(`${ROOT}/reports/manifest.json`, 'utf8'));
const geometry = JSON.parse(readFileSync(`${ROOT}/source/geometry.json`, 'utf8'));
const [W, H] = geometry.size; const [OX, OY] = geometry.origin; const [RW, RH] = geometry.referenceSize;
const EDGE = ['C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe', 'C:/Program Files/Microsoft/Edge/Application/msedge.exe', 'C:/Program Files/Google/Chrome/Application/chrome.exe'].find(existsSync);
if (!EDGE) throw new Error('No Chromium-based browser found for screenshots.');
const browserVersion = execFileSync('powershell', ['-NoProfile', '-Command', `(Get-Item '${EDGE}').VersionInfo.ProductVersion`]).toString().trim();

function screenshot(html, png, width, height, extra = []) {
  execFileSync(EDGE, ['--headless=new', '--disable-gpu', '--hide-scrollbars', '--force-device-scale-factor=1', '--allow-file-access-from-files',
    '--default-background-color=00000000', `--window-size=${width},${height}`, ...extra, `--screenshot=${png}`, pathToFileURL(html).href], { stdio: 'ignore', timeout: 120000 });
}

// 1. Draft tokens with measured contrast.
const tokens = draftTokens(geometry);
const table = contrastTable(tokens);
writeFileSync(`${ROOT}/reports/tokens.draft.json`, JSON.stringify({ ...tokens, contrast: table }, null, 1) + '\n');

// 2. Second renderer: Chromium draws the master SVG at 2×; compare with resvg at the same size on white.
const side = 2;
writeFileSync(`${ROOT}/previews/render-chromium.html`, `<!doctype html><meta charset="utf-8"><style>html,body{margin:0;background:#fff}img{display:block}</style><img src="../exports/svg/zanance-symbol.svg" width="${W * side}" height="${H * side}">`);
screenshot(`${ROOT}/previews/render-chromium.html`, `${ROOT}/previews/render-chromium.png`, W * side, H * side);
const chromium = await sharp(`${ROOT}/previews/render-chromium.png`).removeAlpha().raw().toBuffer({ resolveWithObject: true });
const resvgPng = new Resvg(readFileSync(`${ROOT}/exports/svg/zanance-symbol.svg`, 'utf8'), { fitTo: { mode: 'width', value: W * side }, background: '#ffffff' }).render().asPng();
writeFileSync(`${ROOT}/previews/render-resvg.png`, resvgPng);
const rv = await sharp(resvgPng).removeAlpha().raw().toBuffer({ resolveWithObject: true });
let sum = 0, max = 0, over8 = 0; const n = Math.min(rv.data.length, chromium.data.length) / 3;
for (let i = 0; i < n * 3; i += 3) {
  const d = (Math.abs(rv.data[i] - chromium.data[i]) + Math.abs(rv.data[i + 1] - chromium.data[i + 1]) + Math.abs(rv.data[i + 2] - chromium.data[i + 2])) / 3;
  sum += d; max = Math.max(max, d); if (d > 8) over8++;
}
const rendererComparison = { renderers: [`resvg ${manifest.tools['@resvg/resvg-js']}`, `Chromium (${EDGE.includes('Edge') ? 'Microsoft Edge' : 'Chrome'} ${browserVersion})`], size: [W * side, H * side], sizesMatch: chromium.info.width === rv.info.width && chromium.info.height === rv.info.height, meanAbsDifference: +(sum / n).toFixed(3), maxDifference: +max.toFixed(1), pixelsOver8: over8, pixelsOver8Share: +(over8 / n).toFixed(5) };

// 3. The review page.
const svg = p => `../${p}`;
const tokenRows = table.map(r => `<tr><td>${r.theme}</td><td>${r.foreground}</td><td>${r.background}</td><td><span class="sw" style="background:${r.colors[0]}"></span>${r.colors[0]}</td><td><span class="sw" style="background:${r.colors[1]}"></span>${r.colors[1]}</td><td>${r.ratio.toFixed(2)}:1</td><td>${r.required > 1 ? r.required + ':1 (' + r.kind + ')' : r.kind}</td><td class="${r.pass ? 'ok' : 'bad'}">${r.pass ? 'pass' : 'FAIL'}</td></tr>`).join('');
const html = `<!doctype html>
<html lang="en"><head><meta charset="utf-8"><title>Zanance – vector reconstruction review (candidate)</title>
<style>
body{font:14px/1.45 system-ui,Segoe UI,Arial,sans-serif;margin:24px;color:#111827;background:#F7F8FA;width:1352px}
h1{font-size:22px;margin:0 0 4px} h2{font-size:16px;margin:28px 0 10px;border-top:1px solid #D0D5DD;padding-top:16px}
.badge{display:inline-block;background:#FEF3C7;color:#8A4B08;border-radius:6px;padding:2px 8px;font-weight:600}
.row{display:flex;gap:16px;flex-wrap:wrap;align-items:flex-start} .card{background:#fff;border:1px solid #D0D5DD;border-radius:10px;padding:12px}
.cap{font-size:12px;color:#4B5563;margin-top:6px;max-width:520px}
.stage{position:relative;width:${RW}px;height:${RH}px;overflow:hidden;background:#fff}
.stage img{position:absolute} .vec{left:${OX}px;top:${OY}px;width:${W}px;height:${H}px}
.checker{background:conic-gradient(#e5e7eb 25%,#fff 0 50%,#e5e7eb 0 75%,#fff 0) 0 0/16px 16px}
.bg{width:200px;height:200px;display:flex;align-items:center;justify-content:center;border-radius:8px}
.px img{image-rendering:auto;margin:0 10px 0 0;vertical-align:middle}
.mask{width:120px;height:120px;overflow:hidden;display:inline-block;margin-right:10px}
table{border-collapse:collapse;font-size:12px} td,th{border:1px solid #D0D5DD;padding:3px 6px;text-align:left} .sw{display:inline-block;width:12px;height:12px;border:1px solid #999;margin-right:4px;vertical-align:-2px}
.ok{color:#15803D;font-weight:600} .bad{color:#B42318;font-weight:700}
</style></head><body>
<h1>Zanance – vector reconstruction <span class="badge">candidate — owner review required</span></h1>
<div class="cap">Reference: references/approved/zanance-symbol-reference.png (514×489, sha256 ${manifest.source.referenceSha256.slice(0, 16)}…). Geometry ${manifest.source.geometryHash}. All images on this page are the built files in exports/ and platform-candidates/; nothing is redrawn.</div>

<h2>1. Reference and vector at the same uniform scale (1 reference px = 1 css px)</h2>
<div class="row">
 <div class="card"><div class="stage"><img src="../references/approved/zanance-symbol-reference.png" style="left:0;top:0"></div><div class="cap">Approved reference (background and floor shadow are part of the photo, not of the symbol).</div></div>
 <div class="card"><div class="stage"><img class="vec" src="${svg('exports/svg/zanance-symbol.svg')}"></div><div class="cap">Reconstructed SVG placed at the symbol origin (${OX}, ${OY}); transparent, no outer shadow.</div></div>
</div>
<div class="row" style="margin-top:16px">
 <div class="card"><div class="stage"><img src="../references/approved/zanance-symbol-reference.png" style="left:0;top:0"><img class="vec" src="${svg('exports/svg/zanance-symbol.svg')}" style="opacity:.5"></div><div class="cap">Overlay, vector at 50 % opacity over the reference.</div></div>
 <div class="card"><div class="stage" style="background:#000"><img src="../references/approved/zanance-symbol-reference.png" style="left:0;top:0"><img class="vec" src="${svg('exports/svg/zanance-symbol.svg')}" style="mix-blend-mode:difference"></div><div class="cap">Difference blend (review aid only): black = identical; bright lines = differences. The background turns light where there is no symbol.</div></div>
 <div class="card"><div class="stage"><img src="difference-color.png" style="left:0;top:0"></div><div class="cap">Error heat map from compare.mjs (grey = colour difference ×6; red = silhouette mismatch). Silhouette IoU ${manifest.metrics.color.iou.toFixed(4)}, mean colour error ${manifest.metrics.color.meanAbsError.toFixed(2)}/255 inside the symbol.</div></div>
</div>

<h2>2. Backgrounds</h2>
<div class="row">${[['#FFFFFF', 'white'], ['#EEF1F4', 'light grey'], ['#1F2329', 'dark grey']].map(([c, n]) => `<div><div class="bg" style="background:${c}"><img src="${svg('exports/svg/zanance-symbol.svg')}" height="150"></div><div class="cap">${n}</div></div>`).join('')}
 <div><div class="bg checker"><img src="${svg('exports/svg/zanance-symbol.svg')}" height="150"></div><div class="cap">transparency checker</div></div></div>

<h2>3. Real sizes (16, 24, 32, 48, 64 px)</h2>
<div class="row px"><div class="card">${[16, 24, 32, 48, 64].map(s => `<img src="${svg('exports/svg/zanance-symbol.svg')}" height="${s}">`).join('')}<div class="cap">SVG drawn by the browser at each height (colour).</div></div>
<div class="card" style="background:#1F2329">${[16, 24, 32, 48, 64].map(s => `<img src="${svg('exports/svg/zanance-symbol.svg')}" height="${s}">`).join('')}<div class="cap" style="color:#A9B0BC">Same on dark grey.</div></div>
<div class="card">${[16, 32, 48].map(s => `<img src="${svg(`exports/ico/zanance-favicon-${s}.png`)}" width="${s}" height="${s}">`).join('')}<div class="cap">Favicon PNG entries of the .ico at their real pixel size.</div></div>
<div class="card">${[16, 24, 32, 48, 64].map(s => `<img src="${svg('exports/svg/zanance-symbol-mono-black.svg')}" height="${s}">`).join('')}<div class="cap">Mono black at real sizes.</div></div></div>

<h2>4. Variants (same geometry)</h2>
<div class="row">
 <div class="card"><img src="${svg('exports/svg/zanance-symbol-flat-blue.svg')}" height="180"><div class="cap">Flat blue: one flat colour per surface, no gradient.</div></div>
 <div class="card"><img src="${svg('exports/svg/zanance-symbol-mono-black.svg')}" height="180"><div class="cap">Mono black: one colour, full opacity; openings transparent.</div></div>
 <div class="card" style="background:#1F2329"><img src="${svg('exports/svg/zanance-symbol-mono-white.svg')}" height="180"><div class="cap" style="color:#A9B0BC">Mono white on dark.</div></div>
</div>

<h2>5. Horizontal lockups – wordmark proposal, needs approval</h2>
<div class="row">
 <div class="card"><img src="${svg('exports/svg/zanance-lockup-horizontal-on-light.svg')}" height="110"><div class="cap">Urbanist 700 (OFL-1.1) as outlines, on light. Primary proposal.</div></div>
 <div class="card" style="background:#0F1115"><img src="${svg('exports/svg/zanance-lockup-horizontal-on-dark.svg')}" height="110"><div class="cap" style="color:#A9B0BC">Same on dark: only the wordmark colour changes.</div></div>
 <div class="card"><img src="${svg('exports/svg/zanance-lockup-horizontal-on-light-alt-outfit.svg')}" height="110"><div class="cap">Alternative: Outfit 600 (OFL-1.1).</div></div>
 <div class="card"><img src="${svg('exports/svg/zanance-lockup-horizontal-on-light.svg')}" height="32"> <img src="${svg('exports/svg/zanance-lockup-horizontal-on-light.svg')}" height="24"> <img src="${svg('exports/svg/zanance-lockup-horizontal-on-light.svg')}" height="18"><div class="cap">Lockup at 32, 24 and 18 px height (small-size test).</div></div>
</div>

<h2>6. Platform candidates (not installed in the app)</h2>
<div class="row">
 ${['light', 'dark'].map(t => `<div class="card">${[['50%', 'circle'], ['30%', 'squircle-like'], ['22%', 'rounded square']].map(([r]) => `<div class="mask" style="border-radius:${r};background:url(../platform-candidates/android/zanance-adaptive-background-${t}-432.png) center/150px"><img src="../platform-candidates/android/zanance-adaptive-foreground-432.png" style="width:150px;margin:-15px"></div>`).join('')}<div class="cap">Android adaptive icon, ${t} background, previewed with circle, squircle-like and rounded-square masks (the launcher shows about the inner 72 of 108 dp).</div></div>`).join('')}
 <div class="card">${['light', 'dark'].map(t => `<img src="../platform-candidates/ios/zanance-appicon-${t}-1024.png" width="120" style="border-radius:27px;margin-right:10px">`).join('')}<div class="cap">iOS 1024 px, opaque, square (the rounded corners here are only a preview; the system applies the mask).</div></div>
 <div class="card"><div class="mask" style="border-radius:50%;background:#E5E7EB"><img src="../platform-candidates/android/zanance-adaptive-monochrome-432.png" style="width:150px;margin:-15px"></div><div class="cap">Android themed (monochrome) layer.</div></div>
</div>

<h2>7. Draft colour roles and measured contrast (WCAG 2.x ratio)</h2>
<div class="cap">Brand colours sampled from the master: ${Object.entries(tokens.brand).filter(([k]) => k !== 'note').map(([k, v]) => `<span class="sw" style="background:${v}"></span>${k} ${v}`).join(' · ')}. Text and status pairs are measured below; the logo itself is exempt from text contrast rules.</div>
<table><tr><th>Theme</th><th>Foreground</th><th>Background</th><th>FG</th><th>BG</th><th>Ratio</th><th>Needed</th><th></th></tr>${tokenRows}</table>

<h2>8. Second renderer</h2>
<div class="cap">resvg and ${rendererComparison.renderers[1]} rendered the same SVG at ${W * side}×${H * side}: mean difference ${rendererComparison.meanAbsDifference}/255, ${(rendererComparison.pixelsOver8Share * 100).toFixed(2)} % of pixels differ by more than 8/255 (anti-aliasing at edges).</div>
<div class="row"><img src="render-resvg.png" width="${W}"><img src="render-chromium.png" width="${W}"></div>
</body></html>`;
writeFileSync(`${ROOT}/previews/review.html`, html);
screenshot(`${ROOT}/previews/review.html`, `${ROOT}/previews/review.png`, 1400, 5200, ['--virtual-time-budget=5000']);

// 4. PDF check page for pdf.js (third renderer). pdf.js needs its worker, which file:// blocks and headless screenshots
//    do not wait for: run `node serve-preview.mjs` and open http://127.0.0.1:8765/previews/pdf-check-http.html.
writeFileSync(`${ROOT}/previews/pdf-check-http.html`, `<!doctype html><meta charset="utf-8"><title>PDF check</title><style>body{margin:16px;font:13px system-ui;background:#fff}canvas{border:1px solid #ccc;margin-right:16px}</style>
<div id="s">rendering…</div><canvas id="a"></canvas><canvas id="b"></canvas>
<script type="module">
import * as pdfjs from '/scripts/node_modules/pdfjs-dist/build/pdf.mjs'; pdfjs.GlobalWorkerOptions.workerSrc = '/scripts/node_modules/pdfjs-dist/build/pdf.worker.mjs';
const out = [];
for (const [file, id] of [['../exports/pdf/zanance-symbol.pdf','a'],['../exports/pdf/zanance-symbol-mono-black.pdf','b']]) {
  const doc = await pdfjs.getDocument(file).promise; const page = await doc.getPage(1); const vp = page.getViewport({ scale: 1.2 });
  const c = document.getElementById(id); c.width = vp.width; c.height = vp.height;
  await page.render({ canvasContext: c.getContext('2d'), viewport: vp }).promise;
  const ops = await page.getOperatorList(); const images = ops.fnArray.filter(f => f === pdfjs.OPS.paintImageXObject || f === pdfjs.OPS.paintInlineImageXObject).length;
  out.push(file.split('/').pop() + ': ' + Math.round(page.view[2]) + ' x ' + Math.round(page.view[3]) + ' pt, image operators: ' + images);
}
document.getElementById('s').textContent = 'pdf.js ' + pdfjs.version + ' | ' + out.join(' | ');
</script>`);

writeFileSync(`${ROOT}/reports/verification.json`, JSON.stringify({ browser: `${EDGE} ${browserVersion}`, rendererComparison, contrastFailures: table.filter(r => !r.pass) }, null, 1) + '\n');
console.log(JSON.stringify(rendererComparison), 'contrast failures:', table.filter(r => !r.pass).length);
