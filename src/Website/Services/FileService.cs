using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Website.Services
{
    public class FileService
    {
        public static List<string> FindFilesForRelease(string version)
        {
            var folder = Path.Combine(HttpRuntime.AppDomainAppPath, "App_data", "files", Path.GetFileName(version));
            if (!Directory.Exists(folder))
                return new List<string>();
            return Directory.GetFiles(folder).Select(f=>Path.GetFileName(f)).ToList();
        }
    }
}
