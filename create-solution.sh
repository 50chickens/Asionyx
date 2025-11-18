#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
mkdir -p src
cd src

# Create projects
dotnet new classlib -f net9.0 -n Asionyx.Library.Shared
dotnet new classlib -f net9.0 -n Asionyx.Library.Core

dotnet new web -f net9.0 -n Asionyx.Services.Deployment
dotnet new web -f net9.0 -n Asionyx.Services.Deployment.SystemD

# console client
dotnet new console -f net9.0 -n Asionyx.Services.Deployment.Client

# docker project - just holds Dockerfile
mkdir -p Asionyx.Services.Deployment.Docker

# test projects
dotnet new xunit -f net9.0 -n Asionyx.Services.Deployment.Tests
dotnet new xunit -f net9.0 -n Asionyx.Services.Deployment.Client.Tests

# Create solution and add projects inside src
cd src
if [ ! -f Asionyx.sln ]; then
  dotnet new sln -n Asionyx
fi
dotnet sln Asionyx.sln add **/*.csproj

echo "Solution and projects created. Now add NuGet packages as needed with 'dotnet add package'."