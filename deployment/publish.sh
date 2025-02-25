#! /bin/bash
sudo systemctl stop kestrel-tits.service
sudo cp -rf ~/T-ITS/ITS-Timesheet-Tool/PPMTool/bin/Release/net6.0/publish/* /var/www/tits/
sudo chown www-data:www-data /var/www/tits/PPMTool.db
sudo systemctl start kestrel-tits.service
sudo systemctl status kestrel-tits.service
