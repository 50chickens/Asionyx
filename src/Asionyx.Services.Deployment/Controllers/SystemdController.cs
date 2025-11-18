using Microsoft.AspNetCore.Mvc;
using System.Net.Sockets;
using System.Text;

namespace Asionyx.Services.Deployment.Controllers;

public class SystemdRequest
{
    public string Action { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

[ApiController]
[Route("[controller]")]
public class SystemdController : ControllerBase
{
    private const string Host = "127.0.0.1";
    private const int Port = 6000;

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] SystemdRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Action) || string.IsNullOrWhiteSpace(req.Name))
            return BadRequest("Missing action or name");

        var cmd = $"{req.Action} {req.Name}";
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(Host, Port);
            var stream = client.GetStream();
            var data = Encoding.UTF8.GetBytes(cmd + "\n");
            await stream.WriteAsync(data, 0, data.Length);
            using var sr = new StreamReader(stream, Encoding.UTF8);
            // give the emulator a moment to respond
            await Task.Delay(100);
            var resp = await sr.ReadToEndAsync();
            return Ok(new { command = cmd, result = resp.Trim() });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
