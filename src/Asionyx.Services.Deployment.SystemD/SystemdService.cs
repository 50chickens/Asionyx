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
                // Emulate daemon-reload (noop in this file-backed emulator)
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
            // Attempt to locate a unit file first
            var unitPath = $"/etc/systemd/system/{_serviceName}.service";
            string? exec = null;
            string? workDir = null;
            if (File.Exists(unitPath))
            {
                var lines = await File.ReadAllLinesAsync(unitPath);
                foreach (var l in lines)
                {
                    if (l.TrimStart().StartsWith("ExecStart=", StringComparison.OrdinalIgnoreCase))
                    {
                        exec = l.Substring(l.IndexOf('=') + 1).Trim();
                    }
                    else if (l.TrimStart().StartsWith("WorkingDirectory=", StringComparison.OrdinalIgnoreCase))
                    {
                        workDir = l.Substring(l.IndexOf('=') + 1).Trim();
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(exec))
            {
                // Fallback to mapping a service name to /app/<short>/<fullname>.dll
                var shortName = _serviceName.Split('.').Last();
                var folder = shortName.ToLowerInvariant();
                var dllPath = Path.Combine("/app", folder, _serviceName + ".dll");
                if (!File.Exists(dllPath))
                {
                    Console.Error.WriteLine($"Executable not found: {dllPath}");
                    return false;
                }
                exec = $"dotnet {dllPath}";
            }

            // Start the process described by exec
            var parts = exec.Trim().Split(' ', 2);
            string file = parts[0].Trim('"');
            string args = parts.Length > 1 ? parts[1] : string.Empty;

            var psi = new ProcessStartInfo(file, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            if (!string.IsNullOrWhiteSpace(workDir)) psi.WorkingDirectory = workDir;

            var proc = Process.Start(psi);
            if (proc == null)
            {
                Console.Error.WriteLine("Failed to start process for service {0}", _serviceName);
                return false;
            }

            // Persist PID
            var pidDir = Path.Combine(Directory.GetCurrentDirectory(), "runtime");
            Directory.CreateDirectory(pidDir);
            var pidFile = Path.Combine(pidDir, _serviceName + ".pid");
            await File.WriteAllTextAsync(pidFile, proc.Id.ToString());

            Console.WriteLine($"Service {_serviceName} started (pid={proc.Id})");
            return true;
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
            var pidFile = Path.Combine(Directory.GetCurrentDirectory(), "runtime", _serviceName + ".pid");
            if (!File.Exists(pidFile))
            {
                Console.WriteLine($"{_serviceName} not running");
                return false;
            }
            var t = await File.ReadAllTextAsync(pidFile);
            if (!int.TryParse(t, out var pid))
            {
                File.Delete(pidFile);
                return false;
            }
            try
            {
                var p = Process.GetProcessById(pid);
                p.Kill(entireProcessTree: true);
            }
            catch { }
            try { File.Delete(pidFile); } catch { }
            Console.WriteLine($"Service {_serviceName} stopped");
            return true;
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
            var pidFile = Path.Combine(Directory.GetCurrentDirectory(), "runtime", _serviceName + ".pid");
            if (!File.Exists(pidFile)) return ServiceStatus.Inactive;
            var t = await File.ReadAllTextAsync(pidFile);
            if (!int.TryParse(t, out var pid)) return ServiceStatus.Unknown;
            try
            {
                var p = Process.GetProcessById(pid);
                return ServiceStatus.Active;
            }
            catch
            {
                return ServiceStatus.Inactive;
            }
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
        // Provide a minimal emulation for certain systemctl operations used by this emulator
        if (string.Equals(command, "systemctl", StringComparison.OrdinalIgnoreCase))
        {
            var args = arguments?.Trim() ?? string.Empty;
            if (args.StartsWith("daemon-reload", StringComparison.OrdinalIgnoreCase))
            {
                return new CommandResult { ExitCode = 0, Output = "", Error = "" };
            }
            if (args.StartsWith("is-active", StringComparison.OrdinalIgnoreCase))
            {
                var parts = args.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) return new CommandResult { ExitCode = 3, Output = "", Error = "" };
                var svc = parts[1].Trim();
                var pidFile = Path.Combine(Directory.GetCurrentDirectory(), "runtime", svc + ".pid");
                if (!File.Exists(pidFile)) return new CommandResult { ExitCode = 3, Output = "inactive", Error = "" };
                var t = await File.ReadAllTextAsync(pidFile);
                if (!int.TryParse(t, out var pid)) return new CommandResult { ExitCode = 3, Output = "inactive", Error = "" };
                try { var p = Process.GetProcessById(pid); return new CommandResult { ExitCode = 0, Output = "active", Error = "" }; } catch { return new CommandResult { ExitCode = 3, Output = "inactive", Error = "" }; }
            }
            // For start/stop/enable we return an informative error to prompt higher-level logic to use internal implementation
            return new CommandResult { ExitCode = 3, Output = "", Error = $"Command {arguments} not emulated" };
        }

        // Generic fallback: attempt to execute command if present on system
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
