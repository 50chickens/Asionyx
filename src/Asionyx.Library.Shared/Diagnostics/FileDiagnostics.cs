using Newtonsoft.Json;

namespace Asionyx.Library.Shared.Diagnostics
{
    public class FileDiagnostics : IAppDiagnostics
    {
        private readonly string _directory;

        public FileDiagnostics(string directory)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
            Directory.CreateDirectory(_directory);
        }

        public async Task WriteAsync(string name, object data, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name");

            var fileName = Path.Combine(_directory, name + ".json");
            var tmpName = fileName + ".tmp" + Guid.NewGuid().ToString("N");

            var json = JsonConvert.SerializeObject(data, Formatting.Indented);

            // Write to temp file then move to target to ensure atomicity.
            await File.WriteAllTextAsync(tmpName, json, cancellationToken).ConfigureAwait(false);

            // Replace existing file atomically where possible
            if (File.Exists(fileName))
            {
                File.Replace(tmpName, fileName, null);
            }
            else
            {
                File.Move(tmpName, fileName);
            }
        }

        public async Task<T?> ReadAsync<T>(string name, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name");

            var fileName = Path.Combine(_directory, name + ".json");
            if (!File.Exists(fileName)) return default;

            var json = await File.ReadAllTextAsync(fileName, cancellationToken).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
