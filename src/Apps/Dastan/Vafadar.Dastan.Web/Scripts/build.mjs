// Builds the script of the Dastan website from its TypeScript (ADR 0019): one file, which the site serves
// under a name that changes with its content. Started by the build of the site, which says where the sources
// lie and where the file goes; without that: the sources beside this file, and wwwroot/app.js of the site.
import { build } from 'esbuild';
import { fileURLToPath } from 'node:url';
import { dirname, join, resolve } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const sources = resolve(process.argv[2] ?? join(here, 'src'));
const file = resolve(process.argv[3] ?? join(here, '..', 'wwwroot', 'app.js'));

await build({
  entryPoints: [join(sources, 'app.ts')],
  outfile: file,
  bundle: true,
  format: 'iife',
  target: ['es2022'],
  minify: true,
  // The same bytes from the same sources: a name that changes with the content changes only when it does.
  legalComments: 'none',
  charset: 'utf8',
  logLevel: 'warning',
});
