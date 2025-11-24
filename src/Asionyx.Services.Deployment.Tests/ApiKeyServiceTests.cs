using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Asionyx.Services.Deployment.Logging;
using Asionyx.Services.Deployment.Services;
using Microsoft.AspNetCore.DataProtection;
using NUnit.Framework;

namespace Asionyx.Services.Deployment.Tests;

[TestFixture]
public class ApiKeyServiceTests
{
    [Test]
    public async Task EnsureApiKey_PrefersEnvironmentVariable_AndDoesNotPersist()
    {
        var envKey = "env-test-key-123";
        Environment.SetEnvironmentVariable("API_KEY", envKey);

        var protectorDir = Path.Combine(Path.GetTempPath(), "asionyx-dp-env");
        Directory.CreateDirectory(protectorDir);
        var provider = DataProtectionProvider.Create(protectorDir);

        var svc = new ApiKeyService(provider, new NLogLogger<Asionyx.Services.Deployment.Services.ApiKeyService>());

        // override etc path to a temp file so we can assert it isn't created
        var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".key");
        SetPrivateField(svc, "_etcPath", tempFile);

        var key = await svc.EnsureApiKeyAsync().ConfigureAwait(false);
        Assert.That(key, Is.EqualTo(envKey));
        Assert.That(File.Exists(tempFile), Is.False, "Env-provided key should not be persisted to disk");

        // cleanup
        Environment.SetEnvironmentVariable("API_KEY", null);
        try { Directory.Delete(protectorDir, true); } catch { }
    }

    [Test]
    public async Task EnsureApiKey_PersistsAndReloads_FromEncryptedFile()
    {
        Environment.SetEnvironmentVariable("API_KEY", null);

        var protectorDir = Path.Combine(Path.GetTempPath(), "asionyx-dp-file");
        Directory.CreateDirectory(protectorDir);
        var provider = DataProtectionProvider.Create(protectorDir);

        var tempFile = Path.Combine(protectorDir, "test_asionyx_api_key");

        // First instance: generate & persist
        var svc1 = new ApiKeyService(provider, new NLogLogger<Asionyx.Services.Deployment.Services.ApiKeyService>());
        SetPrivateField(svc1, "_etcPath", tempFile);
        var key1 = await svc1.EnsureApiKeyAsync().ConfigureAwait(false);
        Assert.That(File.Exists(tempFile), Is.True, "Key file should be created");

        // Second instance: should read and return same key
        var provider2 = DataProtectionProvider.Create(protectorDir);
        var svc2 = new ApiKeyService(provider2, new NLogLogger<Asionyx.Services.Deployment.Services.ApiKeyService>());
        SetPrivateField(svc2, "_etcPath", tempFile);
        var key2 = await svc2.EnsureApiKeyAsync().ConfigureAwait(false);

        Assert.That(key2, Is.EqualTo(key1));

        // cleanup
        try { File.Delete(tempFile); } catch { }
        try { Directory.Delete(protectorDir, true); } catch { }
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        var fi = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (fi == null) throw new InvalidOperationException("Field not found: " + fieldName);
        fi.SetValue(instance, value);
    }
}
