// Builds the page of step 19.2 into dist/: the script, the page and the Persian font of the site.
import { build } from 'esbuild';
import { copyFileSync, mkdirSync } from 'node:fs';

mkdirSync('dist', { recursive: true });
await build({ entryPoints: ['src/main.ts'], bundle: true, format: 'iife', target: 'es2020', outfile: 'dist/main.js', minify: process.env.DASTAN_PLAIN !== '1', legalComments: 'none', logLevel: 'info' });
copyFileSync('index.html', 'dist/index.html');
copyFileSync('../../../Vafadar.Dastan.Web/wwwroot/fonts/vazirmatn-nl-wght.woff2', 'dist/vazirmatn-nl-wght.woff2');
