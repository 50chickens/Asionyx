#!/usr/bin/env bash
set -euo pipefail

# start systemd emulator (run via dotnet)
/usr/bin/dotnet /app/systemd/Asionyx.Services.Deployment.SystemD.dll &
SYSTEMD_PID=$!

# start deployment service (run via dotnet)
/usr/bin/dotnet /app/deployment/Asionyx.Services.Deployment.dll &
DEPLOY_PID=$!

wait -n $SYSTEMD_PID $DEPLOY_PID
exit $?
