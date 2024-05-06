#!/bin/bash

# Define the directory where Jackett was installed
INSTALL_DIR1="/opt/Jackett"
INSTALL_DIR2="/opt/jackett"

# Define the systemd service file for Jackett
JACKETT_SERVICE_PATH="/etc/systemd/system/jackett.service"

# Ensure the script is running with superuser privileges
if [ "$(id -u)" -ne 0 ]; then
  echo "This script must be run as root. Try using 'sudo bash $0'."
  exit 1
fi

echo "Starting Jackett uninstallation..."

# Stop the Jackett service
echo "Stopping the Jackett service..."
if systemctl stop jackett.service; then
  echo "Jackett service stopped successfully."
else
  echo "Failed to stop the Jackett service. It may not have been running."
fi

# Disable the Jackett service
echo "Disabling the Jackett service..."
if systemctl disable jackett.service; then
  echo "Jackett service disabled successfully."
else
  echo "Failed to disable the Jackett service."
fi

# Remove the systemd service file
echo "Removing the systemd service file..."
rm -vf "$JACKETT_SERVICE_PATH"

# Reload systemd to remove traces of the Jackett service
echo "Reloading systemd daemon..."
systemctl daemon-reload

# Remove the Jackett installation directory
echo "Removing Jackett installation directory..."
rm -rf "$INSTALL_DIR1"
rm -rf "$INSTALL_DIR2"

echo "Jackett uninstallation finished."
