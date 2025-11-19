namespace Asionyx.Services.Deployment.Services;

public interface IApiKeyService
{
    /// <summary>
    /// Ensure an API key exists (from environment, or persisted encrypted file), persisting an encrypted copy if created.
    /// Returns the plaintext API key.
    /// </summary>
    Task<string> EnsureApiKeyAsync();

    /// <summary>
    /// Validate a provided key against the currently known key.
    /// </summary>
    bool Validate(string provided);
}
