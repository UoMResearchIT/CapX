#! /bin/bash

# SPDX-FileCopyrightText: 2026 University of Manchester
#
# SPDX-License-Identifier: Apache-2.0


## Lives on the build/test system but executed on the production system via the deploy-prod script ##

set -eu

# Stop the applications
sudo systemctl stop kestrel-capx.service
sudo systemctl stop kestrel-capx-api.service

# Backup the DB file after flushing the WAL journal
mkdir -p ~/backup
mkdir -p ~/CapX_Data_Backup
sudo cp /var/www/capx/PPMTool.db* ~/backup
sudo chown mbgm6ah3:users ~/backup/PPMTool.db*
sudo sqlite3 ~/backup/PPMTool.db VACUUM;
filename=PPMTool-$(date +"%Y%m%d-%H%M%S").db
sudo cp -a ~/backup/PPMTool.db ~/CapX_Data_Backup/$filename

# Remove WAL files
sudo rm -f /var/www/capx/PPMTool.db-*

# Publish
sudo cp -rf ~/CapX-Prod/PPMTool/bin/Release/net8.0/publish/* /var/www/capx/
sudo cp -rf ~/CapX-Prod/PPMTool.API/bin/Release/net8.0/publish/* /var/www/capx-api/

# Restart
sudo systemctl start kestrel-capx.service
sudo systemctl status kestrel-capx.service
sudo systemctl start kestrel-capx-api.service
sudo systemctl status kestrel-capx-api.service
