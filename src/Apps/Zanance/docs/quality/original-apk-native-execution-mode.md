# Original APK native AOT-mode binding (D-772)

2 November 2026. D771's current Release evaluation does not retrospectively identify
the original D678 APK configuration. Inspect one declared public native enum in that
retained used package, without reading live state, environment tables or app assemblies.
This addresses a concrete provenance gap; it is not a crash repair or new workload.

## Exact original input

The original signed caller is56815522 bytes with SHA256
`adc8a82af3a626af3cdc8e10995721fb39190b94718a53352d8d4bf5dc785937`,
bound to actual original PID10110/UID10216. Its kept signed path is outside D678's
retired supplementary roots/files and is not a frozen resource. Validate the recorded
size and digest before opening the ZIP. Read only the two named public native entries.
The packaged x64 Mono digest remains
`edfa02c346f7eafc6cab665b27c667a93ecc84b7f40d042110ca4e343b2de8e4`,
exactly matching the admitted original fault module.

The selected lib/x86_64/libxamarin-app.so has SHA256
`ccd0a7326a8b1693383e088a6743eaeb23ceb2069ef1fe813a9aad1f7efa9e07`.
The previously retained exact SDK header declares mono_aot_mode_name as an exported
constant character pointer; header SHA256 is
`e545b044c4b8419c9162a2a3b4716c341a0bcacfe4704fadd222442ed2ecf449`.
Use that declaration, not an assumed offset into another runtime's configuration struct.

## Bounded static interpretation

Require actual ELF64/little-endian/x64, bounded section/program tables, one exported
object-symbol identity of the declared name/size8, exactly one matching relative
relocation and a unique file-backed PT_LOAD mapping. Read at most16 bytes from that
declared enum target. Accept only predeclared runtime-mode labels; refuse an unknown
value without printing its bytes. No pointer, relocation address, arbitrary string,
environment entry, licence/configuration secret or managed app image is exported.
The native image is read in memory and never loaded or executed as native code.

The actual original package's admitted enum is **Normal**. This corroborates normal
AOT configuration for the old APK independently of D771's current project evaluation.
It does not prove every original method's execution engine, interpreter initialization,
finally/OSR transition, GC root or earlier writer. A shared runtime can contain code
for several execution paths; mere symbol presence is not execution evidence.

Therefore the separately identified interpreter finally-OSR correction is still not
admitted as an original fault remedy. Do not force interpreter mode or upgrade a
runtime to manufacture a causal match. Preserve the absent source protection and
missing original method-path evidence as separate facts. No diagnosis is changed to
"interpreter impossible" or "crash fixed" from this one configuration enum.

Only static script/receipt files are created locally; there is no new native image,
build, product test, SDK change, device command, financial operation or cleanup scope.
The original APK/module and evidence remain retained. All older frozen scopes and
protected untracked owner files remain untouched. D752 waits for the owner;
D749/D751/D607 stay terminal. Native fault origin, performance and broader acceptance
remain open.
