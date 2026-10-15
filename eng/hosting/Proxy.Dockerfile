FROM caddy:2.11.7-alpine@sha256:d8542f48d34a9cf4e4c11a478865229840e87e4c96ea3f439101f31a5d35f75f
# The official binary requests a privileged-port file capability. This listener uses 8443, so
# remove that request rather than widening the runtime capability bounding set (D-293).
USER 0:0
RUN setcap -r /usr/bin/caddy
COPY Caddyfile.local /etc/caddy/Caddyfile
COPY Caddyfile.public /etc/caddy/Caddyfile.public
USER 1655:1655
