#!/usr/bin/env bash
set -euo pipefail
TARGET="/opt/dsa-manager/DSA_Manager_Mobile_App"
SOURCE="$(cd "$(dirname "$0")" && pwd)"
STAMP="$(date +%Y%m%d_%H%M%S)"
BACKUP="/opt/dsa-manager/backups/DSA_Manager_source_${STAMP}.tar.gz"
mkdir -p /opt/dsa-manager/backups
if [[ ! -d "$TARGET/backend" || ( ! -f "$TARGET/docker-compose.yml" && ! -f "$TARGET/compose.yml" && ! -f "$TARGET/docker-compose.yaml" ) ]]; then
  echo "Target project/compose not found at $TARGET. No changes made." >&2
  exit 1
fi
# Back up source directories; .env, compose and database volumes are not touched.
tar -czf "$BACKUP" -C "$TARGET" backend frontend
cp -a "$SOURCE/backend" "$TARGET/backend.new"
cp -a "$SOURCE/frontend" "$TARGET/frontend.new"
rm -rf "$TARGET/backend.previous" "$TARGET/frontend.previous"
mv "$TARGET/backend" "$TARGET/backend.previous"
mv "$TARGET/frontend" "$TARGET/frontend.previous"
mv "$TARGET/backend.new" "$TARGET/backend"
mv "$TARGET/frontend.new" "$TARGET/frontend"
cp "$SOURCE/Dockerfile" "$TARGET/Dockerfile"
cp "$SOURCE/.dockerignore" "$TARGET/.dockerignore"
cd "$TARGET"
if ! docker compose build --no-cache api; then
  echo "API build failed. Restoring previous source files..." >&2
  rm -rf "$TARGET/backend" "$TARGET/frontend"
  mv "$TARGET/backend.previous" "$TARGET/backend"
  mv "$TARGET/frontend.previous" "$TARGET/frontend"
  docker compose build api || true
  exit 1
fi
if ! docker compose up -d --no-deps api; then
  echo "API restart failed. Previous source is retained in backend.previous/frontend.previous." >&2
  echo "Source backup: $BACKUP" >&2
  exit 1
fi
echo "Deploy complete. Source backup: $BACKUP"
echo "Database containers/volumes and .env were not removed or recreated."
