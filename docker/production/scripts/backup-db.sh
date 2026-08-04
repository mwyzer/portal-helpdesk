#!/bin/sh
# Daily PostgreSQL backup: pg_dump (custom format) + gzip, with retention pruning.
#
# Run from the host (not inside a container) via cron, e.g. at 02:00:
#   0 2 * * * /opt/aihelpdesk/docker/production/scripts/backup-db.sh >> /var/log/aihelpdesk-backup.log 2>&1
#
# Reads DB connection details from the same .env used by docker-compose.prod.yml, so it
# stays in sync with whatever the running stack is actually using.
#
# Retention: daily backups kept 14 days. Weekly (Sunday) and monthly (1st of month) copies
# are kept longer and are the ones a cloud-sync job (see backup-files.sh's rclone comment)
# should ship offsite — see documentation/deployment-runbook.md for the retention rationale
# (daily: 14d, weekly: 3mo, monthly: 12mo, matching the Phase 7 spec).

set -eu

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ENV_FILE="${ENV_FILE:-$SCRIPT_DIR/../../../.env}"
BACKUP_ROOT="${BACKUP_ROOT:-/var/backups/aihelpdesk}"
COMPOSE_FILE="$SCRIPT_DIR/../docker-compose.prod.yml"

if [ -f "$ENV_FILE" ]; then
  set -a
  . "$ENV_FILE"
  set +a
fi

: "${POSTGRES_DB:=aihelpdesk}"
: "${POSTGRES_USER:?POSTGRES_USER must be set (via .env or environment)}"

DATE=$(date +%Y%m%d-%H%M%S)
DOW=$(date +%u)   # 1=Monday .. 7=Sunday
DOM=$(date +%d)

DAILY_DIR="$BACKUP_ROOT/daily"
WEEKLY_DIR="$BACKUP_ROOT/weekly"
MONTHLY_DIR="$BACKUP_ROOT/monthly"
mkdir -p "$DAILY_DIR" "$WEEKLY_DIR" "$MONTHLY_DIR"

FILENAME="aihelpdesk-$DATE.dump.gz"
DAILY_PATH="$DAILY_DIR/$FILENAME"

echo "[$(date -Iseconds)] Starting backup -> $DAILY_PATH"

# pg_dump runs inside the postgres container (has the client tools + direct socket access),
# custom format (-Fc) so it can be restored selectively with pg_restore; piped through gzip
# on the host side to avoid needing gzip inside the postgres image.
docker compose -f "$COMPOSE_FILE" exec -T postgres \
  pg_dump -U "$POSTGRES_USER" -Fc "$POSTGRES_DB" | gzip > "$DAILY_PATH"

if [ ! -s "$DAILY_PATH" ]; then
  echo "[$(date -Iseconds)] ERROR: backup file is empty, aborting" >&2
  rm -f "$DAILY_PATH"
  exit 1
fi

echo "[$(date -Iseconds)] Backup complete: $(du -h "$DAILY_PATH" | cut -f1)"

# Sunday -> also keep a weekly copy.
if [ "$DOW" = "7" ]; then
  cp "$DAILY_PATH" "$WEEKLY_DIR/$FILENAME"
fi

# 1st of the month -> also keep a monthly copy.
if [ "$DOM" = "01" ]; then
  cp "$DAILY_PATH" "$MONTHLY_DIR/$FILENAME"
fi

# Retention: daily 14 days, weekly 90 days (~3mo), monthly 365 days (~12mo).
find "$DAILY_DIR" -name '*.dump.gz' -mtime +14 -delete
find "$WEEKLY_DIR" -name '*.dump.gz' -mtime +90 -delete
find "$MONTHLY_DIR" -name '*.dump.gz' -mtime +365 -delete

echo "[$(date -Iseconds)] Retention pruning done."

# Weekly offsite sync (same RCLONE_REMOTE convention as backup-files.sh). Only the weekly
# dir is synced from here since it runs daily but the source data barely changes day to day;
# daily backups stay local-only for fast same-day restores.
if [ "$DOW" = "7" ] && [ -n "${RCLONE_REMOTE:-}" ]; then
  if command -v rclone >/dev/null 2>&1; then
    echo "[$(date -Iseconds)] Syncing $WEEKLY_DIR -> $RCLONE_REMOTE/db"
    rclone sync "$WEEKLY_DIR" "$RCLONE_REMOTE/db" --log-level INFO
  else
    echo "[$(date -Iseconds)] WARNING: RCLONE_REMOTE is set but rclone is not installed, skipping offsite sync" >&2
  fi
fi
