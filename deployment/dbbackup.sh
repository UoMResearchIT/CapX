#!/bin/bash

# Define source and destination paths
SRC_DIR="/var/www/capx"
DEST_DIR="$HOME/Database_Hourlies"

# Create destination directory if it doesn't exist
mkdir -p "$DEST_DIR"

# Copy the SQLite database and associated WAL and SHM files
cp "$SRC_DIR/PPMTool.db"* "$DEST_DIR/"

# Vacuum the database to flush WAL and SHM
sqlite3 "$DEST_DIR/PPMTool.db" "VACUUM;"

# Generate timestamped filename
TIMESTAMP=$(date +"%Y%m%d-%H%M%S")
FINAL_DB="$DEST_DIR/PPMTool-$TIMESTAMP.db"

# Move the vacuumed database to the final backup file
mv "$DEST_DIR/PPMTool.db" "$FINAL_DB"

# Remove older backups if more than 48 exist
cd "$DEST_DIR"
BACKUP_COUNT=$(ls -1 PPMTool-*.db 2>/dev/null | wc -l)
if [ "$BACKUP_COUNT" -gt 48 ]; then
    REMOVE_COUNT=$((BACKUP_COUNT - 48))
    ls -1t PPMTool-*.db | tail -n "$REMOVE_COUNT" | xargs rm -f
fi
