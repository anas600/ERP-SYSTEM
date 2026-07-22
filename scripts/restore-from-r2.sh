#!/bin/bash
# scripts/restore-from-r2.sh — DEC-051: Disaster Recovery
#
# Usage:
#   bash scripts/restore-from-r2.sh s3://erp-system-backups/backups/erp-backup-20260722-020000.sql.gz
#   bash scripts/restore-from-r2.sh latest  # restore most recent
#
# Required env:
#   R2_ACCESS_KEY, R2_SECRET_KEY, R2_ENDPOINT, R2_BUCKET
#   NEON_URL — target DB to restore into
# Optional env:
#   R2_PREFIX — default "backups/"

set -euo pipefail

# Validate env
for var in R2_ACCESS_KEY R2_SECRET_KEY R2_ENDPOINT R2_BUCKET NEON_URL; do
  if [ -z "${!var:-}" ]; then
    echo "ERROR: $var is required" >&2
    exit 1
  fi
done

R2_PREFIX="${R2_PREFIX:-backups/}"

# Resolve key
if [ "${1:-}" = "latest" ]; then
  echo "[$(date -u +%H:%M:%S)] Finding latest backup..."
  KEY=$(python3 << PYEOF
import os
import boto3
s3 = boto3.client(
    's3',
    endpoint_url=os.environ['R2_ENDPOINT'],
    aws_access_key_id=os.environ['R2_ACCESS_KEY'],
    aws_secret_access_key=os.environ['R2_SECRET_KEY'],
)
paginator = s3.get_paginator('list_objects_v2')
files = []
for page in paginator.paginate(Bucket=os.environ['R2_BUCKET'], Prefix=os.environ.get('R2_PREFIX', 'backups/')):
    for obj in page.get('Contents', []):
        if obj['Key'].endswith('.sql.gz'):
            files.append(obj)
files.sort(key=lambda x: x['LastModified'], reverse=True)
print(files[0]['Key'] if files else '')
PYEOF
)
  if [ -z "$KEY" ]; then
    echo "ERROR: No backups found" >&2
    exit 1
  fi
  echo "[$(date -u +%H:%M:%S)] Latest: $KEY"
else
  KEY="${1}"
  # Strip s3://bucket/ if present
  KEY=$(echo "$KEY" | sed "s|^s3://$R2_BUCKET/||")
fi

LOCAL_FILE="/tmp/$(basename "$KEY")"
echo "[$(date -u +%H:%M:%S)] Downloading: s3://$R2_BUCKET/$KEY -> $LOCAL_FILE"

# Install boto3 if needed
if ! python3 -c "import boto3" 2>/dev/null; then
  pip3 install --quiet --break-system-packages boto3
fi

# Download
python3 << PYEOF
import os
import boto3
s3 = boto3.client(
    's3',
    endpoint_url=os.environ['R2_ENDPOINT'],
    aws_access_key_id=os.environ['R2_ACCESS_KEY'],
    aws_secret_access_key=os.environ['R2_SECRET_KEY'],
)
s3.download_file(os.environ['R2_BUCKET'], "$KEY", "$LOCAL_FILE")
print("  [OK] Downloaded")
# Check checksum if available
checksum_key = f"$KEY.sha256"
try:
    resp = s3.get_object(Bucket=os.environ['R2_BUCKET'], Key=checksum_key)
    remote_sha = resp['Body'].read().decode().strip()
    import hashlib
    with open("$LOCAL_FILE", 'rb') as f:
        local_sha = hashlib.sha256(f.read()).hexdigest()
    if local_sha == remote_sha:
        print(f"  [OK] SHA256 verified: {local_sha}")
    else:
        print(f"  [WARN] SHA mismatch! local={local_sha}, remote={remote_sha}")
except Exception:
    print("  [SKIP] No checksum file in R2")
PYEOF

# Confirm before restore
echo ""
echo "⚠️  WARNING: This will OVERWRITE the target database!"
echo "Target: $(echo "$NEON_URL" | sed -E 's|.*@([^/]+)/.*|@\1|')"
echo "Source: $LOCAL_FILE (from $KEY)"
read -p "Type 'YES' to proceed: " CONFIRM
if [ "$CONFIRM" != "YES" ]; then
  echo "Aborted."
  exit 1
fi

# Restore
echo "[$(date -u +%H:%M:%S)] Restoring..."
if gunzip -c "$LOCAL_FILE" | psql "$NEON_URL" --single-transaction --no-psqlrc 2>&1 | tail -20; then
  echo "[$(date -u +%H:%M:%S)] === RESTORE COMPLETE ==="
else
  echo "[$(date -u +%H:%M:%S)] === RESTORE FAILED ==="
  exit 1
fi
