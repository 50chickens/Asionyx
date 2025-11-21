using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Asionyx.Library.Testing;

public class TestHelpers
{
    // Temporarily remove the X-API-KEY header, run the action, then restore header values.
    public async Task<T> RunWithoutApiKeyAsync<T>(HttpClient client, Func<Task<T>> action)
    {
        if (client == null) throw new ArgumentNullException(nameof(client));
        var hadHeader = client.DefaultRequestHeaders.Contains("X-API-KEY");
        string[] values = null;
        if (hadHeader)
        {
            values = client.DefaultRequestHeaders.GetValues("X-API-KEY").ToArray();
            client.DefaultRequestHeaders.Remove("X-API-KEY");
        }
        try
        {
            return await action();
        }
        finally
        {
            if (hadHeader && values != null)
            {
                foreach (var v in values) client.DefaultRequestHeaders.Add("X-API-KEY", v);
            }
        }
    }

    // Create a small test nupkg (zip) containing a manifest.json and return the path.
    public string CreateTestNupkg(string name = "testpkg")
    {
        var tmp = Path.GetTempPath();
        var pkgDir = Path.Combine(tmp, "asionyx_pkg_test");
        try { if (Directory.Exists(pkgDir)) Directory.Delete(pkgDir, true); } catch { }
        Directory.CreateDirectory(pkgDir);
        var manifestPath = Path.Combine(pkgDir, "manifest.json");
        File.WriteAllText(manifestPath, $"{{ \"name\": \"{name}\", \"version\": \"0.1.0\" }}");
        var nupkgPath = Path.Combine(tmp, $"{name}.nupkg");
        try { if (File.Exists(nupkgPath)) File.Delete(nupkgPath); } catch { }
        System.IO.Compression.ZipFile.CreateFromDirectory(pkgDir, nupkgPath);
        return nupkgPath;
    }

    // Create a MultipartFormDataContent for the provided nupkg path and return the content and open FileStream.
    // Caller is responsible for disposing both returned values.
    public (MultipartFormDataContent Form, FileStream Stream) CreatePackageForm(string nupkgPath)
    {
        var form = new MultipartFormDataContent();
        var fs = File.OpenRead(nupkgPath);
        var fileContent = new StreamContent(fs);
        form.Add(fileContent, "file", Path.GetFileName(nupkgPath));
        return (form, fs);
    }
}
