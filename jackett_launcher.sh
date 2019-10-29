#!/bin/bash

# Helper script to fix
# https://github.com/Jackett/Jackett/issues/5208#issuecomment-547565515

JACKETT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"

# Launch Jackett
${JACKETT_DIR}/jackett

# Wait until the updater ends
while pgrep JackettUpdater > /dev/null ; do
     sleep 1
done

echo "Jackett update complete" 
