#! /bin/bash

# Stop the applications
sudo systemctl stop kestrel-capx.service
sudo systemctl stop kestrel-capx-api.service

# Flush WAL journals and backup locally
sudo sqlite3 /var/www/capx/PPMTool.db VACUUM;
sudo cp /var/www/capx/PPMTool.db ~/PPMTool-old.db

# Publish the built versions
sudo cp -rf ~/CapX/PPMTool/bin/Release/net8.0/publish/* /var/www/capx/
sudo cp -rf ~/CapX/PPMTool.API/bin/Release/net8.0/publish/* /var/www/capx-api/

# Restart the services
sudo systemctl start kestrel-capx.service
sudo systemctl status kestrel-capx.service
sudo systemctl start kestrel-capx-api.service
sudo systemctl status kestrel-capx-api.service
