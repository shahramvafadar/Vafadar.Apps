#!/usr/bin/env python3
"""Check a deliberately conservative SVG-export policy; never writes input files.

Python 3.10+, standard library only. This is NOT a renderer, a full SVG validator,
a security certification, or a test of similarity to the approved logo.

Usage:
  python tools/check_svg_structure.py exports/svg/zanance-symbol.svg
  python tools/check_svg_structure.py --flat exports/svg/zanance-symbol-mono-black.svg
  python tools/check_svg_structure.py --json exports/svg/*.svg

Exit codes: 0 = structural policy passed; 1 = failed; 2 = CLI usage error.
"""
from __future__ import annotations

import argparse
import collections
import json
import math
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

SVG_NS = 'http://www.w3.org/2000/svg'
ALLOWED = {
    'svg', 'g', 'defs', 'path', 'rect', 'circle', 'ellipse', 'line',
    'polyline', 'polygon', 'linearGradient', 'radialGradient', 'stop',
    'clipPath', 'title', 'desc', 'use', 'symbol',
}
SHAPES = {'path', 'rect', 'circle', 'ellipse', 'line', 'polyline', 'polygon', 'use'}
MAX_BYTES = 8 * 1024 * 1024


def local_name(name: str) -> str:
    return name.rsplit('}', 1)[-1]


def inspect_svg(path: Path, *, flat: bool = False) -> dict:
    errors: list[str] = []
    warnings: list[str] = []
    result: dict = {'file': str(path), 'flat_requested': flat, 'errors': errors,
                    'warnings': warnings, 'structural_pass': False}
    try:
        if path.stat().st_size > MAX_BYTES:
            raise ValueError('File exceeds the 8 MiB inspection limit.')
        text = path.read_text(encoding='utf-8-sig')
        if '<!doctype' in text.lower() or '<!entity' in text.lower():
            raise ValueError('DOCTYPE and entity declarations are outside this export policy.')
        if re.search(r'<\?(?!xml(?:\s|\?>))', text, flags=re.IGNORECASE):
            errors.append('Non-XML processing instruction (for example external stylesheet).')
        root = ET.fromstring(text)
    except (OSError, UnicodeError, ValueError, ET.ParseError) as exc:
        errors.append(f'Cannot inspect: {exc}')
        return result

    if root.tag != f'{{{SVG_NS}}}svg':
        errors.append('Root must be svg in the SVG namespace.')
    try:
        vb = [float(v) for v in re.split(r'[\s,]+', root.attrib.get('viewBox', '').strip())]
        if len(vb) != 4 or not all(math.isfinite(v) for v in vb) or vb[2] <= 0 or vb[3] <= 0:
            raise ValueError
        result['viewBox'] = vb
    except ValueError:
        errors.append('Missing or invalid viewBox (four finite numbers; positive width/height required).')

    ids: set[str] = set()
    refs: set[str] = set()
    counts: collections.Counter[str] = collections.Counter()
    flat_paints: set[str] = set()
    command_count = 0
    for element in root.iter():
        tag = local_name(element.tag)
        counts[tag] += 1
        if not element.tag.startswith(f'{{{SVG_NS}}}') or tag not in ALLOWED:
            errors.append(f'Element outside portable-export policy: {tag}')
        element_id = element.attrib.get('id')
        if element_id:
            if element_id in ids:
                errors.append(f'Duplicate id: {element_id}')
            ids.add(element_id)
        if tag == 'path':
            command_count += len(re.findall(r'[MmZzLlHhVvCcSsQqTtAa]', element.attrib.get('d', '')))
        if flat and tag in {'linearGradient', 'radialGradient', 'stop'}:
            errors.append('Flat variant contains a gradient definition.')
        for raw_key, raw_value in element.attrib.items():
            key = local_name(raw_key)
            value = raw_value.strip()
            lowered = value.lower()
            if key.lower().startswith('on'):
                errors.append(f'Event handler attribute: {key}')
            if key in {'style', 'class'}:
                errors.append('Use explicit presentation attributes, not style/class, in these exports.')
            if key in {'filter', 'mask'}:
                errors.append(f'{key} is outside this conservative portability policy.')
            if 'data:' in lowered or 'javascript:' in lowered:
                errors.append(f'Embedded data or script URL in {key}.')
            if key == 'href':
                if not value.startswith('#') or len(value) < 2:
                    errors.append('href must be a local #id reference.')
                else:
                    refs.add(value[1:])
            if key == 'base':
                errors.append('External base resolution is not allowed.')
            for url in re.findall(r'url\(\s*[\'\"]?([^\)\'\"]+)[\'\"]?\s*\)', value,
                                  flags=re.IGNORECASE):
                url = url.strip()
                if url.startswith('#') and len(url) > 1:
                    refs.add(url[1:])
                else:
                    errors.append(f'Non-local URL reference in {key}.')
            if flat and key in {'opacity', 'fill-opacity', 'stroke-opacity'}:
                try:
                    numeric = float(value[:-1]) / 100 if value.endswith('%') else float(value)
                    if numeric != 1.0:
                        errors.append('Flat artwork must not contain partial or zero explicit opacity.')
                except ValueError:
                    errors.append(f'Unsupported opacity value: {value}')
            if flat and key in {'fill', 'stroke'} and lowered != 'none':
                if not re.fullmatch(r'#[0-9a-fA-F]{6}', value):
                    errors.append(f'Flat paint must be an explicit six-digit solid hex color: {value}')
                else:
                    flat_paints.add(value.upper())

    missing = sorted(refs - ids)
    if missing:
        errors.append('Unresolved local IDs: ' + ', '.join(missing))
    if not any(counts[tag] for tag in SHAPES):
        errors.append('No supported shape element found.')
    if flat and len(flat_paints) != 1:
        errors.append(f'Flat export must declare exactly one solid paint color, found {len(flat_paints)}.')
    if sum(counts[tag] for tag in SHAPES) > 150:
        warnings.append('High shape count: inspect for unnecessary auto-trace or pixel-shaped geometry.')
    if command_count > 1500:
        warnings.append('High path-command count: inspect for noisy tracing.')
    result['elements'] = dict(sorted(counts.items()))
    result['path_command_count'] = command_count
    result['declared_flat_paints'] = sorted(flat_paints)
    result['errors'] = sorted(set(errors))
    result['structural_pass'] = not errors
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('files', nargs='+', type=Path)
    parser.add_argument('--flat', action='store_true', help='Require a one-color opaque geometry export.')
    parser.add_argument('--json', action='store_true', help='Print machine-readable results.')
    args = parser.parse_args()
    results = [inspect_svg(path, flat=args.flat) for path in args.files]
    if args.json:
        print(json.dumps({'scope': 'structural policy only; visual approval still required',
                          'results': results}, indent=2))
    else:
        for result in results:
            print(('STRUCTURAL PASS' if result['structural_pass'] else 'FAIL') + ': ' + result['file'])
            for error in result['errors']:
                print('  ERROR: ' + error)
            for warning in result['warnings']:
                print('  WARNING: ' + warning)
        print('This check does not assess visual fidelity, rendering correctness, or platform readiness.')
    return 0 if all(result['structural_pass'] for result in results) else 1


if __name__ == '__main__':
    sys.exit(main())
