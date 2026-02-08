# Build stage: .NET 9 SDK required (see CONTRIBUTING.md / azure-pipelines.yml)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy repo (build context = repository root)
COPY . .

# Build DateTimeRoutines then publish Jackett.Server (self-contained Linux)
RUN dotnet build "src/DateTimeRoutines/DateTimeRoutines.csproj" -c Release -f netstandard2.0
RUN dotnet publish "src/Jackett.Server/Jackett.Server.csproj" \
    -c Release \
    -f net9.0 \
    -r linux-x64 \
    --self-contained \
    -o /app

# Run stage: minimal runtime (self-contained app has all .NET bits)
FROM mcr.microsoft.com/dotnet/runtime-deps:9.0-bookworm-slim
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

# Jackett UI (same as linuxserver/jackett)
EXPOSE 9117

# Config persisted at /config (mount a volume); same as linuxserver/jackett
ENV XDG_CONFIG_HOME=/config

ENTRYPOINT ["./jackett", "--DataFolder=/config"]
