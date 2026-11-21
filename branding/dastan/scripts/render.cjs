// Runs in the browser container of the tests: draws each SVG at its size in pixels and writes a PNG, then
// the comparison sheet. Called by build.mjs; nothing here knows the logo.
const fs = require('fs');
const path = require('path');

// The container runs Playwright's server through npx; its module is where npx keeps it.
function playwright() {
  try {
    return require('playwright');
  } catch {
    const kept = '/home/pwuser/.npm/_npx';
    for (const folder of fs.existsSync(kept) ? fs.readdirSync(kept) : []) {
      const candidate = path.join(kept, folder, 'node_modules', 'playwright');
      if (fs.existsSync(candidate)) {
        return require(candidate);
      }
    }

    throw new Error('Playwright was not found in the container.');
  }
}

(async () => {
  const [jobsFile, out] = process.argv.slice(2);
  const { jobs, sheet, sheetOut } = JSON.parse(fs.readFileSync(jobsFile, 'utf8'));
  const browser = await playwright().chromium.launch();
  const page = await browser.newPage({ deviceScaleFactor: 1 });
  const files = [];
  for (const job of jobs) {
    await page.setViewportSize({ width: job.width, height: job.height });
    const drawing = job.svg.replace('<svg ', `<svg width="${job.width}" height="${job.height}" style="display:block" `);
    await page.setContent(`<body style="margin:0;background:transparent">${drawing}</body>`);
    const target = path.join(out, job.out);
    fs.mkdirSync(path.dirname(target), { recursive: true });
    const png = await page.screenshot({ path: target, omitBackground: true, clip: { x: 0, y: 0, width: job.width, height: job.height } });

    // What was written, read back: its size and the least alpha of its pixels.
    const seen = await page.evaluate(async encoded => {
      const image = new Image();
      image.src = 'data:image/png;base64,' + encoded;
      await image.decode();
      const canvas = document.createElement('canvas');
      canvas.width = image.naturalWidth;
      canvas.height = image.naturalHeight;
      const context = canvas.getContext('2d');
      context.drawImage(image, 0, 0);
      const pixels = context.getImageData(0, 0, canvas.width, canvas.height).data;
      let least = 255;
      for (let index = 3; index < pixels.length; index += 4) {
        least = Math.min(least, pixels[index]);
      }

      return { width: image.naturalWidth, height: image.naturalHeight, leastAlpha: least };
    }, png.toString('base64'));
    files.push({ out: job.out, ...seen });
  }

  // The sheet is drawn at one pixel for a pixel, so that an icon of 16 pixels is 16 pixels on it.
  await page.setViewportSize({ width: 1500, height: 900 });
  await page.setContent(sheet);
  await page.evaluate(() => Promise.all([...document.images].map(image => image.decode().catch(() => null))));
  const measured = await page.evaluate(() => window.measure ? window.measure() : null);
  fs.mkdirSync(path.dirname(path.join(out, sheetOut)), { recursive: true });
  await page.screenshot({ path: path.join(out, sheetOut), fullPage: true });
  fs.writeFileSync(path.join(out, 'previews', 'report.json'), JSON.stringify({ files, reference: measured }, null, 2) + '\n');
  await browser.close();
  console.log(`Drawn: ${files.length} pictures and the sheet.`);
})();
