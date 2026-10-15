#!/bin/sh
set -eu
umask 077
: "${PGDATA:?Explicit owned data directory required}"
[ "$PGDATA" = /var/lib/postgresql/18/docker ] || exit 2
[ ! -L "$PGDATA" ] || exit 2
mkdir -p "$PGDATA"
if [ ! -f "$PGDATA/PG_VERSION" ]; then
  [ -z "$(ls -A "$PGDATA")" ] || { echo 'Existing incomplete database preserved; automatic initialization refused.' >&2; exit 2; }
  initdb -D "$PGDATA" --auth-local=peer --auth-host=reject --encoding=UTF8 --locale=C.UTF-8
fi
if [ ! -f "$PGDATA/zanance-initialized" ]; then
  # No trust-authenticated TCP bootstrap and no generated owner password are needed.
  pg_ctl -D "$PGDATA" -o "-c listen_addresses='' -c unix_socket_directories=/tmp -c hba_file=/configuration/pg_hba.conf" -w start
  psql -h /tmp -U postgres -v ON_ERROR_STOP=1 -d postgres <<'SQL'
SELECT 'CREATE ROLE zanance_owner LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS' WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname='zanance_owner')\gexec
SELECT 'CREATE ROLE zanance_runtime LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS' WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname='zanance_runtime')\gexec
SELECT 'CREATE DATABASE zanance OWNER zanance_owner' WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname='zanance')\gexec
REVOKE ALL ON DATABASE zanance FROM PUBLIC;
GRANT CONNECT ON DATABASE zanance TO zanance_owner, zanance_runtime;
SQL
  pg_ctl -D "$PGDATA" -m fast -w stop
  touch "$PGDATA/zanance-initialized"
fi
exec postgres -D "$PGDATA" -c listen_addresses='*' -c unix_socket_directories=/tmp \
 -c hba_file=/configuration/pg_hba.conf -c ssl=on -c ssl_min_protocol_version=TLSv1.2 \
 -c ssl_cert_file=/identities/server.crt -c ssl_key_file=/identities/server.key -c ssl_ca_file=/identities/authority.crt \
 -c log_statement=none -c log_connections=off -c log_disconnections=off -c logging_collector=off
