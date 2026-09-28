// Checks of the built candidates (run `node build.mjs` first): structure of every SVG, raster alpha and margins, lossless
// WebP, vector-only PDF/EPS, ICO entries, and unchanged references. These are technical checks, not visual approval.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, readdirSync, existsSync } from 'node:fs';
import { createHash } from 'node:crypto';
import sharp from 'sharp';
import { inspectSvg } from '../lib/svgpolicy.mjs';

const ROOT = '..';
const manifest = JSON.parse(readFileSync(`${ROOT}/reports/manifest.json`, 'utf8'));
const files = dir => readdirSync(`${ROOT}/${dir}`).map(f => `${dir}/${f}`);
const sha = p => createHash('sha256').update(readFileSync(`${ROOT}/${p}`)).digest('hex');

test('references are unchanged (SOURCE_AUDIT.json hashes)', () => {
  const audit = JSON.parse(readFileSync(`${ROOT}/SOURCE_AUDIT.json`, 'utf8'));
  for (const f of audit.files) assert.equal(sha(f.path), f.sha256, f.path);
});

test('every output in the manifest exists with the recorded hash and is marked candidate', () => {
  for (const o of manifest.outputs) {
    assert.ok(existsSync(`${ROOT}/${o.path}`), o.path);
    assert.equal(sha(o.path), o.sha256, o.path);
    assert.match(o.status, /candidate/, o.path);
  }
});

const distributable = [...files('exports/svg'), 'source/zanance-symbol-master.svg', ...files('platform-candidates/android').filter(f => f.endsWith('.svg')), ...files('platform-candidates/maui')];

test('SVG exports pass the structural policy (port of tools/check_svg_structure.py)', () => {
  for (const f of distributable) {
    const r = inspectSvg(`${ROOT}/${f}`, { flat: /mono/.test(f) });
    assert.deepEqual(r.errors, [], `${f}: ${r.errors.join('; ')}`);
    assert.deepEqual(r.warnings, [], `${f}: ${r.warnings.join('; ')}`);
  }
});

test('SVG exports contain no raster, script, foreign content, font dependency or external reference', () => {
  for (const f of distributable) {
    const t = readFileSync(`${ROOT}/${f}`, 'utf8');
    assert.doesNotMatch(t, /<image|<feImage|data:image|foreignObject|<script|<style|<filter|<mask|<text|font-family|https?:\/\/(?!www\.w3\.org\/2000\/svg)/i, f);
  }
});

test('mono exports are one opaque colour; flat blue has no gradient; the master has real gradients', () => {
  const mono = readFileSync(`${ROOT}/exports/svg/zanance-symbol-mono-black.svg`, 'utf8');
  assert.deepEqual([...new Set(mono.match(/fill="[^"]+"/g))], ['fill="#000000"']);
  assert.deepEqual([...new Set(readFileSync(`${ROOT}/exports/svg/zanance-symbol-mono-white.svg`, 'utf8').match(/fill="[^"]+"/g))], ['fill="#FFFFFF"']);
  assert.doesNotMatch(readFileSync(`${ROOT}/exports/svg/zanance-symbol-flat-blue.svg`, 'utf8'), /Gradient|stop-opacity/);
  const master = readFileSync(`${ROOT}/source/zanance-symbol-master.svg`, 'utf8');
  assert.ok((master.match(/<linearGradient/g) ?? []).length >= 5);
  const r = inspectSvg(`${ROOT}/source/zanance-symbol-master.svg`);
  assert.ok(r.path_command_count < 600, `too many path commands: ${r.path_command_count}`);
});

test('transparent PNGs: alpha channel, transparent corners, centred symbol with the documented margin', async () => {
  for (const size of [64, 256, 1024, 4096]) {
    const p = `${ROOT}/exports/png/zanance-symbol-${size}.png`;
    const { data, info } = await sharp(p).raw().toBuffer({ resolveWithObject: true });
    assert.equal(info.channels, 4); assert.equal(info.width, size); assert.equal(info.height, size);
    for (const [x, y] of [[0, 0], [size - 1, 0], [0, size - 1], [size - 1, size - 1]]) assert.equal(data[(y * size + x) * 4 + 3], 0, `corner ${x},${y}`);
    let minX = size, minY = size, maxX = -1, maxY = -1;
    for (let y = 0; y < size; y++) for (let x = 0; x < size; x++) if (data[(y * size + x) * 4 + 3] > 8) { minX = Math.min(minX, x); maxX = Math.max(maxX, x); minY = Math.min(minY, y); maxY = Math.max(maxY, y); }
    const side = Math.max(maxX - minX + 1, maxY - minY + 1) / size;
    assert.ok(Math.abs(side - manifest.pngLayout.symbolFillOfCanvas) < 0.02 + 2 / size, `symbol side ${side}`);
    assert.ok(Math.abs((minX + maxX) / 2 - (size - 1) / 2) < 1 + size * 0.005, 'centred horizontally');
    assert.ok(Math.abs((minY + maxY) / 2 - (size - 1) / 2) < 1 + size * 0.005, 'centred vertically');
  }
});

test('WebP files are lossless: decoded pixels equal the PNG of the same size', async () => {
  for (const size of [256, 512, 1024, 2048]) {
    const a = await sharp(`${ROOT}/exports/webp/zanance-symbol-${size}.webp`).raw().toBuffer();
    const b = await sharp(`${ROOT}/exports/png/zanance-symbol-${size}.png`).raw().toBuffer();
    // Lossless WebP keeps RGB of fully transparent pixels arbitrary; compare premultiplied values.
    let diff = 0; for (let i = 0; i < a.length; i += 4) for (let c = 0; c < 4; c++) diff = Math.max(diff, Math.abs((a[i + c] * (c === 3 ? 255 : a[i + 3])) - (b[i + c] * (c === 3 ? 255 : b[i + 3]))) / 255);
    assert.ok(diff <= 1, `max difference ${diff}`);
  }
});

test('PDFs are vector only, sized to the artwork, with shading only in the colour version', () => {
  for (const f of ['exports/pdf/zanance-symbol.pdf', 'exports/pdf/zanance-symbol-mono-black.pdf']) {
    const t = readFileSync(`${ROOT}/${f}`).toString('latin1');
    assert.doesNotMatch(t, /\/Subtype\s*\/Image|\/DCTDecode|\/JPXDecode|\/CCITTFax|\/Font/, f);
    const box = /\/MediaBox \[([^\]]+)\]/.exec(t)[1].trim().split(/\s+/).map(Number);
    const expected = manifest.outputs.find(o => o.path === f).page;
    assert.deepEqual(box.slice(2).map(Math.round), expected.map(Math.round), f);
    if (f.endsWith('mono-black.pdf')) assert.doesNotMatch(t, /\/Shading/); else assert.match(t, /\/ShadingType 2/);
  }
});

test('EPS files are vector only with a bounding box', () => {
  for (const f of files('exports/eps')) {
    const t = readFileSync(`${ROOT}/${f}`, 'latin1');
    assert.match(t, /^%!PS-Adobe-3\.0 EPSF-3\.0/); assert.match(t, /%%BoundingBox: 0 0 \d+ \d+/);
    assert.doesNotMatch(t, /\bimage\b|colorimage|imagemask/);
    assert.ok((t.match(/curveto/g) ?? []).length > 20);
  }
});

test('ICO contains 16, 32, 48 and 256 px PNG entries', () => {
  const b = readFileSync(`${ROOT}/exports/ico/zanance-favicon.ico`);
  assert.equal(b.readUInt16LE(2), 1); const n = b.readUInt16LE(4); assert.equal(n, 4);
  const sizes = [];
  for (let i = 0; i < n; i++) {
    const e = 6 + i * 16; sizes.push(b[e] || 256);
    const off = b.readUInt32LE(e + 12); assert.equal(b.subarray(off, off + 8).toString('hex'), '89504e470d0a1a0a');
  }
  assert.deepEqual(sizes, [16, 32, 48, 256]);
});

test('Android foreground keeps the symbol inside the 66 dp safe circle of the 108 dp canvas', async () => {
  const { data, info } = await sharp(`${ROOT}/platform-candidates/android/zanance-adaptive-foreground-432.png`).raw().toBuffer({ resolveWithObject: true });
  const c = (info.width - 1) / 2; let r = 0;
  for (let y = 0; y < info.height; y++) for (let x = 0; x < info.width; x++) if (data[(y * info.width + x) * 4 + 3] > 8) r = Math.max(r, Math.hypot(x - c, y - c));
  assert.ok(r <= (33 / 108) * info.width + 1.5, `farthest opaque pixel ${r.toFixed(1)} px`);
});
