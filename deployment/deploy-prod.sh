#! /bin/bash

# Fetch from GitHub
cd ~/T-ITS/ITS-Timesheet-Tool
git fetch
git checkout release
git submodule update --init --recursive
git pull

# Publish the application to folder
cd PPMTool
dotnet publish -c Release -f net6.0

# Copy live DB back to source directory and backup
cd ~/
sudo cp /var/www/tits/PPMTool.db  ~/T-ITS/ITS-Timesheet-Tool/PPMTool/
sudo chown mbgm6ah3:users ~/T-ITS/ITS-Timesheet-Tool/PPMTool/PPMTool.db
filename=PPMTool-$(date +"%Y%m%d-%H%M%S").db
sudo cp -a ~/T-ITS/ITS-Timesheet-Tool/PPMTool/PPMTool.db ~/T_ITS_Data_Backup/$filename

# Run migrations
cd ~/T-ITS/ITS-Timesheet-Tool/PPMTool
dotnet tool restore
dotnet ef database update
cp PPMTool.db ./bin/Release/net6.0/publish/

# Publish and restart the kestrel server
cd ~/
./T-ITS/ITS-Timesheet-Tool/deployment/publish.sh

# Send email to say the deployment ran
mail -s "[T-ITS Prod] Deployment Executed" phil.bradbury@manchester.ac.uk <<< "Deployment of T-ITS Prod has just run!"

