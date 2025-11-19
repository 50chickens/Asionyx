using System.Net.Http.Json;

var baseUrl = "http://localhost:5000";
// simple argument parsing: client [command] [args...]
// commands: info, status, systemd <action> <name>
string command = args.Length > 0 ? args[0].ToLowerInvariant() : "info";
// read api key either from env var or from --api-key <key> argument
var apiKey = Environment.GetEnvironmentVariable("API_KEY");
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--api-key" && i + 1 < args.Length) { apiKey = args[i + 1]; }
    if (args[i] == "--url" && i + 1 < args.Length) { baseUrl = args[i + 1]; }
}

using var client = new HttpClient();
if (!string.IsNullOrWhiteSpace(apiKey)) client.DefaultRequestHeaders.Add("X-API-KEY", apiKey);
try
{
    switch (command)
    {
        case "info":
            var info = await client.GetFromJsonAsync<object>($"{baseUrl}/info");
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(info, Newtonsoft.Json.Formatting.Indented));
            break;
        case "status":
            var status = await client.GetFromJsonAsync<object>($"{baseUrl}/status");
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(status, Newtonsoft.Json.Formatting.Indented));
            break;
        case "systemd":
            if (args.Length < 3) { Console.Error.WriteLine("Usage: client systemd <action> <name>"); return 2; }
            var action = args[1];
            var name = args[2];
            var resp = await client.PostAsJsonAsync($"{baseUrl}/systemd", new { Action = action, Name = name });
            var body = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($"{(int)resp.StatusCode} {resp.ReasonPhrase}\n{body}");
            break;
        default:
            Console.Error.WriteLine($"Unknown command: {command}");
            return 2;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Request failed: {ex.Message}");
    return 1;
}

return 0;
