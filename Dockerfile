FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /source

# Copy the solution file and projects to optimize Docker layer caching
# This ensures that `dotnet restore` runs only when dependencies change, not every time source code changes.
COPY src/Jackett.Common/Jackett.Common.csproj src/Jackett.Common/
COPY src/DateTimeRoutines/DateTimeRoutines.csproj src/DateTimeRoutines/
COPY src/Jackett.Server/Jackett.Server.csproj src/Jackett.Server/

# Restore dependencies for the Server project
# This will also restore referenced projects (Common, DateTimeRoutines)
RUN dotnet restore src/Jackett.Server/Jackett.Server.csproj -r linux-musl-x64

# Copy the remaining source files
COPY . .

# Run clean to ensure no conflicting artifacts are left over before publishing
RUN dotnet clean src/Jackett.Server/Jackett.Server.csproj -c Release

# Publish the application with parallel compilation disabled
# /m:1 disables MSBuild multiprocessor compilation.
# /p:UseSharedCompilation=false disables the MSBuild node reuse.
# These flags together eliminate the file-locking ("The process cannot access the file... because it is being used by another process")
# issue that can occur in constrained Docker build environments like Heroku's builder.
RUN dotnet publish src/Jackett.Server/Jackett.Server.csproj \
    -c Release \
    -f net9.0 \
    -r linux-musl-x64 \
    --no-restore \
    -o /app/publish \
    /p:PublishTrimmed=false \
    /m:1 \
    /p:UseSharedCompilation=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
WORKDIR /app

# Install required dependencies
RUN apk add --no-cache tzdata icu-libs zlib

# We need to set this environment variable so that the .NET runtime can use the icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Copy the published output from the build stage
COPY --from=build /app/publish .

# Create non-root user
RUN addgroup -S jackett && adduser -S jackett -G jackett && \
    chown -R jackett:jackett /app

USER jackett

# Run the application
# --NoUpdates prevents the built-in updater from trying to mutate the container's read-only files
CMD ["./jackett", "--NoUpdates"]
