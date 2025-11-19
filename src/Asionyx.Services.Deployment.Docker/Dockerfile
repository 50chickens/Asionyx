# Dockerfile for Asionyx.Services.Deployment + SystemD emulator (for integration tests)
# Use Debian Trixie and avoid Microsoft container images; install dotnet packages from Microsoft feed instead

## Use official Microsoft .NET SDK and ASP.NET runtime images to avoid manual apt setup
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /src

# Copy repository and publish projects using the official SDK image
COPY . /src
RUN dotnet publish Asionyx.Services.Deployment -c Release -o /app/deployment
RUN dotnet publish Asionyx.Services.Deployment.SystemD -c Release -o /app/systemd
RUN dotnet publish Asionyx.Services.HelloWorld -c Release -o /app/helloworld

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Copy published outputs from builder
COPY --from=builder /app/deployment ./deployment
COPY --from=builder /app/systemd ./systemd
COPY --from=builder /app/helloworld ./helloworld

# Copy entrypoint script (from repo path relative to build context)
COPY Asionyx.Services.Deployment.Docker/entrypoint.sh /app/entrypoint.sh

# Ensure the ASP.NET app listens on port 5000 inside the container
ENV ASPNETCORE_URLS=http://+:5000

RUN sed -i 's/\r$//' /app/entrypoint.sh \
	&& chmod +x /app/entrypoint.sh

EXPOSE 5000
ENTRYPOINT ["/app/entrypoint.sh"]
