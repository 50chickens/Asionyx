using System;
using System.Collections.Generic;

namespace Testcontainers.Sshd;

/// <summary>
/// Reusable startup callbacks for Sshd containers used by integration tests.
/// Keep callbacks idempotent and tolerant — they may run on images that already
/// have the packages or users installed.
/// </summary>
public static class SshdStartupHelpers
{
    /// <summary>
    /// Configure the container so it contains a non-root test user and an SSH server.
    /// The callback will:
    /// - create the user if missing
    /// - install openssh-server if missing (apt-based)
    /// - create the user's .ssh directory and populate authorized_keys from $PUBLIC_KEY
    /// - create a passwordless sudoers entry
    /// - attempt to start sshd so the container accepts SSH connections
    ///
    /// This is deliberately permissive: it attempts each step but does not throw on
    /// failure so tests running against different base images (including linuxserver
    /// or the minimal CI image) can reuse the same callback.
    /// </summary>
    public static SshdBuilder WithTestUserSetup(this SshdBuilder builder, string username = "tcuser")
    {
        _ = builder ?? throw new ArgumentNullException(nameof(builder));

        return builder.WithStartupCallback(async (container, ct) =>
        {

            var sshdPortInsideContainer = SshdBuilder.SshdPort;
            var createUserCommand = $"id -u {username} || useradd -m -s /bin/bash {username}";
            var createSudoersCommand = $"echo \"{username} ALL=(ALL) NOPASSWD:ALL\" > /etc/sudoers.d/{username} || true && chmod 440 /etc/sudoers.d/{username}";
            var installSshCommand = "apt-get update || true && apt-get install -y --no-install-recommends openssh-server || true";
            var createSshdDirCommand = "mkdir -p /var/run/sshd || true";
            var fixPortCmd = $"(grep -q '^Port {sshdPortInsideContainer}' /etc/ssh/sshd_config >/dev/null 2>&1) || (sed -i 's/^#Port 22/Port {sshdPortInsideContainer}/' /etc/ssh/sshd_config 2>/dev/null || echo 'Port {sshdPortInsideContainer}' >> /etc/ssh/sshd_config)";
            var startSshCmd = "(service ssh status >/dev/null 2>&1 && service ssh restart) || (systemctl enable --now ssh || /etc/init.d/ssh start) || (sshd || /usr/sbin/sshd) || true";
            var setupSshKeysCmd = $"mkdir -p /home/{username}/.ssh && (if [ -n \"$PUBLIC_KEY\" ]; then echo \"$PUBLIC_KEY\" > /home/{username}/.ssh/authorized_keys; fi) || true && chmod 600 /home/{username}/.ssh/authorized_keys || true && chown -R {username}:{username} /home/{username}/.ssh || true";
            var waitForSshd = $"'for i in {{1..30}}; do (echo >/dev/tcp/127.0.0.1/{sshdPortInsideContainer}) && exit 0 || sleep 1; done; exit 0'";

            List<string> commandsToExecute =
            [
                createUserCommand,
                createSudoersCommand,
                installSshCommand,
                createSshdDirCommand,
                fixPortCmd,
                startSshCmd,
                setupSshKeysCmd,
                waitForSshd

            ];
            commandsToExecute.ForEach(async commandToExecute =>
            {
                var result = await container.ExecAsync(new[] { "sh", "-c", commandToExecute }, ct);   
            });
        });
    }
}
