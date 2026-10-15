#!/usr/bin/env bash
# Real Linux service qualification only; never a deployment or an owner database.
set -euo pipefail
umask 077

case "${1:-}" in
  protocol|planning|operations) scenario="$1" ;;
  *) echo 'Select protocol, planning or operations.' >&2; exit 2 ;;
esac
: "${ZANANCE_SYNC_TEST_DB:?An isolated loopback test database is required.}"
: "${ZANANCE_SYNC_TEST_ROOT:?An absolute independently owned fixture root is required.}"
: "${ZANANCE_SYNC_TEST_RUNTIME:?The published Linux test runtime is required.}"
: "${ZANANCE_SYNC_TEST_PRIVATE:?An independent private qualification directory is required.}"
[[ "$ZANANCE_SYNC_TEST_ROOT" = /* && "$ZANANCE_SYNC_TEST_PRIVATE" = /* ]] || exit 2
[[ ! -e "$ZANANCE_SYNC_TEST_ROOT" ]] || { echo 'Preserve the existing fixture; automatic retries are refused.' >&2; exit 2; }
runtime="$ZANANCE_SYNC_TEST_RUNTIME/Vafadar.Zanance.Sync.Tests.dll"
[[ -f "$runtime" ]] || exit 2
mkdir -p "$ZANANCE_SYNC_TEST_PRIVATE"
chmod 700 "$ZANANCE_SYNC_TEST_PRIVATE"
certificate="$ZANANCE_SYNC_TEST_PRIVATE/protection.crt"
key="$ZANANCE_SYNC_TEST_PRIVATE/protection.key"
if [[ ! -e "$certificate" && ! -e "$key" ]]; then
  # Test-only wrapping authority, separate from the service's encrypted TLS identity/key ring.
  # Private bytes remain in this mode-0700 directory and are never uploaded with test reports.
  openssl req -x509 -newkey rsa:3072 -sha256 -days 2 -noenc \
    -subj '/CN=Zanance fictional Linux qualification' -keyout "$key" -out "$certificate" \
    > "$ZANANCE_SYNC_TEST_PRIVATE/certificate-creation.log" 2>&1
fi
[[ -f "$certificate" && -f "$key" && ! -L "$certificate" && ! -L "$key" ]] || exit 2
chmod 600 "$certificate" "$key"
export Service__ProtectionCertificate="$certificate"
export Service__ProtectionPrivateKey="$key"
results="$ZANANCE_SYNC_TEST_PRIVATE/results"
mkdir -p "$results"

run_case() {
  local method="$1" report="$2"
  dotnet "$runtime" -method "*$method" -noLogo -noColor -failSkips -failWarns -result-xml "$results/$report.xml"
  # An empty selection is not success, even when the runner returns exit code zero.
  grep -q 'total="1"' "$results/$report.xml"
  grep -q 'passed="1"' "$results/$report.xml"
}

case "$scenario" in
  protocol)
    run_case Two_actual_clients_preserve_financial_choices_and_resume_an_unacknowledged_commit initial
    # A distinct OS process must decrypt the same retained service identity and financial state.
    run_case Duplicate_full_settlements_keep_one_actual_payment_after_explicit_related_row_review cold
    ;;
  planning)
    run_case Planning_domain_refusals_preserve_original_rows_and_publish_only_a_valid_complete_graph planning
    ;;
  operations)
    run_case Owner_staging_bounds_reclaim_abandoned_work_and_collection_preserves_inflight_pieces operations
    ;;
esac
