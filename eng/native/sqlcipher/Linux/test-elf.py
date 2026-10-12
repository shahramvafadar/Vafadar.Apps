#!/usr/bin/env python3
"""Exercise refusal of malformed native candidates without loading or executing them."""
import json, pathlib, struct, subprocess, sys, uuid
library = pathlib.Path(sys.argv[1]).resolve(); abi = sys.argv[2]
root = pathlib.Path(sys.argv[3]).resolve()
expected = pathlib.Path(__file__).resolve().parents[4]/'artifacts'/'native-sqlcipher'
if root != expected and expected not in root.parents:
    raise SystemExit('Keep negative ELF evidence in owned native artifacts')
root = root/('elf-fixture-'+uuid.uuid4().hex); root.mkdir(parents=True)
verifier = pathlib.Path(__file__).with_name('verify-elf.py')
original = library.read_bytes()
positive = subprocess.run([sys.executable,str(verifier),str(library),abi],capture_output=True,text=True)
if positive.returncode: raise SystemExit(positive.stderr)
phoff = struct.unpack_from('<Q',original,32)[0]; size,count = struct.unpack_from('<HH',original,54)
headers = [(phoff+i*size,struct.unpack_from('<IIQQQQQQ',original,phoff+i*size)) for i in range(count)]
load = next(offset for offset,s in headers if s[0] == 1)
stack = next(offset for offset,s in headers if s[0] == 0x6474e551)
relro = next(offset for offset,s in headers if s[0] == 0x6474e552)
cases = []
for name in ['wrong-abi','small-load-alignment','writable-executable-load','executable-stack','missing-relro','missing-key-export']:
    b = bytearray(original)
    if name == 'wrong-abi': struct.pack_into('<H',b,18,62 if abi=='arm64-v8a' else 183)
    elif name == 'small-load-alignment': struct.pack_into('<Q',b,load+48,4096)
    elif name == 'writable-executable-load': struct.pack_into('<I',b,load+4,7)
    elif name == 'executable-stack': struct.pack_into('<I',b,stack+4,7)
    elif name == 'missing-relro': struct.pack_into('<I',b,relro,0)
    else: b = b.replace(b'sqlite3_key\0',b'sqlite3_kez\0')
    file = root/(name+'.so'); file.write_bytes(b)
    result = subprocess.run([sys.executable,str(verifier),str(file),abi],capture_output=True,text=True)
    if not result.returncode: raise SystemExit('Unsafe ELF accepted: '+name)
    cases.append({'case':name,'refusal':result.stderr.strip()})
print(json.dumps({'positiveNativePassed':True,'refusedCases':cases,'neverLoadedOrExecuted':True}))
