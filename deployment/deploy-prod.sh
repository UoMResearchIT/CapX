#! /bin/bash
set -eu

# Fetch from GitHub
cd ~/CapX-Prod
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

# Sync DB from production and flush WAL journal
cd ~/
sudo scp -i ~/.ssh/id_rsa mbgm6ah3@balex.itservices.manchester.ac.uk:/var/www/capx/PPMTool.db* ~/CapX-Prod/PPMTool/
sudo chown mbgm6ah3:users ~/CapX-Prod/PPMTool/PPMTool.db*
sudo sqlite3 ~/CapX-Prod/PPMTool/PPMTool.db VACUUM;

# Run migrations and copy to publish folder
cd ~/CapX-Prod/PPMTool
dotnet tool restore
set -a
source /var/www/capx/variables.env
set +a
dotnet ef database update
cp PPMTool.db* ./bin/Release/net8.0/publish/

# Copy publish directories over to the production system
cd ~/
rsync -av --exclude='.git' -e "ssh -i ~/.ssh/id_rsa" ~/CapX-Prod mbgm6ah3@balex.itservices.manchester.ac.uk:~/

# Publish and restart the kestrel server on the production system
ssh mbgm6ah3@balex.itservices.manchester.ac.uk 'bash -s' < "/home/mbgm6ah3/publish-prod.sh"

# Send email to say the deployment ran
mail -s "[CapX Prod] Deployment Executed" adrian.harwood@manchester.ac.uk <<< "Deployment of CapX Prod has just run!"

