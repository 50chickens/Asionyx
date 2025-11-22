using System.IO;
using System.Text;

namespace Asionyx.Services.Deployment.Tests
{
    public class TestDiagnostics
    {
        public void AppendDiag(string message)
        {
            try
            {
                var dir = Path.Combine(Path.GetTempPath(), "Asionyx");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, "TestDiagnostics.txt");
                using (var fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read))
                using (var sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    sw.WriteLine(message);
                    sw.Flush();
                }
            }
            catch
            {
                // swallow - diagnostics must not interfere with test execution
            }
        }
    }
}
