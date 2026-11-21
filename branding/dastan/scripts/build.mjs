// Builds every asset of the Dastan logo from its one source, source/dastan-geometry.json.
//
//   node branding/dastan/scripts/build.mjs            the SVG files, the generated code of the site
//   node branding/dastan/scripts/build.mjs --raster   and the PNG and ICO files, the icons of the site, the sheet
//
// The SVG files are written by Node alone. A PNG is a drawing of an SVG by a browser: the script uses the
// browser of the test container of Dastan (src/Apps/Dastan/compose.yaml, "docker compose -p test up -d browser"),
// named by DASTAN_BROWSER_CONTAINER (default test-browser-1). Nothing is downloaded and nothing is installed.
import { execFileSync } from 'node:child_process';
import { mkdirSync, readFileSync, writeFileSync, copyFileSync, rmSync, existsSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const repository = resolve(root, '..', '..');
const site = join(repository, 'src', 'Apps', 'Dastan', 'Vafadar.Dastan.Web');
const geometry = JSON.parse(readFileSync(join(root, 'source', 'dastan-geometry.json'), 'utf8'));
const { indigo, ink, white } = geometry.colors;
const black = '#000000';
const raster = process.argv.includes('--raster');
const container = process.env.DASTAN_BROWSER_CONTAINER ?? 'test-browser-1';

const write = (path, text) => { mkdirSync(dirname(path), { recursive: true }); writeFileSync(path, text.replace(/\r?\n/g, '\n')); };
const number = value => (Math.round(value * 1000) / 1000).toString();

// --- The drawings ---------------------------------------------------------------------------------------------

// The symbol: the open D, the dot in its opening, the page inside it. `small` is the symbol for 16 to 24 pixels:
// the same three shapes, the page and the dot made heavier so that they do not vanish (see the README).
function symbol(fill, { small = false } = {}) {
  const { letter, page, dot } = geometry.symbol;
  const heavier = small ? ` stroke="${fill}" stroke-width="${geometry.small.pageStroke}" stroke-linejoin="round"` : '';
  const radius = small ? dot.r + geometry.small.dotGrowth : dot.r;
  return `<path fill="${fill}" d="${letter}"/><path fill="${fill}"${heavier} d="${page}"/><circle fill="${fill}" cx="${dot.cx}" cy="${dot.cy}" r="${number(radius)}"/>`;
}

const wordmark = fill => `<path fill="${fill}" fill-rule="evenodd" d="${geometry.wordmark.outline}"/>`;

const svg = (width, height, title, body) =>
  `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${width} ${height}" role="img" aria-label="${title}"><title>${title}</title>${body}</svg>\n`;

const symbolSvg = fill => svg(geometry.symbol.width, geometry.symbol.height, 'Dastan', symbol(fill));
const lockupSvg = (symbolFill, wordFill) => svg(geometry.lockup.width, geometry.lockup.height, 'Dastan', symbol(symbolFill) + wordmark(wordFill));

// The symbol alone in a square: how high it stands in the square, and whether the square has corners of its own.
function iconSvg({ size = 512, share, corner = 0, small = false, ground = indigo, fill = white }) {
  const scale = (size * share) / geometry.symbol.height;
  const [x, y] = [(size - geometry.symbol.width * scale) / 2, (size - geometry.symbol.height * scale) / 2];
  return svg(size, size, 'Dastan',
    `<rect width="${size}" height="${size}"${corner > 0 ? ` rx="${number(size * corner)}"` : ''} fill="${ground}"/>`
    + `<g transform="translate(${number(x)} ${number(y)}) scale(${number(scale)})">${symbol(fill, { small })}</g>`);
}

// A maskable icon is cut to a shape by the platform: the whole symbol – with its dot – stays inside the circle
// whose radius is 40 % of the square. The box of the symbol is inside that circle when half its diagonal is.
const halfDiagonal = Math.hypot(geometry.symbol.width, geometry.symbol.height) / 2;
const maskableShare = (0.4 * 0.94 * geometry.symbol.height) / halfDiagonal;
const icons = {
  app: { share: 0.58, corner: 0.225 },
  maskable: { share: maskableShare },
  touch: { share: 0.58 },
  small: { share: 0.7, corner: 0.19, small: true },
};

const files = {
  // The masters.
  'source/dastan-symbol.svg': symbolSvg(indigo),
  'source/dastan-logo.svg': lockupSvg(indigo, ink),
  // The variants.
  'exports/svg/dastan-logo-color.svg': lockupSvg(indigo, ink),
  'exports/svg/dastan-logo-white.svg': lockupSvg(white, white),
  'exports/svg/dastan-logo-black.svg': lockupSvg(black, black),
  'exports/svg/dastan-symbol-indigo.svg': symbolSvg(indigo),
  'exports/svg/dastan-symbol-white.svg': symbolSvg(white),
  'exports/svg/dastan-symbol-black.svg': symbolSvg(black),
  'exports/svg/dastan-app-icon.svg': iconSvg(icons.app),
  'exports/svg/dastan-app-icon-maskable.svg': iconSvg(icons.maskable),
  'exports/svg/dastan-app-icon-small.svg': iconSvg({ ...icons.small, size: 64 }),
  'exports/svg/favicon.svg': iconSvg(icons.app),
};

for (const [name, text] of Object.entries(files)) {
  // What an asset must never hold: a picture of pixels, a script, anything fetched from elsewhere.
  if (/<image|<script|href=|url\(|<text|font-family/i.test(text)) {
    throw new Error(`${name} holds something an asset of the logo must not hold.`);
  }

  write(join(root, name), text);
}

// --- What the site draws itself -------------------------------------------------------------------------------

write(join(site, 'Site', 'BrandGeometry.cs'), `// <auto-generated>
// Written by branding/dastan/scripts/build.mjs from branding/dastan/source/dastan-geometry.json. Do not edit:
// change the geometry and run the script.
// </auto-generated>
namespace Vafadar.Dastan.Web.Site;

/// <summary>
/// The geometry of the Dastan logo, for what the site draws itself: the logo in the head of every page and the
/// picture of a shared link. The symbol begins at 0,0; the wordmark stands beside it in the same units.
/// </summary>
public static class BrandGeometry
{
    /// <summary>The indigo of the symbol.</summary>
    public const string Indigo = "${indigo}";

    /// <summary>The ink of the wordmark.</summary>
    public const string Ink = "${ink}";

    /// <summary>The width of the symbol.</summary>
    public const int SymbolWidth = ${geometry.symbol.width};

    /// <summary>The height of the symbol, and of the symbol with the wordmark.</summary>
    public const int Height = ${geometry.symbol.height};

    /// <summary>The width of the symbol with the wordmark.</summary>
    public const int LockupWidth = ${geometry.lockup.width};

    /// <summary>The open D of the symbol.</summary>
    public const string Letter = "${geometry.symbol.letter}";

    /// <summary>The page inside the D.</summary>
    public const string Page = "${geometry.symbol.page}";

    /// <summary>The centre of the dot, across.</summary>
    public const float DotX = ${geometry.symbol.dot.cx}f;

    /// <summary>The centre of the dot, down.</summary>
    public const float DotY = ${geometry.symbol.dot.cy}f;

    /// <summary>The radius of the dot.</summary>
    public const float DotRadius = ${geometry.symbol.dot.r}f;

    /// <summary>The outlines of the wordmark "Dastan"; inner outlines are holes (even-odd).</summary>
    public const string Wordmark = "${geometry.wordmark.outline}";
}
`);

console.log(`Written: ${Object.keys(files).length} SVG files and the geometry of the site.`);
if (!raster) {
  console.log('PNG, ICO, the icons of the site and the sheet: run again with --raster.');
  process.exit(0);
}

// --- Pixels ---------------------------------------------------------------------------------------------------

const jobs = [];
const draw = (out, text, width, height, opaque = false) => jobs.push({ out, svg: text, width, height, opaque });
const lockupHeight = width => Math.round((width * geometry.lockup.height) / geometry.lockup.width);
const symbolHeight = width => Math.round((width * geometry.symbol.height) / geometry.symbol.width);
for (const [name, a, b] of [['color', indigo, ink], ['white', white, white], ['black', black, black]]) {
  for (const width of [512, 1024, 2048]) {
    draw(`exports/png/dastan-logo-${name}-${width}.png`, lockupSvg(a, b), width, lockupHeight(width));
  }
}

for (const [name, fill] of [['indigo', indigo], ['white', white], ['black', black]]) {
  for (const width of [256, 512, 1024]) {
    draw(`exports/png/dastan-symbol-${name}-${width}.png`, symbolSvg(fill), width, symbolHeight(width));
  }
}

draw('exports/icons/icon-192.png', iconSvg(icons.app), 192, 192);
draw('exports/icons/icon-512.png', iconSvg(icons.app), 512, 512);
draw('exports/icons/icon-maskable-192.png', iconSvg(icons.maskable), 192, 192, true);
draw('exports/icons/icon-maskable-512.png', iconSvg(icons.maskable), 512, 512, true);
draw('exports/icons/apple-touch-icon.png', iconSvg(icons.touch), 180, 180, true);
draw('exports/icons/favicon-48.png', iconSvg(icons.app), 48, 48);
draw('exports/icons/favicon-32.png', iconSvg({ ...icons.small, size: 64 }), 32, 32);
draw('exports/icons/favicon-16.png', iconSvg({ ...icons.small, size: 64 }), 16, 16);

const sheet = readFileSync(join(root, 'scripts', 'sheet.html'), 'utf8')
  .replace('{{reference}}', readFileSync(join(root, 'references', 'dastan-logo-approved.webp')).toString('base64'))
  .replace(/\{\{([a-z-]+)(?::([a-z]+))?\}\}/g, (_, name, how) => {
    const text = ({
      'logo-color': lockupSvg(indigo, ink), 'logo-white': lockupSvg(white, white), 'logo-black': lockupSvg(black, black),
      'symbol-indigo': symbolSvg(indigo), 'symbol-white': symbolSvg(white), 'symbol-black': symbolSvg(black),
      'app-icon': iconSvg(icons.app), 'maskable': iconSvg(icons.maskable), 'small': iconSvg({ ...icons.small, size: 64 }),
      'symbol-small': svg(geometry.symbol.width, geometry.symbol.height, 'Dastan', symbol(indigo, { small: true })),
    })[name];
    if (text === undefined) {
      throw new Error(`The sheet asks for ${name}, which there is not.`);
    }

    return how === 'data' ? `data:image/svg+xml;base64,${Buffer.from(text).toString('base64')}` : text.replace('<svg ', '<svg class="art" ');
  });

const work = join(root, '.work');
rmSync(work, { recursive: true, force: true });
mkdirSync(work, { recursive: true });
writeFileSync(join(work, 'jobs.json'), JSON.stringify({ jobs, sheet, sheetOut: 'previews/comparison.png' }));
const docker = (...args) => execFileSync('docker', args, { stdio: ['ignore', 'pipe', 'inherit'] }).toString();
docker('exec', container, 'rm', '-rf', '/tmp/dastan-logo');
docker('exec', container, 'mkdir', '-p', '/tmp/dastan-logo');
docker('cp', join(work, 'jobs.json'), `${container}:/tmp/dastan-logo/jobs.json`);
docker('cp', join(root, 'scripts', 'render.cjs'), `${container}:/tmp/dastan-logo/render.cjs`);
console.log(docker('exec', container, 'node', '/tmp/dastan-logo/render.cjs', '/tmp/dastan-logo/jobs.json', '/tmp/dastan-logo/out').trim());
docker('cp', `${container}:/tmp/dastan-logo/out/.`, work);
docker('exec', container, 'rm', '-rf', '/tmp/dastan-logo');
for (const job of [...jobs, { out: 'previews/comparison.png' }, { out: 'previews/report.json' }]) {
  mkdirSync(dirname(join(root, job.out)), { recursive: true });
  copyFileSync(join(work, job.out), join(root, job.out));
}

// favicon.ico: three pictures of 16, 32 and 48 pixels, each kept as the PNG it is.
const pictures = [16, 32, 48].map(size => ({ size, data: readFileSync(join(root, 'exports', 'icons', `favicon-${size}.png`)) }));
const head = Buffer.alloc(6 + pictures.length * 16);
head.writeUInt16LE(0, 0);
head.writeUInt16LE(1, 2);
head.writeUInt16LE(pictures.length, 4);
let offset = head.length;
pictures.forEach((picture, index) => {
  const at = 6 + index * 16;
  head.writeUInt8(picture.size, at);
  head.writeUInt8(picture.size, at + 1);
  head.writeUInt16LE(1, at + 4);
  head.writeUInt16LE(32, at + 6);
  head.writeUInt32LE(picture.data.length, at + 8);
  head.writeUInt32LE(offset, at + 12);
  offset += picture.data.length;
});
mkdirSync(join(root, 'exports', 'ico'), { recursive: true });
writeFileSync(join(root, 'exports', 'ico', 'favicon.ico'), Buffer.concat([head, ...pictures.map(picture => picture.data)]));
rmSync(work, { recursive: true, force: true });

// --- The site -------------------------------------------------------------------------------------------------

const place = (from, to) => { mkdirSync(dirname(join(site, 'wwwroot', to)), { recursive: true }); copyFileSync(join(root, from), join(site, 'wwwroot', to)); };
place('exports/svg/favicon.svg', 'favicon.svg');
place('exports/ico/favicon.ico', 'favicon.ico');
for (const name of ['icon-192.png', 'icon-512.png', 'icon-maskable-192.png', 'icon-maskable-512.png', 'apple-touch-icon.png']) {
  place(`exports/icons/${name}`, `icons/${name}`);
}

for (const earlier of ['icon.svg', 'icons/icon-maskable.svg']) {
  if (existsSync(join(site, 'wwwroot', earlier))) {
    rmSync(join(site, 'wwwroot', earlier));
  }
}

// What the pixels say: sizes, and grounds that are whole where they must be and open where they must be.
const report = JSON.parse(readFileSync(join(root, 'previews', 'report.json'), 'utf8'));
for (const entry of report.files) {
  const job = jobs.find(one => one.out === entry.out);
  if (entry.width !== job.width || entry.height !== job.height) {
    throw new Error(`${entry.out} is ${entry.width}×${entry.height}, not ${job.width}×${job.height}.`);
  }

  if (job.opaque !== (entry.leastAlpha === 255)) {
    throw new Error(`${entry.out}: the ground is ${entry.leastAlpha === 255 ? 'whole' : 'open'} and should not be.`);
  }
}

console.log(`Written: ${jobs.length} PNG files, favicon.ico, the icons of the site and previews/comparison.png.`);
