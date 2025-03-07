#! /bin/bash

# SPDX-FileCopyrightText: 2025 University of Manchester
#
# SPDX-License-Identifier: apache-2.0

# Publish the application
sudo systemctl stop kestrel-capx.service
sudo cp /var/www/capx/PPMTool.db ~/PPMTool-old.db
sudo cp -rf ~/CapX/PPMTool/bin/Release/net6.0/publish/* /var/www/capx/
sudo systemctl start kestrel-capx.service
sudo systemctl status kestrel-capx.service

# Publish the API
sudo systemctl stop kestrel-capx-api.service
sudo cp -rf ~/CapX/PPMTool.API/bin/Release/net6.0/publish/* /var/www/capx-api/
sudo systemctl start kestrel-capx-api.service
sudo systemctl status kestrel-capx-api.service
