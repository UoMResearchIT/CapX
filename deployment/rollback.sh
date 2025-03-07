#!/bin/bash

# Function to display help
show_help() {
    echo "Usage: $0 -m <migration> -t <tag>"
    echo ""
    echo "Options:"
    echo "  -m, --migration   The migration to roll back to (e.g., 20250109132755_RemoveZeroTimesheetEntries)"
    echo "  -t, --tag         The git tag to check out (e.g., Release_v1.12.3)"
    echo "  -h, --help        Display this help message"
}

# Parse command line arguments
while [[ "$#" -gt 0 ]]; do
    case $1 in
        -m|--migration) migration="$2"; shift ;;
        -t|--tag) tag="$2"; shift ;;
        -h|--help) show_help; exit 0 ;;
        *) echo "Unknown parameter passed: $1"; show_help; exit 1 ;;
    esac
    shift
done

# Check if both migration and tag are provided
if [ -z "$migration" ] || [ -z "$tag" ]; then
    echo "Error: Both migration and tag must be provided."
    show_help
    exit 1
fi

# Copy the DB from production and roll back to migration
if sudo cp /var/www/capx/PPMTool.db ~/CapX/PPMTool/; then
    sudo chown mbgm6ah3:users ~/CapX/PPMTool/PPMTool.db
else
    echo "Failed to copy the database."
    exit 1
fi

cd ~/CapX/PPMTool || exit
dotnet tool restore
dotnet ef database update "$migration"

# Check out tag to roll back to
cd ~/CapX || exit
git fetch
git checkout "$tag"
git submodule update --init --recursive
git pull

# Build the rolled back application
cd PPMTool || exit
dotnet publish -c Release -f net6.0

# Build the rolled back API
cd ../PPMTool.API || exit
dotnet publish -c Release -f net6.0

# Copy rolled back DB to publish folder and deploy
cp ~/CapX/PPMTool/PPMTool.db ~/CapX/PPMTool/bin/Release/net6.0/publish/
cd ~/
./publish.sh