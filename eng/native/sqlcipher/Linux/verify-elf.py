#!/usr/bin/env python3
"""Validate the actual ELF64 ABI, 16 KiB segments, exports and system-only dependencies."""
import hashlib, json, pathlib, struct, sys
p = pathlib.Path(sys.argv[1]); abi = sys.argv[2]; b = p.read_bytes()
if b[:6] != b'\x7fELF\x02\x01' or len(b) < 64:
    raise SystemExit('Expected little-endian ELF64')
if struct.unpack_from('<H', b, 16)[0] != 3 or struct.unpack_from('<H', b, 18)[0] != {'arm64-v8a':183, 'x86_64':62}[abi]:
    raise SystemExit('Native type or ABI differs')
phoff = struct.unpack_from('<Q', b, 32)[0]; entsize, count = struct.unpack_from('<HH', b, 54)
if entsize != 56 or phoff + count*entsize > len(b):
    raise SystemExit('Invalid native segment table')
segments = [struct.unpack_from('<IIQQQQQQ', b, phoff+i*entsize) for i in range(count)]
loads = [s for s in segments if s[0] == 1]
if not loads or any(s[7] < 16384 or s[2] % 16384 != s[3] % 16384 or s[1]&3 == 3 for s in loads):
    raise SystemExit('Invalid 16 KiB alignment or writable executable load segment')
if not any(s[0] == 0x6474e552 for s in segments) or not any(s[0] == 0x6474e551 and not s[1]&1 for s in segments):
    raise SystemExit('RELRO or non-executable stack missing')
def offset(address):
    for s in loads:
        if s[3] <= address < s[3]+s[5]: return s[2]+address-s[3]
    raise ValueError('Native address outside file loads')
dynamic = next(s for s in segments if s[0] == 2)
pairs = [struct.unpack_from('<QQ', b, i) for i in range(dynamic[2],dynamic[2]+dynamic[5],16)]
values = dict(pairs)
if not (24 in values or values.get(30,0)&8 or values.get(0x6ffffffb,0)&1):
    raise SystemExit('Immediate binding missing')
strings = offset(values[5])
def text(index):
    start = strings+index; end = b.index(0,start)
    return b[start:end].decode('ascii')
needed = sorted(text(v) for k,v in pairs if k == 1)
if not needed or set(needed)-{'libc.so','libdl.so','libm.so','liblog.so'}:
    raise SystemExit('Unexpected native dependency')
shoff = struct.unpack_from('<Q', b, 40)[0]; shsize, shcount = struct.unpack_from('<HH', b, 58)
if shsize != 64 or shoff+shcount*shsize > len(b): raise SystemExit('Invalid native section table')
sections = [struct.unpack_from('<IIQQQQIIQQ', b, shoff+i*shsize) for i in range(shcount)]
symbols = next(s for s in sections if s[1] == 11)
if symbols[9] != 24: raise SystemExit('Invalid native dynamic symbol size')
exports = set()
for i in range(symbols[4], symbols[4]+symbols[5],24):
    name,info,other,section,value,size = struct.unpack_from('<IBBHQQ',b,i)
    if section and info>>4 in (1,2) and other&3 == 0: exports.add(text(name))
required = {'sqlite3_open_v2','sqlite3_key','sqlite3_rekey','sqlite3_backup_init','sqlite3_serialize','sqlite3_deserialize'}
if not required <= exports or any(not x.startswith(('sqlite3','sqlcipher')) for x in exports):
    raise SystemExit('Required original exports missing or static crypto exposed')
print(json.dumps({'abi':abi,'nativeSha256':hashlib.sha256(b).hexdigest(),'loadAlignment16KiB':True,
    'noWritableExecutableSegment':True,'relroImmediateBindingNonExecutableStack':True,
    'systemDependencies':needed,'requiredExportsPresent':True,'staticCryptoNotExposed':True}))
