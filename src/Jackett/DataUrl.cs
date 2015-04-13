using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett
{
    public class DataUrl
    {
        static Dictionary<string, string> ImageMimeTypes = new Dictionary<string, string>{
            { ".jpg", "data:image/jpeg" },
            { ".jpeg", "data:image/jpeg" },
            { ".png", "data:image/png" },
            { ".gif", "data:image/gif" }
        };

        public static string ReadFileToDataUrl(string file)
        {
            string mime = ImageMimeTypes[Path.GetExtension(file)];
            return "data:" + mime + ";base64," + Convert.ToBase64String(File.ReadAllBytes(file));
        }

        public static string BytesToDataUrl(byte[] bytes, string mimeType = "image/jpg")
        {
            return "data:" + mimeType + ";base64," + Convert.ToBase64String(bytes);
        }
    }
}
