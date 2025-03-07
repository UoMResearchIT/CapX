#!/usr/bin/env bash
set -e

# Copyright (c) 2025 The University of Manchester
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     https://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

# Generate random file name; from https://stackoverflow.com/a/2794106/301832
OUTPUT_FILE=''
for i in {0..7}; do
    OUTPUT_FILE+=$(printf "%x" $(($RANDOM%16)) )
done
# Add directory and suffix
OUTPUT_FILE=$(realpath $OUTPUT_FILE.json)

# Fetch default branch dynamically
DEFAULT_BRANCH=$(gh repo view --json defaultBranchRef --jq .defaultBranchRef.name)
echo "Default branch detected: $DEFAULT_BRANCH"
echo "Fetching ruleset for repository: $REPO_NAME, branch: $DEFAULT_BRANCH..."

# Fetch the response and save it as 'ruleset_response.json'
gh api "https://api.github.com/repos/$REPO_NAME/rules/branches/$DEFAULT_BRANCH" > $OUTPUT_FILE

echo "::group::Response data"
jq . < $OUTPUT_FILE
echo "::endgroup::"

echo "file=$OUTPUT_FILE" >> $GITHUB_OUTPUT

# Check if the response is empty or not an array
RULESET_COUNT=$(jq length < $OUTPUT_FILE)
if [[ "$RULESET_COUNT" -eq 0 ]]; then
    echo "No branch protection rules are found for repository $REPO_NAME on branch $DEFAULT_BRANCH."
    echo "Please define branch protection rules for $REPO_NAME repository."
    exit 1
fi
