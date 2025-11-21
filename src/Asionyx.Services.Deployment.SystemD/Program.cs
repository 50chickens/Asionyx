using System.Runtime.InteropServices;
using Asionyx;
using Asionyx.Library.Shared.Diagnostics;

// Minimal CLI-only systemd emulator for integration tests.
// - No TCP or sockets.
// - Assumes any managed application is a .NET application and will be launched via `dotnet <dll>`.
// - Unit files may be placed in `units/` (relative to CWD). If a unit file contains [Service]ExecStart it will be used verbatim.
// - If no unit file is present, the emulator will map a service name like `Asionyx.Services.HelloWorld` to
//   `/app/<shortname>/<fullname>.dll` and launch it via `dotnet /app/<shortname>/<fullname>.dll`.

internal class Program
{
    private static int Main(string[] args)
    {
        var UnitsDir = Path.Combine(Directory.GetCurrentDirectory(), "units");
        var RuntimeDir = Path.Combine(Directory.GetCurrentDirectory(), "runtime");
        var LogsDir = Path.Combine(Directory.GetCurrentDirectory(), "logs");
        Directory.CreateDirectory(UnitsDir);
        Directory.CreateDirectory(RuntimeDir);
        Directory.CreateDirectory(LogsDir);

        // Diagnostics writer for systemd emulator. Use configuration file `appsettings.json` if present.
        IAppDiagnostics? diag = null;
        IAppDiagnostics? diagAlt = null;
        try
        {
            // Read appsettings.json (if provided) and use its Diagnostics configuration.
            string? useStdout = null;
            string? dirCfg = null;
            var settingsPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            if (File.Exists(settingsPath))
            {
                try
                {
                    var jsonText = File.ReadAllText(settingsPath);
                    var jobj = Newtonsoft.Json.Linq.JObject.Parse(jsonText);
                    useStdout = jobj.SelectToken("Diagnostics.ToStdout")?.ToString();
                    dirCfg = jobj.SelectToken("Diagnostics.Dir")?.ToString();
                }
                catch { }
            }

            if (!string.IsNullOrWhiteSpace(useStdout) && (useStdout == "1" || useStdout.Equals("true", StringComparison.OrdinalIgnoreCase)))
            {
                diag = new ConsoleDiagnostics();
                diag.WriteAsync("systemd_startup", new { Timestamp = DateTime.UtcNow, Message = "SystemD emulator started (stdout)", UnitsDir, RuntimeDir, LogsDir }).GetAwaiter().GetResult();
            }
            else
            {
                var targetDir = !string.IsNullOrWhiteSpace(dirCfg) ? dirCfg! : LogsDir;
                diag = new FileDiagnostics(targetDir);
                diag.WriteAsync("systemd_startup", new { Timestamp = DateTime.UtcNow, Message = "SystemD emulator started", UnitsDir, RuntimeDir, LogsDir }).GetAwaiter().GetResult();
            }

            // Also attempt to write to container-wide diagnostics path used by orchestrator
            try
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var alt = "/var/asionyx/diagnostics";
                    Directory.CreateDirectory(alt);
                    if (!string.IsNullOrWhiteSpace(useStdout) && (useStdout == "1" || useStdout.Equals("true", StringComparison.OrdinalIgnoreCase)))
                    {
                        diagAlt = new ConsoleDiagnostics();
                        diagAlt.WriteAsync("systemd_startup", new { Timestamp = DateTime.UtcNow, Message = "SystemD emulator started (alt stdout)", UnitsDir, RuntimeDir, LogsDir }).GetAwaiter().GetResult();
                    }
                    else
                    {
                        diagAlt = new FileDiagnostics(alt);
                        diagAlt.WriteAsync("systemd_startup", new { Timestamp = DateTime.UtcNow, Message = "SystemD emulator started (alt)", UnitsDir, RuntimeDir, LogsDir }).GetAwaiter().GetResult();
                    }
                }
            }
            catch { }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to initialise diagnostics: {ex.Message}");
        }

        if (args.Length == 0)
        {
            PrintHelp();
            return 0;
        }

        var cmd = args[0].ToLowerInvariant();
        if (cmd == "help") { PrintHelp(); return 0; }
        try
        {
            return cmd switch
            {
                "add" => AddUnit(args.Skip(1).ToArray()),
                "remove" => RemoveUnit(args.Skip(1).ToArray()),
                "start" => StartUnit(args.Skip(1).ToArray()),
                "stop" => StopUnit(args.Skip(1).ToArray()),
                "status" => StatusUnit(args.Skip(1).ToArray()),
                "daemon-reload" or "daemon_reload" or "daemonreload" => DaemonReload(),
                "help" => 0,
                _ => UnknownCommand(cmd)
            };
        }
        catch (Exception ex)
        {
            try
            {
                if (diag != null)
                {
                    diag.WriteAsync($"fatal_{DateTime.UtcNow:yyyyMMddHHmmssfff}", new { Timestamp = DateTime.UtcNow, Error = ex.ToString() }).GetAwaiter().GetResult();
                }
                if (diagAlt != null)
                {
                    diagAlt.WriteAsync($"fatal_{DateTime.UtcNow:yyyyMMddHHmmssfff}", new { Timestamp = DateTime.UtcNow, Error = ex.ToString() }).GetAwaiter().GetResult();
                }
            }
            catch { }
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 3;
        }

        int UnknownCommand(string c)
        {
            Console.Error.WriteLine($"Unknown command: {c}");
            PrintHelp();
            return 2;
        }

        int AddUnit(string[] parts)
        {
            if (parts.Length == 0)
            {
                Console.Error.WriteLine("add requires a source file path or a unit name (with content via stdin)");
                return 2;
            }
            var source = parts[0];
            if (File.Exists(source))
            {
                var dest = Path.Combine(UnitsDir, Path.GetFileName(source));
                File.Copy(source, dest, overwrite: true);
                Console.WriteLine($"added unit: {Path.GetFileName(dest)}");
                return 0;
            }
            var unitName = source.EndsWith(".service", StringComparison.OrdinalIgnoreCase) ? source : source + ".service";
            var destPath = Path.Combine(UnitsDir, unitName);
            using var stdin = Console.OpenStandardInput();
            using var sr = new StreamReader(stdin);
            var content = sr.ReadToEnd();
            if (string.IsNullOrWhiteSpace(content))
            {
                Console.Error.WriteLine("No unit file content provided on stdin.");
                return 2;
            }
            File.WriteAllText(destPath, content);
            Console.WriteLine($"added unit: {unitName}");
            return 0;
        }

        int RemoveUnit(string[] parts)
        {
            if (parts.Length == 0)
            {
                Console.Error.WriteLine("remove requires a unit name");
                return 2;
            }
            var unitName = parts[0].EndsWith(".service", StringComparison.OrdinalIgnoreCase) ? parts[0] : parts[0] + ".service";
            var path = Path.Combine(UnitsDir, unitName);
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"unit not found: {unitName}");
                return 1;
            }
            File.Delete(path);
            Console.WriteLine($"removed unit: {unitName}");
            return 0;
        }

        int StartUnit(string[] parts)
        {
            if (parts.Length == 0)
            {
                Console.Error.WriteLine("start requires a unit name");
                return 2;
            }
            var unitName = parts[0].EndsWith(".service", StringComparison.OrdinalIgnoreCase) ? parts[0] : parts[0] + ".service";
            var unitPath = Path.Combine(UnitsDir, unitName);

            string? exec = null;
            if (File.Exists(unitPath))
            {
                var unit = ParseUnitFile(unitPath);
                if (unit.TryGetValue("Service", out var svc) && svc.TryGetValue("ExecStart", out var ex)) exec = ex;
            }

            if (string.IsNullOrWhiteSpace(exec))
            {
                // Map service name to /app/<short>/<fullname>.dll and run `dotnet <dll>`
                var shortName = parts[0].Split('.').Last();
                var folder = shortName.ToLowerInvariant();
                var dllPath = Path.Combine("/app", folder, parts[0] + ".dll");
                if (!File.Exists(dllPath))
                {
                    var msg = $"Executable not found: {dllPath}";
                    Console.Error.WriteLine(msg);
                    try { diag?.WriteAsync($"start_fail_{DateTime.UtcNow:yyyyMMddHHmmssfff}", new { Timestamp = DateTime.UtcNow, Message = msg, DllPath = dllPath }).GetAwaiter().GetResult(); } catch { }
                    try { diagAlt?.WriteAsync($"start_fail_{DateTime.UtcNow:yyyyMMddHHmmssfff}", new { Timestamp = DateTime.UtcNow, Message = msg, DllPath = dllPath }).GetAwaiter().GetResult(); } catch { }
                    return 1;
                }
                exec = $"dotnet {dllPath}";
            }

            // Use the SystemdService implementation to start the service
            var svcName = parts[0].EndsWith(".service", StringComparison.OrdinalIgnoreCase) ? parts[0].Substring(0, parts[0].Length - 8) : parts[0];
            var settings = new SystemdServiceSettings();
            var sd = new SystemdService(svcName, settings);
            var started = sd.StartAsync().GetAwaiter().GetResult();
            return started ? 0 : 3;
        }

        int StopUnit(string[] parts)
        {
            if (parts.Length == 0)
            {
                Console.Error.WriteLine("stop requires a unit name");
                return 2;
            }
            var unitName = parts[0].EndsWith(".service", StringComparison.OrdinalIgnoreCase) ? parts[0] : parts[0] + ".service";
            var pidFile = Path.Combine(RuntimeDir, unitName + ".pid");
            if (!File.Exists(pidFile))
            {
                Console.WriteLine($"{unitName} not running");
                return 1;
            }
            try
            {
                var svcName = parts[0].EndsWith(".service", StringComparison.OrdinalIgnoreCase) ? parts[0].Substring(0, parts[0].Length - 8) : parts[0];
                var settings = new SystemdServiceSettings();
                var sd = new SystemdService(svcName, settings);
                var stopped = sd.StopAsync().GetAwaiter().GetResult();
                return stopped ? 0 : 3;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"failed to stop {unitName}: {ex.Message}");
                return 3;
            }
        }

        int StatusUnit(string[] parts)
        {
            if (parts.Length == 0)
            {
                Console.Error.WriteLine("status requires a unit name");
                return 2;
            }
            var unitName = parts[0].EndsWith(".service", StringComparison.OrdinalIgnoreCase) ? parts[0] : parts[0] + ".service";
            var pidFile = Path.Combine(RuntimeDir, unitName + ".pid");
            if (!File.Exists(pidFile))
            {
                Console.WriteLine($"{unitName} not running");
                return 3;
            }
            try
            {
                var svcName = parts[0].EndsWith(".service", StringComparison.OrdinalIgnoreCase) ? parts[0].Substring(0, parts[0].Length - 8) : parts[0];
                var settings = new SystemdServiceSettings();
                var sd = new SystemdService(svcName, settings);
                var status = sd.GetStatusAsync().GetAwaiter().GetResult();
                if (status == SystemdService.ServiceStatus.Active)
                {
                    Console.WriteLine($"{unitName} running");
                    return 0;
                }
                else
                {
                    Console.WriteLine($"{unitName} not running");
                    return 3;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"status check failed for {unitName}: {ex.Message}");
                return 4;
            }
        }

        int DaemonReload()
        {
            var units = Directory.GetFiles(UnitsDir, "*.service").Select(Path.GetFileName).ToArray();
            Console.WriteLine($"Daemon reloaded. {units.Length} unit(s) available.");
            foreach (var u in units) Console.WriteLine($" - {u}");
            return 0;
        }

        void PrintHelp()
        {
            Console.WriteLine("Asionyx systemd emulator - minimal drop-in for systemctl usage in tests");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  add <path-to-unit-file>        - copy unit file into units/ or use a unit name and provide content via stdin");
            Console.WriteLine("  remove <unit>                 - remove unit file from units/");
            Console.WriteLine("  start <unit>                  - start unit (reads ExecStart from unit file or launches dotnet /app/<short>/<fullname>.dll)");
            Console.WriteLine("  stop <unit>                   - stop unit (kills PID from runtime/<unit>.pid)");
            Console.WriteLine("  status <unit>                 - check unit status");
            Console.WriteLine("  daemon-reload                  - reload unit files from disk (no-op for file-driven emulator)");
        }

        Dictionary<string, Dictionary<string, string>> ParseUnitFile(string path)
        {
            var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            var lines = File.ReadAllLines(path);
            string? section = null;
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith(";")) continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = line.Substring(1, line.Length - 2).Trim();
                    result[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    continue;
                }
                if (section == null) continue;
                var idx = line.IndexOf('=');
                if (idx <= 0) continue;
                var key = line.Substring(0, idx).Trim();
                var val = line.Substring(idx + 1).Trim();
                result[section][key] = val;
            }
            return result;
        }
    }
}
