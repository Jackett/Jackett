#!/bin/sh

# Helper script to fix
# https://github.com/Jackett/Jackett/issues/5208#issuecomment-547565515

# Get full Jackett root path
JACKETT_DIR="$(dirname "$(readlink -f "$0")")"

# Launch Jackett (with CLI parameters)
"${JACKETT_DIR}/jackett" --NoRestart "$@"

# Get user running the service
JACKETT_USER=$(whoami)

# Wait until the updater ends
while pgrep -u "${JACKETT_USER}" JackettUpdater > /dev/null; do
    sleep 1
done
