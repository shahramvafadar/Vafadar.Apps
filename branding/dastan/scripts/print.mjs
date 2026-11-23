// Writes the files of the Dastan logo for print and the guide of the brand, from source/dastan-geometry.json.
//
//   node branding/dastan/scripts/print.mjs          exports/print/*.pdf and *.eps, guide/brand-guide.html
//   node branding/dastan/scripts/print.mjs --pdf    and guide/dastan-brand-guide.pdf, drawn by a browser
//
// A file for print is written by Node alone: the outlines of the geometry as the paths of a PDF and of an EPS,
// filled with the colours of print (CMYK) – no picture of pixels, no font, nothing embedded. The guide is one
// page of HTML that holds everything it shows. Its PDF is that page printed by the browser of the test
// container of Dastan (see build.mjs); nothing is downloaded and nothing is installed.
import { execFileSync } from 'node:child_process';
import { mkdirSync, readFileSync, writeFileSync, copyFileSync, rmSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const geometry = JSON.parse(readFileSync(join(root, 'source', 'dastan-geometry.json'), 'utf8'));
const { indigo, ink, white, ivory } = geometry.colors;
const container = process.env.DASTAN_BROWSER_CONTAINER ?? 'test-browser-1';
const number = value => (Math.round(value * 1000) / 1000).toString();

// --- The colours of print --------------------------------------------------------------------------------------
// The colours of the brand are defined for a screen (sRGB). These are the values a printer is given to begin
// with: chosen so that the indigo stays a clean blue-violet and the ink a deep, slightly blue black, with less
// than 300 % of ink in all. They are not measured: no proof was printed. A proof decides, and then this table
// is changed – nowhere else.
const print = {
  indigo: [0.9, 0.88, 0, 0.05],
  ink: [0.7, 0.65, 0.3, 0.85],
  black: [0, 0, 0, 1],
  white: [0, 0, 0, 0],
};

// The clear space: the diameter of the dot, on every side.
const clear = Math.round(geometry.symbol.dot.r * 2);

// --- The outlines ----------------------------------------------------------------------------------------------

// The path data of the geometry holds M, C, L and Z, absolute. Anything else is refused.
function steps(data) {
  const found = [];
  for (const [, letter, numbers] of data.matchAll(/([A-Za-z])([^A-Za-z]*)/g)) {
    const values = numbers.trim().length === 0 ? [] : numbers.trim().split(/[\s,]+/).map(Number);
    const take = { M: 2, L: 2, C: 6, Z: 0 }[letter];
    if (take === undefined || values.some(Number.isNaN) || (take === 0 ? values.length !== 0 : values.length === 0 || values.length % take !== 0)) {
      throw new Error(`The geometry holds a step of a path this script does not write: ${letter}${numbers.slice(0, 40)}`);
    }

    if (take === 0) {
      found.push({ letter, values: [] });
    }

    for (let at = 0; at < values.length; at += take) {
      // After a first pair, further pairs of a move are lines.
      found.push({ letter: letter === 'M' && at > 0 ? 'L' : letter, values: values.slice(at, at + take) });
    }
  }

  return found;
}

// A circle as four curves.
function circle({ cx, cy, r }) {
  const k = r * 0.5522847498;
  return [
    { letter: 'M', values: [cx + r, cy] },
    { letter: 'C', values: [cx + r, cy + k, cx + k, cy + r, cx, cy + r] },
    { letter: 'C', values: [cx - k, cy + r, cx - r, cy + k, cx - r, cy] },
    { letter: 'C', values: [cx - r, cy - k, cx - k, cy - r, cx, cy - r] },
    { letter: 'C', values: [cx + k, cy - r, cx + r, cy - k, cx + r, cy] },
    { letter: 'Z', values: [] },
  ];
}

const symbolShapes = colour => [
  { colour, steps: steps(geometry.symbol.letter) },
  { colour, steps: steps(geometry.symbol.page) },
  { colour, steps: circle(geometry.symbol.dot) },
];
const wordShapes = colour => [{ colour, steps: steps(geometry.wordmark.outline), evenOdd: true }];

// A drawing: its size without the clear space, its shapes, and – for a logo that stands on a ground – the ground.
const drawings = {
  'dastan-logo-color-cmyk': { title: 'Dastan – logo, colour (CMYK)', width: geometry.lockup.width, height: geometry.lockup.height, shapes: [...symbolShapes(print.indigo), ...wordShapes(print.ink)] },
  'dastan-logo-black': { title: 'Dastan – logo, black', width: geometry.lockup.width, height: geometry.lockup.height, shapes: [...symbolShapes(print.black), ...wordShapes(print.black)] },
  'dastan-logo-reversed-cmyk': { title: 'Dastan – logo, white on indigo (CMYK)', width: geometry.lockup.width, height: geometry.lockup.height, ground: print.indigo, shapes: [...symbolShapes(print.white), ...wordShapes(print.white)] },
  'dastan-symbol-indigo-cmyk': { title: 'Dastan – symbol, indigo (CMYK)', width: geometry.symbol.width, height: geometry.symbol.height, shapes: symbolShapes(print.indigo) },
  'dastan-symbol-black': { title: 'Dastan – symbol, black', width: geometry.symbol.width, height: geometry.symbol.height, shapes: symbolShapes(print.black) },
  'dastan-symbol-reversed-cmyk': { title: 'Dastan – symbol, white on indigo (CMYK)', width: geometry.symbol.width, height: geometry.symbol.height, ground: print.indigo, shapes: symbolShapes(print.white) },
};

// --- PDF -------------------------------------------------------------------------------------------------------
// One page whose box is the drawing with its clear space; a unit of the geometry is a point. The file is plain
// ASCII without compression, so that it can be read and compared; the places of its objects are counted in bytes.
function pdf({ title, width, height, shapes, ground }) {
  const [pageWidth, pageHeight] = [width + clear * 2, height + clear * 2];
  const lines = [];
  if (ground) {
    lines.push(`${ground.map(number).join(' ')} k`, `0 0 ${pageWidth} ${pageHeight} re f`);
  }

  // The geometry counts down from the top, a PDF up from the foot.
  lines.push('q', `1 0 0 -1 ${clear} ${pageHeight - clear} cm`);
  for (const shape of shapes) {
    lines.push(`${shape.colour.map(number).join(' ')} k`);
    for (const { letter, values } of shape.steps) {
      lines.push(letter === 'Z' ? 'h' : `${values.map(number).join(' ')} ${{ M: 'm', L: 'l', C: 'c' }[letter]}`);
    }

    lines.push(shape.evenOdd ? 'f*' : 'f');
  }

  lines.push('Q');
  const content = lines.join('\n');
  const objects = [
    '<< /Type /Catalog /Pages 2 0 R >>',
    '<< /Type /Pages /Kids [3 0 R] /Count 1 >>',
    `<< /Type /Page /Parent 2 0 R /MediaBox [0 0 ${pageWidth} ${pageHeight}] /TrimBox [${clear} ${clear} ${pageWidth - clear} ${pageHeight - clear}] /Contents 4 0 R /Resources << >> >>`,
    `<< /Length ${Buffer.byteLength(content, 'latin1')} >>\nstream\n${content}\nendstream`,
    `<< /Title (${title.replace(/[^\x20-\x7e]/g, '-').replace(/[()\\]/g, '')}) /Creator (branding/dastan/scripts/print.mjs) >>`,
  ];
  let file = '%PDF-1.4\n';
  const places = [];
  objects.forEach((object, index) => {
    places.push(Buffer.byteLength(file, 'latin1'));
    file += `${index + 1} 0 obj\n${object}\nendobj\n`;
  });
  const table = Buffer.byteLength(file, 'latin1');
  file += `xref\n0 ${objects.length + 1}\n0000000000 65535 f \n${places.map(place => `${String(place).padStart(10, '0')} 00000 n \n`).join('')}`;
  file += `trailer\n<< /Size ${objects.length + 1} /Root 1 0 R /Info 5 0 R >>\nstartxref\n${table}\n%%EOF\n`;
  return file;
}

// --- EPS -------------------------------------------------------------------------------------------------------
function eps({ title, width, height, shapes, ground }) {
  const [pageWidth, pageHeight] = [width + clear * 2, height + clear * 2];
  const lines = [
    '%!PS-Adobe-3.0 EPSF-3.0',
    `%%BoundingBox: 0 0 ${pageWidth} ${pageHeight}`,
    `%%HiResBoundingBox: 0 0 ${pageWidth} ${pageHeight}`,
    `%%Title: ${title.replace(/[^\x20-\x7e]/g, '-')}`,
    '%%Creator: branding/dastan/scripts/print.mjs',
    '%%LanguageLevel: 2',
    '%%DocumentProcessColors: Cyan Magenta Yellow Black',
    '%%EndComments',
    'gsave',
  ];
  if (ground) {
    lines.push(`${ground.map(number).join(' ')} setcmykcolor`, `0 0 ${pageWidth} ${pageHeight} rectfill`);
  }

  lines.push(`${clear} ${pageHeight - clear} translate`, '1 -1 scale');
  for (const shape of shapes) {
    lines.push(`${shape.colour.map(number).join(' ')} setcmykcolor`, 'newpath');
    for (const { letter, values } of shape.steps) {
      lines.push(letter === 'Z' ? 'closepath' : `${values.map(number).join(' ')} ${{ M: 'moveto', L: 'lineto', C: 'curveto' }[letter]}`);
    }

    lines.push(shape.evenOdd ? 'eofill' : 'fill');
  }

  lines.push('grestore', '%%EOF');
  return lines.join('\n') + '\n';
}

const write = (path, text) => { mkdirSync(dirname(path), { recursive: true }); writeFileSync(path, text, 'latin1'); };
for (const [name, drawing] of Object.entries(drawings)) {
  write(join(root, 'exports', 'print', `${name}.pdf`), pdf(drawing));
  write(join(root, 'exports', 'print', `${name}.eps`), eps(drawing));
}

// --- The guide -------------------------------------------------------------------------------------------------

const symbol = fill => {
  const { letter, page, dot } = geometry.symbol;
  return `<path fill="${fill}" d="${letter}"/><path fill="${fill}" d="${page}"/><circle fill="${fill}" cx="${dot.cx}" cy="${dot.cy}" r="${dot.r}"/>`;
};
const wordmark = fill => `<path fill="${fill}" fill-rule="evenodd" d="${geometry.wordmark.outline}"/>`;
const lockup = (a, b) => symbol(a) + wordmark(b);
const art = (width, height, body, { pad = 0, label = 'Dastan', extra = '' } = {}) =>
  `<svg xmlns="http://www.w3.org/2000/svg" viewBox="${-pad} ${-pad} ${width + pad * 2} ${height + pad * 2}" role="img" aria-label="${label}"${extra}>${body}</svg>`;
const logo = (a, b, options) => art(geometry.lockup.width, geometry.lockup.height, lockup(a, b), options);
const mark = (fill, options) => art(geometry.symbol.width, geometry.symbol.height, symbol(fill), options);

// How well two colours of a screen stand apart (WCAG 2): the ratio of their luminances.
const luminance = hex => {
  const [r, g, b] = [1, 3, 5].map(at => parseInt(hex.slice(at, at + 2), 16) / 255).map(part => (part <= 0.04045 ? part / 12.92 : ((part + 0.055) / 1.055) ** 2.4));
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
};
const contrast = (one, other) => {
  const [light, dark] = [luminance(one), luminance(other)].sort((a, b) => b - a);
  return ((light + 0.05) / (dark + 0.05)).toFixed(1);
};
const rgb = hex => [1, 3, 5].map(at => parseInt(hex.slice(at, at + 2), 16)).join(', ');
const cmyk = values => values.map(value => Math.round(value * 100)).join(' / ');

// The clear space, drawn: the logo, the box that stays free around it, and the dot that measures it.
const { width: W, height: H } = geometry.lockup;
const clearDrawing = art(W, H,
  `<rect x="${-clear}" y="${-clear}" width="${W + clear * 2}" height="${H + clear * 2}" fill="none" stroke="#B4443A" stroke-width="3" stroke-dasharray="14 10"/>`
  + `<rect x="0" y="0" width="${W}" height="${H}" fill="none" stroke="#B4443A" stroke-width="1.500" opacity=".5"/>`
  + lockup(indigo, ink)
  + [[-clear / 2, H / 2], [W + clear / 2, H / 2], [W / 2, -clear / 2], [W / 2, H + clear / 2]]
    .map(([x, y]) => `<circle cx="${number(x)}" cy="${number(y)}" r="${geometry.symbol.dot.r}" fill="#B4443A" opacity=".35"/>`).join(''),
  { pad: clear + 20, label: 'The clear space around the logo: the diameter of its dot on every side' });

// What is not done with the logo, each drawn and struck through.
const wrong = (label, body, viewBox = `-40 -40 ${W + 80} ${H + 80}`) =>
  `<figure class="wrong"><svg xmlns="http://www.w3.org/2000/svg" viewBox="${viewBox}" role="img" aria-label="Not like this: ${label}">${body}`
  + `<line x1="-30" y1="${H + 30}" x2="${W + 30}" y2="-30" stroke="#B4443A" stroke-width="10" stroke-linecap="round"/></svg><figcaption>${label}</figcaption></figure>`;
const misuse = [
  wrong('stretched or squeezed', `<g transform="translate(0 ${H * 0.2}) scale(1 .6)">${lockup(indigo, ink)}</g>`),
  wrong('turned', `<g transform="rotate(-12 ${W / 2} ${H / 2})">${lockup(indigo, ink)}</g>`),
  wrong('in another colour', lockup('#C2410C', '#0F766E')),
  wrong('as an outline', `<g fill="none" stroke="${indigo}" stroke-width="5">${lockup('none', 'none').replaceAll('fill="none"', '')}</g>`),
  wrong('with a shadow or another effect', `<g opacity=".3" transform="translate(14 14)">${lockup('#000000', '#000000')}</g>${lockup(indigo, ink)}`),
  wrong('on a ground it does not stand out from', `<rect x="-40" y="-40" width="${W + 80}" height="${H + 80}" fill="#4A4999"/>${lockup(indigo, ink)}`),
  wrong('on a picture or a pattern', `<defs><pattern id="busy" width="60" height="60" patternUnits="userSpaceOnUse"><rect width="60" height="60" fill="#E7D9B8"/><circle cx="18" cy="18" r="16" fill="#9C6B3F"/><rect x="34" y="30" width="22" height="26" fill="#5B7F6B"/></pattern></defs><rect x="-40" y="-40" width="${W + 80}" height="${H + 80}" fill="url(#busy)"/>${lockup(indigo, ink)}`),
  wrong('with the symbol and the wordmark set anew', `<g transform="translate(${W - geometry.symbol.width} 0)">${symbol(indigo)}</g><g transform="translate(${-geometry.wordmark.box[0]} 0)">${wordmark(ink)}</g>`),
  wrong('the wordmark without the symbol', wordmark(ink)),
  wrong('with a part taken away or added', `<path fill="${indigo}" d="${geometry.symbol.letter}"/>${wordmark(ink)}`),
];

const colours = [
  { name: 'Indigo', hex: indigo, print: print.indigo, use: 'The symbol. The ground of the app icon and of a reversed logo. The colour of what can be pressed on the site.' },
  { name: 'Ink', hex: ink, print: print.ink, use: 'The wordmark on a light ground. Text.' },
  { name: 'White', hex: white, print: print.white, use: 'The logo on indigo and on a dark ground.' },
  { name: 'Ivory', hex: ivory, print: null, use: 'The light ground of the site. A ground, never part of a logo; on paper it is the paper.' },
];

const guide = `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Dastan – the guide of the brand</title>
<style>
  :root { --indigo: ${indigo}; --ink: ${ink}; --ivory: ${ivory}; --line: #D9D6E3; --quiet: #56556B; --wrong: #B4443A; }
  * { box-sizing: border-box; }
  body { margin: 0; background: var(--ivory); color: var(--ink); font: 16px/1.55 "Segoe UI Variable", "Segoe UI", system-ui, -apple-system, "Helvetica Neue", Arial, sans-serif; }
  main { max-width: 62rem; margin: 0 auto; padding: 2rem 1rem 4rem; }
  h1 { font-size: 2rem; line-height: 1.2; margin: 1.5rem 0 .5rem; }
  h2 { font-size: 1.35rem; margin: 3rem 0 .75rem; padding-top: 1.25rem; border-top: 1px solid var(--line); break-after: avoid; }
  h3 { font-size: 1.05rem; margin: 1.5rem 0 .5rem; break-after: avoid; }
  p, li { max-width: 46rem; }
  .lead { font-size: 1.125rem; color: var(--quiet); }
  .note { color: var(--quiet); font-size: .9375rem; }
  svg { display: block; width: 100%; height: auto; }
  .cover { padding: 3rem 1.5rem; background: #fff; border: 1px solid var(--line); border-radius: .75rem; }
  .cover svg { max-width: 34rem; margin: 0 auto; }
  .plates { display: grid; grid-template-columns: repeat(auto-fit, minmax(15rem, 1fr)); gap: 1rem; margin: 1rem 0; }
  figure { margin: 0; break-inside: avoid; }
  figcaption { margin-top: .5rem; font-size: .875rem; color: var(--quiet); }
  .plate { padding: 1.75rem; border: 1px solid var(--line); border-radius: .5rem; background: #fff; }
  .plate.ivory { background: var(--ivory); }
  .plate.indigo { background: var(--indigo); border-color: var(--indigo); }
  .plate.ink { background: var(--ink); border-color: var(--ink); }
  .plate.small svg { max-width: 7rem; margin: 0 auto; }
  .wrong svg { padding: .5rem; border: 1px solid var(--line); border-radius: .5rem; background: #fff; }
  .wrong figcaption::before { content: "Not like this: "; color: var(--wrong); font-weight: 600; }
  .swatches { display: grid; grid-template-columns: repeat(auto-fit, minmax(13rem, 1fr)); gap: 1rem; margin: 1rem 0; }
  .swatch { border: 1px solid var(--line); border-radius: .5rem; overflow: hidden; background: #fff; break-inside: avoid; }
  .swatch .chip { height: 5.5rem; border-bottom: 1px solid var(--line); }
  .swatch dl { margin: 0; padding: .75rem 1rem 1rem; display: grid; grid-template-columns: auto 1fr; gap: .15rem .75rem; font-size: .875rem; }
  .swatch dt { color: var(--quiet); }
  .swatch dd { margin: 0; font-variant-numeric: tabular-nums; }
  .swatch h3 { margin: .75rem 1rem 0; }
  .swatch p { margin: 0; padding: 0 1rem 1rem; font-size: .875rem; color: var(--quiet); }
  .scroll { overflow-x: auto; }
  table { border-collapse: collapse; width: 100%; font-size: .9375rem; }
  th, td { text-align: start; padding: .5rem .75rem; border-bottom: 1px solid var(--line); vertical-align: top; }
  th { font-weight: 600; white-space: nowrap; }
  code { font: .875em ui-monospace, "Cascadia Mono", Consolas, monospace; overflow-wrap: anywhere; }
  .sizes { display: flex; flex-wrap: wrap; align-items: end; gap: 1.5rem; padding: 1.5rem; background: #fff; border: 1px solid var(--line); border-radius: .5rem; }
  .sizes figure svg { width: auto; }
  .fa { font-family: Vazirmatn, "Segoe UI", Tahoma, sans-serif; }
  @media print { body { background: #fff; } main { max-width: none; padding: 0; } h2 { margin-top: 2rem; } @page { size: A4; margin: 16mm; } }
</style>
</head>
<body>
<main>
  <div class="cover">${logo(indigo, ink, { pad: clear })}</div>
  <h1>Dastan – the guide of the brand</h1>
  <p class="lead">How the logo, the colours and the name of Dastan are used – on the site, in an app, on paper and beside the marks of others. It is made from the geometry of the logo by a script; where the two disagree, the geometry is right and this page is made again.</p>
  <p class="note">Owner of the brand: Shahram Vafadar. All rights reserved. The logo was chosen on 2026-11-21 (decision D-082) and this guide asked for on 2026-11-23 (decision D-087). The mark is not registered.</p>

  <h2>1. The logo</h2>
  <p>The logo is a <strong>symbol</strong> and a <strong>wordmark</strong>. The symbol is an open capital D with a dot in its lower left opening and a thin page inside its bowl: a letter, a point to begin at, and something to read. The wordmark is the name “Dastan”. It is artwork – outlines, not a font – and is never set in type.</p>
  <p>Both are flat shapes in one colour each. There is no gradient, no shadow, no outline and no ground that belongs to the logo.</p>
  <div class="plates">
    <figure><div class="plate">${logo(indigo, ink)}</div><figcaption>The logo: the symbol and the wordmark, side by side. This is the form to use wherever there is room.</figcaption></figure>
    <figure><div class="plate small">${mark(indigo)}</div><figcaption>The symbol alone: where there is no room for the wordmark, and in every square – an icon, an avatar, a stamp.</figcaption></figure>
  </div>
  <p>The wordmark never stands without the symbol. A square shows the symbol alone – never the whole logo made small.</p>

  <h2>2. The versions and when to use each</h2>
  <div class="plates">
    <figure><div class="plate ivory">${logo(indigo, ink)}</div><figcaption><strong>Colour</strong> – indigo and ink. On white, on ivory and on any light, calm ground. The first choice.</figcaption></figure>
    <figure><div class="plate indigo">${logo(white, white)}</div><figcaption><strong>Reversed</strong> – white on indigo. Where the ground is the colour of the brand.</figcaption></figure>
    <figure><div class="plate ink">${logo(white, white)}</div><figcaption><strong>White</strong> – on ink and on any dark, calm ground. The dark appearance of the site.</figcaption></figure>
    <figure><div class="plate">${logo('#000000', '#000000')}</div><figcaption><strong>Black</strong> – where only one colour can be printed: a stamp, a fax, an engraving, a newspaper.</figcaption></figure>
  </div>
  <p>There are three colourings and no others: indigo with ink, all white, all black. The symbol alone has the same three: indigo, white, black.</p>

  <h2>3. Clear space</h2>
  <p>Around the logo, on every side, at least the <strong>diameter of its dot</strong> stays free – ${clear} units of the ${geometry.symbol.height} the symbol is high, a sixth of its height. Nothing stands inside it: no text, no edge of a page, no other mark, no edge of a picture. More is better.</p>
  <figure><div class="plate">${clearDrawing}</div><figcaption>The dashed line is the nearest anything else comes. The files for print are cut to exactly this box.</figcaption></figure>
  <p>The symbol alone has the same clear space, measured by the same dot.</p>

  <h2>4. The smallest sizes</h2>
  <div class="scroll"><table>
    <thead><tr><th>What</th><th>On a screen</th><th>On paper</th><th>Below that</th></tr></thead>
    <tbody>
      <tr><td>The logo</td><td>24 pixels high (84 wide)</td><td>8 mm high (28 mm wide)</td><td>use the symbol alone</td></tr>
      <tr><td>The symbol</td><td>24 pixels high</td><td>6 mm high</td><td>on a screen: the small-size variant</td></tr>
      <tr><td>The small-size variant of the symbol</td><td>16 to 32 pixels</td><td>not for print</td><td>nothing: 16 pixels is the least</td></tr>
    </tbody>
  </table></div>
  <p>At 16 pixels the page of the symbol would be thinner than a third of a pixel. For 16 and 32 pixels only, a variant draws the page a little heavier and the dot a little larger; it is the icon of a browser tab and nothing else. The sizes for paper are where the page of the symbol is still a line a press can hold; they were not tried on a press – ask for a proof below 10 mm.</p>
  <div class="sizes">
    ${[96, 48, 32, 24].map(size => `<figure><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${W} ${H}" height="${size}" role="img" aria-label="The logo, ${size} pixels high">${lockup(indigo, ink)}</svg><figcaption>${size} px</figcaption></figure>`).join('')}
    ${[48, 24].map(size => `<figure><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${geometry.symbol.width} ${geometry.symbol.height}" height="${size}" role="img" aria-label="The symbol, ${size} pixels high">${symbol(indigo)}</svg><figcaption>${size} px</figcaption></figure>`).join('')}
  </div>

  <h2>5. Colours</h2>
  <div class="swatches">
    ${colours.map(colour => `<div class="swatch"><div class="chip" style="background:${colour.hex}"></div><h3>${colour.name}</h3><dl><dt>Hex</dt><dd><code>${colour.hex}</code></dd><dt>RGB</dt><dd>${rgb(colour.hex)}</dd><dt>CMYK</dt><dd>${colour.print ? cmyk(colour.print) : '– (the paper)'}</dd></dl><p>${colour.use}</p></div>`).join('')}
  </div>
  <p>The colours are defined for a screen, in sRGB; the hex value is the colour. <strong>The CMYK values are where a printer begins, not a measurement</strong>: no proof has been printed and no spot colour (Pantone or another) has been chosen. Ask the printer for a proof, compare it with a calibrated screen showing the hex value, and have the table in <code>scripts/print.mjs</code> changed to what the proof needs. The files for print carry these values as plain CMYK without an embedded profile.</p>
  <h3>What stands on what</h3>
  <div class="scroll"><table>
    <thead><tr><th>Logo</th><th>Ground</th><th>Contrast</th><th></th></tr></thead>
    <tbody>
      <tr><td>Indigo and ink</td><td>White</td><td>${contrast(indigo, white)} : 1 and ${contrast(ink, white)} : 1</td><td>yes</td></tr>
      <tr><td>Indigo and ink</td><td>Ivory</td><td>${contrast(indigo, ivory)} : 1 and ${contrast(ink, ivory)} : 1</td><td>yes</td></tr>
      <tr><td>White</td><td>Indigo</td><td>${contrast(white, indigo)} : 1</td><td>yes</td></tr>
      <tr><td>White</td><td>Ink</td><td>${contrast(white, ink)} : 1</td><td>yes</td></tr>
      <tr><td>Indigo and ink</td><td>Ink, or any dark ground</td><td>${contrast(indigo, ink)} : 1</td><td>no – use white</td></tr>
      <tr><td>Any</td><td>A photograph, a pattern, a gradient</td><td>–</td><td>no – put the logo on a calm field of white, ivory, indigo or ink</td></tr>
    </tbody>
  </table></div>
  <p>On a ground of another colour, use the white logo where the ground is dark and the black one where it is light, and only where each stands apart from the ground at 4.5 : 1 or more.</p>

  <h2>6. Type</h2>
  <p>The wordmark is not type and no font is named for it. Beside the logo, Dastan sets its texts as the site does:</p>
  <ul>
    <li><strong>Latin script</strong>: the font of the reader's system – Segoe UI Variable, San Francisco, Roboto – on a screen. No Latin font is part of the brand yet; for print, a quiet humanist sans-serif of the same kind is used until one is chosen.</li>
    <li><strong>Persian and Arabic script</strong>: Vazirmatn, without Latin digits (the “NL” cut), under the SIL Open Font License. It comes with the site.</li>
  </ul>
  <p>Never set the name in a font to stand in for the wordmark, and never place a line of text so close to the logo that it reads as part of it.</p>

  <h2>7. The name</h2>
  <div class="scroll"><table>
    <thead><tr><th>Write</th><th>Not</th><th>Why</th></tr></thead>
    <tbody>
      <tr><td>Dastan</td><td>DASTAN, dastan, DaStan</td><td>One capital, as a name.</td></tr>
      <tr><td><span class="fa" lang="fa" dir="rtl">داستان</span></td><td>a transliteration back into Latin letters in a Persian sentence</td><td>In Persian text the name is the Persian word.</td></tr>
      <tr><td>Dastan Studio</td><td>Dastan-Studio, DastanStudio</td><td>The tool of the editors. It shows the symbol and its name in words, never the wordmark.</td></tr>
      <tr><td>the Dastan logo</td><td>“the D”, “the Dastan icon” for the logo</td><td>The icon is the square with the symbol; the logo is the symbol and the wordmark.</td></tr>
    </tbody>
  </table></div>
  <p>“Dastan” means “story”. The name is not translated and not explained beside the logo.</p>

  <h2>8. Right to left</h2>
  <p>The logo is the same on a page written from right to left: the D and the wordmark are Latin, and a drawing is not mirrored. It moves to where a page begins – the right-hand corner – and keeps its own order, the symbol before the wordmark.</p>

  <h2>9. Icons</h2>
  <div class="plates">
    <figure><div class="plate small">${art(512, 512, `<rect width="512" height="512" rx="115" fill="${indigo}"/><g transform="translate(${number((512 - geometry.symbol.width * (512 * 0.58 / geometry.symbol.height)) / 2)} ${number(512 * 0.21)}) scale(${number(512 * 0.58 / geometry.symbol.height)})">${symbol(white)}</g>`, { label: 'The app icon' })}</div><figcaption><strong>The app icon</strong>: the white symbol on an indigo square with round corners. The symbol is 58 % of the height of the square.</figcaption></figure>
    <figure><div class="plate small">${art(512, 512, `<rect width="512" height="512" fill="${indigo}"/><circle cx="256" cy="256" r="${number(512 * 0.4)}" fill="none" stroke="#fff" stroke-width="3" stroke-dasharray="10 8" opacity=".6"/><g transform="translate(${number((512 - geometry.symbol.width * 0.86) / 2)} ${number((512 - geometry.symbol.height * 0.86) / 2)}) scale(.86)">${symbol(white)}</g>`, { label: 'The maskable icon and the circle that is always kept' })}</div><figcaption><strong>Where a platform cuts the shape itself</strong>: a whole square, with the whole symbol inside the circle that is always kept.</figcaption></figure>
  </div>
  <p>An icon is made by the script, not by hand: its sizes and its grounds are checked when it is made. Do not put the wordmark into a square, and do not draw the symbol in another colour to make an icon “fit” a theme.</p>

  <h2>10. What is not done</h2>
  <div class="plates">${misuse.join('')}</div>
  <p>And never: redraw the logo; trace it from a picture; cut it out of a screenshot; animate its parts apart from each other; use it as a letter in a word; put it into the logo of somebody else.</p>

  <h2>11. Beside the marks of others</h2>
  <ul>
    <li>Each mark keeps its own clear space; between Dastan and another mark at least twice the diameter of the dot stays free.</li>
    <li>The marks stand on one line, at the same optical height, with a thin upright line or nothing between them. Dastan is not made part of another mark and no other mark is put into the D.</li>
    <li>Who shows the logo to say that something is by, with or about Dastan uses the files of this folder unchanged, and asks the owner first.</li>
  </ul>

  <h2>12. The files</h2>
  <div class="scroll"><table>
    <thead><tr><th>For</th><th>Take</th><th>Colours</th></tr></thead>
    <tbody>
      <tr><td>A site, an app, a document on a screen</td><td><code>exports/svg/dastan-logo-{color,white,black}.svg</code>, <code>dastan-symbol-{indigo,white,black}.svg</code></td><td>sRGB</td></tr>
      <tr><td>Where an SVG cannot be used</td><td><code>exports/png/</code> – the logo 512, 1024 and 2048 pixels wide, the symbol 256, 512 and 1024; no ground</td><td>sRGB</td></tr>
      <tr><td>A printer, in colour</td><td><code>exports/print/dastan-logo-color-cmyk.pdf</code> (or <code>.eps</code>), <code>dastan-symbol-indigo-cmyk</code></td><td>CMYK ${cmyk(print.indigo)} and ${cmyk(print.ink)}</td></tr>
      <tr><td>A printer, on the colour of the brand</td><td><code>exports/print/dastan-logo-reversed-cmyk</code>, <code>dastan-symbol-reversed-cmyk</code> – the indigo field is part of the file and is as large as the clear space</td><td>paper white on ${cmyk(print.indigo)}</td></tr>
      <tr><td>A printer, one colour</td><td><code>exports/print/dastan-logo-black</code>, <code>dastan-symbol-black</code></td><td>black 100</td></tr>
      <tr><td>Icons</td><td><code>exports/icons/</code>, <code>exports/ico/favicon.ico</code></td><td>sRGB</td></tr>
    </tbody>
  </table></div>
  <p>A file for print is outlines only – no picture of pixels and no font – and can be made as large as a wall. Its page is the logo with its clear space; its trim box is the logo itself. To make every file again: <code>node branding/dastan/scripts/build.mjs --raster</code> and <code>node branding/dastan/scripts/print.mjs --pdf</code>.</p>

  <h2>13. What this guide does not settle</h2>
  <ul>
    <li>The colours on paper: the CMYK values are untried. No spot colour is chosen.</li>
    <li>A Latin font of the brand, and with it stationery, a business card, a presentation.</li>
    <li>A voice for texts: the site has its own rules for how it speaks, in its documents.</li>
    <li>The registration of the mark, and what others may do with it without asking.</li>
  </ul>
</main>
</body>
</html>
`;
mkdirSync(join(root, 'guide'), { recursive: true });
writeFileSync(join(root, 'guide', 'brand-guide.html'), guide);

// --- The guide as a PDF ----------------------------------------------------------------------------------------
if (process.argv.includes('--pdf')) {
  const work = join(root, '.work');
  rmSync(work, { recursive: true, force: true });
  mkdirSync(work, { recursive: true });
  const docker = (...args) => execFileSync('docker', args, { stdio: ['ignore', 'pipe', 'inherit'] }).toString();
  docker('exec', container, 'rm', '-rf', '/tmp/dastan-guide');
  docker('exec', container, 'mkdir', '-p', '/tmp/dastan-guide');
  docker('cp', join(root, 'guide', 'brand-guide.html'), `${container}:/tmp/dastan-guide/brand-guide.html`);
  docker('cp', join(root, 'scripts', 'guide-pdf.cjs'), `${container}:/tmp/dastan-guide/guide-pdf.cjs`);
  console.log(docker('exec', container, 'node', '/tmp/dastan-guide/guide-pdf.cjs', '/tmp/dastan-guide/brand-guide.html', '/tmp/dastan-guide/dastan-brand-guide.pdf').trim());
  docker('cp', `${container}:/tmp/dastan-guide/dastan-brand-guide.pdf`, join(work, 'dastan-brand-guide.pdf'));
  docker('exec', container, 'rm', '-rf', '/tmp/dastan-guide');
  copyFileSync(join(work, 'dastan-brand-guide.pdf'), join(root, 'guide', 'dastan-brand-guide.pdf'));
  rmSync(work, { recursive: true, force: true });
}

console.log(`Written: ${Object.keys(drawings).length} PDF and ${Object.keys(drawings).length} EPS files for print, and the guide of the brand.`);
