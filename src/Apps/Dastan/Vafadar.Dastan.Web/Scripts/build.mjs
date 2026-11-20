// Builds the scripts of the Dastan website from their TypeScript (ADR 0019): app.js, the script of every page,
// and graph.js and timeline.js, the drawings of a graph (ADR 0020) and of a timeline (ADR 0021), which only a
// page with such a drawing asks for.
// Each is one file, which the site serves under a name that changes with its content. Started by the build of
// the site, which says where the sources lie and where the files go; without that: the sources beside this
// file, and wwwroot of the site.
import { build } from 'esbuild';
import { fileURLToPath } from 'node:url';
import { dirname, join, resolve } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const sources = resolve(process.argv[2] ?? join(here, 'src'));
const files = resolve(process.argv[3] ?? join(here, '..', 'wwwroot'));

await build({
  entryPoints: { app: join(sources, 'app.ts'), graph: join(sources, 'graph.ts'), timeline: join(sources, 'timeline.ts') },
  outdir: files,
  bundle: true,
  format: 'iife',
  target: ['es2022'],
  minify: true,
  // The same bytes from the same sources: a name that changes with the content changes only when it does.
  legalComments: 'none',
  charset: 'utf8',
  logLevel: 'warning',
});
