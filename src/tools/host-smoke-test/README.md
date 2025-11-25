Host smoke-test for Asionyx deployment

This folder contains `host-smoke-test.ps1`, a PowerShell script to exercise a live host deployment flow using SSH + private key.

What it does
- Verifies SSH connectivity using the provided private key
- Compresses the local `PublishDir` and uploads it to the remote host
- Extracts the publish into a `RemoteDir` (default `/opt/Asionyx.Service.Deployment.Linux`)
- Optionally installs a `.service` unit file and starts it
- Queries `/status` on the remote host (via remote `curl` or pwsh)

Usage example:

```pwsh
pwsh .\host-smoke-test.ps1 -Host myhost -User ubuntu -KeyPath C:\keys\id_rsa -PublishDir C:\projects\Asionyx\publish\deployment -RemoteDir /opt/Asionyx.Service.Deployment.Linux -ServiceName deployment-service -ApiPort 5001 -ApiKey the-api-key
```

Notes:
- The script uses the system `ssh` and `scp` commands. Ensure they are available in PATH (OpenSSH client).
- The script requires that the SSH user has passwordless sudo to install systemd units and manage services, or run as root.
- The script does not attempt to modify firewall rules. Ensure the service port is accessible if you plan to query the API from your workstation.

If you'd like, I can:
- Add a small wrapper that publishes the .NET project automatically before running this script.
- Add CI integration to optionally run a smoke test against a known host (with secrets stored in your CI secret store).
