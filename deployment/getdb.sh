#! /bin/bash

# SPDX-FileCopyrightText: 2026 University of Manchester
#
# SPDX-License-Identifier: apache-2.0

set -eu
sudo cp -f /var/www/capx/capx-state/PPMTool.db* ./
sudo sqlite3 PPMTool.db VACUUM;
