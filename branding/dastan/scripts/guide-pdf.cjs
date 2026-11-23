// Runs in the browser container of the tests: prints the guide of the brand, one page of HTML, as a PDF.
// Called by print.mjs; nothing here knows the logo.
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
  const [page, out] = process.argv.slice(2);
  const browser = await playwright().chromium.launch();
  const tab = await browser.newPage();
  await tab.setContent(fs.readFileSync(page, 'utf8'), { waitUntil: 'load' });
  await tab.pdf({ path: out, format: 'A4', printBackground: true, margin: { top: '16mm', bottom: '16mm', left: '16mm', right: '16mm' } });
  const pages = (fs.readFileSync(out, 'latin1').match(/\/Type\s*\/Page[^s]/g) || []).length;
  await browser.close();
  console.log(`Printed: the guide, ${pages} pages.`);
})();
