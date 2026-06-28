# Heroku Deployment Guide for Jackett

This guide explains how to deploy Jackett to Heroku using its Docker container and Heroku Container Registry.

## Overview
To natively support Heroku's environment constraints, several modifications were made to the core application:
* **Configuration:** Support for the `JACKETT_CONFIG_DIR` environment variable was added to override the default local path.
* **Networking:** Kestrel now dynamically binds to `http://0.0.0.0:${PORT}` matching Heroku's assigned port constraints.
* **Logging:** Introduced the `HEROKU` environment variable. When set, file logging is disabled, relying entirely on stdout/stderr.
* **Health Check:** A lightweight endpoint (`/health`) returning plain text `OK` was introduced.
* **Docker:** A multi-stage `.dockerignore` and `Dockerfile` was created focusing on the lightweight Alpine image.

Heroku uses an **ephemeral filesystem**, meaning any files written locally (like newly downloaded configuration or caches) are discarded during dyno restarts (at least once every 24 hours).

## Requirements
* Heroku account.
* Heroku Team access (if deploying to a team).
* GitHub account.
* Fork of this repository.
* Heroku API Key (available in your Account Settings).
* GitHub Personal Access Token (if deploying a private repository).

## Repository Preparation
1. **Fork the Repository**: Navigate to the upstream Jackett repository and click "Fork".
2. **Keep Upstream Remote**: Make sure you pull updates from the original repository periodically to stay up-to-date.
3. **Select Deployment Branch**: Deploy from a stable branch or `master`.

## Heroku App Creation
1. Access the Heroku Dashboard and select **New > Create new app**.
2. **Naming conventions**: Pick a unique application name, e.g., `my-jackett-instance`.
3. **Choosing a Region**: Select the region closest to where you plan to host your dependent services (e.g., Sonarr/Radarr) to reduce latency.
4. **Team apps**: If using a Heroku Team, ensure it's selected in the owner dropdown.

## Required Config Vars
Heroku provides configuration variables under **Settings > Config Vars**.

* `PORT` - Handled automatically by Heroku. Specifies the internal port.
* `JACKETT_CONFIG_DIR` - If set, tells Jackett where to store configuration. Not strictly required, defaults to standard OS locations.
* `ASPNETCORE_ENVIRONMENT` - Set to `Production`.
* `HEROKU` - Set to `true` to disable persistent file logging and ensure stdout logging.

## Container Deployment
If using the Heroku CLI, you can deploy the container manually:

```bash
# Login to Heroku Container Registry
heroku container:login

# Build the Docker image and push it to Heroku
heroku container:push web -a <your-app-name>

# Release the container
heroku container:release web -a <your-app-name>
```

## Deployment from GitHub
Heroku also supports deploying natively from GitHub via the **Deploy** tab.
1. Connect your GitHub account.
2. Search for your Jackett fork.
3. Enable **Automatic Deploys** for your deployment branch.
4. Trigger a **Manual Deploy** to perform the first build.

## Verification
1. Navigate to `https://<your-app-name>.herokuapp.com/health`. You should see `OK`.
2. Navigate to `https://<your-app-name>.herokuapp.com/` to view the Web UI.
3. Check **View Logs** in the Heroku Dashboard to ensure Jackett started cleanly.

## Updating
When the upstream repository updates:
1. Sync your fork with upstream.
2. If Automatic Deploys are enabled, Heroku will automatically build and deploy the new version.
3. If using CLI, run `git pull` then push the new container manually.

## Troubleshooting
* **Port binding**: Ensure you have NOT manually overridden `ASPNETCORE_URLS` in config vars, which might override the automatic `PORT` binding.
* **Missing config directory**: Jackett automatically creates the data folder if missing.
* **Crash loop**: View logs using `heroku logs --tail -a <your-app-name>`.
* **Build failures**: Ensure you are using the correct branch that has the Dockerfile in the root.
* **Memory limits**: Jackett is typically lightweight, but if you exceed memory limits (Error R14), upgrade the dyno size or reduce the number of active indexers.
* **Dyno restarts**: Dynos restart daily. Due to the ephemeral storage, Jackett configuration changes made in the UI will reset to defaults.
* **Logging**: Ensure `HEROKU=true` is set to funnel logs directly into `heroku logs`.

## FAQ
* **Can I persist Jackett configuration?**
  Heroku's filesystem is ephemeral. To persist settings, configure Jackett locally, then commit your configuration files (`config.json`, `Indexers/`, etc.) into your Git repository before deploying.
