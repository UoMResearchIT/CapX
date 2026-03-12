#! /bin/bash
set -eu -o pipefail

# Location of the repo root
REPO_DIR="${HOME}/CapX"

cd "${REPO_DIR}"
git fetch
git checkout dev

# Compare with upstream dev
output=$(git rev-list --left-right --count HEAD...@{upstream} | cut -f2 || echo 0)

if [ "${output}" -gt 0 ]; then
    echo "Pulling and deploying (changes detected on dev)"
    "${REPO_DIR}/deployment/deploy.sh"
else
    echo "Up-to-date on dev branch; no redeploy"
    exit 0
fi