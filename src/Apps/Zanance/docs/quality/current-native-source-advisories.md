# Current native source advisory correlation - D-950

## Exact current scope

D948 checks the managed package graph and explicitly leaves native sources separate.
This finite review correlates the tracked native source pins, qualification lineage
and actual current D949 shipping cipher members with fresh official advisories.
It does not rebuild/load/execute native code, access any device or private database,
rerun successful crypto/financial workloads, or change a dependency automatically.

The tracked inputs.lock.json SHA256 is
`770688af8a3774f695f8a2119574376e8c2b5b34bc6af4d777f62654b2d07de6`,
matching android.runtime.qualification.json sourceInputsSha256. Pins remain SQLCipher
v4.19.0/c4b275a47932888216bade83aff2bbc73df0ff85 and OpenSSL
3.5.9/45e844fa2a14ec92d146bd8f5778ac130b6625fb. Actual D949 shipping APKc38842ce
is hash-bound. Its public cipher members equal both the tracked qualification and
selected candidate5 inventory: ARM64 48d06bc639afdd119833d998da14976c64fc4fb3a9cc43dac682879cea72475c;
x64 20b009b7ce0fc50bf31fe6e321216085a2c9ee3f3e14e0eb6757597380c2c568.
These are source-pin/tracked-qualification/module-hash correlations, not an
independent reproducible-build proof of all compiler inputs or runtime dispatch.
An expected default package-cache manifest path is absent; it supplies no provenance
and no replacement cache/download or invented manifest is used.

## OpenSSL known affected ranges

The complete [official 3.5 vulnerability list](https://www.openssl-library.org/news/vulnerabilities-3.5/index.html)
is retrieved and retained with response headers, time, size and digest. All61unique
CVE entries have exactly one explicit 3.5 lower-inclusive/upper-exclusive numeric
range; unknown/ambiguous range syntax refuses. Six new private numeric boundary
controls include exact lower/upper bounds and 3.5.10 ordering. Version3.5.9 matches
zero of those61affected ranges. This closes only matching against this dated list:
not undisclosed vulnerabilities, all other libraries, source/binary reproducibility,
configuration, exploit reachability, performance or whole security. A version-range
match is not replaced by an assumption that TLS code is unused.

## SQLCipher maintenance distinction

The [official 4.19 announcement](https://www.zetetic.net/blog/2026/09/08/sqlcipher-4.19.0-release/)
places its two low-risk export-alias/invalid-hexkey fixes in4.19, affecting4.18and
earlier. The tracked pin is4.19.0. This is published version applicability, not a
repeated exploit/crypto test or new source-level proof of every fix in a binary.
The vendor's prebuilt-edition provider matrix does not describe our separately
built OpenSSL-backed package; our pin/qualification evidence remains authoritative.

The newer [4.20 release](https://www.zetetic.net/blog/2026/10/27/sqlcipher-4.20.0-release/)
updates SQLite to3.54.1 and changes migration error handling/WebAssembly finalization.
Its existence is recorded rather than equated with a demonstrated vulnerability in
our pinned build. Underlying SQLite changes/CVE applicability and the local migration
call path still need separate assessment before deciding on a native update. This
review does not claim SQLCipher4.19 is the newest or all native security is complete.
SQLCipher5 beta is not adopted or treated as a compatible maintenance replacement.

## Retention and remaining work

Three official public documents are fetched once locally; raw HTML/headers/digests,
61range rows, six controls and current module/source proof remain in
`artifacts/d950-native-source-advisories`. No downloaded package, build, app test,
cleanup, financial action, phone/emulator command, runtime/GC/KDF change, frozen
scope read or old-success replay occurs. Only documentation is committed. New local
links and diff-check are verified; no product build/test is repeated for these docs.
D949 is already pushed, with its sole delayed CI observation still pending at this
step; no polling or final CI result is inferred. D944 original physical attempt stays
held while the owner is away, and Main adoption waits. Overall delivery, original
fault/ANR, whole performance/security/accessibility and platform/external gates stay
open; independent available work continues.
