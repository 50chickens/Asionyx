#!/usr/bin/env bash
set -euo pipefail

# Start the deployment service in the foreground. The deployment service will invoke
# the systemd emulator CLI (`/app/systemd/Asionyx.Services.Deployment.SystemD.dll`) via
# `Process.Start` when needed; we do not run the emulator as a daemon here.
/usr/bin/dotnet /app/deployment/Asionyx.Services.Deployment.dll
exit $?
