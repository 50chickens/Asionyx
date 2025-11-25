using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Asionyx.Tools.Deployment.Client.Library.Ssh;

[TestFixture]
[Category("RequiresHost")]
public class DeploymentClientSshInfoIntegrationTests
{
    [SetUp]
    public void SkipIfWindowsHost()
    {
        // Use existing helper to skip if no Docker host available
        RequiresHostHelper.EnsureHostOrIgnore();
    }

    [Test]
    public static async Task Deploy_Using_SshClient_And_Verify_InfoEndpoint()
    {
        // Use helper to start the SSH-enabled container and create keys
        using var helper = new SshTestHelper("pistomp");
        await helper.StartContainerAsync();

        var host = helper.Host;
        var sshPort = helper.SshPort;
        var username = helper.Username;
        var hostKeyPath = helper.HostKeyPath;

        // Publish or find published deployment output
        var repoPublishDir = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "publish", "Asionyx.Service.Deployment.Linux"));
        string publishDir;
        if (Directory.Exists(repoPublishDir) && File.Exists(Path.Combine(repoPublishDir, "Asionyx.Service.Deployment.Linux.dll")))
        {
            publishDir = repoPublishDir;
            TestContext.WriteLine($"Using repo-published server output at: {publishDir}");
        }
        else
        {
            publishDir = Path.Combine(Path.GetTempPath(), "asionyx_publish_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(publishDir);
            var projectPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "src", "Asionyx.Service.Deployment.Linux", "Asionyx.Service.Deployment.Linux.csproj");
            projectPath = Path.GetFullPath(projectPath);
            var psi = new ProcessStartInfo("dotnet", $"publish \"{projectPath}\" -c Debug -o \"{publishDir}\"") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi);
            Assert.That(p, Is.Not.Null, "dotnet publish process failed to start");
            var sout = await p.StandardOutput.ReadToEndAsync();
            var serr = await p.StandardError.ReadToEndAsync();
            p.WaitForExit();
            Assert.That(p.ExitCode, Is.EqualTo(0), () => $"dotnet publish failed. STDOUT:\n{sout}\nSTDERR:\n{serr}");
        }

        // Prepare remote deploy dir and upload using SshBootstrapper
        var remoteDeployDir = "/opt/Asionyx.Service.Deployment.Linux";
        var sb = new SshBootstrapper(host, username, hostKeyPath, sshPort);
        sb.UploadDirectory(host, sshPort, username, hostKeyPath, publishDir, remoteDeployDir);

        // Verify uploaded dll exists
        using (var fs = File.OpenRead(hostKeyPath))
        {
            var pk = new Renci.SshNet.PrivateKeyFile(fs);
            var keyAuth = new Renci.SshNet.PrivateKeyAuthenticationMethod(username, pk);
            var conn = new Renci.SshNet.ConnectionInfo(host, sshPort, username, keyAuth);
            using var client = new Renci.SshNet.SshClient(conn);
            client.Connect();
            var checkCmd = client.RunCommand($"test -f {remoteDeployDir}/Asionyx.Service.Deployment.Linux.dll && echo exists || echo missing");
            Assert.That(checkCmd.ExitStatus, Is.EqualTo(0));
            var result = checkCmd.Result.Trim();
            Assert.That(result, Is.EqualTo("exists"), "Uploaded dll not found on remote container");
            client.Disconnect();
        }

        // Run the deployment client's SSH runner to perform install/start
        var options = new SshOptions { Host = host, Port = sshPort, User = username, KeyPath = hostKeyPath, PublishDir = publishDir };
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
        var runner = new SshCliRunner(options, config);

        var rc = await runner.HandleAsync(genPrefix: null, genRetry: false, toStdout: false, doVerify: false, verifyHostConfig: false, checkService: false, serviceName: null, hostOverride: null, portOverride: null, userOverride: null, keyOverride: null, clearJournal: false, ensureRemoteDir: false, ensureUserDataDir: false, installSystemdUnit: false);
        Assert.That(rc, Is.EqualTo(0), "SshCliRunner deployment flow failed (non-zero exit)");

        // Exec inside container to query /info endpoint
        async Task<(int ExitCode, string StdOut, string StdErr)> ExecRoot(string cmd)
        {
            var container = helper.Container;
            var res = await container.ExecAsync(new[] { "/bin/sh", "-c", cmd }, System.Threading.CancellationToken.None).ConfigureAwait(false);
            return (checked((int)res.ExitCode), res.Stdout ?? string.Empty, res.Stderr ?? string.Empty);
        }

        var (curlExit, curlOut, curlErr) = await ExecRoot("curl -sS http://localhost:5001/info || true");
        if (string.IsNullOrWhiteSpace(curlOut))
        {
            var (pwExit, pwOut, pwErr) = await ExecRoot("pwsh -NoProfile -NonInteractive -EncodedCommand $([Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes(\"try { $r = Invoke-RestMethod -Uri 'http://localhost:5001/info'; $r | ConvertTo-Json -Compress } catch { Write-Output 'invoke-failed'; exit 4 }\"))) || true");
            Assert.That(pwExit, Is.EqualTo(0), () => $"Failed to query /info with pwsh. out:{pwOut} err:{pwErr}");
            Assert.That(pwOut, Is.Not.Null.And.Not.EqualTo(string.Empty), "/info returned empty payload (pwsh)");
        }
        else
        {
            Assert.That(curlOut, Is.Not.Null.And.Not.EqualTo(string.Empty), "/info returned empty payload (curl)");
        }

        // cleanup publishDir when we created it
        try { if (publishDir.StartsWith(Path.GetTempPath())) Directory.Delete(publishDir, true); } catch { }
    }
}
