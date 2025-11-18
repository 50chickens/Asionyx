using System.Net.Http.Json;

Console.WriteLine("Asionyx Deployment Client - calling /info endpoint...");
var baseUrl = args.Length>0? args[0] : "http://localhost:5000";
using var client = new HttpClient();
try
{
    var info = await client.GetFromJsonAsync<object>($"{baseUrl}/info");
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(info, new System.Text.Json.JsonSerializerOptions{WriteIndented=true}));
}
catch(Exception ex)
{
    Console.Error.WriteLine($"Request failed: {ex.Message}");
    return 1;
}

return 0;
