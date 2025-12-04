#!/usr/bin/env bash
# Build and install BitCrafts.Certificates on AlmaLinux/RHEL-like systems.
# Usage: sudo ./deploy_almalinux.sh [--no-self-contained] [--rid RID] [--install-apache] [--selinux]
# Default behavior: self-contained publish with auto-detected RID, no apache install, apply SELinux fixes if requested.
set -euo pipefail

SERVICE_NAME="bitcrafts-certificates"
SERVICE_USER="bitcrafts"
SERVICE_GROUP="$SERVICE_USER"
INSTALL_DIR="/opt/bitcrafts/certificates"
DATA_DIR="/srv/bitcrafts/certificates"
ENV_DIR="/etc/bitcrafts"
ENV_FILE="$ENV_DIR/certificates.env"
REPO_ROOT="$(pwd)"
PROJECT_PATH="$REPO_ROOT/BitCrafts.Certificates/BitCrafts.Certificates.csproj"
PUBLISH_DIR="$INSTALL_DIR"

# Defaults
SELF_CONTAINED=true
RID=""
INSTALL_APACHE=false
ENABLE_SELINUX=false

usage() {
  cat <<EOF
Usage: sudo $0 [options]
Options:
  --no-self-contained   Build framework-dependent (uses system dotnet runtime)
  --rid RID             Specify runtime identifier (e.g. linux-x64, linux-arm64)
  --install-apache      Install and enable httpd as reverse proxy (Apache on RHEL)
  --selinux             Apply SELinux file contexts and booleans (best-effort)
  -h|--help             Show this help
EOF
}

# Parse args
while [[ $# -gt 0 ]]; do
  case "$1" in
    --no-self-contained) SELF_CONTAINED=false; shift ;;
    --rid) RID="$2"; shift 2 ;;
    --install-apache) INSTALL_APACHE=true; shift ;;
    --selinux) ENABLE_SELINUX=true; shift ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Unknown option: $1"; usage; exit 1 ;;
  esac
done

if [ "$EUID" -ne 0 ]; then
  echo "This script must be run as root (or with sudo)." >&2
  exit 2
fi

# Detect RID if needed
if [ -z "$RID" ]; then
  ARCH=$(uname -m)
  case "$ARCH" in
    x86_64|amd64) RID="linux-x64" ;;
    aarch64|arm64) RID="linux-arm64" ;;
    *) echo "Unknown architecture '$ARCH' - please pass --rid <RID>" >&2; exit 3 ;;
  esac
fi

# Check for dotnet (we may still need it for framework-dependent or SDK publish)
if ! command -v dotnet >/dev/null 2>&1; then
  if [ "$SELF_CONTAINED" = true ]; then
    echo "dotnet not found, but self-contained publish will be performed on the build host. Ensure build host has dotnet SDK." >&2
  else
    echo "ERROR: 'dotnet' not found. Install .NET runtime (or SDK) before running this script in framework-dependent mode." >&2
    exit 4
  fi
fi

# Idempotent user creation
if ! id -u "$SERVICE_USER" >/dev/null 2>&1; then
  useradd --system --no-create-home --shell /sbin/nologin --user-group "$SERVICE_USER"
fi

# Prepare directories
mkdir -p "$INSTALL_DIR"
mkdir -p "$DATA_DIR"
mkdir -p "$ENV_DIR"
chown -R "$SERVICE_USER":"$SERVICE_GROUP" "$INSTALL_DIR" "$DATA_DIR" || true
chmod 750 "$INSTALL_DIR" "$DATA_DIR" || true

# Publish the dotnet app
echo "Publishing the app to $PUBLISH_DIR (self-contained=$SELF_CONTAINED, RID=$RID)"
# Prepare publish directory: remove previous contents but keep directory itself
if [ -d "$PUBLISH_DIR" ]; then
  rm -rf "${PUBLISH_DIR:?}/"*
else
  mkdir -p "$PUBLISH_DIR"
fi

if [ "$SELF_CONTAINED" = true ]; then
  echo "Running self-contained publish for RID=$RID"
  dotnet publish "$PROJECT_PATH" -c Release -r "$RID" --self-contained true -o "$PUBLISH_DIR"
  # Ensure the main binary is executable
  if [ -f "$PUBLISH_DIR/BitCrafts.Certificates" ]; then
    chmod +x "$PUBLISH_DIR/BitCrafts.Certificates"
  fi
else
  echo "Running framework-dependent publish (requires dotnet runtime on target)"
  dotnet publish "$PROJECT_PATH" -c Release -o "$PUBLISH_DIR"
fi

# Ensure ownership and permissions for published files
chown -R "$SERVICE_USER":"$SERVICE_GROUP" "$PUBLISH_DIR"
chmod -R 750 "$PUBLISH_DIR"

# Make sure run wrapper exists and is executable
if [ ! -f "$PUBLISH_DIR/run.sh" ]; then
  cp "$REPO_ROOT/deploy/run.sh" "$PUBLISH_DIR/run.sh"
fi
chown "$SERVICE_USER":"$SERVICE_GROUP" "$PUBLISH_DIR/run.sh"
chmod 750 "$PUBLISH_DIR/run.sh"

# Create environment file (do NOT put secrets here). Owned by root and group-owned by service user.
if [ ! -f "$ENV_FILE" ]; then
  cat > "$ENV_FILE" <<EOF
# Environment for BitCrafts Certificates service
ASPNETCORE_ENVIRONMENT=Production
# Application data dir (where CA keys, DB and logs are stored)
BITCRAFTS_DATA_DIR=$DATA_DIR
# Bind to localhost only; use apache or other proxy for TLS in front
ASPNETCORE_URLS=http://127.0.0.1:5000
# Disable dotnet telemetry message
DOTNET_TELEMETRY_OPTOUT=1
EOF
  chmod 0640 "$ENV_FILE"
  chown root:"$SERVICE_GROUP" "$ENV_FILE"
else
  echo "Using existing env file: $ENV_FILE"
fi

# Install systemd unit (backup first if different)
UNIT_PATH="/etc/systemd/system/$SERVICE_NAME.service"
if [ -f "$UNIT_PATH" ]; then
  if ! cmp -s "$REPO_ROOT/deploy/bitcrafts.service" "$UNIT_PATH"; then
    echo "Backing up existing unit to ${UNIT_PATH}.bak"
    cp "$UNIT_PATH" "${UNIT_PATH}.bak"
    cp "$REPO_ROOT/deploy/bitcrafts.service" "$UNIT_PATH"
  else
    echo "Systemd unit already identical; leaving in place."
  fi
else
  cp "$REPO_ROOT/deploy/bitcrafts.service" "$UNIT_PATH"
fi
chmod 0644 "$UNIT_PATH"

echo "Reloading systemd and enabling service..."
systemctl daemon-reload
systemctl enable "$SERVICE_NAME" || true
systemctl restart "$SERVICE_NAME" || true

# Optional: install/enable Apache (httpd) reverse proxy
if [ "$INSTALL_APACHE" = true ]; then
  echo "Installing and configuring Apache (httpd) reverse-proxy"
  # Install httpd if missing
  if ! command -v httpd >/dev/null 2>&1; then
    dnf install -y httpd mod_proxy mod_proxy_http
  fi
  # Install the conf
  APACHE_CONF_PATH="/etc/httpd/conf.d/bitcrafts.conf"
  if [ ! -f "$APACHE_CONF_PATH" ]; then
    cp "$REPO_ROOT/deploy/apache/bitcrafts.conf" "$APACHE_CONF_PATH"
  fi
  systemctl enable --now httpd
fi

# Optional: SELinux adjustments (best-effort)
if [ "$ENABLE_SELINUX" = true ]; then
  if command -v getenforce >/dev/null 2>&1 && [ "$(getenforce)" = "Enforcing" ]; then
    echo "SELinux is enforcing; applying best-effort file contexts and booleans"
    # Allow httpd to connect to network
    setsebool -P httpd_can_network_connect on || true
    # Ensure semanage exists or install it
    if ! command -v semanage >/dev/null 2>&1; then
      echo "Installing policycoreutils-python-utils for semanage"
      dnf install -y policycoreutils-python-utils || true
    fi
    if command -v semanage >/dev/null 2>&1; then
      semanage fcontext -a -t bin_t "$INSTALL_DIR(/.*)?" || true
      restorecon -Rv "$INSTALL_DIR" || true
    else
      echo "semanage not available; skipping fcontext adjustments."
    fi
  else
    echo "SELinux not enforcing or getenforce unavailable; skipping SELinux steps."
  fi
fi

# SELinux note for admin
if [ "$ENABLE_SELINUX" = true ]; then
  echo "If SELinux is enforcing you should verify file contexts and allow httpd_can_network_connect is set."
fi

# Final status
echo "Installation complete. Service status:"
systemctl status --no-pager "$SERVICE_NAME"

echo "Notes:"
echo " - The application install directory is: $PUBLISH_DIR"
echo " - Data directory is: $DATA_DIR (owned by $SERVICE_USER)"
echo " - Environment file: $ENV_FILE (permissions 0640)"
echo " - Systemd unit: $UNIT_PATH"
echo " - To enable Apache reverse-proxy, run this script with --install-apache"

echo "Security reminder: do NOT store secrets in the service unit. Consider using a secrets manager or placing the Root CA key into a secure HSM in production."
