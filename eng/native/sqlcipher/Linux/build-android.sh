#!/usr/bin/env bash
set -euo pipefail
export LANG=C LC_ALL=C
export SOURCE_DATE_EPOCH="$(python3 -c 'import json; print(next(x for x in json.load(open("/inputs/source-manifest.json", encoding="utf-8-sig")) if x["name"]=="openssl")["sourceDateEpoch"])' )"
python3 "$(dirname "$0")/verify-host.py" /inputs/android.inputs.lock.json > /output/host-proof.json
mkdir -p /home/nativebuild/work
cd /home/nativebuild/work
mkdir openssl sqlcipher amalgamation
python3 - <<'PY'
import hashlib,json,pathlib
for x in json.loads(pathlib.Path('/inputs/source-manifest.json').read_text(encoding='utf-8-sig')):
 p=pathlib.Path('/inputs')/(x['name']+'.tar')
 assert hashlib.sha256(p.read_bytes()).hexdigest()==x['archiveSha256'], 'Native source archive differs'
PY
tar -xf /inputs/openssl.tar -C openssl
tar -xf /inputs/sqlcipher.tar -C sqlcipher
cd amalgamation
../sqlcipher/configure --disable-tcl --with-tempstore=always --enable-fts5 > /output/amalgamation-configure.log 2>&1
make -j2 sqlite3.c > /output/amalgamation-build.log 2>&1
cd ..
export PATH="$ANDROID_NDK_ROOT/toolchains/llvm/prebuilt/linux-x86_64/bin:$PATH"
for abi in arm64-v8a x86_64; do
 case "$abi" in
  arm64-v8a) target=android-arm64; triple=aarch64-linux-android ;;
  x86_64) target=android-x86_64; triple=x86_64-linux-android ;;
 esac
 mkdir "crypto-$abi" "/output/$abi"
 cd "crypto-$abi"
 perl ../openssl/Configure "$target" -D__ANDROID_API__=24 -U__ANDROID_API__ no-shared no-module no-autoload-config no-filenames -fPIC -fstack-protector-strong "-ffile-prefix-map=/home/nativebuild/work=/usr/src/sqlcipher" > "/output/$abi/configure.log" 2>&1
 make -j2 build_generated libcrypto.a > "/output/$abi/crypto-build.log" 2>&1
 "$triple"24-clang -shared -O2 -fPIC -fstack-protector-strong -fvisibility=hidden '-DSQLITE_API=__attribute__((visibility("default")))' -DSQLITE_HAS_CODEC -DSQLITE_TEMP_STORE=3 -DSQLITE_THREADSAFE=1 -DSQLITE_DEFAULT_FOREIGN_KEYS=1 -DSQLITE_EXTRA_INIT=sqlcipher_extra_init -DSQLITE_EXTRA_SHUTDOWN=sqlcipher_extra_shutdown -DSQLCIPHER_CRYPTO_OPENSSL -DSQLITE_ENABLE_COLUMN_METADATA -DSQLITE_ENABLE_FTS5 -Iinclude -I../openssl/include -I../amalgamation ../amalgamation/sqlite3.c libcrypto.a -ldl -lm -llog -Wl,-z,relro,-z,now,--exclude-libs,ALL,-z,max-page-size=16384,--build-id=sha1,-soname,libsqlcipher.so -o "/output/$abi/libsqlcipher.so" > "/output/$abi/cipher-build.log" 2>&1
 "$triple"24-clang -O2 -fPIE -pie -Wall -Wextra -Werror -I../amalgamation "$(dirname "$0")/android-probe.c" -L"/output/$abi" -lsqlcipher '-Wl,-rpath,$ORIGIN' -Wl,-z,relro,-z,now,-z,max-page-size=16384 -o "/output/$abi/native-probe" > "/output/$abi/probe-build.log" 2>&1
 python3 "$(dirname "$0")/verify-elf.py" "/output/$abi/libsqlcipher.so" "$abi" > "/output/$abi/elf-proof.json"
 llvm-readelf -h -l -d -s "/output/$abi/libsqlcipher.so" > "/output/$abi/elf-report.txt"
 cd ..
done
cp amalgamation/sqlite3.h /output/sqlite3.h
sha256sum /output/*/libsqlcipher.so > /output/native-sha256.txt

mkdir -p /output/licenses
cp sqlcipher/LICENSE.md /output/licenses/SQLCipher.txt
cp openssl/LICENSE.txt /output/licenses/OpenSSL.txt
cp /opt/android-ndk-r30/NOTICE.toolchain /output/licenses/NDK-CompilerRuntime.txt
