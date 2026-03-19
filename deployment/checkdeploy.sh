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

# Location of the repo root
REPO_DIR="${HOME}/CapX"

# Checkout the correct branch with potential submodules
cd "${REPO_DIR}"
git fetch
if [[ "$ENVIRONMENT" == "dev" ]]; then
  BRANCH="dev"
else
  BRANCH="release"
fi
git switch "$BRANCH"
git submodule update --init --recursive

# Compare with upstream dev
output=$(git rev-list --left-right --count HEAD...@{upstream} | cut -f2 || echo 0)

if [ "${output}" -gt 0 ]; then
  # Run the dploy script with the same environment
  echo "Pulling and deploying as changes detected on $BRANCH"
  "${REPO_DIR}/deployment/deploy.sh ${ENVIRONMENT}"
else
  echo "Up-to-date on $BRANCH branch; no redeploy"
  exit 0
fi