#!/usr/bin/env bash
set -euo pipefail

# Start the SystemD emulator (published output under /app/systemd) as a background process
# so it can manage services independently of the deployment web service.
if [ -d "/app/systemd" ]; then
	if [ -f "/app/systemd/Asionyx.Services.Deployment.SystemD" ]; then
		# native/self-contained executable
		/app/systemd/Asionyx.Services.Deployment.SystemD &
	elif [ -f "/app/systemd/Asionyx.Services.Deployment.SystemD.dll" ]; then
		# run via dotnet
		/usr/bin/dotnet /app/systemd/Asionyx.Services.Deployment.SystemD.dll &
	else
		echo "Warning: SystemD emulator not found under /app/systemd"
	fi
else
	echo "Warning: /app/systemd directory not found"
fi

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
