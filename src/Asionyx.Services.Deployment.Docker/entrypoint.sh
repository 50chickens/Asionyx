#!/usr/bin/env bash
set -euo pipefail

# Do NOT auto-start the SystemD emulator here. The emulator binary is installed under
# `/app/systemd` and should be invoked by the deployment service CLI when required
# so that unit creation/start/stop operations are performed on-demand by the service.

# Start the deployment service in the foreground (entrypoint of the container)
if [ -f "/app/deployment/Asionyx.Services.Deployment" ]; then
	/app/deployment/Asionyx.Services.Deployment
elif [ -f "/app/deployment/Asionyx.Services.Deployment.dll" ]; then
	/usr/bin/dotnet /app/deployment/Asionyx.Services.Deployment.dll
else
	echo "Error: deployment service not found under /app/deployment" >&2
	exit 1
fi

exit $?
