#!/bin/bash
set -euo pipefail

# Parse
ENVIRONMENT="${1:-dev}"

# Validate
case "$ENVIRONMENT" in
  dev|prod)
    ;;
  *)
    echo "Error: environment must be either 'dev' or 'prod'." >&2
    echo "Usage: $0 [dev|prod]" >&2
    exit 1
    ;;
esac
echo "Environment: $ENVIRONMENT"

REPO_DIR="${HOME}/CapX"
COMPOSE_FILE="${REPO_DIR}/compose.yml"
ENV_FILE="/var/www/capx/variables.env"

# --- DB location settings ---
BIND_MOUNT_DIR="/var/www/capx/capx-state"
DB_FILE="${BIND_MOUNT_DIR}/PPMTool.db"

# --- Prod server setting for copying over prod DB ---
REMOTE_USER_HOST="mbgm6ah3@balex.itservices.manchester.ac.uk"
REMOTE_PATH="/var/www/capx/PPMTool.db*"
SSH_KEY="${HOME}/.ssh/id_rsa"

# --- Mail Notfiication Settings ---
MAIL_TO="adrian.harwood@manchester.ac.uk"
if [[ "$ENVIRONMENT" == "dev" ]]; then
  MAIL_SUBJECT="[CapX Test] Deployment Executed"
  MAIL_BODY="Deployment of CapX Test has just run!"
else
  MAIL_SUBJECT="[CapX Prod] Deployment Executed"
  MAIL_BODY="Deployment of CapX Prod has just run!"
fi

# --- Make sure the local directory where the DB sits exists ---
echo "==> Ensure host DB dir exists (bind mount)"
if [ -n "${BIND_MOUNT_DIR}" ]; then
  sudo mkdir -p "${BIND_MOUNT_DIR}"
  sudo chown "$(id -u)":"$(id -g)" "${BIND_MOUNT_DIR}"
fi

# --- Take down the containers to kill access to DB file ---
echo "==> Stop running stack to avoid SQLite locks"
sudo docker compose -f "${COMPOSE_FILE}" --env-file "${ENV_FILE}" down || true

# -- Back the DB up ---
if [ -f "${DB_FILE}" ]; then
  sudo cp -a "${DB_FILE}" "${DB_FILE}.bak"
fi

# --- If on the dev system then sync the DB from production ---
if [[ "$ENVIRONMENT" == "dev" ]]; then
  
  echo "==> Sync DB from production (copy *.db* and vacuum)"
  # Copy to a temp staging location first
  STAGING_DIR="$(mktemp -d)"
  trap 'rm -rf "${STAGING_DIR}"' EXIT
  sudo scp -i "${SSH_KEY}" "${REMOTE_USER_HOST}:${REMOTE_PATH}" "${STAGING_DIR}/"
  
  # We only want the main .db; -wal/-shm will be dropped post-vacuum
  if [ ! -f "${STAGING_DIR}/PPMTool.db" ]; then
	echo "ERROR: PPMTool.db not found in staging copy" >&2
	exit 2
  fi

  # Vacuum to checkpoint and compact
  sudo sqlite3 "${STAGING_DIR}/PPMTool.db" 'PRAGMA wal_checkpoint(TRUNCATE); VACUUM;'

  # --- Place DB where the container will see it ---
  echo "==> Using bind mount: ${BIND_MOUNT_DIR}"
  sudo cp -f "${STAGING_DIR}/PPMTool.db" "${DB_FILE}"
  sudo chown "$(id -u)":"$(id -g)" "${DB_FILE}"
  sudo chmod 664 "${DB_FILE}"
fi

echo "==> Build and start app (wait for healthy)"
sudo docker compose -f "${COMPOSE_FILE}" --env-file "${ENV_FILE}" up -d --build --remove-orphans --wait --wait-timeout 60

echo "==> Compose status"
sudo docker compose -f "${COMPOSE_FILE}" --env-file "${ENV_FILE}" ps

# Send email notification
if command -v mail >/dev/null 2>&1; then
  echo "${MAIL_BODY}" | mail -s "${MAIL_SUBJECT}" "${MAIL_TO}" || true
fi

echo "==> Deployment complete"