// Opens addresses of the built page in Chromium of the container and says what each page said:
// node try.mjs "?case=family&plain=1" "?case=relations"
import { createServer } from 'node:http';
import { readFileSync, existsSync } from 'node:fs';
import { join, extname } from 'node:path';
import { chromium } from 'playwright-core';

const types = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript', '.woff2': 'font/woff2' };
const server = createServer((request, response) => {
    const name = new URL(request.url, 'http://x').pathname.replace(/^\/$/, '/index.html');
    const file = join('dist', name.replace(/\.\./g, ''));
    if (!existsSync(file)) { response.writeHead(404).end(); return; }
    response.writeHead(200, { 'content-type': types[extname(file)] ?? 'application/octet-stream' }).end(readFileSync(file));
});
await new Promise(done => server.listen(0, '127.0.0.1', done));
const browser = await chromium.connect(process.env.DASTAN_BROWSER ?? 'ws://127.0.0.1:53333/', { exposeNetwork: '<loopback>' });
for (const query of process.argv.slice(2)) {
    const page = await (await browser.newContext({ viewport: { width: 1280, height: 900 } })).newPage();
    const said = [];
    page.on('pageerror', error => said.push(String(error.stack ?? error).split('\n').slice(0, 4).join(' | ')));
    await page.goto(`http://localhost:${server.address().port}/${query}`);
    await page.waitForTimeout(1500);
    if (process.env.DASTAN_SEEN) await page.screenshot({ path: process.env.DASTAN_SEEN + '/try-' + query.replace(/[^a-z0-9]+/gi, '-') + '.png' });
    console.log(query, JSON.stringify({ ready: await page.evaluate(() => window.dastanReady === true), said }));
}
await browser.close();
server.close();
