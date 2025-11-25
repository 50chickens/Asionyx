using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Testcontainers.Sshd;

public class SshTestHelper : IDisposable
{
    public string Username { get; private set; }
    public string HostKeyPath { get; private set; }
    public string PrivatePem { get; private set; }
    public SshdContainer Container { get; private set; }

    private bool _started = false;

    public SshTestHelper(string username = "pistomp")
    {
        Username = username;
    }

    public async Task StartContainerAsync(string image = "audio-linux/ci-systemd-trixie:local")
    {
        // Generate RSA keypair
        var keyGen = new Org.BouncyCastle.Crypto.Generators.RsaKeyPairGenerator();
        keyGen.Init(new Org.BouncyCastle.Crypto.KeyGenerationParameters(new Org.BouncyCastle.Security.SecureRandom(), 2048));
        var keyPair = keyGen.GenerateKeyPair();

        string privatePem;
        using (var sw = new StringWriter())
        {
            var pw = new Org.BouncyCastle.OpenSsl.PemWriter(sw);
            pw.WriteObject(keyPair.Private);
            pw.Writer.Flush();
            privatePem = sw.ToString();
        }

        PrivatePem = privatePem;

        // Write private key to a temp host file that will be copied into the container
        var hostKeyPath = Path.Combine(Path.GetTempPath(), $"ssh_test_key_{Guid.NewGuid():N}");
        File.WriteAllText(hostKeyPath, privatePem, Encoding.ASCII);
        HostKeyPath = hostKeyPath;

        var builder = new SshdBuilder()
            .WithImage(image)
            .WithBindMount("/sys/fs/cgroup", "/sys/fs/cgroup")
            .WithTestUserSetup(Username)
            .WithPrivateKeyFileCopied(HostKeyPath, containerPrivateKeyPath: $"/home/{Username}/.ssh/id_rsa", containerPublicKeyPath: $"/home/{Username}/.ssh/authorized_keys");

        var container = builder.Build();
        await container.StartAsync(System.Threading.CancellationToken.None);

        Container = container;
        _started = true;
    }

    public async Task<string?> GetApiKeyAsync()
    {
        if (Container == null) return null;
        try
        {
            var res = await Container.ExecAsync(new[] { "/bin/sh", "-c", "cat /app/appsettings.json || cat /app/appsettings.Development.json" }, System.Threading.CancellationToken.None).ConfigureAwait(false);
            var outp = res.Stdout ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(outp))
            {
                var m = System.Text.RegularExpressions.Regex.Match(outp, "\"ApiKey\"\\s*:\\s*\"([^\"]+)\"");
                if (m.Success) return m.Groups[1].Value;
            }

            // fallback to environment variables inside container
            var envRes = await Container.ExecAsync(new[] { "/bin/sh", "-c", "printenv X_API_KEY 2>/dev/null || printenv API_KEY 2>/dev/null || true" }, System.Threading.CancellationToken.None).ConfigureAwait(false);
            var envOut = envRes.Stdout ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(envOut)) return envOut.Trim();
        }
        catch { }
        return null;
    }

    public string Host => Container?.Hostname ?? string.Empty;

    public int SshPort => Container != null ? Convert.ToInt32(Container.GetMappedPublicPort(SshdBuilder.SshdPort)) : 0;

    public async ValueTask DisposeAsync()
    {
        Dispose();
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        try {
            if (_started && Container != null) Container.StopAsync().GetAwaiter().GetResult();
        } catch { }
        try { if (File.Exists(HostKeyPath)) File.Delete(HostKeyPath); } catch { }
    }

    void IDisposable.Dispose() => Dispose();
}
