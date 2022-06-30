#!/bin/bash

# If you have problems installing Jackett, please open an issue on
# https://github.com/Jackett/Jackett/issues

# Setting up colors
BOLDRED="$(printf '\033[1;31m')"
BOLDGREEN="$(printf '\033[1;32m')"
NC="$(printf '\033[0m')" # No Color

# Check if the install script is running as root
if [ "$EUID" -ne 0 ]; then
    echo "${BOLDRED}ERROR${NC}: Please run this script as root"
    exit 1
fi

# Check if Jackett service is running
JACKETT_SERVICE="jackett.service"
echo "Checking if the service '${JACKETT_SERVICE}' is running ..."
if systemctl is-active --quiet "${JACKETT_SERVICE}"; then
    echo "Service '${JACKETT_SERVICE}' is running"

    # Stop and unload the service
    if systemctl stop "${JACKETT_SERVICE}"; then
        echo "Service '${JACKETT_SERVICE}' stopped"
    else
        echo "${BOLDRED}ERROR${NC}: The service '${JACKETT_SERVICE}' Can not be stopped"
        exit 1
    fi

else
    echo "Service '${JACKETT_SERVICE}' is not running"
fi

# Move working directory to Jackett's
JACKETT_DIR="$(dirname "$(readlink -f "$0")")"
echo "Jackett will be installed in '${JACKETT_DIR}'"
if ! cd "${JACKETT_DIR}"; then
    echo "${BOLDRED}ERROR${NC}: Can not cd into '${JACKETT_DIR}' folder"
    exit 1
fi

# Check if we're running from Jackett's directory
if [ ! -f ./jackett ]; then
    echo "${BOLDRED}ERROR${NC}: Can not locate 'jackett' file in '${JACKETT_DIR}'."
    echo "Is the script in the right directory?"
    exit 1
fi

# Check if Jackett's owner is root
JACKETT_USER="$(stat -c "%U" ./jackett)"
if [ "${JACKETT_USER}" == "root" ] || [ "${JACKETT_USER}" == "UNKNOWN" ] ; then
    echo "${BOLDRED}ERROR${NC}: The owner of Jackett directory is '${JACKETT_USER}'."
    echo "Please, change the owner with the command 'chown <user>:<user> -R \"${JACKETT_DIR}\"'"
    echo "The user <user> will be used to run Jackett."
    exit 1
fi
echo "Jackett will be executed with the user '${JACKETT_USER}'"

# Write the systemd service descriptor
JACKETT_SERVICE_PATH="/etc/systemd/system/${JACKETT_SERVICE}"
echo "Creating Jackett unit file in '${JACKETT_SERVICE_PATH}' ..."
cat > "${JACKETT_SERVICE_PATH}" <<EOL
[Unit]
Description=Jackett Daemon
After=network.target

[Service]
SyslogIdentifier=jackett
Restart=always
RestartSec=5
Type=simple
User=${JACKETT_USER}
Group=${JACKETT_USER}
WorkingDirectory=${JACKETT_DIR}
ExecStart=/bin/sh "${JACKETT_DIR}/jackett_launcher.sh"
TimeoutStopSec=30

[Install]
WantedBy=multi-user.target

EOL
if [ $? -ne 0 ]; then
    echo "${BOLDRED}ERROR${NC}: Can not create the file '${JACKETT_SERVICE_PATH}'"
    echo "The UnitPath of systemd changes from one distribution to another. You may have to edit the script and change the path manually."
    exit 1
fi

echo "Installing Jackett service ..."
# Reload systemd daemon
if ! systemctl daemon-reload; then
    echo "${BOLDRED}ERROR${NC}: Can not reload systemd daemon"
    exit 1
fi

# Enable the service for following restarts
if ! systemctl enable "${JACKETT_SERVICE}"; then
    echo "${BOLDRED}ERROR${NC}: Can not enable the service '${JACKETT_SERVICE}'"
    exit 1
fi

# Run the service
if systemctl start "${JACKETT_SERVICE}"; then
    echo "${BOLDGREEN}Service successfully installed and launched!${NC}"
else
    echo "${BOLDRED}ERROR${NC}: Can not start the service '${JACKETT_SERVICE}'"
    exit 1
fi
