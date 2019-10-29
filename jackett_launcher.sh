#!/bin/bash

# Helper script to fix
# https://github.com/Jackett/Jackett/issues/5208#issuecomment-547565515

JACKETT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"

# Launch Jackett
${JACKETT_DIR}/jackett

# Get user running the service
JACKETT_USER=$(whoami)

# Wait until the updater ends
while pgrep -u ${JACKETT_USER} JackettUpdater > /dev/null ; do
     sleep 1
done

echo "Jackett update complete" 
