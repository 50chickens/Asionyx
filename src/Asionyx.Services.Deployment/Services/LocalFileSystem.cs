namespace Asionyx.Services.Deployment.Services
{
    public class LocalFileSystem : IFileSystem
    {
        public bool FileExists(string path) => System.IO.File.Exists(path);
        public string ReadAllText(string path) => System.IO.File.ReadAllText(path);
        public void WriteAllText(string path, string content) => System.IO.File.WriteAllText(path, content);
        public void DeleteFile(string path) { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
        public bool DirectoryExists(string path) => System.IO.Directory.Exists(path);
        public string[] GetFileSystemEntries(string path) => System.IO.Directory.GetFileSystemEntries(path);
        public void CreateDirectory(string path) { if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path); }
        public void DeleteDirectory(string path, bool recursive) { if (System.IO.Directory.Exists(path)) System.IO.Directory.Delete(path, recursive); }
    }
}
