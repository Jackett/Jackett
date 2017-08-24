#!/bin/bash

# Stop and unload the service if it's running
launchctl remove org.user.Jackett

# Check if we're running from Jackett's directory
if [ ! -f ./JackettConsole.exe ]; then
    echo "Couldn't locate JackettConsole.exe. Are you running from the right directory?"
    exit 1
fi
jackettdir="$(pwd)"

# Check if mono is installed
command -v mono >/dev/null 2>&1 || { echo >&2 "Jackett requires Mono but it's not installed. Aborting."; exit 1; }
monodir="$(dirname $(command -v mono))"

# Check that no other service called Jackett is already running
if [[ $(launchctl list | grep org.user.Jackett) ]]; then
    echo "Jackett already seems to be running as a service. Please stop it before running this script again."
    exit 1
fi

# Write the plist to LaunchAgents
cat >~/Library/LaunchAgents/org.user.Jackett.plist <<EOL
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>EnvironmentVariables</key>
    <dict>
        <key>PATH</key>
        <string>/usr/bin:/bin:/usr/sbin:/sbin:${monodir}</string>
    </dict>
    <key>KeepAlive</key>
    <true/>
    <key>Label</key>
    <string>org.user.Jackett</string>
    <key>ProgramArguments</key>
    <array>
        <string>${monodir}/mono</string>
        <string>--debug</string>
        <string>JackettConsole.exe</string>
        <string>--NoRestart</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
    <key>WorkingDirectory</key>
    <string>${jackettdir}</string>
</dict>
</plist>

EOL

# Run the agent
launchctl load ~/Library/LaunchAgents/org.user.Jackett.plist

# Check that it's running
if [[ $(launchctl list | grep org.user.Jackett) ]]; then
    echo "Agent successfully installed and launched!"
else
    cat << EOL
Could not launch agent. The installation might have failed.
Please open an issue on https://github.com/Jackett/Jackett/issues and paste following information:
Mono directory: \`${monodir}\`
Jackett directory: \`${jackettdir}\`
EOL
fi
