#! /bin/bash
set -eu

cd CapX-Prod
git fetch
git checkout release
output=$(git rev-list --left-right --count HEAD...@{upstream} | cut -f2)

if [ "$output" -gt 0 ]; then
    # Pull and redeploy
    echo "Pulling and deploying production..."
	../deploy-prod.sh
else
    # No need to redeploy
    echo "Up-to-date on release branch so no need to redeploy"
    exit 1
fi
