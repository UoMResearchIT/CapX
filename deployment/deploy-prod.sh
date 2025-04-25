#! /bin/bash

# Fetch from GitHub
cd ~/CapX
git fetch
git checkout release
git submodule update --init --recursive
git pull

# Publish the application to folder
cd PPMTool
dotnet publish -c Release -f net8.0

# Publish the API to folder
cd ../PPMTool.API
dotnet publish -c Release -f net8.0

# Copy live DB back to source directory and backup
cd ~/
sudo cp /var/www/capx/PPMTool.db  ~/CapX/PPMTool/
sudo chown mbgm6ah3:users ~/CapX/PPMTool/PPMTool.db
filename=PPMTool-$(date +"%Y%m%d-%H%M%S").db
sudo cp -a ~/CapX/PPMTool/PPMTool.db ~/CapX_Data_Backup/$filename

# Run migrations
cd ~/CapX/PPMTool
dotnet tool restore
dotnet ef database update
cp PPMTool.db ./bin/Release/net6.0/publish/

# Publish and restart the kestrel server
cd ~/
./publish.sh

# Send email to say the deployment ran
mail -s "[CapX Prod] Deployment Executed" adrian.harwood@manchester.ac.uk <<< "Deployment of CapX Prod has just run!"

