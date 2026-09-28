// Serves branding/zanance on http://127.0.0.1:8765 (read-only, localhost only, stops after 10 minutes), so that
// previews/pdf-check-http.html can run pdf.js (module + worker) to draw the PDFs:
//   node serve-preview.mjs   then open http://127.0.0.1:8765/previews/pdf-check-http.html
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { resolve, extname, sep } from 'node:path';
const root = resolve('..'); const types = { '.html': 'text/html', '.mjs': 'text/javascript', '.js': 'text/javascript', '.pdf': 'application/pdf', '.png': 'image/png', '.svg': 'image/svg+xml' };
const server = createServer(async (req, res) => {
  const path = resolve(root, '.' + decodeURIComponent(new URL(req.url, 'http://x').pathname));
  if (!path.startsWith(root + sep)) { res.writeHead(403).end(); return; }
  let body; try { body = await readFile(path); } catch { res.writeHead(404).end(); return; }
  res.writeHead(200, { 'content-type': types[extname(path)] ?? 'application/octet-stream' }).end(body);
}).listen(8765, '127.0.0.1');
setTimeout(() => server.close(), 600000);


