// Looks at the page of step 19.2 in real browsers – the ones of the container of src/Apps/Dastan/compose.yaml –
// and keeps what it saw as pictures: node look.mjs [out folder]. DASTAN_BROWSER names the server of the browsers.
//
// Per engine (Chromium, Firefox, WebKit), case (family, relations) and width (360, 1280) it keeps:
//   {engine}-{case}-{width}.png              the page as it opens
//   …-fitted.png                             after the control's fitToPage
//   …-zoomed.png                             after ctrl+wheel and a drag (zoom and offset are in report.json)
//   …-export.svg / …-export-svg.png          the control's own SVG export, and how it looks as a file by itself
//   …-export-png.png                         the control's own PNG export
//   …-export-repaired.svg, …-export-svg-repaired.png, …-export-png-repaired.png   the same after the repair of
//                                            direction and font that the page does itself
// and, at 1280 only, the page with each switch that shows a behaviour of the control without its workaround:
//   …-bare-{switch}.png
// report.json holds the numbers: zoom and offset before and after, what the keyboard did, errors of the page.
import { createServer } from 'node:http';
import { readFileSync, mkdirSync, writeFileSync, existsSync, statSync } from 'node:fs';
import { join, extname } from 'node:path';
import { chromium, firefox, webkit } from 'playwright-core';

const out = process.argv[2] ?? 'seen';
mkdirSync(out, { recursive: true });
const types = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript', '.woff2': 'font/woff2' };
const server = createServer((request, response) => {
    const name = new URL(request.url, 'http://x').pathname.replace(/^\/$/, '/index.html');
    const file = join('dist', name.replace(/\.\./g, ''));
    if (!existsSync(file)) { response.writeHead(404).end(); return; }
    response.writeHead(200, { 'content-type': types[extname(file)] ?? 'application/octet-stream' }).end(readFileSync(file));
});
await new Promise(done => server.listen(0, '127.0.0.1', done));
const address = `http://localhost:${server.address().port}`;
const where = process.env.DASTAN_BROWSER ?? 'ws://127.0.0.1:53333/';
const report = { bundleBytes: statSync('dist/main.js').size };

// The switches of the page that take a workaround away, each with the case it belongs to.
const bare = [
    ['family', 'fix=none'], ['family', 'fix=ltr'], ['family', 'fix=css'], ['family', 'wrap=char'],
    ['family', 'layout=tree'], ['family', 'plain=1'], ['relations', 'same=1'], ['relations', 'curve=1'], ['relations', 'fix=none'],
];

// Opens one address and waits until the diagram stands and the Persian font is loaded.
async function open(browser, width, query, problems) {
    const context = await browser.newContext({ viewport: { width, height: 900 }, locale: 'fa-IR', deviceScaleFactor: 2 });
    const page = await context.newPage();
    page.on('console', message => { if (message.type() === 'error') problems.push(message.text()); });
    page.on('pageerror', error => problems.push(String(error.stack ?? error).slice(0, 400)));
    await page.goto(`${address}/?${query}`);
    try { await page.waitForFunction(() => window.dastanReady === true, null, { timeout: 8000 }); }
    catch { problems.push('the page did not get ready'); }
    await page.evaluate(() => document.fonts.ready);
    await page.waitForTimeout(500);
    return { context, page };
}

const view = page => page.evaluate(() => ({
    zoom: window.dastanDiagram.scrollSettings.currentZoom,
    x: window.dastanDiagram.scrollSettings.horizontalOffset,
    y: window.dastanDiagram.scrollSettings.verticalOffset,
}));
const selected = page => page.evaluate(() => ({
    nodes: window.dastanDiagram.selectedItems.nodes.map(node => node.id),
    connectors: window.dastanDiagram.selectedItems.connectors.map(connector => connector.id),
    focus: document.activeElement?.id || document.activeElement?.tagName,
}));

for (const [engine, type] of Object.entries({ chromium, firefox, webkit })) {
    const browser = await type.connect(where, { exposeNetwork: '<loopback>' });
    report[engine] = {};
    for (const which of ['family', 'relations']) {
        for (const width of [360, 1280]) {
            const problems = [];
            const { context, page } = await open(browser, width, `case=${which}`, problems);
            const key = `${engine}-${which}-${width}`;
            await page.screenshot({ path: join(out, key + '.png') });
            const facts = { problems };
            facts.banner = await page.evaluate(() => document.body.innerText.includes('trial version of Syncfusion'));
            // Whether the control computed the same wrapping as the browser drew: the widest line of any card text
            // against the width of the column it must fit in (158 px).
            facts.widestLine = await page.evaluate(() => Math.round(Math.max(...[...document.querySelectorAll('#diagram text tspan')].map(line => line.getComputedTextLength()))));

            // Keyboard (G): what the control itself does when the diagram has the focus.
            const keyboard = {};
            await page.locator('#diagramcontent').focus();
            keyboard.afterFocus = await selected(page);
            await page.keyboard.press('Tab'); keyboard.afterTab = await selected(page);
            await page.keyboard.press('Tab'); keyboard.afterSecondTab = await selected(page);
            await page.locator('#diagramcontent').focus();
            await page.keyboard.press('ArrowRight'); keyboard.afterArrowRight = await selected(page);
            await page.keyboard.press('ArrowDown'); keyboard.afterArrowDown = await selected(page);
            await page.keyboard.press('Control+a'); keyboard.afterControlA = await selected(page);
            await page.keyboard.press('Escape'); keyboard.afterEscape = await selected(page);
            // A click on the first card, then the arrow keys: does the selection walk, or is the node pushed?
            const first = await page.evaluate(() => { const node = window.dastanDiagram.nodes[0]; const box = document.getElementById(node.id + '-card').getBoundingClientRect(); return { id: node.id, x: box.x + box.width / 2, y: box.y + box.height / 2, offsetX: node.offsetX }; });
            await page.mouse.click(first.x, first.y);
            keyboard.afterClick = await selected(page);
            await page.keyboard.press('ArrowRight');
            keyboard.afterClickThenArrowRight = { ...(await selected(page)), nodeMovedBy: await page.evaluate(([id, was]) => window.dastanDiagram.getObject(id).offsetX - was, [first.id, first.offsetX]) };
            await page.keyboard.press('Tab'); keyboard.afterClickThenTab = await selected(page);
            // Does Tab ever leave the diagram? Twelve more presses – more than there are nodes and connectors.
            const walk = [];
            for (let press = 0; press < 12; press++) { await page.keyboard.press('Tab'); const now = await selected(page); walk.push(now.focus + ':' + [...now.nodes, ...now.connectors].join('+')); }
            keyboard.twelveMoreTabs = walk;
            await page.keyboard.press('Control+z');
            await page.keyboard.press('Escape');
            await page.evaluate(() => window.dastanDiagram.clearSelection());
            facts.keyboard = keyboard;

            // Fitted to the window, then zoomed with the wheel and moved by dragging, as a visitor would.
            await page.evaluate(() => window.dastanDiagram.fitToPage({ mode: 'Page', region: 'Content' }));
            await page.waitForTimeout(300);
            await page.screenshot({ path: join(out, key + '-fitted.png') });
            const box = await page.locator('#diagram').boundingBox();
            // An empty spot of the diagram, so that the drag moves the view and not a card.
            const spot = { x: box.x + box.width - 12, y: box.y + box.height - 12 };
            facts.before = await view(page);
            await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
            await page.keyboard.down('Control'); await page.mouse.wheel(0, -400); await page.keyboard.up('Control');
            await page.waitForTimeout(300);
            facts.afterWheel = await view(page);
            await page.mouse.move(spot.x, spot.y);
            await page.mouse.down(); await page.mouse.move(spot.x - 60, spot.y - 40, { steps: 6 }); await page.mouse.up();
            await page.waitForTimeout(300);
            facts.afterDrag = await view(page);
            await page.screenshot({ path: join(out, key + '-zoomed.png') });

            // What the control exports: its own SVG and its own PNG. Each is shown as a picture file by itself
            // (an <img>), the way it would look once downloaded – without the page's font, direction or styles.
            // Then the same two once more, as the page repaired them (dastanExportRepaired in src/main.ts).
            for (const [format, how] of [['SVG', 'dastanExport'], ['PNG', 'dastanExport'], ['SVG', 'dastanExportRepaired'], ['PNG', 'dastanExportRepaired']]) {
                const repaired = how === 'dastanExportRepaired';
                const name = repaired ? format + '-repaired' : format;
                try {
                    const made = await page.evaluate(([call, wanted]) => window[call](wanted), [how, format]);
                    facts[name] = { length: made.length };
                    if (format === 'SVG') {
                        Object.assign(facts[name], { texts: (made.match(/<text/g) ?? []).length, images: (made.match(/<image/g) ?? []).length, namesFont: made.includes('Vazirmatn'), embedsFont: made.includes('@font-face') });
                        writeFileSync(join(out, key + (repaired ? '-export-repaired.svg' : '-export.svg')), made);
                    }
                    await page.evaluate(async ([kind, data]) => {
                        const place = document.getElementById('exported');
                        place.innerHTML = '';
                        const image = new Image();
                        image.src = kind === 'SVG' ? 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(data) : data;
                        place.append(image);
                        await image.decode().catch(() => undefined);
                    }, [format, made]);
                    await page.waitForTimeout(500);
                    await page.locator('#exported').screenshot({ path: join(out, `${key}-export-${name.toLowerCase()}.png`) });
                } catch (error) { facts[name] = { failed: String(error).slice(0, 300) }; }
            }
            report[engine][`${which}-${width}`] = facts;
            await context.close();
        }
    }
    // The bare behaviours of the control, one picture each.
    for (const [which, change] of bare) {
        const problems = [];
        const { context, page } = await open(browser, 1280, `case=${which}&${change}`, problems);
        const key = `${engine}-${which}-1280-bare-${change.replace(/[^a-z0-9]+/gi, '-')}`;
        await page.locator('#diagram').screenshot({ path: join(out, key + '.png') });
        if (change === 'curve=1') {
            // The bent curves once more after one node was pushed by one pixel with an arrow key.
            const card = await page.locator('#rostam-card').boundingBox();
            await page.mouse.click(card.x + card.width / 2, card.y + card.height / 2);
            await page.keyboard.press('ArrowRight');
            await page.evaluate(() => window.dastanDiagram.clearSelection());
            await page.waitForTimeout(300);
            await page.locator('#diagram').screenshot({ path: join(out, key + '-after-move.png') });
        }
        report[engine][`${which}-bare-${change}`] = { problems };
        await context.close();
    }
    await browser.close();
}
server.close();
writeFileSync(join(out, 'report.json'), JSON.stringify(report, null, 2));
console.log(JSON.stringify(report));
