#!/bin/sh
set -eu
umask 077
purpose="${ZANANCE_DEPLOYMENT_PURPOSE:?Explicit deployment purpose required}"
case "$purpose" in qualification|production) ;; *) echo 'Unsupported deployment purpose.' >&2; exit 2 ;; esac
mkdir -p /private /secrets/authority
purpose_file=/secrets/authority/deployment-purpose
if [ -f "$purpose_file" ]; then
  [ "$(cat "$purpose_file")" = "$purpose" ] || { echo 'Existing machine authority belongs to another deployment purpose; reuse refused.' >&2; exit 2; }
elif [ "$purpose" = production ] && [ -f /secrets/authority/complete.json ]; then
  echo 'Production requires a fresh classified machine authority; legacy authority is preserved.' >&2; exit 2
else
  printf '%s' "$purpose" > "$purpose_file"
  sync -f "$purpose_file"
fi
if [ "$purpose" = production ]; then
  [ ! -f /private/fictitious-hosting.marker ] || { echo 'Fictional service data must never be activated publicly.' >&2; exit 2; }
  if [ ! -f /private/production-hosting.marker ]; then
    [ -z "$(find /private -mindepth 1 -print -quit)" ] || { echo 'Existing private data cannot be silently reclassified as production.' >&2; exit 2; }
    printf '%s' 'Zanance fresh production authority v1' > /private/production-hosting.marker
    sync -f /private/production-hosting.marker
  fi
else
  [ ! -f /private/production-hosting.marker ] || { echo 'Production data cannot become a qualification fixture.' >&2; exit 2; }
  if [ ! -f /private/fictitious-hosting.marker ]; then
    printf '%s' 'Zanance fictional hosting authority v1' > /private/fictitious-hosting.marker
    sync -f /private/fictitious-hosting.marker
  fi
fi
operation="${ZANANCE_CERTIFICATE_OPERATION:-provision}"
case "$operation" in provision|prepare-transport-renewal|renew-transport) ;; *) echo 'Unsupported certificate operation.' >&2; exit 2 ;; esac
dotnet /app/Bootstrap.dll "$operation" /secrets
# Fixed project-owned mount roots only; never enumerate host paths or other projects.
chown -R 70:70 /secrets/database
chown -R 1654:1654 /secrets/api /secrets/operator
chmod 755 /secrets/trust
chmod 644 /secrets/trust/authority.crt
mkdir -p /database/18/docker /private /proxy-data
chown 70:70 /database /database/18 /database/18/docker
chown 1654:1654 /private
chown 1655:1655 /proxy-data
chmod 700 /database/18/docker /private /proxy-data
