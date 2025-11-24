namespace Asionyx.Services.Deployment.Controllers
{
    public class FileResultDto
    {
        public string Type { get; set; } = string.Empty; // "file" or "directory"
        public string Path { get; set; } = string.Empty;
        public string? Content { get; set; }
        public string[]? Entries { get; set; }
    }

    public class ActionResultDto
    {
        public string Result { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string[]? Entries { get; set; }
    }

    public class ErrorDto
    {
        public string Error { get; set; } = string.Empty;
        public string? Path { get; set; }
        public string? Detail { get; set; }
    }
}
