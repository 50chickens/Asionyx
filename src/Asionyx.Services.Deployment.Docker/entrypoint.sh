#!/usr/bin/env bash
set -euo pipefail
# start systemd emulator
/app/systemd/Asionyx.Services.Deployment.SystemD &
SYSTEMD_PID=$!
# start deployment service
/app/deployment/Asionyx.Services.Deployment &
DEPLOY_PID=$!

wait -n $SYSTEMD_PID $DEPLOY_PID
exit $?
