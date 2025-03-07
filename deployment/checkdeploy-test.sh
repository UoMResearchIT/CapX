#! /bin/bash

# SPDX-FileCopyrightText: 2025 University of Manchester
#
# SPDX-License-Identifier: apache-2.0

cd ~/CapX
git fetch
git checkout dev
output=$(git rev-list --left-right --count HEAD...@{upstream} | cut -f2)

if [ "$output" -gt 0 ]; then
    # Pull and redeploy
    echo "Pulling and deploying"
	../deployment/deploy-test.sh
else
    # No need to redeploy
    echo "Up-to-date on dev branch so no need to redeploy"
    exit 1
fi
