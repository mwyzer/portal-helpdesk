#!/bin/sh
# Weekly backup of the `uploads` Docker volume (tickets attachments, KB documents,
# candidate CVs, generated letters, etc.) to a local archive, then optionally synced
# offsite with rclone.
#
# Run from the host via cron, e.g. weekly at 03:00 Sunday:
#   0 3 * * 0 /opt/aihelpdesk/docker/production/scripts/backup-files.sh >> /var/log/aihelpdesk-backup.log 2>&1
#
# rclone push is opt-in: only runs if RCLONE_REMOTE is set (e.g. "b2:aihelpdesk-backups" or
# "s3:my-bucket/aihelpdesk"). Without it, this script still produces a local tarball, which is
# enough for on-host disaster recovery but NOT enough if the VPS itself is lost — configure
# rclone for real offsite retention. See documentation/deployment-runbook.md.

set -eu

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ENV_FILE="${ENV_FILE:-$SCRIPT_DIR/../../../.env}"
BACKUP_ROOT="${BACKUP_ROOT:-/var/backups/aihelpdesk}"
COMPOSE_FILE="$SCRIPT_DIR/../docker-compose.prod.yml"
VOLUME_NAME="${UPLOADS_VOLUME:-production_uploads}"

if [ -f "$ENV_FILE" ]; then
  set -a
  . "$ENV_FILE"
  set +a
fi

DATE=$(date +%Y%m%d-%H%M%S)
FILES_DIR="$BACKUP_ROOT/files"
mkdir -p "$FILES_DIR"

ARCHIVE="$FILES_DIR/uploads-$DATE.tar.gz"

echo "[$(date -Iseconds)] Archiving uploads volume -> $ARCHIVE"

# Mount the named volume read-only into a throwaway alpine container and tar it up, rather
# than assuming a host bind-mount path (the volume driver location varies by Docker install).
docker run --rm \
  -v "$VOLUME_NAME:/data:ro" \
  -v "$FILES_DIR:/backup" \
  alpine sh -c "tar -czf /backup/$(basename "$ARCHIVE") -C /data ."

if [ ! -s "$ARCHIVE" ]; then
  echo "[$(date -Iseconds)] ERROR: archive is empty, aborting" >&2
  rm -f "$ARCHIVE"
  exit 1
fi

echo "[$(date -Iseconds)] Archive complete: $(du -h "$ARCHIVE" | cut -f1)"

# Retention: keep 90 days of weekly local archives.
find "$FILES_DIR" -name 'uploads-*.tar.gz' -mtime +90 -delete

if [ -n "${RCLONE_REMOTE:-}" ]; then
  if command -v rclone >/dev/null 2>&1; then
    echo "[$(date -Iseconds)] Syncing $FILES_DIR -> $RCLONE_REMOTE"
    rclone sync "$FILES_DIR" "$RCLONE_REMOTE" --log-level INFO
  else
    echo "[$(date -Iseconds)] WARNING: RCLONE_REMOTE is set but rclone is not installed, skipping offsite sync" >&2
  fi
else
  echo "[$(date -Iseconds)] RCLONE_REMOTE not set, skipping offsite sync (local archive only)"
fi
