#!/usr/bin/env bash
set -euo pipefail
export LANG=C LC_ALL=C
export SOURCE_DATE_EPOCH="$(python3 -c 'import json; print(next(x for x in json.load(open("/inputs/source-manifest.json", encoding="utf-8-sig")) if x["name"]=="openssl")["sourceDateEpoch"])')"
python3 "$(dirname "$0")/verify-host.py" /inputs/android.inputs.lock.json > /output/host-proof.json
mkdir -p /home/nativebuild/upstream
cd /home/nativebuild/upstream
mkdir openssl sqlcipher crypto fixture
python3 - <<'PY'
import hashlib,json,pathlib
for x in json.loads(pathlib.Path('/inputs/source-manifest.json').read_text(encoding='utf-8-sig')):
    assert hashlib.sha256((pathlib.Path('/inputs')/(x['name']+'.tar')).read_bytes()).hexdigest()==x['archiveSha256']
PY
tar -xf /inputs/openssl.tar -C openssl
tar -xf /inputs/sqlcipher.tar -C sqlcipher
cd crypto
perl ../openssl/Configure linux-x86_64 no-shared no-module no-autoload-config no-filenames -fPIC -fstack-protector-strong > /output/crypto-configure.log 2>&1
make -j2 build_generated libcrypto.a > /output/crypto-build.log 2>&1
cd ../fixture
../sqlcipher/configure --with-tempstore=always --enable-fts5 CFLAGS="-O2 -DSQLITE_HAS_CODEC -DSQLITE_EXTRA_INIT=sqlcipher_extra_init -DSQLITE_EXTRA_SHUTDOWN=sqlcipher_extra_shutdown -DSQLCIPHER_CRYPTO_OPENSSL -DSQLCIPHER_TEST -DSQLITE_DEFAULT_FOREIGN_KEYS=1 -DSQLITE_ENABLE_COLUMN_METADATA -I/home/nativebuild/upstream/crypto/include -I/home/nativebuild/upstream/openssl/include" LDFLAGS="/home/nativebuild/upstream/crypto/libcrypto.a -pthread -ldl" > /output/fixture-configure.log 2>&1
make -j2 testfixture > /output/fixture-build.log 2>&1
ldd ./testfixture > /output/fixture-dependencies.txt
if grep -E 'libcrypto\.so|libssl\.so|not found' /output/fixture-dependencies.txt; then
    echo 'Fixture imported a system crypto library or unresolved dependency' >&2; exit 1
fi
cat > /output/identity.tcl <<'TCL'
sqlite3 db :memory: -key fictitious-runtime-identity
set cipher [db one {PRAGMA cipher_version}]
set provider [db one {PRAGMA cipher_provider_version}]
if {![string match {4.19.0*} $cipher] || ![string match {*OpenSSL 3.5.9*} $provider]} {
    error {Compiled fixture cipher/provider differs from reviewed official inputs}
}
puts "$cipher | $provider"
db close
TCL
./testfixture /output/identity.tcl > /output/identity.log 2>&1
cp ./testfixture /output/testfixture
./testfixture ../sqlcipher/test/sqlcipher.test > /output/sqlcipher-test.log 2>&1
python3 - <<'PY'
import json,pathlib,re
text=pathlib.Path('/output/sqlcipher-test.log').read_text()
match=re.search(r'0 errors out of (\d+) tests',text)
assert match and int(match[1])>0 and 'All memory allocations freed - no leaks' in text, 'Required upstream result missing'
pathlib.Path('/output/upstream-proof.json').write_text(json.dumps({'suite':'official-sqlcipher','tests':int(match[1]),'errors':0,'reportedNoLeaks':True,'reviewedNativeIdentityMatched':True,'notFullSqliteMobileOrAppAcceptance':True},indent=2))
PY
