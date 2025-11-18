using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using System.Net;

// Simple systemd-like emulator.
// If invoked with args (e.g. `start <name>`) it will act as a CLI client and send the command to the daemon.
// If invoked with no args it will run as a daemon listening on 127.0.0.1:6000 and manage processes.

const int Port = 6000;

if (args.Length > 0)
{
    // CLI mode: send args to daemon
    var cmd = string.Join(' ', args);
    try
    {
        using var client = new TcpClient();
        client.Connect(IPAddress.Loopback, Port);
        var stream = client.GetStream();
        var data = Encoding.UTF8.GetBytes(cmd + "\n");
        stream.Write(data, 0, data.Length);
        using var sr = new StreamReader(stream, Encoding.UTF8);
        var resp = sr.ReadToEnd();
        Console.Write(resp);
        Environment.Exit(0);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Failed to send command to systemd emulator: {ex.Message}");
        Environment.Exit(2);
    }
}

// Daemon mode
var listener = new TcpListener(IPAddress.Loopback, Port);
listener.Start();
Console.WriteLine($"SystemD emulator listening on 127.0.0.1:{Port}");

var services = new Dictionary<string, Process>(StringComparer.OrdinalIgnoreCase);

_ = Task.Run(async () =>
{
    while (true)
    {
        var client = await listener.AcceptTcpClientAsync();
        _ = Task.Run(async () =>
        {
            try
            {
                using var stream = client.GetStream();
                using var sr = new StreamReader(stream, Encoding.UTF8);
                using var sw = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
                var line = await sr.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) return;
                var parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var cmd = parts[0].ToLowerInvariant();
                var name = parts.Length > 1 ? parts[1] : string.Empty;

                switch (cmd)
                {
                    case "start":
                        if (services.ContainsKey(name)) { await sw.WriteLineAsync($"{name} already running"); break; }
                        // map service name to published path: use last token as folder name lowercased
                        var shortName = name.Split('.').Last();
                        var folder = shortName.ToLowerInvariant();
                        var exe = $"/app/{folder}/{name}";
                        if (!File.Exists(exe)) { await sw.WriteLineAsync($"Executable not found: {exe}"); break; }
                        var pstart = new ProcessStartInfo(exe) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
                        var proc = Process.Start(pstart)!;
                        services[name] = proc;
                        await sw.WriteLineAsync($"started {name} (pid {proc.Id})");
                        break;
                    case "stop":
                        if (!services.TryGetValue(name, out var p)) { await sw.WriteLineAsync($"{name} not running"); break; }
                        try { p.Kill(); await sw.WriteLineAsync($"stopped {name}"); }
                        catch (Exception ex) { await sw.WriteLineAsync($"failed to stop {name}: {ex.Message}"); }
                        services.Remove(name);
                        break;
                    case "status":
                        if (services.TryGetValue(name, out var sproc) && !sproc.HasExited) await sw.WriteLineAsync($"{name} running (pid {sproc.Id})"); else await sw.WriteLineAsync($"{name} not running");
                        break;
                    default:
                        await sw.WriteLineAsync($"unknown command: {cmd}");
                        break;
                }
            }
            catch { }
            finally { client.Close(); }
        });
    }
});

// keep daemon alive
await Task.Delay(Timeout.Infinite);
