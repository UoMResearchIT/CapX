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

echo "Comparing repository ruleset with RSE Department's defined ruleset.."

red=$(printf '\033[0;31m')
green=$(printf '\033[0;32m')
yellow=$(printf '\033[0;33m')
reset=$(printf '\033[0m')

# Load actual and expected rulesets using jq
ACTUAL_RULES=$(jq -c 'map({type, parameters: (if has("parameters") then .parameters else {} end)})' "$CURRENT_FILE")
EXPECTED_RULES=$(jq -c 'map({type, parameters: (if has("parameters") then .parameters else {} end)})' "$EXPECTED_FILE")

echo ::group::Data to compare
echo "${yellow}Actual Rules:${reset} $ACTUAL_RULES"
echo "${yellow}Expected Rules:${reset} $EXPECTED_RULES"
echo ::endgroup::

# Ensure the rules are properly retrieved
if [[ -z "$ACTUAL_RULES" || "$ACTUAL_RULES" == "null" || -z "$EXPECTED_RULES" || "$EXPECTED_RULES" == "null" ]]; then
    echo "${red}Error:${reset} One of the rulesets is empty or invalid. Check API response."
    exit 1
fi

# Function to compare rules dynamically
compare_rule() {
    local expected_rule=$1
    local actual_rule=$2

    # Extract and compare parameters
    EXPECTED_PARAMS=$(echo "$expected_rule" | jq -c '.parameters // {}')
    ACTUAL_PARAMS=$(echo "$actual_rule" | jq -c '.parameters // {}')

    # Ensure all expected parameters exist in actual (ignore extra ones)
    while read -r param; do
        EXPECTED_VALUE=$(echo "$EXPECTED_PARAMS" | jq -r --arg param "$param" '.[$param]')
        ACTUAL_VALUE=$(echo "$ACTUAL_PARAMS" | jq -r --arg param "$param" '.[$param]')

        if [[ "$EXPECTED_VALUE" != "$ACTUAL_VALUE" ]]; then
            echo "${red}Mismatch${reset} found in parameter '${yellow}$param${reset}':"
            echo "${yellow}Expected:${reset} $EXPECTED_VALUE"
            echo "${yellow}Actual:${reset} $ACTUAL_VALUE"
            return 1
        fi
    done < <(echo "$EXPECTED_PARAMS" | jq -r 'keys[]')

    return 0
}

MATCHED=true

# Compare each expected rule against actual rules
while read -r expected_rule; do
    rule_type=$(echo "$expected_rule" | jq -r '.type')
    actual_rule=$(echo "$ACTUAL_RULES" | jq -c --arg type "$rule_type" '.[] | select(.type == $type)')

    if [[ -z "$actual_rule" ]]; then
        echo "${red}Missing expected rule:${yellow} $rule_type${reset}"
        MATCHED=false
        continue
    fi

    if ! compare_rule "$expected_rule" "$actual_rule"; then
        MATCHED=false
    fi
done < <(echo "$EXPECTED_RULES" | jq -c '.[]')

if [[ "$MATCHED" == false ]]; then
    echo "❌ ${red}Ruleset mismatch${reset} - your Branch Protection Rules do not match with the RSE Department's defined Branch Protection Rules."
    exit 1
fi

echo "✅ ${green}Ruleset matched${reset} with RSE Department's defined Branch Protection Rules"
