#! /bin/bash

cd ~/CapX
git fetch
git checkout dev
output=$(git rev-list --left-right --count HEAD...@{upstream} | cut -f2)

if [ "$output" -gt 0 ]; then
    # Pull and redeploy
    echo "Pulling and deploying"
	../deployment/deploy-prod.sh
else
    # No need to redeploy
    echo "Up-to-date on dev branch so no need to redeploy"
    exit 1
fi
