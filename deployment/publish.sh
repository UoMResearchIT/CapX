#! /bin/bash
sudo systemctl stop kestrel-capx.service
sudo cp -rf ~/CapX/PPMTool/bin/Release/net6.0/publish/* /var/www/capx/
sudo systemctl start kestrel-capx.service
sudo systemctl status kestrel-capx.service
