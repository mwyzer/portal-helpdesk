#!/bin/sh
# Restore the PostgreSQL database from a backup produced by backup-db.sh.
#
# DESTRUCTIVE: this drops and recreates the target database. Requires explicit confirmation.
#
# Usage:
#   ./restore-db.sh /var/backups/aihelpdesk/daily/aihelpdesk-20260804-020000.dump.gz
#
# See documentation/deployment-runbook.md for the full restore procedure, including how to
# restore into a scratch database first to verify a backup without touching production.

set -eu

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ENV_FILE="${ENV_FILE:-$SCRIPT_DIR/../../../.env}"
COMPOSE_FILE="$SCRIPT_DIR/../docker-compose.prod.yml"

if [ -f "$ENV_FILE" ]; then
  set -a
  . "$ENV_FILE"
  set +a
fi

: "${POSTGRES_DB:=aihelpdesk}"
: "${POSTGRES_USER:?POSTGRES_USER must be set (via .env or environment)}"

BACKUP_FILE="${1:?Usage: restore-db.sh <path-to-backup.dump.gz>}"

if [ ! -f "$BACKUP_FILE" ]; then
  echo "ERROR: backup file not found: $BACKUP_FILE" >&2
  exit 1
fi

echo "About to DROP and restore database '$POSTGRES_DB' from:"
echo "  $BACKUP_FILE"
echo "This will PERMANENTLY discard all current data in '$POSTGRES_DB'."
printf 'Type the database name to confirm: '
read -r CONFIRM
if [ "$CONFIRM" != "$POSTGRES_DB" ]; then
  echo "Confirmation did not match, aborting."
  exit 1
fi

echo "[$(date -Iseconds)] Stopping backend so it releases connections..."
docker compose -f "$COMPOSE_FILE" stop backend

echo "[$(date -Iseconds)] Dropping and recreating database..."
docker compose -f "$COMPOSE_FILE" exec -T postgres psql -U "$POSTGRES_USER" -d postgres \
  -c "DROP DATABASE IF EXISTS \"$POSTGRES_DB\";" \
  -c "CREATE DATABASE \"$POSTGRES_DB\" OWNER \"$POSTGRES_USER\";"

echo "[$(date -Iseconds)] Restoring from backup..."
gunzip -c "$BACKUP_FILE" | docker compose -f "$COMPOSE_FILE" exec -T postgres \
  pg_restore -U "$POSTGRES_USER" -d "$POSTGRES_DB" --no-owner --role="$POSTGRES_USER"

echo "[$(date -Iseconds)] Restarting backend..."
docker compose -f "$COMPOSE_FILE" start backend

echo "[$(date -Iseconds)] Restore complete. Verify with: curl -f http://localhost:8080/api/health"
