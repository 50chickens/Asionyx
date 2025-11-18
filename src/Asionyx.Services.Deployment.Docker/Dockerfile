# Dockerfile for Asionyx.Services.Deployment + SystemD emulator (for integration tests)
# Use Debian Trixie and avoid Microsoft container images; install dotnet packages from Microsoft feed instead

# Builder stage: Debian + dotnet SDK to publish apps
FROM debian:trixie-slim AS builder
WORKDIR /src

# Install prerequisites and Microsoft package feed to get dotnet SDK
RUN apt-get update \
	&& apt-get install -y --no-install-recommends wget ca-certificates gnupg apt-transport-https \
	&& wget -qO /tmp/packages-microsoft-prod.deb https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb \
	&& dpkg -i /tmp/packages-microsoft-prod.deb \
	&& apt-get update \
	&& apt-get install -y --no-install-recommends dotnet-sdk-9.0 \
	&& rm -rf /var/lib/apt/lists/* /tmp/packages-microsoft-prod.deb

# Copy repository from build context (expect build context to be repository root)
COPY . /src

# Publish projects
RUN dotnet publish src/Asionyx.Services.Deployment -c Release -o /app/deployment
RUN dotnet publish src/Asionyx.Services.Deployment.SystemD -c Release -o /app/systemd
RUN dotnet publish src/Asionyx.Services.HelloWorld -c Release -o /app/helloworld


# Base runtime image: Debian + .NET runtime + PowerShell (these are runtime dependencies only)
FROM debian:trixie-slim AS base
WORKDIR /app

RUN apt-get update \
	&& apt-get install -y --no-install-recommends wget ca-certificates gnupg apt-transport-https \
	&& wget -qO /tmp/packages-microsoft-prod.deb https://packages.microsoft.com/config/debian/12/packages-microsoft-prod.deb \
	&& dpkg -i /tmp/packages-microsoft-prod.deb \
	&& apt-get update \
	&& apt-get install -y --no-install-recommends dotnet-runtime-9.0 powershell \
	&& rm -rf /var/lib/apt/lists/* /tmp/packages-microsoft-prod.deb


# Final integration image derives from base and contains only published outputs
FROM base AS final
WORKDIR /app

# Copy published outputs from builder
COPY --from=builder /app/deployment ./deployment
COPY --from=builder /app/systemd ./systemd
COPY --from=builder /app/helloworld ./helloworld

# Copy entrypoint script (from repo path relative to build context)
COPY src/Asionyx.Services.Deployment.Docker/entrypoint.sh /app/entrypoint.sh
RUN chmod +x /app/entrypoint.sh

EXPOSE 5000
ENTRYPOINT ["/app/entrypoint.sh"]
