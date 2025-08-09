#! /bin/bash
set -eu
sudo cp -f /var/www/capx/PPMTool.db* ./
sudo sqlite3 PPMTool.db VACUUM;
