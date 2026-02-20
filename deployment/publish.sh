#! /bin/bash

# SPDX-FileCopyrightText: 2026 University of Manchester
#
# SPDX-License-Identifier: Apache-2.0
# SPDX-License-Identifier: apache-2.0

set -eu

# Stop the applications
sudo systemctl stop kestrel-capx.service
sudo systemctl stop kestrel-capx-api.service

# Backup locally removing old files first
sudo rm -rf ~/PPMTool.db*
sudo cp /var/www/capx/PPMTool.db* ~/
sudo chown mbgm6ah3:users ~/PPMTool.db*
sudo sqlite3 ~/PPMTool.db VACUUM;
filename=PPMTool-old.db
sudo cp -a ~/PPMTool.db ~/$filename

# Remove WAL files
sudo rm -f /var/www/capx/PPMTool.db-*

# Publish the built versions
sudo cp -rf ~/CapX/PPMTool/bin/Release/net8.0/publish/* /var/www/capx/
sudo cp -rf ~/CapX/PPMTool.API/bin/Release/net8.0/publish/* /var/www/capx-api/

# Restart the services
sudo systemctl start kestrel-capx.service
sudo systemctl status kestrel-capx.service
sudo systemctl start kestrel-capx-api.service
sudo systemctl status kestrel-capx-api.service
