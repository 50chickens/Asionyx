namespace Asionyx.Core;

public class ServiceConfiguration
{
    /// <summary>
    /// Service name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Service description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Command to execute
    /// </summary>
    public string ExecStart { get; set; } = string.Empty;

    /// <summary>
    /// Working directory
    /// </summary>
    public string WorkingDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Restart policy (e.g., "on-failure", "always", "no")
    /// </summary>
    public string Restart { get; set; } = "on-failure";

    /// <summary>
    /// Restart delay in seconds
    /// </summary>
    public int RestartSec { get; set; } = 10;

    /// <summary>
    /// Service type (e.g., "simple", "forking", "oneshot")
    /// </summary>
    public string Type { get; set; } = "simple";
}
