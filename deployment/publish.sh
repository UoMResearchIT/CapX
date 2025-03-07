#! /bin/bash

# SPDX-FileCopyrightText: 2025 University of Manchester
#
# SPDX-License-Identifier: apache-2.0

sudo systemctl stop kestrel-tits.service
sudo cp /var/www/tits/PPMTool.db ~/PPMTool-old.db
sudo cp -rf ~/T-ITS/ITS-Timesheet-Tool/PPMTool/bin/Release/net6.0/publish/* /var/www/tits/
sudo systemctl start kestrel-tits.service
sudo systemctl status kestrel-tits.service
