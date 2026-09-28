// The same cases as tools/test_check_svg_structure.py, run against the Node port (lib/svgpolicy.mjs).
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { inspectSvg } from '../lib/svgpolicy.mjs';

const START = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">';
function check(body, flat = false, raw = null) {
  const path = join(mkdtempSync(join(tmpdir(), 'svgpolicy-')), 'fixture.svg');
  writeFileSync(path, raw ?? START + body + '</svg>', 'utf8');
  return inspectSvg(path, { flat });
}

test('solid shape', () => assert.ok(check('<path d="M0 0L10 0L5 10Z" fill="#000000"/>', true).structural_pass));
test('vector gradient passes, but not as flat', () => {
  const body = '<defs><linearGradient id="a"><stop offset="0" stop-color="#000000"/><stop offset="1" stop-color="#ffffff"/></linearGradient></defs><path d="M0 0L10 0L5 10Z" fill="url(#a)"/>';
  assert.ok(check(body).structural_pass); assert.ok(!check(body, true).structural_pass);
});
test('wrapped bitmap', () => assert.ok(!check('<image href="data:image/png;base64,AA=="/>').structural_pass));
test('external use', () => assert.ok(!check('<use href="https://example.invalid/icon.svg#x"/>').structural_pass));
test('missing reference', () => assert.ok(!check('<path d="M0 0L10 0L5 10Z" fill="url(#missing)"/>').structural_pass));
test('active content', () => assert.ok(!check('<path d="M0 0L10 0L5 10Z" onclick="alert(1)"/>').structural_pass));
test('two colours are not flat', () => assert.ok(!check('<rect width="10" height="10" fill="#000000"/><circle cx="5" cy="5" r="2" fill="#ffffff"/>', true).structural_pass));
test('font-dependent text', () => assert.ok(!check('<text x="0" y="20">Zanance</text>').structural_pass));
test('partial alpha is not flat', () => assert.ok(!check('<path d="M0 0L10 0L5 10Z" fill="#000000" opacity="0.5"/>', true).structural_pass));
test('doctype rejected', () => assert.ok(!check('', false, '<!DOCTYPE svg>' + START + '</svg>').structural_pass));
