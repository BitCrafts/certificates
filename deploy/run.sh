#!/usr/bin/env bash
# Run wrapper that starts the app using a self-contained binary if present, otherwise uses 'dotnet' on the dll.
set -euo pipefail
APP_DIR="/opt/bitcrafts/certificates"
SELF_CONTAINED_BINARY="$APP_DIR/BitCrafts.Certificates"
DLL_PATH="$APP_DIR/BitCrafts.Certificates.dll"
ENV_FILE="/etc/bitcrafts/certificates.env"

# Load env file if present
if [ -f "$ENV_FILE" ]; then
  # shellcheck disable=SC1090
  . "$ENV_FILE"
fi

cd "$APP_DIR"

if [ -x "$SELF_CONTAINED_BINARY" ]; then
  exec "$SELF_CONTAINED_BINARY"
elif [ -f "$DLL_PATH" ]; then
  exec /usr/bin/dotnet "$DLL_PATH"
else
  echo "ERROR: No application binary found in $APP_DIR" >&2
  exit 1
fi

