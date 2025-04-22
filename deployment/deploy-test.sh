#! /bin/bash

# Fetch from GitHub
cd ~/CapX
git fetch
git checkout dev
git submodule update --init --recursive
git pull

# Publish the application to folder
cd PPMTool
dotnet publish -c Release -f net8.0

# Publish the API to folder
cd ../PPMTool.API
dotnet publish -c Release -f net8.0

# Sync DB from production
cd ~/
sudo scp -i ~/.ssh/id_rsa mbgm6ah3@balex.itservices.manchester.ac.uk:/var/www/capx/PPMTool.db ~/CapX/PPMTool/
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
mail -s "[CapX Test] Deployment Executed" adrian.harwood@manchester.ac.uk <<< "Deployment of CapX Test has just run!"

