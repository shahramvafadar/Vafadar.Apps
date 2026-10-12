#!/usr/bin/env python3
"""Verify the reviewed container build tools without changing the host or application data."""
import hashlib, json, pathlib, subprocess, sys
lock = json.loads(pathlib.Path(sys.argv[1]).read_text(encoding='utf-8-sig'))
if lock['schemaVersion'] != 1:
    raise SystemExit('Unsupported Android input schema')
ndk = pathlib.Path('/opt/android-ndk-r30')
if lock['ndk']['revision'] not in (ndk/'source.properties').read_text():
    raise SystemExit('NDK revision differs from review')
if pathlib.Path('/opt/ndk-archive-sha256.txt').read_text().strip() != lock['ndk']['sha256']:
    raise SystemExit('Restored NDK archive identity differs from review')
clang = ndk/'toolchains/llvm/prebuilt/linux-x86_64/bin/clang'
identity = subprocess.check_output([str(clang), '--version'], text=True)
if 'clang version '+lock['ndk']['clangVersion'] not in identity:
    raise SystemExit('Native compiler differs from review')
for name, version in lock['hostPackages'].items():
    actual = subprocess.check_output(['dpkg-query', '-W', '-f=${Version}', name], text=True)
    if actual != version:
        raise SystemExit('Host build package differs from review: '+name)
print(json.dumps({'reviewedToolsMatched': True, 'ndkRevision': lock['ndk']['revision'],
                  'clangSha256': hashlib.sha256(clang.read_bytes()).hexdigest()}))
