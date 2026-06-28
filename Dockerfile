FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /source

# Copy the source code
COPY . .

# Publish the application targeting net9.0
RUN dotnet publish src/Jackett.Server/Jackett.Server.csproj -c Release -f net9.0 -r linux-musl-x64 -o /app/publish /p:PublishTrimmed=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
WORKDIR /app

# Install required dependencies
RUN apk add --no-cache tzdata icu-libs zlib

# We need to set this environment variable so that the .NET runtime can use the icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Copy the published output from the build stage
COPY --from=build /app/publish .

# Expose default port
EXPOSE 9117

# Create non-root user
RUN addgroup -S jackett && adduser -S jackett -G jackett && \
    chown -R jackett:jackett /app

USER jackett

# Run the application
# --NoUpdates prevents the built-in updater from trying to mutate the container's read-only files
CMD ["./jackett", "--NoUpdates"]
