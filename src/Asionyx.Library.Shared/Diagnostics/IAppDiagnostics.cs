namespace Asionyx.Library.Shared.Diagnostics
{
    public interface IAppDiagnostics
    {
        /// <summary>
        /// Write a diagnostics object to disk as an atomic JSON file named '{name}.json'.
        /// </summary>
        Task WriteAsync(string name, object data, CancellationToken cancellationToken = default);

        /// <summary>
        /// Read a diagnostics JSON file and deserialize it to the requested type.
        /// Returns default(T) if the file does not exist.
        /// </summary>
        Task<T?> ReadAsync<T>(string name, CancellationToken cancellationToken = default);
    }
}
