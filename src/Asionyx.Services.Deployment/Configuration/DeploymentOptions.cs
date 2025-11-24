namespace Asionyx.Services.Deployment.Configuration
{
    public class DeploymentOptions
    {
        public string UploadsDir { get; set; } = "/var/asionyx_uploads";
        public string SystemdExecPath { get; set; } = "/usr/local/bin/Asionyx.Services.Deployment.SystemD";
        public string DiagnosticsDir { get; set; } = "/var/asionyx/diagnostics";
        public int ProcessTimeoutSeconds { get; set; } = 60;
        public string ApiKeyPath { get; set; } = "/etc/asionyx_api_key";
        // Optional API key supplied via configuration (appsettings). When present this takes precedence
        // over environment-provided keys for normal runtime. Integration tests may still inject an
        // environment variable at container startup which will be respected if configuration is not set.
        public string? ApiKey { get; set; }
    }
}
