// Output formats that are written directly (no conversion tool): wordmark outlines, EPS, ICO and vector PDF.
import { readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import opentype from 'opentype.js';
import PDFDocument from 'pdfkit';
import SVGtoPDF from 'svg-to-pdfkit';
import { createHash } from 'node:crypto';

const FIXED_DATE = new Date('2026-09-28T00:00:00Z');

const require = createRequire(import.meta.url);

/** Wordmark proposals (OFL fonts from @fontsource, used only to create outlines; no font file is shipped). */
export const WORDMARK_FONTS = {
  urbanist: { family: 'Urbanist', weight: 700, file: '@fontsource/urbanist/files/urbanist-latin-700-normal.woff', package: '@fontsource/urbanist' },
  outfit: { family: 'Outfit', weight: 600, file: '@fontsource/outfit/files/outfit-latin-600-normal.woff', package: '@fontsource/outfit' },
};

/** Returns the outline of `text` as an absolute SVG path, with its advance box (y grows downwards, baseline at 0). */
export function wordmarkPath(fontKey, text, size) {
  const spec = WORDMARK_FONTS[fontKey];
  const path = require.resolve(spec.file);
  const buffer = readFileSync(path);
  const font = opentype.parse(buffer.buffer.slice(buffer.byteOffset, buffer.byteOffset + buffer.byteLength));
  const p = font.getPath(text, 0, 0, size, { kerning: true });
  const box = p.getBoundingBox();
  const version = JSON.parse(readFileSync(require.resolve(`${spec.package}/package.json`), 'utf8')).version;
  return {
    d: p.toPathData(2), box,
    capHeight: (font.tables.os2.sCapHeight / font.unitsPerEm) * size,
    xHeight: (font.tables.os2.sxHeight / font.unitsPerEm) * size,
    fontName: font.names.fullName.en, fontVersion: font.names.version.en, packageVersion: version, license: 'OFL-1.1',
  };
}

/** Parses an absolute path with M, L, C, Q, Z into subpaths of cubic segments (Q is raised to C). */
export function parsePath(d) {
  const tokens = d.match(/[MLCQZ]|-?\d*\.?\d+(?:e-?\d+)?/gi);
  const out = []; let cur = null, pen = [0, 0], cmd = null; let i = 0;
  const num = () => parseFloat(tokens[i++]);
  while (i < tokens.length) {
    if (/^[MLCQZ]$/i.test(tokens[i])) cmd = tokens[i++].toUpperCase();
    if (cmd === 'M') { cur = { start: [num(), num()], segs: [] }; out.push(cur); pen = cur.start; cmd = 'L'; }
    else if (cmd === 'L') { const p = [num(), num()]; cur.segs.push([pen, pen, p, p]); pen = p; }
    else if (cmd === 'C') { const c1 = [num(), num()], c2 = [num(), num()], p = [num(), num()]; cur.segs.push([pen, c1, c2, p]); pen = p; }
    else if (cmd === 'Q') {
      const q = [num(), num()], p = [num(), num()];
      const c1 = [pen[0] + (2 / 3) * (q[0] - pen[0]), pen[1] + (2 / 3) * (q[1] - pen[1])];
      const c2 = [p[0] + (2 / 3) * (q[0] - p[0]), p[1] + (2 / 3) * (q[1] - p[1])];
      cur.segs.push([pen, c1, c2, p]); pen = p;
    } else if (cmd === 'Z') { cur.closed = true; pen = cur.start; cmd = null; }
    else i++;
  }
  return out;
}

const n = v => (Math.round(v * 1000) / 1000).toString();

/**
 * Writes an EPS (PostScript level 2, vector only) of flat-painted paths: `items` = [{ d, rgb:[r,g,b] 0–255 }], in
 * artwork units with y down; the page is the artwork box plus `margin`. `clip` (optional path) clips all items.
 */
export function eps({ width, height, margin, items, clip, title }) {
  const W = width + 2 * margin, H = height + 2 * margin;
  const pathPs = d => parsePath(d).map(sp => `${n(sp.start[0])} ${n(sp.start[1])} moveto\n`
    + sp.segs.map(s => `${n(s[1][0])} ${n(s[1][1])} ${n(s[2][0])} ${n(s[2][1])} ${n(s[3][0])} ${n(s[3][1])} curveto`).join('\n')
    + (sp.closed ? '\nclosepath' : '')).join('\n');
  const body = items.map(it => `newpath\n${pathPs(it.d)}\n${it.rgb.map(c => n(c / 255)).join(' ')} setrgbcolor eofill`).join('\n');
  return `%!PS-Adobe-3.0 EPSF-3.0
%%Title: ${title}
%%Creator: branding/zanance/scripts/build.mjs
%%BoundingBox: 0 0 ${Math.ceil(W)} ${Math.ceil(H)}
%%HiResBoundingBox: 0 0 ${n(W)} ${n(H)}
%%LanguageLevel: 2
%%Pages: 1
%%EndComments
save
${n(margin)} ${n(H - margin)} translate 1 -1 scale
${clip ? `newpath\n${pathPs(clip)}\neoclip\n` : ''}${body}
restore
showpage
%%EOF
`;
}

/** Packs PNG images (each a square, 1–256 px) into a Windows .ico with PNG-compressed entries. */
export function ico(pngs) {
  const header = Buffer.alloc(6); header.writeUInt16LE(0, 0); header.writeUInt16LE(1, 2); header.writeUInt16LE(pngs.length, 4);
  const entries = []; let offset = 6 + 16 * pngs.length;
  for (const { size, data } of pngs) {
    const e = Buffer.alloc(16);
    e.writeUInt8(size >= 256 ? 0 : size, 0); e.writeUInt8(size >= 256 ? 0 : size, 1);
    e.writeUInt8(0, 2); e.writeUInt8(0, 3); e.writeUInt16LE(1, 4); e.writeUInt16LE(32, 6);
    e.writeUInt32LE(data.length, 8); e.writeUInt32LE(offset, 12);
    entries.push(e); offset += data.length;
  }
  return Buffer.concat([header, ...entries, ...pngs.map(p => p.data)]);
}

/** Renders an SVG into a one-page vector PDF of the SVG's own size (points = SVG units) plus `margin`. */
export function pdf(svg, width, height, margin, title) {
  return new Promise(resolve => {
    const doc = new PDFDocument({ size: [width + 2 * margin, height + 2 * margin], margin: 0, compress: false, info: { Title: title, Creator: 'branding/zanance/scripts/build.mjs', Producer: 'pdfkit', CreationDate: FIXED_DATE, ModDate: FIXED_DATE } });
    const chunks = []; doc.on('data', c => chunks.push(c));
    // Reproducible output: fixed dates, and the file ID derived from the content instead of a random value.
    doc.on('end', () => {
      let text = Buffer.concat(chunks).toString('latin1');
      const id = createHash('md5').update(text.replace(/\/ID \[[^\]]*\]/, '')).digest('hex');
      text = text.replace(/\/ID \[[^\]]*\]/, `/ID [<${id}> <${id}>]`);
      resolve(Buffer.from(text, 'latin1'));
    });
    SVGtoPDF(doc, svg, margin, margin, { width, height, assumePt: true });
    doc.end();
  });
}
