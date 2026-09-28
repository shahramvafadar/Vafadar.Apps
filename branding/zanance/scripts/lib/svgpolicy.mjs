// A Node port of tools/check_svg_structure.py (same rules and messages), because Python is not available on the build
// machine. Structural policy only: it does not judge visual fidelity or platform readiness.
import { readFileSync, statSync } from 'node:fs';

const SVG_NS = 'http://www.w3.org/2000/svg';
const ALLOWED = new Set(['svg', 'g', 'defs', 'path', 'rect', 'circle', 'ellipse', 'line', 'polyline', 'polygon', 'linearGradient', 'radialGradient', 'stop', 'clipPath', 'title', 'desc', 'use', 'symbol']);
const SHAPES = new Set(['path', 'rect', 'circle', 'ellipse', 'line', 'polyline', 'polygon', 'use']);
const MAX_BYTES = 8 * 1024 * 1024;

export function inspectSvg(path, { flat = false } = {}) {
  const errors = [], warnings = [];
  const result = { file: path, flat_requested: flat, errors, warnings, structural_pass: false };
  let text;
  try {
    if (statSync(path).size > MAX_BYTES) throw new Error('File exceeds the 8 MiB inspection limit.');
    text = readFileSync(path, 'utf8').replace(/^﻿/, '');
    if (/<!doctype|<!entity/i.test(text)) throw new Error('DOCTYPE and entity declarations are outside this export policy.');
  } catch (e) { errors.push(`Cannot inspect: ${e.message}`); return result; }
  if (/<\?(?!xml(?:\s|\?>))/i.test(text)) errors.push('Non-XML processing instruction (for example external stylesheet).');

  const body = text.replace(/<!--[\s\S]*?-->/g, '').replace(/<\?xml[\s\S]*?\?>/, '');
  const tags = [...body.matchAll(/<([A-Za-z][\w:.-]*)((?:\s+[\w:.-]+\s*=\s*(?:"[^"]*"|'[^']*'))*)\s*\/?>/g)];
  if (!tags.length) { errors.push('Cannot inspect: no elements'); return result; }
  const ids = new Set(), refs = new Set(), counts = {}, flatPaints = new Set(); let commandCount = 0;
  tags.forEach((m, index) => {
    const tag = m[1].split(':').pop();
    const attrs = Object.fromEntries([...m[2].matchAll(/([\w:.-]+)\s*=\s*(?:"([^"]*)"|'([^']*)')/g)].map(a => [a[1], a[2] ?? a[3]]));
    counts[tag] = (counts[tag] ?? 0) + 1;
    if (index === 0) {
      if (tag !== 'svg' || attrs.xmlns !== SVG_NS) errors.push('Root must be svg in the SVG namespace.');
      const vb = (attrs.viewBox ?? '').trim().split(/[\s,]+/).map(Number);
      if (vb.length !== 4 || !vb.every(Number.isFinite) || vb[2] <= 0 || vb[3] <= 0) errors.push('Missing or invalid viewBox (four finite numbers; positive width/height required).');
      else result.viewBox = vb;
    }
    if (!ALLOWED.has(tag) || m[1].includes(':')) errors.push(`Element outside portable-export policy: ${tag}`);
    if (attrs.id) { if (ids.has(attrs.id)) errors.push(`Duplicate id: ${attrs.id}`); ids.add(attrs.id); }
    if (tag === 'path') commandCount += (attrs.d ?? '').match(/[MmZzLlHhVvCcSsQqTtAa]/g)?.length ?? 0;
    if (flat && ['linearGradient', 'radialGradient', 'stop'].includes(tag)) errors.push('Flat variant contains a gradient definition.');
    for (const [rawKey, rawValue] of Object.entries(attrs)) {
      const key = rawKey.split(':').pop(); const value = rawValue.trim(); const lowered = value.toLowerCase();
      if (key === 'xmlns') continue;
      if (key.toLowerCase().startsWith('on')) errors.push(`Event handler attribute: ${key}`);
      if (key === 'style' || key === 'class') errors.push('Use explicit presentation attributes, not style/class, in these exports.');
      if (key === 'filter' || key === 'mask') errors.push(`${key} is outside this conservative portability policy.`);
      if (lowered.includes('data:') || lowered.includes('javascript:')) errors.push(`Embedded data or script URL in ${key}.`);
      if (key === 'href') { if (!value.startsWith('#') || value.length < 2) errors.push('href must be a local #id reference.'); else refs.add(value.slice(1)); }
      if (key === 'base') errors.push('External base resolution is not allowed.');
      for (const u of value.matchAll(/url\(\s*['"]?([^)'"]+)['"]?\s*\)/gi)) {
        const url = u[1].trim(); if (url.startsWith('#') && url.length > 1) refs.add(url.slice(1)); else errors.push(`Non-local URL reference in ${key}.`);
      }
      if (flat && ['opacity', 'fill-opacity', 'stroke-opacity'].includes(key)) {
        const numeric = value.endsWith('%') ? parseFloat(value) / 100 : parseFloat(value);
        if (Number.isNaN(numeric)) errors.push(`Unsupported opacity value: ${value}`); else if (numeric !== 1) errors.push('Flat artwork must not contain partial or zero explicit opacity.');
      }
      if (flat && (key === 'fill' || key === 'stroke') && lowered !== 'none') {
        if (!/^#[0-9a-fA-F]{6}$/.test(value)) errors.push(`Flat paint must be an explicit six-digit solid hex color: ${value}`); else flatPaints.add(value.toUpperCase());
      }
    }
  });
  const missing = [...refs].filter(r => !ids.has(r)).sort();
  if (missing.length) errors.push('Unresolved local IDs: ' + missing.join(', '));
  const shapes = Object.entries(counts).filter(([t]) => SHAPES.has(t)).reduce((a, [, c]) => a + c, 0);
  if (!shapes) errors.push('No supported shape element found.');
  if (flat && flatPaints.size !== 1) errors.push(`Flat export must declare exactly one solid paint color, found ${flatPaints.size}.`);
  if (shapes > 150) warnings.push('High shape count: inspect for unnecessary auto-trace or pixel-shaped geometry.');
  if (commandCount > 1500) warnings.push('High path-command count: inspect for noisy tracing.');
  result.elements = counts; result.path_command_count = commandCount; result.declared_flat_paints = [...flatPaints].sort();
  result.errors = [...new Set(errors)].sort(); result.structural_pass = result.errors.length === 0;
  return result;
}
