#! /bin/bash

# SPDX-FileCopyrightText: 2026 University of Manchester
#
# SPDX-License-Identifier: Apache-2.0

set -eu

# Define source and destination paths
SRC_DIR="/var/www/capx"
DEST_DIR="$HOME/Database_Hourlies"

# Create destination directory if it doesn't exist
mkdir -p "$DEST_DIR"

# Backup the SQLite DB
sqlite3 "$SRC_DIR/PPMTool.db" ".backup '$DEST_DIR/PPMTool.db'"
if [ $? -ne 0 ]; then
    echo "BACKUP failed. Aborting backup." >&2
    exit 1
fi

# Vacuum the database to flush WAL and SHM and shrink
sqlite3 "$DEST_DIR/PPMTool.db" "VACUUM;"
if [ $? -ne 0 ]; then
    echo "VACUUM failed. Aborting backup." >&2
    exit 1
fi

# Generate timestamped filename
TIMESTAMP=$(date +"%Y%m%d-%H%M%S")
FINAL_DB="$DEST_DIR/PPMTool-$TIMESTAMP.db"

# Move the vacuumed database to the final backup file
mv "$DEST_DIR/PPMTool.db" "$FINAL_DB"

# Remove older backups if more than 72 exist
cd "$DEST_DIR"
BACKUP_COUNT=$(ls -1 PPMTool-*.db 2>/dev/null | wc -l)
if [ "$BACKUP_COUNT" -gt 72 ]; then
    REMOVE_COUNT=$((BACKUP_COUNT - 72))
    ls -1t PPMTool-*.db | tail -n "$REMOVE_COUNT" | xargs rm -f
fi