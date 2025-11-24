namespace Asionyx.Services.Deployment.Services
{
    public interface IFileSystem
    {
        bool FileExists(string path);
        string ReadAllText(string path);
        void WriteAllText(string path, string content);
        void DeleteFile(string path);
        bool DirectoryExists(string path);
        string[] GetFileSystemEntries(string path);
        void CreateDirectory(string path);
        void DeleteDirectory(string path, bool recursive);
    }
}
