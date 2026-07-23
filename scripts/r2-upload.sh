#!/bin/bash
# scripts/r2-upload.sh — DEC-051: Upload local pg_dump to Cloudflare R2
#
# Usage:
#   bash scripts/r2-upload.sh /tmp/erp-backup-20260722-020000.sql.gz
#
# Required env:
#   R2_ACCESS_KEY    — Cloudflare R2 access key
#   R2_SECRET_KEY    — Cloudflare R2 secret key
#   R2_ENDPOINT      — R2 endpoint URL (e.g., https://ACCT.r2.cloudflarestorage.com)
#   R2_BUCKET        — bucket name (e.g., erp-system-backups)
# Optional env:
#   R2_PREFIX        — object key prefix (default "backups/")
#   R2_STORAGE_CLASS — STANDARD | STANDARD_IA | GLACIER (default STANDARD)
#   DELETE_LOCAL     — delete local file after upload (default 1)
#   R2_KEEP_VERSIONS — number of versions to retain (default 30)

set -euo pipefail

# Validate args
if [ $# -lt 1 ]; then
  echo "Usage: $0 <local-file> [checksum-file]" >&2
  echo "  local-file:    path to .sql.gz file from pg-dump.sh"
  echo "  checksum-file: optional .sha256 file (auto-detected if not given)"
  exit 1
fi

LOCAL_FILE="$1"
CHECKSUM_FILE="${2:-${LOCAL_FILE}.sha256}"

# Validate env
for var in R2_ACCESS_KEY R2_SECRET_KEY R2_ENDPOINT R2_BUCKET; do
  if [ -z "${!var:-}" ]; then
    echo "ERROR: $var is required" >&2
    exit 1
  fi
done

# Validate local file
if [ ! -s "$LOCAL_FILE" ]; then
  echo "ERROR: Local file missing or empty: $LOCAL_FILE" >&2
  exit 1
fi

# Setup
R2_PREFIX="${R2_PREFIX:-backups/}"
R2_STORAGE_CLASS="${R2_STORAGE_CLASS:-STANDARD}"
DELETE_LOCAL="${DELETE_LOCAL:-1}"
R2_KEEP_VERSIONS="${R2_KEEP_VERSIONS:-30}"
BASENAME=$(basename "$LOCAL_FILE")
KEY="${R2_PREFIX}${BASENAME}"
LOCAL_SIZE=$(stat -c%s "$LOCAL_FILE" 2>/dev/null || stat -f%z "$LOCAL_FILE")
LOCAL_SHA=$(sha256sum "$LOCAL_FILE" | awk '{print $1}')

echo "[$(date -u +%H:%M:%S)] === DEC-051 R2 upload started ==="
echo "[$(date -u +%H:%M:%S)] Local file: $LOCAL_FILE ($LOCAL_SIZE bytes)"
echo "[$(date -u +%H:%M:%S)] Local SHA256: $LOCAL_SHA"
echo "[$(date -u +%H:%M:%S)] Target: s3://$R2_BUCKET/$KEY"
echo "[$(date -u +%H:%M:%S)] Storage class: $R2_STORAGE_CLASS"

# Install boto3 if needed
if ! python3 -c "import boto3" 2>/dev/null; then
  echo "[$(date -u +%H:%M:%S)] Installing boto3..."
  pip3 install --quiet --break-system-packages boto3
fi

# Upload via Python (boto3) — more reliable than aws CLI in CI environments
python3 << PYEOF
import os, sys, hashlib
import boto3
from botocore.config import Config

s3 = boto3.client(
    's3',
    endpoint_url=os.environ['R2_ENDPOINT'],
    aws_access_key_id=os.environ['R2_ACCESS_KEY'],
    aws_secret_access_key=os.environ['R2_SECRET_KEY'],
    config=Config(
        retries={'max_attempts': 3, 'mode': 'standard'},
        connect_timeout=30,
        read_timeout=60,
    ),
)

local_file = "$LOCAL_FILE"
key = "$KEY"
sha = "$LOCAL_SHA"
size = $LOCAL_SIZE
storage_class = "$R2_STORAGE_CLASS"

# Read file, compute checksum for integrity
with open(local_file, 'rb') as f:
    data = f.read()
actual_sha = hashlib.sha256(data).hexdigest()
if actual_sha != sha:
    print(f"ERROR: SHA mismatch! local={actual_sha}, expected={sha}")
    sys.exit(1)
print(f"  [OK] SHA256 verified: {actual_sha}")

# Upload with multipart (works for large files)
try:
    s3.upload_file(
        local_file, "$R2_BUCKET", key,
        ExtraArgs={
            'StorageClass': storage_class,
            'Metadata': {
                'sha256': sha,
                'source': 'dec-051-nightly',
            }
        }
    )
    print(f"  [OK] Uploaded: s3://$R2_BUCKET/{key}")
except Exception as e:
    print(f"ERROR: Upload failed: {e}")
    sys.exit(1)

# Verify upload
response = s3.head_object(Bucket="$R2_BUCKET", Key=key)
remote_size = response['ContentLength']
remote_etag = response['ETag'].strip('"')
if remote_size != size:
    print(f"ERROR: Size mismatch! remote={remote_size}, local={size}")
    sys.exit(1)
print(f"  [OK] Remote size verified: {remote_size} bytes")

# Upload checksum file too
checksum_key = f"{key}.sha256"
s3.put_object(
    Bucket="$R2_BUCKET",
    Key=checksum_key,
    Body=sha.encode(),
    StorageClass=storage_class,
    Metadata={'source': 'dec-051-nightly-checksum'},
)
print(f"  [OK] Checksum uploaded: s3://$R2_BUCKET/{checksum_key}")
PYEOF

# Rotation: keep only last N versions
if [ "$R2_KEEP_VERSIONS" -gt 0 ]; then
  echo "[$(date -u +%H:%M:%S)] Rotating: keeping last $R2_KEEP_VERSIONS versions"
  python3 << PYEOF
import os
import boto3
from datetime import datetime

s3 = boto3.client(
    's3',
    endpoint_url=os.environ['R2_ENDPOINT'],
    aws_access_key_id=os.environ['R2_ACCESS_KEY'],
    aws_secret_access_key=os.environ['R2_SECRET_KEY'],
)

prefix = "$R2_PREFIX"
keep = $R2_KEEP_VERSIONS

# List all .sql.gz files (exclude .sha256)
paginator = s3.get_paginator('list_objects_v2')
pages = paginator.paginate(Bucket="$R2_BUCKET", Prefix=prefix)

all_files = []
for page in pages:
    for obj in page.get('Contents', []):
        if obj['Key'].endswith('.sql.gz'):
            all_files.append(obj)

# Sort by LastModified descending
all_files.sort(key=lambda x: x['LastModified'], reverse=True)

# Delete old ones
to_delete = all_files[keep:]
for obj in to_delete:
    s3.delete_object(Bucket="$R2_BUCKET", Key=obj['Key'])
    print(f"  [DELETE] {obj['Key']} ({obj['LastModified'].isoformat()})")

if not to_delete:
    print(f"  [OK] Only {len(all_files)} versions present (≤ {keep} limit)")
else:
    print(f"  [OK] Deleted {len(to_delete)} old versions, kept {len(all_files) - len(to_delete)}")
PYEOF
fi

# Optional: delete local
if [ "$DELETE_LOCAL" = "1" ]; then
  echo "[$(date -u +%H:%M:%S)] Deleting local file (DELETE_LOCAL=1)"
  rm -f "$LOCAL_FILE" "$CHECKSUM_FILE"
fi

echo "[$(date -u +%H:%M:%S)] === DEC-051 R2 upload complete ==="
