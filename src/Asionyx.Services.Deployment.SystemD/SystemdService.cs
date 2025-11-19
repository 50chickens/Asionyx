using System.Diagnostics;
using Asionyx.Core;

namespace Asionyx;

public class SystemdService
{
    private readonly string _serviceName;
    private readonly SystemdServiceSettings _settings;

    public SystemdService(string serviceName, SystemdServiceSettings settings)
    {
        _serviceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<bool> CreateServiceAsync(string execStart, string description, string workingDirectory = null, string restart = null, int? restartSec = null, string type = null)
    {
        var config = new ServiceConfiguration
        {
            Name = _serviceName,
            Description = description,
            ExecStart = execStart,
            WorkingDirectory = workingDirectory ?? _settings.DefaultWorkingDirectory,
            Restart = restart ?? _settings.DefaultRestartPolicy,
            RestartSec = restartSec ?? _settings.DefaultRestartSec,
            Type = type ?? "simple"
        };

        var serviceContent = _settings.Template
            .Replace("{Description}", config.Description)
            .Replace("{Type}", config.Type)
            .Replace("{ExecStart}", config.ExecStart)
            .Replace("{WorkingDirectory}", config.WorkingDirectory)
            .Replace("{Restart}", config.Restart)
            .Replace("{RestartSec}", config.RestartSec.ToString());

        var serviceFilePath = $"/etc/systemd/system/{_serviceName}.service";

        try
        {
            await File.WriteAllTextAsync(serviceFilePath, serviceContent);
            await ExecuteCommandAsync("systemctl", "daemon-reload");
            Console.WriteLine($"Service {_serviceName} created successfully at {serviceFilePath}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating service: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> StartAsync()
    {
        try
        {
            var result = await ExecuteCommandAsync("systemctl", $"start {_serviceName}");
            if (result.ExitCode == 0)
            {
                Console.WriteLine($"Service {_serviceName} started successfully");
                return true;
            }
            else
            {
                Console.WriteLine($"Failed to start service {_serviceName}: {result.Error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting service: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> StopAsync()
    {
        try
        {
            var result = await ExecuteCommandAsync("systemctl", $"stop {_serviceName}");
            if (result.ExitCode == 0)
            {
                Console.WriteLine($"Service {_serviceName} stopped successfully");
                return true;
            }
            else
            {
                Console.WriteLine($"Failed to stop service {_serviceName}: {result.Error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping service: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> EnableAsync()
    {
        try
        {
            var result = await ExecuteCommandAsync("systemctl", $"enable {_serviceName}");
            if (result.ExitCode == 0)
            {
                Console.WriteLine($"Service {_serviceName} enabled successfully");
                return true;
            }
            else
            {
                Console.WriteLine($"Failed to enable service {_serviceName}: {result.Error}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enabling service: {ex.Message}");
            return false;
        }
    }

    public async Task<ServiceStatus> GetStatusAsync()
    {
        try
        {
            var result = await ExecuteCommandAsync("systemctl", $"is-active {_serviceName}");
            var status = result.Output.Trim();

            return status switch
            {
                "active" => ServiceStatus.Active,
                "inactive" => ServiceStatus.Inactive,
                "failed" => ServiceStatus.Failed,
                _ => ServiceStatus.Unknown,
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting service status: {ex.Message}");
            return ServiceStatus.Unknown;
        }
    }

    public async Task<bool> IsRunningAsync()
    {
        var status = await GetStatusAsync();
        var isRunning = status == ServiceStatus.Active;
        Console.WriteLine($"Service {_serviceName} is {(isRunning ? "running" : "not running")} (status: {status})");
        return isRunning;
    }

    private async Task<CommandResult> ExecuteCommandAsync(string command, string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        process.Start();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return new CommandResult
        {
            ExitCode = process.ExitCode,
            Output = output,
            Error = error
        };
    }

    public enum ServiceStatus
    {
        Active,
        Inactive,
        Failed,
        Unknown
    }
}
