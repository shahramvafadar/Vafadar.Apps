FROM ubuntu@sha256:f610ab94648195aa356059f5b41d6085c9d4d903c072430cdd1af7bdb646106b AS archive-trust
ENV DEBIAN_FRONTEND=noninteractive
# Bootstrap HTTPS trust from the immutable Noble release package in a separate stage.
# Only its public certificate bundle crosses into the reviewed build image; no moving dependencies do.
RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates=20240203 && rm -rf /var/lib/apt/lists/*

FROM ubuntu@sha256:f610ab94648195aa356059f5b41d6085c9d4d903c072430cdd1af7bdb646106b
ENV DEBIAN_FRONTEND=noninteractive
COPY --from=archive-trust /etc/ssl/certs/ca-certificates.crt /etc/ssl/certs/ca-certificates.crt
# D-433: a pinned base image does not freeze a moving APT archive. Restore the original
# reviewed package closure from Ubuntu's signed timestamped snapshot, retaining TLS verification.
RUN printf 'Types: deb\nURIs: https://snapshot.ubuntu.com/ubuntu/20261012T000000Z/\nSuites: noble noble-updates noble-security\nComponents: main universe\nSigned-By: /usr/share/keyrings/ubuntu-archive-keyring.gpg\n' > /etc/apt/sources.list.d/ubuntu.sources && apt-get -o Acquire::Retries=3 -o APT::Update::Error-Mode=any update && apt-get install -y --no-install-recommends build-essential perl python3 tcl-dev unzip ca-certificates && rm -rf /var/lib/apt/lists/*
RUN python3 - <<'PY'
import urllib.request,hashlib,pathlib,zipfile
p=pathlib.Path('/tmp/ndk.zip')
with urllib.request.urlopen('https://dl.google.com/android/repository/android-ndk-r30-linux.zip') as r, p.open('wb') as f:
 while chunk:=r.read(1024*1024): f.write(chunk)
assert p.stat().st_size==738633529, 'Official NDK size differs'
h=hashlib.sha1();s=hashlib.sha256()
with p.open('rb') as f:
 while chunk:=f.read(1024*1024): h.update(chunk);s.update(chunk)
assert h.hexdigest()=='5107f898313790e449e87eee2183d9a20602dee9', 'Official NDK published checksum differs'
assert s.hexdigest()=='753611f410d002cfcd3f3dc2ef49aad532089d3180b436c060a90bf0fcb64df2', 'Reviewed NDK SHA256 differs'
pathlib.Path('/opt/ndk-archive-sha256.txt').write_text(s.hexdigest()+'\n')
PY
RUN unzip -q /tmp/ndk.zip -d /opt && rm /tmp/ndk.zip && grep -q '30.0.16248370' /opt/android-ndk-r30/source.properties
# Matched runner UIDs need traversal to owned work mounts, never write access to this parent.
RUN useradd --create-home --uid 10001 nativebuild && chmod 0755 /home/nativebuild
ENV ANDROID_NDK_ROOT=/opt/android-ndk-r30
USER nativebuild
WORKDIR /home/nativebuild
