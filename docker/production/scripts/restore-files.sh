#!/bin/sh
# Restore the `uploads` Docker volume from an archive produced by backup-files.sh.
#
# DESTRUCTIVE: this replaces the current contents of the uploads volume. Requires explicit
# confirmation.
#
# Usage:
#   ./restore-files.sh /var/backups/aihelpdesk/files/uploads-20260804-030000.tar.gz

set -eu

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
COMPOSE_FILE="$SCRIPT_DIR/../docker-compose.prod.yml"
VOLUME_NAME="${UPLOADS_VOLUME:-production_uploads}"

ARCHIVE="${1:?Usage: restore-files.sh <path-to-uploads-archive.tar.gz>}"

if [ ! -f "$ARCHIVE" ]; then
  echo "ERROR: archive not found: $ARCHIVE" >&2
  exit 1
fi

echo "About to REPLACE all contents of the '$VOLUME_NAME' volume with:"
echo "  $ARCHIVE"
printf 'Type "restore" to confirm: '
read -r CONFIRM
if [ "$CONFIRM" != "restore" ]; then
  echo "Confirmation did not match, aborting."
  exit 1
fi

echo "[$(date -Iseconds)] Stopping backend so it releases open file handles..."
docker compose -f "$COMPOSE_FILE" stop backend

echo "[$(date -Iseconds)] Clearing and restoring volume..."
docker run --rm \
  -v "$VOLUME_NAME:/data" \
  -v "$(dirname "$ARCHIVE"):/backup" \
  alpine sh -c "rm -rf /data/* /data/..?* /data/.[!.]* 2>/dev/null; tar -xzf /backup/$(basename "$ARCHIVE") -C /data"

echo "[$(date -Iseconds)] Restarting backend..."
docker compose -f "$COMPOSE_FILE" start backend

echo "[$(date -Iseconds)] Restore complete."
