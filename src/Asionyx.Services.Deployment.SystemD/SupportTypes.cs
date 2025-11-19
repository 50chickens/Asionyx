namespace Asionyx;

public class SystemdServiceSettings
{
    public string DefaultWorkingDirectory { get; set; } = "/app";
    public string DefaultRestartPolicy { get; set; } = "on-failure";
    public int DefaultRestartSec { get; set; } = 5;
    public string Template { get; set; } = "[Unit]\nDescription={Description}\n[Service]\nType={Type}\nExecStart={ExecStart}\nWorkingDirectory={WorkingDirectory}\nRestart={Restart}\nRestartSec={RestartSec}\n";
}

public class CommandResult
{
    public int ExitCode { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}
