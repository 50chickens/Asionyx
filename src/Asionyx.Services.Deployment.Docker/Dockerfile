# Dockerfile for Asionyx.Services.Deployment + SystemD emulator (for integration tests)
# Use Debian Trixie and avoid Microsoft container images; install dotnet packages from Microsoft feed instead

## Use official Microsoft .NET SDK and ASP.NET runtime images to avoid manual apt setup
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Expect pre-published outputs under `publish/` created by the orchestrator/CI.
# This avoids building the solution inside the image; the orchestrator must run
# `dotnet restore/build/publish` and place outputs in `publish/deployment`,
# `publish/systemd` and `publish/helloworld` prior to `docker build`.

# Copy published outputs from build context (created by orchestrator)
COPY publish/deployment ./deployment
COPY publish/systemd ./systemd
# Also install the self-contained SystemD executable into /usr/local/bin so
# the deployment service can invoke it directly without fallbacks.
COPY publish/systemd/ /usr/local/bin/
RUN chmod +x /usr/local/bin/Asionyx.Services.Deployment.SystemD || true
COPY publish/helloworld ./helloworld

# Copy entrypoint script (from repo path relative to build context)
COPY Asionyx.Services.Deployment.Docker/entrypoint.sh /app/entrypoint.sh

# Ensure the ASP.NET app listens on port 5000 inside the container
ENV ASPNETCORE_URLS=http://+:5000

RUN sed -i 's/\r$//' /app/entrypoint.sh \
	&& chmod +x /app/entrypoint.sh

EXPOSE 5000
ENTRYPOINT ["/app/entrypoint.sh"]
