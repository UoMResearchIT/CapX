#! /bin/bash

# Fetch from GitHub
cd ~/CapX
git fetch
git checkout release
git pull

# Publish the application to folder
cd PPMTool
dotnet publish -c Release

# Copy live DB back to source directory
cd ~/
sudo cp /var/www/capx/PPMTool.db  ~/CapX/PPMTool/
sudo chown mbgm6ah3:users ~/CapX/PPMTool/PPMTool.db

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

